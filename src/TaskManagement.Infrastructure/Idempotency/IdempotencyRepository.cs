using Microsoft.EntityFrameworkCore;
using TaskManagement.Infrastructure.Data;

namespace TaskManagement.Infrastructure.Idempotency;

/// <summary>
/// Репозиторий для хранения записей идемпотентности.
/// </summary>
public class IdempotencyRepository : IIdempotencyRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="IdempotencyRepository"/>.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    public IdempotencyRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IdempotencyRecord?> GetByKeyAsync(string key)
    {
        return await _context.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.Key == key && !r.IsExpired());
    }

    /// <inheritdoc />
    public async Task SaveAsync(IdempotencyRecord record)
    {
        await _context.IdempotencyRecords.AddAsync(record);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateAsync(IdempotencyRecord record)
    {
        _context.IdempotencyRecords.Update(record);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<int> DeleteExpiredAsync()
    {
        var expiredRecords = _context.IdempotencyRecords
            .Where(r => r.IsExpired());

        var count = await expiredRecords.CountAsync();
        _context.IdempotencyRecords.RemoveRange(expiredRecords);
        await _context.SaveChangesAsync();

        return count;
    }
}
