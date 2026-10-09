using FluentAssertions;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;
using TaskManagement.Domain.Events;
using Xunit;

namespace TaskManagement.ProducerConsumer.Tests;

/// <summary>
/// Модульные тесты для событий Producer/Consumer.
/// </summary>
public class EventTests
{
    /// <summary>
    /// Проверяет, что TaskCreatedEvent корректно инициализирует свойства.
    /// </summary>
    [Fact]
    public void TaskCreatedEvent_ShouldHaveCorrectProperties()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var title = "Test Task";
        var description = "Test Description";

        // Act
        var @event = new TaskCreatedEvent(taskId, title, description);

        // Assert
        @event.TaskId.Should().Be(taskId);
        @event.Title.Should().Be(title);
        @event.Description.Should().Be(description);
        @event.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Проверяет, что TaskUpdatedEvent корректно инициализирует свойства.
    /// </summary>
    [Fact]
    public void TaskUpdatedEvent_ShouldHaveCorrectProperties()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var title = "Updated Task";
        var description = "Updated Description";
        var status = TaskStatus.InProgress;

        // Act
        var @event = new TaskUpdatedEvent(taskId, title, description, status);

        // Assert
        @event.TaskId.Should().Be(taskId);
        @event.Title.Should().Be(title);
        @event.Description.Should().Be(description);
        @event.Status.Should().Be(status);
    }

    /// <summary>
    /// Проверяет, что TaskDeletedEvent корректно инициализирует свойства.
    /// </summary>
    [Fact]
    public void TaskDeletedEvent_ShouldHaveCorrectProperties()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var title = "Deleted Task";

        // Act
        var @event = new TaskDeletedEvent(taskId, title);

        // Assert
        @event.TaskId.Should().Be(taskId);
        @event.Title.Should().Be(title);
    }

    /// <summary>
    /// Проверяет, что базовый класс TaskEvent корректно устанавливает TaskId.
    /// </summary>
    [Fact]
    public void TaskEvent_BaseClass_ShouldSetTaskId()
    {
        // Arrange
        var taskId = Guid.NewGuid();

        // Act
        var @event = new TaskCreatedEvent(taskId, "Title", "Desc");

        // Assert
        @event.TaskId.Should().Be(taskId);
    }
}
