using TaskManagement.Domain.Outbox;

namespace TaskManagement.Application.Interfaces;

/// <summary>
/// Репозиторий для управления сообщениями в Outbox.
/// </summary>
public interface IOutboxRepository
{
    /// <summary>
    /// Добавляет сообщение в Outbox.
    /// </summary>
    /// <param name="message">Сообщение для добавления.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Task.</returns>
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает список неотправленных сообщений с пагинацией.
    /// </summary>
    /// <param name="page">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Кортеж из списка сообщений и общего количества.</returns>
    Task<(List<OutboxMessage> Messages, int TotalCount)> GetPendingAsync(
        int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Помечает сообщение как отправленное.
    /// </summary>
    /// <param name="message">Сообщение для обновления.</param>
    void MarkAsSent(OutboxMessage message);

    /// <summary>
    /// Получает список сообщений в статусе Sending с пагинацией.
    /// </summary>
    /// <param name="page">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Кортеж из списка сообщений и общего количества.</returns>
    Task<(List<OutboxMessage> Messages, int TotalCount)> GetSendingAsync(
        int page, int pageSize, CancellationToken cancellationToken = default);
}
