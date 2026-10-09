using System.Collections.Concurrent;
using System.Text.Json;

namespace TaskManagement.Api;

/// <summary>
/// Middleware для ограничения частоты запросов (Rate Limiting).
/// Использует sliding window алгоритм с автоматической очисткой устаревших записей.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitConfig _config;

    private readonly ConcurrentDictionary<string, List<DateTime>> _rateLimitStore = new();
    private readonly object _storeLock = new();
    private DateTime _lastCleanup = DateTime.UtcNow;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="RateLimitingMiddleware"/>.
    /// </summary>
    /// <param name="next">Следующий middleware в конвейере.</param>
    /// <param name="logger">Логгер.</param>
    /// <param name="config">Конфигурация ограничения частоты запросов.</param>
    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        RateLimitConfig config)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Асинхронно обрабатывает запрос и применяет ограничение частоты.
    /// </summary>
    /// <param name="context">HTTP-контекст.</param>
    /// <returns>Task, представляющая асинхронную операцию.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;

        // Применяем ограничение только к API endpoints
        if (!path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        var clientIp = GetClientIp(context);
        var now = DateTime.UtcNow;
        var shouldReject = false;
        int remainingRequests;

        // Обновляем счётчик (sliding window)
        lock (_storeLock)
        {
            var requestTimes = _rateLimitStore.GetOrAdd(clientIp, _ => new List<DateTime>());

            // Удаляем устаревшие записи (sliding window)
            var windowStart = now - _config.Period;
            requestTimes.RemoveAll(t => t < windowStart);

            // Проверяем лимит
            if (requestTimes.Count >= _config.Limit)
            {
                shouldReject = true;
                remainingRequests = 0;
                _logger.LogWarning("Превышен лимит запросов для IP: {ClientIp}, Path: {Path}, Count: {Count}",
                    clientIp, path, requestTimes.Count);
            }
            else
            {
                requestTimes.Add(now);
                remainingRequests = _config.Limit - requestTimes.Count;
            }
        }

        // Добавляем заголовки с информацией о лимите
        context.Response.Headers.TryAdd("X-RateLimit-Limit", _config.Limit.ToString());
        context.Response.Headers.TryAdd("X-RateLimit-Remaining", remainingRequests.ToString());

        // Отклоняем запрос, если лимит превышен
        if (shouldReject)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.TryAdd("Retry-After", _config.Period.TotalSeconds.ToString());
            context.Response.Headers.ContentType = "application/json";

            var response = JsonSerializer.Serialize(new
            {
                error = "Превышен лимит запросов",
                retryAfter = (int)_config.Period.TotalSeconds
            });

            await context.Response.WriteAsync(response);
            return;
        }

        // Периодическая очистка устаревших записей (каждые 30 секунд)
        if (now - _lastCleanup > TimeSpan.FromSeconds(30))
        {
            CleanupStaleEntries();
            _lastCleanup = now;
        }

        await _next(context);
    }

    /// <summary>
    /// Безопасно определяет IP-адрес клиента с защитой от spoofing.
    /// </summary>
    /// <param name="context">HTTP-контекст.</param>
    /// <returns>IP-адрес клиента.</returns>
    private static string GetClientIp(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();

        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0)
            {
                return ips[0];
            }
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Удаляет устаревшие записи из хранилища.
    /// </summary>
    private void CleanupStaleEntries()
    {
        var now = DateTime.UtcNow;
        var windowStart = now - _config.Period;

        lock (_storeLock)
        {
            foreach (var kvp in _rateLimitStore.ToList())
            {
                var requestTimes = kvp.Value;
                lock (requestTimes)
                {
                    requestTimes.RemoveAll(t => t < windowStart);

                    if (requestTimes.Count == 0)
                    {
                        _rateLimitStore.TryRemove(kvp.Key, out _);
                    }
                }
            }
        }

        _logger.LogDebug("Rate limit store cleaned up. Remaining entries: {Count}", _rateLimitStore.Count);
    }
}
