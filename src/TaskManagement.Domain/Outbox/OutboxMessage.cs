namespace TaskManagement.Domain.Outbox;

/// <summary>
/// Сообщение в Outbox-паттерне для гарантированной доставки событий.
/// </summary>
public class OutboxMessage
{
    /// <summary>
    /// Максимальное количество попыток отправки.
    /// </summary>
    public const int MaxRetryCount = 5;

    /// <summary>
    /// Уникальный идентификатор сообщения.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Тип события (полное имя типа).
    /// </summary>
    public string EventType { get; private set; }

    /// <summary>
    /// Содержимое события в формате JSON.
    /// </summary>
    public string Content { get; private set; }

    /// <summary>
    /// Время создания сообщения.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Время отправки (null если не отправлено).
    /// </summary>
    public DateTime? SentAt { get; private set; }

    /// <summary>
    /// Статус сообщения.
    /// </summary>
    public OutboxMessageStatus Status { get; private set; }

    /// <summary>
    /// Количество попыток отправки.
    /// </summary>
    public int RetryCount { get; private set; }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="OutboxMessage"/>.
    /// </summary>
    /// <param name="eventType">Тип события.</param>
    /// <param name="content">Содержимое в JSON.</param>
    private OutboxMessage(string eventType, string content)
    {
        Id = Guid.NewGuid();
        EventType = eventType;
        Content = content;
        CreatedAt = DateTime.UtcNow;
        Status = OutboxMessageStatus.Pending;
        RetryCount = 0;
    }

    /// <summary>
    /// Создаёт новое сообщение для отправки.
    /// </summary>
    /// <param name="eventType">Тип события.</param>
    /// <param name="content">Содержимое в JSON.</param>
    /// <returns>Новый экземпляр OutboxMessage.</returns>
    public static OutboxMessage Create(string eventType, string content)
    {
        return new OutboxMessage(eventType, content);
    }

    /// <summary>
    /// Помечает сообщение как отправленное.
    /// </summary>
    public void MarkAsSent()
    {
        SentAt = DateTime.UtcNow;
        Status = OutboxMessageStatus.Sent;
    }

    /// <summary>
    /// Помечает сообщение как отправляемое (в процессе).
    /// </summary>
    public void MarkAsSending()
    {
        Status = OutboxMessageStatus.Sending;
    }

    /// <summary>
    /// Возвращает сообщение в статус Pending для повторной попытки отправки.
    /// </summary>
    public void MarkAsPending()
    {
        Status = OutboxMessageStatus.Pending;
    }

    /// <summary>
    /// Увеличивает счётчик попыток отправки.
    /// </summary>
    public void IncrementRetryCount()
    {
        RetryCount++;
    }

    /// <summary>
    /// Помечает сообщение как неудачно отправленное после исчерпания попыток.
    /// </summary>
    public void MarkAsFailed()
    {
        Status = OutboxMessageStatus.Failed;
    }

    /// <summary>
    /// Проверяет, не исчерпано ли максимальное количество попыток.
    /// </summary>
    /// <returns>true, если можно повторить попытку; иначе false.</returns>
    public bool CanRetry() => RetryCount < MaxRetryCount;
}

/// <summary>
/// Статус сообщения в Outbox.
/// </summary>
public enum OutboxMessageStatus
{
    /// <summary>
    /// Ожидает отправки.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Отправляется в данный момент.
    /// </summary>
    Sending = 1,

    /// <summary>
    /// Отправлено успешно.
    /// </summary>
    Sent = 2,

    /// <summary>
    /// Отправка не удалась после исчерпания попыток.
    /// </summary>
    Failed = 3
}
