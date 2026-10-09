namespace TaskManagement.Domain.Enums;

/// <summary>
/// Возможные состояния задачи.
/// </summary>
public enum TaskStatus
{
    /// <summary>
    /// Задача создана, но ещё не начата.
    /// </summary>
    New = 0,

    /// <summary>
    /// Задача находится в работе.
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Задача завершена.
    /// </summary>
    Completed = 2
}
