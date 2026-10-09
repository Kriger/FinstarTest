using TaskManagement.Application.DTOs;
using DomainTask = TaskManagement.Domain.Models.Task;
using DomainStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Application.Tests.Factories;

/// <summary>
/// Фабрика для создания тестовых сущностей и DTO задач.
/// Thread-safe implementation для параллельного запуска тестов xUnit.
/// </summary>
public static class TaskFactory
{
    private static readonly ThreadLocal<int> _seed = new(() => 0);

    private static int Seed => _seed.Value++;

    /// <summary>
    /// Создаёт доменную сущность задачи с тестовыми данными.
    /// </summary>
    public static DomainTask Create(Guid? userId = null, string? title = null, string? description = null)
    {
        var id = userId ?? Guid.NewGuid();
        return DomainTask.Create(
            id,
            title ?? $"Test Task {Seed}",
            description ?? $"Description {Seed}");
    }

    /// <summary>
    /// Создаёт список доменных сущностей задач.
    /// </summary>
    public static List<DomainTask> CreateList(int count, Guid? userId = null)
    {
        return Enumerable.Range(0, count)
            .Select(_ => Create(userId))
            .ToList();
    }

    /// <summary>
    /// Создаёт TaskDto из доменной сущности.
    /// </summary>
    public static TaskDto CreateDto(DomainTask task)
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

    /// <summary>
    /// Создаёт TaskDto с указанными значениями.
    /// </summary>
    public static TaskDto CreateDto(
        Guid? id = null,
        string? title = null,
        string? description = null,
        DomainStatus? status = null,
        DateTime? createdAt = null,
        DateTime? updatedAt = null,
        Guid? createdBy = null,
        Guid? updatedBy = null)
    {
        return new TaskDto
        {
            Id = id ?? Guid.NewGuid(),
            Title = title ?? $"Test Task {Seed}",
            Description = description ?? $"Description {Seed}",
            Status = status ?? DomainStatus.New,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            UpdatedAt = updatedAt ?? DateTime.UtcNow,
            CreatedBy = createdBy ?? Guid.NewGuid(),
            UpdatedBy = updatedBy ?? Guid.NewGuid()
        };
    }

    /// <summary>
    /// Создаёт список TaskDto.
    /// </summary>
    public static List<TaskDto> CreateDtoList(int count, DomainStatus? status = null)
    {
        return Enumerable.Range(0, count)
            .Select(_ => CreateDto(status: status))
            .ToList();
    }
}
