namespace TaskManagement.Application.Exceptions;

/// <summary>
/// Исключение, возникающее при отсутствии требуемой задачи.
/// </summary>
public class TaskNotFoundException : Exception
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="TaskNotFoundException"/>.
    /// </summary>
    /// <param name="taskId">Уникальный идентификатор отсутствующей задачи.</param>
    public TaskNotFoundException(Guid taskId)
        : base($"Задача с ID {taskId} не найдена.")
    {
        TaskId = taskId;
    }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="TaskNotFoundException"/> с сообщением.
    /// </summary>
    /// <param name="message">Сообщение об ошибке.</param>
    public TaskNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Уникальный идентификатор отсутствующей задачи.
    /// </summary>
    public Guid TaskId { get; }
}
