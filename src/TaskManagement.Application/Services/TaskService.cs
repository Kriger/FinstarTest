using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using TaskManagement.Application.Configuration;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Mapping;
using TaskManagement.Domain.Events;
using TaskManagement.Domain.Outbox;
using DomainTask = TaskManagement.Domain.Models.Task;
using OutboxMessage = TaskManagement.Domain.Outbox.OutboxMessage;

namespace TaskManagement.Application.Services;

/// <summary>
/// Сервис, реализующий бизнес-логику для CRUD-операций с задачами.
/// Использует Outbox-паттерн для гарантированной доставки событий.
/// </summary>
public class TaskService : ITaskService
{
    private const string CacheKeyPrefix = "tasks_paged_";
    private const string SearchCacheKeyPrefix = "tasks_search_";
    private readonly ITaskRepository _taskRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITaskMapper _taskMapper;
    private readonly IDistributedCache _cache;
    private readonly IUserContext _userContext;
    private readonly int _cacheDurationSeconds;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="TaskService"/>.
    /// </summary>
    /// <param name="taskRepository">Репозиторий для доступа к данным.</param>
    /// <param name="outboxRepository">Репозиторий Outbox для гарантированной доставки событий.</param>
    /// <param name="unitOfWork">Единица работы для атомарного сохранения.</param>
    /// <param name="taskMapper">Маппер для преобразования сущностей.</param>
    /// <param name="cache">Распределённый кэш (Redis).</param>
    /// <param name="userContext">Контекст текущего пользователя.</param>
    /// <param name="cachingSettings">Настройки кэширования.</param>
    public TaskService(
        ITaskRepository taskRepository,
        IOutboxRepository outboxRepository,
        IUnitOfWork unitOfWork,
        ITaskMapper taskMapper,
        IDistributedCache cache,
        IUserContext userContext,
        IOptions<CachingSettings> cachingSettings)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _outboxRepository = outboxRepository ?? throw new ArgumentNullException(nameof(outboxRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _taskMapper = taskMapper ?? throw new ArgumentNullException(nameof(taskMapper));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _cacheDurationSeconds = cachingSettings.Value.DefaultCacheDurationSeconds;
    }

    /// <summary>
    /// Создаёт новую задачу из предоставленного DTO.
    /// </summary>
    /// <param name="dto">DTO для создания задачи.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Созданная задача в виде DTO.</returns>
    public async Task<TaskDto> CreateAsync(CreateTaskDto dto, CancellationToken cancellationToken = default)
    {
        var task = new DomainTask(dto.Title, dto.Description, _userContext.UserId);
        await _taskRepository.AddAsync(task, cancellationToken);

        var createdEvent = new TaskCreatedEvent(task.Id, task.Title, task.Description);
        var outboxMessage = CreateOutboxMessage(createdEvent);
        await _outboxRepository.AddAsync(outboxMessage, cancellationToken);

        // Атомарное сохранение задачи и Outbox-сообщения в одной транзакции
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        InvalidateCache();
        return _taskMapper.ToDto(task);
    }

    /// <summary>
    /// Получает задачи с пагинацией.
    /// </summary>
    /// <param name="page">Номер страницы (начиная с 1).</param>
    /// <param name="pageSize">Количество элементов на странице.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат с пагинацией.</returns>
    public async Task<PaginatedResult<TaskDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{page}_{pageSize}";
        return await GetFromCacheAsync(cacheKey, page, pageSize, ct => _taskRepository.GetPagedAsync(page, pageSize, ct), cancellationToken);
    }

    /// <summary>
    /// Получает задачу по уникальному идентификатору.
    /// </summary>
    /// <param name="id">Уникальный идентификатор задачи.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача в виде DTO.</returns>
    /// <exception cref="TaskNotFoundException">Выбрасывается, когда задача с указанным ID не найдена.</exception>
    public async Task<TaskDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new TaskNotFoundException(id);

