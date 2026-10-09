using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using TaskManagement.Api.Filters;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Api.Controllers;

/// <summary>
/// REST-контроллер для операций управления задачами.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class TaskController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ILogger<TaskController> _logger;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="TaskController"/>.
    /// </summary>
    /// <param name="taskService">Сервис задач.</param>
    /// <param name="logger">Логгер.</param>
    public TaskController(
        ITaskService taskService,
        ILogger<TaskController> logger)
    {
        _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Создает новую задачу.
    /// </summary>
    /// <param name="dto">DTO для создания задачи.</param>
    /// <returns>Созданная задача.</returns>
    /// <response code="201">Задача успешно создана.</response>
    /// <response code="400">Некорректное тело запроса.</response>
    [HttpPost]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ServiceFilter(typeof(ValidationActionFilter<CreateTaskDto>))]
    public async Task<ActionResult<TaskDto>> Create([FromBody] CreateTaskDto dto)
    {
        _logger.LogInformation("Создание новой задачи с заголовком: {Title}", dto.Title);
        var task = await _taskService.CreateAsync(dto);
        return CreatedAtAction(nameof(Create), new { id = task.Id }, task);
    }

    /// <summary>
    /// Получает список задач с пагинацией.
    /// </summary>
    /// <param name="page">Номер страницы (по умолчанию: 1).</param>
    /// <param name="pageSize">Размер страницы (по умолчанию: 20, макс: 100).</param>
    /// <returns>Отфильтрованный по страницам список задач.</returns>
    /// <response code="200">Список задач успешно получен.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<TaskDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<TaskDto>>> List(
        [FromQuery] [Range(1, int.MaxValue)] int page = 1,
        [FromQuery] [Range(1, 100)] int pageSize = 20)
    {
        _logger.LogInformation("Получение задач: Страница {Page}, Размер {PageSize}", page, pageSize);
        var result = await _taskService.GetPagedAsync(page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Получает задачу по её уникальному идентификатору.
    /// </summary>
    /// <param name="id">Уникальный идентификатор задачи.</param>
    /// <returns>Задача, если найдена.</returns>
    /// <response code="200">Задача успешно получена.</response>
    /// <response code="404">Задача не найдена.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDto>> GetById(Guid id)
    {
        _logger.LogInformation("Получение задачи по ID: {Id}", id);
        var task = await _taskService.GetByIdAsync(id);
        return Ok(task);
    }

    /// <summary>
    /// Обновляет существующую задачу.
    /// </summary>
    /// <param name="id">Уникальный идентификатор задачи.</param>
    /// <param name="dto">DTO для обновления задачи.</param>
    /// <returns>Обновленная задача.</returns>
    /// <response code="200">Задача успешно обновлена.</response>
    /// <response code="400">Некорректное тело запроса.</response>
    /// <response code="404">Задача не найдена.</response>
    /// <response code="409">Конфликт оптимистичной блокировки.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ServiceFilter(typeof(ValidationActionFilter<UpdateTaskDto>))]
    public async Task<ActionResult<TaskDto>> Update(Guid id, [FromBody] UpdateTaskDto dto)
    {
        _logger.LogInformation("Обновление задачи с ID: {Id}", id);

        try
        {
            var task = await _taskService.UpdateAsync(id, dto);
            return Ok(task);
        }
        catch (ConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Конфликт оптимистичной блокировки при обновлении задачи {Id}", id);
            return Conflict(new ProblemDetails { Title = "Конфликт оптимистичной блокировки", Detail = ex.Message });
        }
    }

    /// <summary>
    /// Удаляет задачу по её уникальному идентификатору.
    /// </summary>
    /// <param name="id">Уникальный идентификатор задачи.</param>
    /// <returns>Отсутствует содержимое при успешном удалении.</returns>
    /// <response code="204">Задача успешно удалена.</response>
    /// <response code="404">Задача не найдена.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id)
    {
        _logger.LogInformation("Удаление задачи с ID: {Id}", id);
        await _taskService.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Ищет задачи по фильтрам и параметрам поиска.
    /// </summary>
    /// <param name="status">Статус для фильтрации (опциональный).</param>
    /// <param name="searchTitle">Подстрока для поиска по заголовку (опциональная).</param>
    /// <param name="page">Номер страницы (по умолчанию: 1).</param>
    /// <param name="pageSize">Размер страницы (по умолчанию: 20, макс: 100).</param>
    /// <returns>Отфильтрованный по страницам список задач.</returns>
    /// <response code="200">Задачи успешно найдены.</response>
    /// <response code="400">Некорректный статус.</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(PaginatedResult<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResult<TaskDto>>> Search(
        [FromQuery] string? status,
        [FromQuery] string? searchTitle,
        [FromQuery] [Range(1, int.MaxValue)] int page = 1,
        [FromQuery] [Range(1, 100)] int pageSize = 20)
    {
        var filter = new TaskFilterDto
        {
            Page = page,
            PageSize = pageSize,
            SearchTitle = searchTitle?.Trim()
        };

        if (!string.IsNullOrEmpty(status))
        {
            if (!Enum.TryParse(status, ignoreCase: true, out TaskStatus parsedStatus))
            {
                return BadRequest(new { error = $"Недопустимое значение статуса: '{status}'. Допустимые значения: New, InProgress, Completed." });
            }

            filter.Status = parsedStatus;
        }

        _logger.LogInformation("Поиск задач: Статус {Status}, Заголовок {SearchTitle}, Страница {Page}, Размер {PageSize}",
            status, searchTitle, page, pageSize);

        var result = await _taskService.SearchAsync(filter);
        AppendPaginationHeaders(result);

        return Ok(result);
    }

    /// <summary>
    /// Добавляет заголовки пагинации в ответ.
    /// </summary>
    /// <param name="result">Результат с пагинацией.</param>
    private void AppendPaginationHeaders(PaginatedResult<TaskDto> result)
    {
        Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
        Response.Headers.Append("X-Page-Number", result.Page.ToString());
        Response.Headers.Append("X-Page-Size", result.PageSize.ToString());
        Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());
        Response.Headers.Append("X-Has-Next-Page", result.HasNextPage.ToString());
        Response.Headers.Append("X-Has-Previous-Page", result.HasPreviousPage.ToString());

        if (result.HasNextPage)
        {
            var nextUrl = Url.Action("Search", "Task", new { page = result.Page + 1, pageSize = result.PageSize }, Request.Scheme);
            Response.Headers.Append("Link", $"<{nextUrl}>; rel=\"next\"");
        }

        if (result.HasPreviousPage)
        {
            var prevUrl = Url.Action("Search", "Task", new { page = result.Page - 1, pageSize = result.PageSize }, Request.Scheme);
            Response.Headers.Append("Link", $"<{prevUrl}>; rel=\"prev\"");
        }
    }
}
