using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Application.DTOs;

/// <summary>
/// Параметры фильтрации и поиска задач.
/// </summary>
public class TaskFilterDto
{
    /// <summary>
    /// Статус для фильтрации (опциональный).
    /// </summary>
    public TaskStatus? Status { get; set; }

    /// <summary>
    /// Подстрока для поиска по заголовку (опциональная).
    /// </summary>
    public string? SearchTitle { get; set; }

    /// <summary>
    /// Номер страницы (начиная с 1).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Размер страницы.
    /// </summary>
    public int PageSize { get; set; } = 20;
}
