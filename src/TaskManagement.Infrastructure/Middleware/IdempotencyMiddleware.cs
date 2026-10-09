using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Serilog;
using System.IO;
using System.Text.Json;

namespace TaskManagement.Infrastructure.Middleware;

/// <summary>
/// Middleware для обеспечения идемпотентности POST-запросов.
/// Предотвращает дублирование ресурсов при повторных отправках одного запроса.
/// </summary>
public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IdempotencyMiddleware> _logger;
    private static readonly Serilog.ILogger _serilog = Log.ForContext<IdempotencyMiddleware>();

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="IdempotencyMiddleware"/>.
    /// </summary>
    /// <param name="next">Следующий делегат в конвейере.</param>
    /// <param name="logger">Логгер.</param>
    public IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Обрабатывает запрос с проверкой ключа идемпотентности.
    /// </summary>
    /// <param name="context">HTTP-контекст запроса.</param>
    /// <param name="cache">Кэш для хранения ответов.</param>
    /// <returns>Task.</returns>
    public async Task InvokeAsync(HttpContext context, IDistributedCache cache)
    {
        var idempotencyKey = context.Request.Headers["Idempotency-Key"].FirstOrDefault();

        if (string.IsNullOrEmpty(idempotencyKey))
        {
            // Нет ключа идемпотентности — пропускаем запрос
            await _next(context);
            return;
        }

        // Проверяем кэш на наличие сохранённого ответа
        var cacheKey = $"idempotency_{idempotencyKey}";
        var cachedResponse = await cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedResponse))
        {
            // Ответ уже сохранён — возвращаем его
            var response = JsonSerializer.Deserialize<IdempotencyResponseDto>(cachedResponse);
            if (response != null)
            {
                context.Response.StatusCode = response.StatusCode;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(response.Body);
                _serilog.Information(
                    "Идемпотентный запрос возвращён из кэша. Key: {Key}, Status: {Status}",
                    idempotencyKey,
                    response.StatusCode);
                return;
            }
        }

        // Сохраняем тело запроса для сравнения
        var originalBodyStream = context.Request.Body;
        context.Request.EnableBuffering();
        var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
        context.Request.Body = originalBodyStream;

        // Проверяем, нет ли уже обработанного запроса с этим ключом
        var existingRecord = await cache.GetStringAsync($"idempotency_record_{idempotencyKey}");
        if (!string.IsNullOrEmpty(existingRecord))
        {
            var record = JsonSerializer.Deserialize<IdempotencyRecordDto>(existingRecord);
            if (record?.Status == "Completed")
            {
                // Запрос уже обработан — возвращаем сохранённый ответ
                context.Response.StatusCode = record.HttpStatusCode ?? 200;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(record.ResponseBody ?? string.Empty);
                _serilog.Information(
                    "Идемпотентный запрос (completed) возвращён. Key: {Key}",
                    idempotencyKey);
                return;
            }

            // Запрос в обработке — возвращаем 409 Conflict
            context.Response.StatusCode = 409;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                error = "Запрос уже обрабатывается. Пожалуйста, попробуйте позже."
            }));
            return;
        }

        // Создаём запись о начале обработки
        var newRecord = new IdempotencyRecordDto
        {
            Key = idempotencyKey,
            HttpMethod = context.Request.Method,
            RequestBody = requestBody,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow.ToString("o")
        };

        var recordExpiry = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
        };

        await cache.SetStringAsync(
            $"idempotency_record_{idempotencyKey}",
            JsonSerializer.Serialize(newRecord),
            recordExpiry);

        // Перезаписываем тело запроса
        context.Request.Body = originalBodyStream;

        // Выполняем запрос с буферизацией response body
        using var responseBodyBuffer = new MemoryStream();
        var originalResponseBody = context.Response.Body;
        context.Response.Body = responseBodyBuffer;

        await _next(context);

        // Копируем буферизированный ответ обратно в original body
        responseBodyBuffer.Seek(0, SeekOrigin.Begin);

        // Сохраняем ответ в кэш для идемпотентности
        if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
        {
            var responseText = await new StreamReader(responseBodyBuffer).ReadToEndAsync();
            responseBodyBuffer.Seek(0, SeekOrigin.Begin);

            // Сохраняем ответ
            var responseDto = new IdempotencyResponseDto
            {
                StatusCode = context.Response.StatusCode,
                Body = responseText
            };

            var responseExpiry = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            };

            await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(responseDto), responseExpiry);
            await cache.SetStringAsync(
                $"idempotency_record_{idempotencyKey}",
                JsonSerializer.Serialize(new IdempotencyRecordDto
                {
                    Key = idempotencyKey,
                    HttpMethod = context.Request.Method,
                    RequestBody = requestBody,
                    Status = "Completed",
                    HttpStatusCode = context.Response.StatusCode,
                    ResponseBody = responseText,
                    CreatedAt = DateTime.UtcNow.ToString("o")
                }),
                recordExpiry);

            _serilog.Information(
                "Идемпотентный запрос обработан. Key: {Key}, Status: {Status}",
                idempotencyKey,
                context.Response.StatusCode);
        }
        else
        {
            // При ошибке удаляем запись о обработке
            await cache.RemoveAsync($"idempotency_record_{idempotencyKey}");
            _serilog.Warning(
                "Идемпотентный запрос завершился ошибкой. Key: {Key}, Status: {Status}",
                idempotencyKey,
                context.Response.StatusCode);
        }

        // Копируем буферизированный ответ обратно в original body
        await responseBodyBuffer.CopyToAsync(originalResponseBody);
    }
}

/// <summary>
/// DTO для хранения записи идемпотентности в кэше.
/// </summary>
public class IdempotencyRecordDto
{
    /// <summary>
    /// Ключ идемпотентности.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// HTTP-метод.
    /// </summary>
    public string HttpMethod { get; set; } = string.Empty;

    /// <summary>
    /// Тело запроса.
    /// </summary>
    public string RequestBody { get; set; } = string.Empty;

    /// <summary>
    /// Статус обработки.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// HTTP-статус ответа.
    /// </summary>
    public int? HttpStatusCode { get; set; }

    /// <summary>
    /// Тело ответа.
    /// </summary>
    public string ResponseBody { get; set; } = string.Empty;

    /// <summary>
    /// Время создания.
    /// </summary>
    public string CreatedAt { get; set; } = string.Empty;
}

/// <summary>
/// DTO для хранения ответа в кэше.
/// </summary>
public class IdempotencyResponseDto
{
    /// <summary>
    /// HTTP-статус ответа.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Тело ответа.
    /// </summary>
    public string Body { get; set; } = string.Empty;
}
