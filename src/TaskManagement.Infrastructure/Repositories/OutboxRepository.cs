using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Outbox;
using TaskManagement.Infrastructure.Data;

namespace TaskManagement.Infrastructure.Repositories;

/// <summary>
/// Репозиторий для управления сообщениями в Outbox с Circuit Breaker.
/// </summary>
public class OutboxRepository : IOutboxRepository
{
    private readonly AppDbContext _context;
    private readonly DbCommandExecutor _executor;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="OutboxRepository"/>.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    /// <param name="executor">Исполнитель команд БД.</param>
    public OutboxRepository(AppDbContext context, DbCommandExecutor executor)
    {
        _context = context;
        _executor = executor;
    }

    /// <inheritdoc />
    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        await _executor.ExecuteAsync(
            async () => { await _context.OutboxMessages.AddAsync(message, cancellationToken); },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(List<OutboxMessage> Messages, int TotalCount)> GetPendingAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _executor.ExecuteAsync(async () =>
        {
            var query = _context.OutboxMessages
                .Where(m => m.Status == Domain.Outbox.OutboxMessageStatus.Pending)
                .OrderBy(m => m.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);
            var messages = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (messages, totalCount);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(List<OutboxMessage> Messages, int TotalCount)> GetSendingAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _executor.ExecuteAsync(async () =>
        {
            var query = _context.OutboxMessages
                .Where(m => m.Status == Domain.Outbox.OutboxMessageStatus.Sending)
                .OrderBy(m => m.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);
            var messages = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (messages, totalCount);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public void MarkAsSent(OutboxMessage message)
    {
        message.MarkAsSent();
    }
}
