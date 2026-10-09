namespace TaskManagement.Domain.Events;

/// <summary>
/// Базовый класс для событий, связанных с задачами.
/// </summary>
public abstract class TaskEvent
{
    /// <summary>
    /// Уникальный идентификатор события для идемпотентности и дедупликации.
    /// </summary>
    public Guid EventId { get; }

    /// <summary>
    /// Уникальный идентификатор задачи.
    /// </summary>
    public Guid TaskId { get; }

    /// <summary>
    /// Временная метка возникновения события.
    /// </summary>
    public DateTime OccurredAt { get; }

    /// <summary>
    /// Инициализирует новый экземпляр базового класса <see cref="TaskEvent"/>.
    /// </summary>
    /// <param name="taskId">Уникальный идентификатор задачи.</param>
    protected TaskEvent(Guid taskId)
    {
        EventId = Guid.NewGuid();
        TaskId = taskId;
        OccurredAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Возвращает строковое представление события для маршрутизации.
    /// </summary>
    /// <returns>Имя типа события.</returns>
    public string GetRoutingKey() => GetType().Name;
}

/// <summary>
/// Событие создания задачи.
/// </summary>
public class TaskCreatedEvent : TaskEvent
{
    /// <summary>
    /// Заголовок задачи.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Описание задачи.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="TaskCreatedEvent"/>.
    /// </summary>
    /// <param name="taskId">Уникальный идентификатор задачи.</param>
    /// <param name="title">Заголовок задачи.</param>
    /// <param name="description">Описание задачи.</param>
    public TaskCreatedEvent(Guid taskId, string title, string description)
        : base(taskId)
    {
        Title = title;
        Description = description;
    }
}

/// <summary>
/// Событие обновления задачи.
/// </summary>
public class TaskUpdatedEvent : TaskEvent
{
    /// <summary>
    /// Заголовок задачи.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Описание задачи.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Текущий статус задачи.
    /// </summary>
    public Domain.Enums.TaskStatus Status { get; }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="TaskUpdatedEvent"/>.
    /// </summary>
    /// <param name="taskId">Уникальный идентификатор задачи.</param>
    /// <param name="title">Заголовок задачи.</param>
    /// <param name="description">Описание задачи.</param>
    /// <param name="status">Текущий статус задачи.</param>
    public TaskUpdatedEvent(Guid taskId, string title, string description, Domain.Enums.TaskStatus status)
        : base(taskId)
    {
        Title = title;
        Description = description;
        Status = status;
    }
}

/// <summary>
/// Событие удаления задачи.
/// </summary>
public class TaskDeletedEvent : TaskEvent
{
    /// <summary>
    /// Заголовок задачи.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="TaskDeletedEvent"/>.
    /// </summary>
    /// <param name="taskId">Уникальный идентификатор задачи.</param>
    /// <param name="title">Заголовок задачи.</param>
    public TaskDeletedEvent(Guid taskId, string title)
        : base(taskId)
    {
        Title = title;
    }
}
