using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Application.DTOs;
using TaskManagement.Infrastructure.Data;
using Xunit;

namespace TaskManagement.Integration.Tests;

/// <summary>
/// Интеграционные тесты для API контроллера задач.
/// </summary>
public class TaskControllerIntegrationTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TaskControllerIntegrationTests()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Проверяет, что создание задачи возвращает 201 и созданную задачу.
    /// </summary>
    [Fact]
    public async Task CreateTask_ShouldReturn201AndCreatedTask()
    {
        // Arrange
        var dto = new CreateTaskDto
        {
            Title = "Test Task",
            Description = "Test Description"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/task", dto);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<TaskDto>();
        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Task");
        result.Status.Should().Be(Domain.Enums.TaskStatus.New);
    }

    /// <summary>
    /// Проверяет, что получение пагинированного списка задач возвращает 200.
    /// </summary>
    [Fact]
    public async Task GetPagedTasks_ShouldReturn200AndPaginatedResult()
    {
        // Act
        var response = await _client.GetAsync("/api/task?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<TaskDto>>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    /// <summary>
    /// Проверяет, что получение задачи по ID возвращает 200, когда задача существует.
    /// </summary>
    [Fact]
    public async Task GetTaskById_ShouldReturn200_WhenTaskExists()
    {
        // Arrange - сначала создаём задачу
        var createDto = new CreateTaskDto { Title = "Find Me", Description = "Desc" };
        var createResponse = await _client.PostAsJsonAsync("/api/task", createDto);
        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskDto>();
        createdTask.Should().NotBeNull();

        // Act
        var response = await _client.GetAsync($"/api/task/{createdTask!.Id}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TaskDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(createdTask.Id);
    }

    /// <summary>
    /// Проверяет, что получение задачи по ID возвращает 404, когда задача не найдена.
    /// </summary>
    [Fact]
    public async Task GetTaskById_ShouldReturn404_WhenTaskNotFound()
    {
        // Act
        var response = await _client.GetAsync($"/api/task/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Проверяет, что обновление задачи возвращает 200, когда задача существует.
    /// </summary>
    [Fact]
    public async Task UpdateTask_ShouldReturn200_WhenTaskExists()
    {
        // Arrange
        var createDto = new CreateTaskDto { Title = "Original", Description = "Desc" };
        var createResponse = await _client.PostAsJsonAsync("/api/task", createDto);
        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskDto>();
        createdTask.Should().NotBeNull();

        var updateDto = new UpdateTaskDto { Title = "Updated Title", Status = Domain.Enums.TaskStatus.InProgress };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/task/{createdTask!.Id}", updateDto);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TaskDto>();
        result.Should().NotBeNull();
        result!.Title.Should().Be("Updated Title");
        result.Status.Should().Be(Domain.Enums.TaskStatus.InProgress);
    }

    /// <summary>
    /// Проверяет, что удаление задачи возвращает 204, когда задача существует.
    /// </summary>
    [Fact]
    public async Task DeleteTask_ShouldReturn204_WhenTaskExists()
    {
        // Arrange
        var createDto = new CreateTaskDto { Title = "Delete Me", Description = "Desc" };
        var createResponse = await _client.PostAsJsonAsync("/api/task", createDto);
        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskDto>();
        createdTask.Should().NotBeNull();

        // Act
        var response = await _client.DeleteAsync($"/api/task/{createdTask!.Id}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);
    }
}
