using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Moq;
using TaskManagement.Application.Configuration;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Mapping;
using TaskManagement.Application.Services;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;
using DomainTask = TaskManagement.Domain.Models.Task;
using TaskManagement.Domain.Outbox;
using TestTaskFactory = TaskManagement.Application.Tests.Factories.TaskFactory;
using Xunit;

namespace TaskManagement.Application.Tests;

/// <summary>
/// Модульные тесты для класса <see cref="TaskService"/>.
/// </summary>
public class TaskServiceTests
{
    private readonly Mock<ITaskRepository> _taskRepositoryMock;
    private readonly Mock<IOutboxRepository> _outboxRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ITaskMapper> _taskMapperMock;
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<IUserContext> _userContextMock;
    private readonly Mock<IOptions<CachingSettings>> _cachingSettingsMock;
    private readonly TaskService _taskService;

    public TaskServiceTests()
    {
        _taskRepositoryMock = new Mock<ITaskRepository>();
        _outboxRepositoryMock = new Mock<IOutboxRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _taskMapperMock = new Mock<ITaskMapper>();
        _cacheMock = new Mock<IDistributedCache>();
        _userContextMock = new Mock<IUserContext>();
        _cachingSettingsMock = new Mock<IOptions<CachingSettings>>();

        _cachingSettingsMock.Setup(s => s.Value).Returns(new CachingSettings { DefaultCacheDurationSeconds = 30 });

        _userContextMock.Setup(u => u.UserId).Returns(Guid.NewGuid());
        _userContextMock.Setup(u => u.UserName).Returns("testuser");
        _userContextMock.Setup(u => u.IsAuthenticated).Returns(true);

        _taskService = new TaskService(
            _taskRepositoryMock.Object,
            _outboxRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _taskMapperMock.Object,
            _cacheMock.Object,
            _userContextMock.Object,
            _cachingSettingsMock.Object);
    }

    /// <summary>
    /// Проверяет, что метод CreateAsync создаёт задачу и публикует событие.
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldCreateTaskAndPublishEvent()
    {
        // Arrange
        var dto = new CreateTaskDto
        {
            Title = "Test Task",
            Description = "Test Description"
        };

        var task = DomainTask.Create(_userContextMock.Object.UserId, dto.Title, dto.Description);
        var dtoResult = TestTaskFactory.CreateDto(task);

        _taskRepositoryMock.Setup(r => r.AddAsync(It.IsAny<DomainTask>(), default));
        _taskMapperMock.Setup(m => m.ToDto(It.IsAny<DomainTask>())).Returns(dtoResult);

        _outboxRepositoryMock.Setup(r => r.AddAsync(It.IsAny<OutboxMessage>(), default));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(default));

        // Act
        var result = await _taskService.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Test Task");
        result.Description.Should().Be("Test Description");
        result.Status.Should().Be(TaskStatus.New);

