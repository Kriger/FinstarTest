using TaskManagement.Application.DTOs;

namespace TaskManagement.Application.Interfaces;

/// <summary>
/// Интерфейс для бизнес-логики управления задачами.
/// </summary>
public interface ITaskService
{
    /// <summary>
    /// Создаёт новую задачу.
    /// </summary>
    /// <param name="dto">DTO для создания задачи.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Созданная задача в виде DTO.</returns>
    Task<TaskDto> CreateAsync(CreateTaskDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает задачи с пагинацией.
    /// </summary>
    /// <param name="page">Номер страницы (начиная с 1).</param>
    /// <param name="pageSize">Количество элементов на странице.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат с пагинацией.</returns>
    Task<PaginatedResult<TaskDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает задачу по уникальному идентификатору.
    /// </summary>
    /// <param name="id">Уникальный идентификатор задачи.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача в виде DTO.</returns>
    /// <exception cref="Exceptions.TaskNotFoundException">Выбрасывается, когда задача с указанным ID не найдена.</exception>
    Task<TaskDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет существующую задачу.
    /// </summary>
    /// <param name="id">Уникальный идентификатор обновляемой задачи.</param>
    /// <param name="dto">DTO для обновления задачи.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Обновлённая задача в виде DTO.</returns>
    Task<TaskDto> UpdateAsync(Guid id, UpdateTaskDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет задачу по уникальному идентификатору.
    /// </summary>
    /// <param name="id">Уникальный идентификатор удаляемой задачи.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает задачи с фильтрацией и поиском.
    /// </summary>
    /// <param name="filter">Параметры фильтрации.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Отфильтрованный список задач с пагинацией.</returns>
    Task<PaginatedResult<TaskDto>> SearchAsync(TaskFilterDto filter, CancellationToken cancellationToken = default);
}
