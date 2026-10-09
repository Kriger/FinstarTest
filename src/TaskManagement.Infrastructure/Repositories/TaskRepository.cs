using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Infrastructure.Data;
using DomainTask = TaskManagement.Domain.Models.Task;

namespace TaskManagement.Infrastructure.Repositories;

/// <summary>
/// Реализация ITaskRepository на основе Entity Framework Core с Circuit Breaker.
/// </summary>
public class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _context;
    private readonly DbCommandExecutor _executor;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="TaskRepository"/>.
    /// </summary>
    /// <param name="context">Контекст приложения базы данных.</param>
    /// <param name="executor">Исполнитель команд БД.</param>
    public TaskRepository(AppDbContext context, DbCommandExecutor executor)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

    /// <inheritdoc />
    public async Task AddAsync(DomainTask task, CancellationToken cancellationToken = default)
    {
        await _executor.ExecuteAsync(
            async () => { await _context.Tasks.AddAsync(task, cancellationToken); },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DomainTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _executor.ExecuteAsync(
            () => _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(List<DomainTask> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _executor.ExecuteAsync(async () =>
        {
            var query = _context.Tasks.Where(t => !t.IsDeleted);
            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderBy(t => t.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(DomainTask task)
    {
        await _executor.ExecuteAsync(
            () => Task.FromResult(_context.Tasks.Update(task)),
            CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<(List<DomainTask> Items, int TotalCount)> SearchAsync(TaskFilterDto filter, CancellationToken cancellationToken = default)
    {
        return await _executor.ExecuteAsync(async () =>
        {
            var query = _context.Tasks.Where(t => !t.IsDeleted).AsQueryable();

            if (filter.Status.HasValue)
            {
                query = query.Where(t => t.Status == filter.Status.Value);
            }

            if (!string.IsNullOrEmpty(filter.SearchTitle))
            {
                var searchTitle = filter.SearchTitle.Trim().ToLowerInvariant();
                if (!string.IsNullOrEmpty(searchTitle))
                {
                    query = query.Where(t => EF.Functions.Like(t.Title.ToLowerInvariant(), $"%{searchTitle}%"));
                }
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderBy(t => t.Id)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }, cancellationToken);
    }
}
