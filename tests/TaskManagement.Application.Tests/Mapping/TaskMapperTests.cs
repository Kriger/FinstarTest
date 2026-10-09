using FluentAssertions;
using TaskManagement.Application.Mapping;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;
using DomainTask = TaskManagement.Domain.Models.Task;
using Xunit;

namespace TaskManagement.Application.Tests.Mapping;

/// <summary>
/// Модульные тесты для TaskMapper.
/// </summary>
public class TaskMapperTests
{
    private readonly TaskMapper _mapper;

    public TaskMapperTests()
    {
        _mapper = new TaskMapper();
    }

    /// <summary>
    /// Проверяет, что ToDto маппит все свойства задачи.
    /// </summary>
    [Fact]
    public void ToDto_ShouldMapAllProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var task = new DomainTask("Title", "Description", userId);

        // Act
        var dto = _mapper.ToDto(task);

        // Assert
        dto.Id.Should().Be(task.Id);
        dto.Title.Should().Be(task.Title);
        dto.Description.Should().Be(task.Description);
        dto.Status.Should().Be(TaskStatus.New);
        dto.CreatedAt.Should().Be(task.CreatedAt);
        dto.UpdatedAt.Should().Be(task.UpdatedAt);
        dto.CreatedBy.Should().Be(userId);
        dto.UpdatedBy.Should().Be(userId);
    }

    /// <summary>
    /// Проверяет, что ToDto корректно маппит обновлённый статус задачи.
    /// </summary>
    [Fact]
    public void ToDto_ShouldMapUpdatedStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var task = new DomainTask("Title", "Description", userId);
        task.UpdateStatus(TaskStatus.InProgress, userId);
        task.UpdateStatus(TaskStatus.Completed, userId);

        // Act
        var dto = _mapper.ToDto(task);

        // Assert
        dto.Status.Should().Be(TaskStatus.Completed);
    }
}
