using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Infrastructure.Data;

/// <summary>
/// Реализация IUnitOfWork для AppDbContext.
/// Обеспечивает атомарное сохранение изменений во всех репозиториях.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private readonly DbCommandExecutor _executor;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="UnitOfWork"/>.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    /// <param name="executor">Исполнитель команд БД.</param>
    public UnitOfWork(AppDbContext context, DbCommandExecutor executor)
    {
        _context = context;
        _executor = executor;
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _executor.SaveChangesAsync(cancellationToken);
            return 0;
        }
        catch (DatabaseCircuitBreakerOpenException)
        {
            throw;
        }
        catch (DatabaseTimeoutException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyException("Задача была изменена другим пользователем. Пожалуйста, обновите данные и попробуйте снова.");
        }
        catch (DbUpdateException)
        {
            throw new DatabaseOperationException("Произошла ошибка при сохранении данных в базу данных.");
        }
    }
}
