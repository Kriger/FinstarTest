using TaskManagement.Domain.Events;

namespace TaskManagement.Application.Interfaces;

/// <summary>
/// Интерфейс для публикации доменных событий в очередь сообщений.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Публикует событие в очередь сообщений.
    /// </summary>
    /// <typeparam name="TEvent">Тип события, должен наследоваться от TaskEvent.</typeparam>
    /// <param name="event">Событие для публикации.</param>
    /// <returns>Таск, представляющий асинхронную операцию.</returns>
    Task PublishAsync<TEvent>(TEvent @event) where TEvent : TaskEvent;

    /// <summary>
    /// Публикует сырое событие по типу и содержимому.
    /// </summary>
    /// <param name="eventType">Тип события.</param>
    /// <param name="content">Содержимое в JSON.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Таск, представляющий асинхронную операцию.</returns>
    Task PublishRawAsync(string eventType, string content, CancellationToken cancellationToken = default);
}
