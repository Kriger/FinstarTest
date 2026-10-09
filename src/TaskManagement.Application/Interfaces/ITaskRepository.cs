using TaskManagement.Application.DTOs;
using DomainTask = TaskManagement.Domain.Models.Task;

namespace TaskManagement.Application.Interfaces;

/// <summary>
/// Интерфейс для операций доступа к данным задач.
/// </summary>
public interface ITaskRepository
{
    /// <summary>
    /// Добавляет новую задачу в репозиторий.
    /// </summary>
    /// <param name="task">Добавляемая задача.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task AddAsync(DomainTask task, CancellationToken cancellationToken = default);

    /// <summary>
    /// Находит задачу по уникальному идентификатору.
    /// </summary>
    /// <param name="id">Уникальный идентификатор задачи.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача, если найдена; иначе null.</returns>
    Task<DomainTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает задачи с пагинацией.
    /// </summary>
    /// <param name="page">Номер страницы (начиная с 1).</param>
    /// <param name="pageSize">Количество элементов на странице.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Кортеж со списком задач и общим количеством.</returns>
    Task<(List<DomainTask> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет существующую задачу в репозитории.
    /// </summary>
    /// <param name="task">Обновляемая задача.</param>
    Task UpdateAsync(DomainTask task);

    /// <summary>
    /// Ищет задачи по фильтрам.
    /// </summary>
    /// <param name="filter">Параметры фильтрации.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Кортеж со списком задач и общим количеством.</returns>
    Task<(List<DomainTask> Items, int TotalCount)> SearchAsync(TaskFilterDto filter, CancellationToken cancellationToken = default);
}
