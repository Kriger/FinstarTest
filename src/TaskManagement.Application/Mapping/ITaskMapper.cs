using DomainTask = TaskManagement.Domain.Models.Task;
using TaskDto = TaskManagement.Application.DTOs.TaskDto;

namespace TaskManagement.Application.Mapping;

/// <summary>
/// Интерфейс для маппинга доменных сущностей в DTO.
/// </summary>
public interface ITaskMapper
{
    /// <summary>
    /// Маппит доменную сущность Task в TaskDto.
    /// </summary>
    /// <param name="task">Сущность задачи для маппинга.</param>
    /// <returns>Сопоставленный TaskDto.</returns>
    TaskDto ToDto(DomainTask task);
}