        _taskRepositoryMock.Verify(r => r.AddAsync(It.IsAny<DomainTask>(), default), Times.Once);
        _outboxRepositoryMock.Verify(r => r.AddAsync(It.IsAny<OutboxMessage>(), default), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    /// <summary>
    /// Проверяет, что метод GetPagedAsync возвращает пагинированный список задач.
    /// </summary>
    [Fact]
    public async Task GetPagedAsync_ShouldReturnPagedTasks()
    {
        // Arrange
        var userId = _userContextMock.Object.UserId;
        var tasks = TestTaskFactory.CreateList(2, userId);

        _taskRepositoryMock.Setup(r => r.GetPagedAsync(1, 20, default))
            .Returns(Task.FromResult((Items: tasks, TotalCount: 2)));
        _taskMapperMock.Setup(m => m.ToDto(It.IsAny<DomainTask>()))
            .Returns((DomainTask t) => TestTaskFactory.CreateDto(t));

        // Act
        var result = await _taskService.GetPagedAsync(1, 20);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items[0].Title.Should().Be(tasks[0].Title);
        result.Items[1].Title.Should().Be(tasks[1].Title);
    }

    /// <summary>
    /// Проверяет, что GetByIdAsync возвращает задачу, когда она существует.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_ShouldReturnTask_WhenTaskExists()
    {
        // Arrange
        var userId = _userContextMock.Object.UserId;
        var task = TestTaskFactory.Create(userId);
        var dtoResult = TestTaskFactory.CreateDto(task);

        _taskRepositoryMock.Setup(r => r.GetByIdAsync(task.Id, default))
            .ReturnsAsync(task);
        _taskMapperMock.Setup(m => m.ToDto(task)).Returns(dtoResult);

        // Act
        var result = await _taskService.GetByIdAsync(task.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(task.Id);
        result.Title.Should().Be(task.Title);
    }

    /// <summary>
    /// Проверяет, что GetByIdAsync выбрасывает TaskNotFoundException, когда задача не найдена.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_ShouldThrowTaskNotFoundException_WhenTaskNotFound()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(taskId, default))
            .ReturnsAsync((DomainTask?)null);

        // Act & Assert
        await Assert.ThrowsAsync<TaskNotFoundException>(() => _taskService.GetByIdAsync(taskId));
    }

    /// <summary>
    /// Проверяет, что метод UpdateAsync обновляет задачу и публикует событие.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ShouldUpdateTaskAndPublishEvent()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var userId = _userContextMock.Object.UserId;
        var task = TestTaskFactory.Create(userId);
        var dto = new UpdateTaskDto
        {
            Title = "Updated Title",
            Status = TaskStatus.InProgress
        };

        var dtoResult = TestTaskFactory.CreateDto(
            id: task.Id,
            title: "Updated Title",
            status: TaskStatus.InProgress,
            updatedBy: userId);

        _taskRepositoryMock.Setup(r => r.GetByIdAsync(taskId, default))
            .ReturnsAsync(task);
        _taskRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<DomainTask>()));
        _outboxRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Domain.Outbox.OutboxMessage>(), default));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(default));
        _taskMapperMock.Setup(m => m.ToDto(It.IsAny<DomainTask>())).Returns(dtoResult);

        // Act
        var result = await _taskService.UpdateAsync(taskId, dto);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Updated Title");
        result.Status.Should().Be(TaskStatus.InProgress);

        _taskRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<DomainTask>()), Times.Once);
        _outboxRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Domain.Outbox.OutboxMessage>(), default), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    /// <summary>
    /// Проверяет, что UpdateAsync выбрасывает исключение, когда задача не найдена.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenTaskNotFound()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var dto = new UpdateTaskDto { Title = "Updated Title" };

        _taskRepositoryMock.Setup(r => r.GetByIdAsync(taskId, default))
            .ReturnsAsync((DomainTask?)null);

        // Act & Assert
        await Assert.ThrowsAsync<TaskNotFoundException>(() => _taskService.UpdateAsync(taskId, dto));
    }

    /// <summary>
    /// Проверяет, что метод DeleteAsync публикует событие и удаляет задачу.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_ShouldPublishEventAndRemoveTask()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var userId = _userContextMock.Object.UserId;
        var task = TestTaskFactory.Create(userId);

        _taskRepositoryMock.Setup(r => r.GetByIdAsync(taskId, default))
            .ReturnsAsync(task);
        _outboxRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Domain.Outbox.OutboxMessage>(), default));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(default));

        // Act
        await _taskService.DeleteAsync(taskId);

        // Assert
        _outboxRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Domain.Outbox.OutboxMessage>(), default), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    /// <summary>
    /// Проверяет, что DeleteAsync выбрасывает исключение, когда задача не найдена.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_ShouldThrow_WhenTaskNotFound()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(taskId, default))
            .ReturnsAsync((DomainTask?)null);

        // Act & Assert
        await Assert.ThrowsAsync<TaskNotFoundException>(() => _taskService.DeleteAsync(taskId));
    }
}
