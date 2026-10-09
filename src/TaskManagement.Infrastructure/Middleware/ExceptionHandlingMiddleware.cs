using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TaskManagement.Application.Exceptions;

namespace TaskManagement.Infrastructure.Middleware;

/// <summary>
/// Стандартизированный ответ об ошибке API.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Сообщение об ошибке.
    /// </summary>
    public string Error { get; init; } = string.Empty;

    /// <summary>
    /// Код состояния HTTP.
    /// </summary>
    public int StatusCode { get; init; }

    /// <summary>
    /// Идентификатор трассировки для отслеживания.
    /// </summary>
    public string TraceId { get; init; } = string.Empty;
}

/// <summary>
/// Middleware, перехватывающее необработанные исключения и возвращающее структурированные ответы об ошибках.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="ExceptionHandlingMiddleware"/>.
    /// </summary>
    /// <param name="next">Следующее middleware в конвейере.</param>
    /// <param name="logger">Логгер.</param>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Вызывает middleware для обработки исключений.
    /// </summary>
    /// <param name="context">HTTP контекст.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var (statusCode, message) = exception switch
        {
            TaskNotFoundException => ((int)HttpStatusCode.NotFound, exception.Message),
            ConcurrencyException => ((int)HttpStatusCode.Conflict, exception.Message),
            KeyNotFoundException => ((int)HttpStatusCode.NotFound, exception.Message),
            ArgumentException => ((int)HttpStatusCode.BadRequest, exception.Message),
            _ => ((int)HttpStatusCode.InternalServerError, "Произошла непредвиденная ошибка. Пожалуйста, попробуйте позже.")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = new ErrorResponse
        {
            Error = message,
            StatusCode = statusCode,
            TraceId = context.TraceIdentifier
        };

        var json = System.Text.Json.JsonSerializer.Serialize(response);
        return context.Response.WriteAsync(json);
    }
}
