using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Application.DTOs;

/// <summary>
/// DTO, представляющий задачу для ответов API.
/// </summary>
public record TaskDto
{
    /// <summary>
    /// Уникальный идентификатор задачи.
    /// </summary>
    public Guid Id { get; init; } = Guid.Empty;

    /// <summary>
    /// Заголовок задачи.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Описание задачи.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Текущий статус задачи.
    /// </summary>
    public TaskStatus Status { get; init; }

    /// <summary>
    /// Временная метка создания задачи.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Временная метка последнего обновления задачи.
    /// </summary>
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Идентификатор пользователя, создавшего задачу.
    /// </summary>
    public Guid CreatedBy { get; init; }

    /// <summary>
    /// Идентификатор пользователя, обновившего задачу.
    /// </summary>
    public Guid UpdatedBy { get; init; }
}
