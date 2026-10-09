using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Application.DTOs;

/// <summary>
/// DTO для обновления существующей задачи.
/// </summary>
public class UpdateTaskDto
{
    /// <summary>
    /// Заголовок задачи (опциональный, макс. 200 символов).
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Описание задачи (опциональный, макс. 2000 символов).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Статус задачи (опциональный).
    /// </summary>
    public TaskStatus? Status { get; set; }
}
