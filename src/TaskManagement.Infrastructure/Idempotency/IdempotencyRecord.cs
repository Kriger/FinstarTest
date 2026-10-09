namespace TaskManagement.Infrastructure.Idempotency;

/// <summary>
/// Запись ключа идемпотентности для предотвращения дублирования запросов.
/// </summary>
public class IdempotencyRecord
{
    /// <summary>
    /// Уникальный ключ идемпотентности.
    /// </summary>
    public string Key { get; private set; }

    /// <summary>
    /// HTTP-метод запроса (GET, POST, PUT, DELETE).
    /// </summary>
    public string HttpMethod { get; private set; }

    /// <summary>
    /// Запрос в формате JSON.
    /// </summary>
    public string RequestBody { get; private set; }

    /// <summary>
    /// HTTP-статус ответа.
    /// </summary>
    public int HttpStatusCode { get; private set; }

    /// <summary>
    /// Ответ в формате JSON.
    /// </summary>
    public string ResponseBody { get; private set; } = string.Empty;

    /// <summary>
    /// Время создания записи.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Время истечения срока действия (по умолчанию + 24 часа).
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// Статус обработки.
    /// </summary>
    public IdempotencyStatus Status { get; private set; }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="IdempotencyRecord"/>.
    /// </summary>
    /// <param name="key">Ключ идемпотентности.</param>
    /// <param name="httpMethod">HTTP-метод.</param>
    /// <param name="requestBody">Тело запроса в JSON.</param>
    private IdempotencyRecord(string key, string httpMethod, string requestBody)
    {
        Key = key;
        HttpMethod = httpMethod;
        RequestBody = requestBody;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = CreatedAt.AddHours(24);
        Status = IdempotencyStatus.Pending;
    }

    /// <summary>
    /// Создаёт новую запись для ключа идемпотентности.
    /// </summary>
    /// <param name="key">Ключ идемпотентности.</param>
    /// <param name="httpMethod">HTTP-метод.</param>
    /// <param name="requestBody">Тело запроса в JSON.</param>
    /// <returns>Новая запись IdempotencyRecord.</returns>
    public static IdempotencyRecord Create(string key, string httpMethod, string requestBody)
    {
        return new IdempotencyRecord(key, httpMethod, requestBody);
    }

    /// <summary>
    /// Помечает запись как обработанную с результатом.
    /// </summary>
    /// <param name="httpStatusCode">HTTP-статус ответа.</param>
    /// <param name="responseBody">Тело ответа в JSON.</param>
    public void MarkAsCompleted(int httpStatusCode, string responseBody)
    {
        HttpStatusCode = httpStatusCode;
        ResponseBody = responseBody;
        Status = IdempotencyStatus.Completed;
    }

    /// <summary>
    /// Проверяет, не истёк ли срок действия записи.
    /// </summary>
    /// <returns>True, если запись ещё действительна.</returns>
    public bool IsExpired()
    {
        return DateTime.UtcNow > ExpiresAt;
    }
}

/// <summary>
/// Статус обработки записи идемпотентности.
/// </summary>
public enum IdempotencyStatus
{
    /// <summary>
    /// Ожидает обработки.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Обработана успешно.
    /// </summary>
    Completed = 1,

    /// <summary>
    /// Истёк срок действия.
    /// </summary>
    Expired = 2
}
