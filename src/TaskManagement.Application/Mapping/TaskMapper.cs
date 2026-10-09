using DomainTask = TaskManagement.Domain.Models.Task;
using TaskDto = TaskManagement.Application.DTOs.TaskDto;

namespace TaskManagement.Application.Mapping;

/// <summary>
/// Реализация маппера доменных сущностей в DTO.
/// </summary>
public class TaskMapper : ITaskMapper
{
    /// <summary>
    /// Маппит доменную сущность Task в TaskDto.
    /// </summary>
    /// <param name="task">Сущность задачи для маппинга.</param>
    /// <returns>Сопоставленный TaskDto.</returns>
    public TaskDto ToDto(DomainTask task)
    {
        return new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            CreatedBy = task.CreatedBy,
            UpdatedBy = task.UpdatedBy
        };
    }
}