        return _taskMapper.ToDto(task);
    }

    /// <summary>
    /// Обновляет существующую задачу с указанным DTO.
    /// </summary>
    /// <param name="id">Уникальный идентификатор обновляемой задачи.</param>
    /// <param name="dto">DTO для обновления задачи.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Обновлённая задача в виде DTO.</returns>
    /// <exception cref="TaskNotFoundException">Выбрасывается, когда задача с указанным ID не найдена или удалена.</exception>
    public async Task<TaskDto> UpdateAsync(Guid id, UpdateTaskDto dto, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new TaskNotFoundException(id);

        if (task.IsDeleted)
        {
            throw new TaskNotFoundException(id);
        }

        var hasChanges = task.ApplyUpdates(dto.Title, dto.Description, dto.Status, _userContext.UserId);

        await _taskRepository.UpdateAsync(task);

        if (hasChanges)
        {
            var updatedEvent = new TaskUpdatedEvent(task.Id, task.Title, task.Description, task.Status);
            var outboxMessage = CreateOutboxMessage(updatedEvent);
            await _outboxRepository.AddAsync(outboxMessage, cancellationToken);
        }

        // Атомарное сохранение задачи и Outbox-сообщения в одной транзакции
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        InvalidateCache();
        return _taskMapper.ToDto(task);
    }

    /// <summary>
    /// Удаляет задачу по уникальному идентификатору.
    /// </summary>
    /// <param name="id">Уникальный идентификатор удаляемой задачи.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new TaskNotFoundException(id);

        task.Delete(_userContext.UserId);

        var deletedEvent = new TaskDeletedEvent(task.Id, task.Title);
        var outboxMessage = CreateOutboxMessage(deletedEvent);
        await _outboxRepository.AddAsync(outboxMessage, cancellationToken);

        // Атомарное сохранение задачи и Outbox-сообщения в одной транзакции
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        InvalidateCache();
    }

    /// <summary>
    /// Получает задачи с фильтрацией и поиском.
    /// </summary>
    /// <param name="filter">Параметры фильтрации.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Отфильтрованный список задач с пагинацией.</returns>
    public async Task<PaginatedResult<TaskDto>> SearchAsync(TaskFilterDto filter, CancellationToken cancellationToken = default)
    {
        var sanitizedTitle = Uri.EscapeDataString(filter.SearchTitle ?? string.Empty);
        var cacheKey = $"{SearchCacheKeyPrefix}{filter.Status?.ToString() ?? "None"}_{sanitizedTitle}_{filter.Page}_{filter.PageSize}";
        return await GetFromCacheAsync(cacheKey, filter.Page, filter.PageSize, ct => _taskRepository.SearchAsync(filter, ct), cancellationToken);
    }

    /// <summary>
    /// Инвалидирует все кэшированные данные задач (по префиксу).
    /// </summary>
    private void InvalidateCache()
    {
        // Redis не поддерживает удаление по паттерну без SCAN, поэтому полагаемся на TTL
        // Кэш автоматически инвалидируется по истечении времени жизни
    }

    private async Task<PaginatedResult<TaskDto>> GetFromCacheAsync(
        string cacheKey,
        int page,
        int pageSize,
        Func<CancellationToken, Task<(List<DomainTask> Items, int TotalCount)>> fetchFunc,
        CancellationToken cancellationToken)
    {
        var cachedResult = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedResult))
        {
            return JsonSerializer.Deserialize<PaginatedResult<TaskDto>>(cachedResult)
                ?? throw new InvalidOperationException("Не удалось десериализовать кэш.");
        }

        var (items, totalCount) = await fetchFunc(cancellationToken);
        var totalPages = totalCount > 0 ? (totalCount - 1) / pageSize + 1 : 0;

        var result = new PaginatedResult<TaskDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            Items = [.. items.Select(_taskMapper.ToDto)]
        };

        var cacheEntryOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_cacheDurationSeconds)
        };

        var json = JsonSerializer.Serialize(result);
        await _cache.SetStringAsync(cacheKey, json, cacheEntryOptions);

        return result;
    }

    /// <summary>
    /// Создаёт сообщение Outbox из доменного события.
    /// </summary>
    /// <param name="event">Доменное событие.</param>
    /// <returns>Сообщение Outbox.</returns>
    private static OutboxMessage CreateOutboxMessage(TaskEvent @event)
    {
        var content = JsonSerializer.Serialize(@event);
        return OutboxMessage.Create(@event.GetType().Name, content);
    }
}
