using System.Security.Cryptography;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Domain.Models;

/// <summary>
/// Сущность задачи в системе управления задачами.
/// </summary>
public class Task
{
    /// <summary>
    /// Уникальный идентификатор задачи.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Заголовок задачи.
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Описание задачи.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Текущий статус задачи.
    /// </summary>
    public TaskStatus Status { get; private set; }

    /// <summary>
    /// Временная метка создания задачи.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Временная метка последнего обновления задачи.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Идентификатор пользователя, создавшего задачу.
    /// </summary>
    public Guid CreatedBy { get; private set; }

    /// <summary>
    /// Идентификатор пользователя, обновившего задачу.
    /// </summary>
    public Guid UpdatedBy { get; private set; }

    /// <summary>
    /// Версия строки для оптимистичной блокировки.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    /// <summary>
    /// Флаг, указывающий, удалена ли задача (soft delete).
    /// </summary>
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// Временная метка удаления задачи.
    /// </summary>
    public DateTime? DeletedAt { get; private set; }

    /// <summary>
    /// Параметр по умолчанию для EF Core.
    /// </summary>
    public Task()
    {
        Title = string.Empty;
        Description = string.Empty;
        RowVersion = new byte[16];
    }

    /// <summary>
    /// Создаёт новую задачу с указанным заголовком, описанием и идентификатором пользователя.
    /// </summary>
    /// <param name="title">Заголовок задачи.</param>
    /// <param name="description">Описание задачи.</param>
    /// <param name="userId">Идентификатор пользователя, создающего задачу.</param>
    public Task(string title, string description, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Заголовок не может быть пустым.", nameof(title));
        if (title.Length > 200)
            throw new ArgumentException("Заголовок не должен превышать 200 символов.", nameof(title));
        if (description.Length > 2000)
            throw new ArgumentException("Описание не должно превышать 2000 символов.", nameof(description));

        Id = Guid.NewGuid();
        Title = title;
        Description = description;
        Status = TaskStatus.New;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        CreatedBy = userId;
        UpdatedBy = userId;
        RowVersion = new byte[16];
        RandomNumberGenerator.Fill(RowVersion);
    }

    /// <summary>
    /// Создаёт новую задачу (статический фабричный метод).
    /// </summary>
    public static Task Create(Guid userId, string? title = null, string? description = null)
    {
        return new Task(title ?? "Default Task", description ?? string.Empty, userId);
    }

    /// <summary>
    /// Обновляет заголовок задачи.
    /// </summary>
    /// <param name="title">Новый заголовок.</param>
    /// <param name="userId">Идентификатор пользователя, выполняющего обновление.</param>
    public void UpdateTitle(string title, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Заголовок не может быть пустым.", nameof(title));
        if (title.Length > 200)
            throw new ArgumentException("Заголовок не должен превышать 200 символов.", nameof(title));

        Title = title;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = userId;
        IncrementRowVersion();
    }

    /// <summary>
    /// Обновляет описание задачи.
    /// </summary>
    /// <param name="description">Новое описание.</param>
    /// <param name="userId">Идентификатор пользователя, выполняющего обновление.</param>
    public void UpdateDescription(string description, Guid userId)
    {
        if (string.IsNullOrEmpty(description))
            throw new ArgumentException("Описание не может быть пустым.", nameof(description));
        if (description.Length > 2000)
            throw new ArgumentException("Описание не должно превышать 2000 символов.", nameof(description));

        Description = description;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = userId;
        IncrementRowVersion();
    }

    /// <summary>
    /// Обновляет статус задачи.
    /// </summary>
    /// <param name="status">Новый статус.</param>
    /// <param name="userId">Идентификатор пользователя, выполняющего обновление.</param>
    public void UpdateStatus(TaskStatus status, Guid userId)
    {
        if (!Enum.IsDefined(typeof(TaskStatus), status))
            throw new ArgumentException($"Недопустимое значение статуса: {status}.", nameof(status));

        ValidateStatusTransition(Status, status);

        Status = status;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = userId;
        IncrementRowVersion();
    }

    /// <summary>
    /// Проверяет допустимость перехода в указанный статус.
    /// </summary>
    private static void ValidateStatusTransition(TaskStatus currentStatus, TaskStatus newStatus)
    {
        if (currentStatus == newStatus)
            return;

        if (currentStatus == TaskStatus.Completed)
            throw new InvalidOperationException("Нельзя изменить статус завершённой задачи.");

        if (currentStatus == TaskStatus.New && newStatus != TaskStatus.InProgress)
            throw new InvalidOperationException("Задачу можно перевести только в статус InProgress.");

        if (currentStatus == TaskStatus.InProgress && newStatus != TaskStatus.Completed)
            throw new InvalidOperationException("Задачу из статуса InProgress можно перевести только в Completed.");
    }

    /// <summary>
    /// Применяет обновления к задаче из DTO.
    /// </summary>
    /// <param name="title">Новый заголовок (может быть null).</param>
    /// <param name="description">Новое описание (может быть null).</param>
    /// <param name="status">Новый статус (может быть null).</param>
    /// <param name="userId">Идентификатор пользователя, выполняющего обновление.</param>
    /// <returns>true, если изменения были применены; иначе false.</returns>
    public bool ApplyUpdates(string? title, string? description, TaskStatus? status, Guid userId)
    {
        var hasChanges = false;

        if (!string.IsNullOrEmpty(title))
        {
            UpdateTitle(title, userId);
            hasChanges = true;
        }

        if (!string.IsNullOrEmpty(description))
        {
            UpdateDescription(description, userId);
            hasChanges = true;
        }

        if (status is not null)
        {
            UpdateStatus(status.Value, userId);
            hasChanges = true;
        }

        return hasChanges;
    }

    /// <summary>
    /// Помечает задачу как удалённую (soft delete).
    /// </summary>
    /// <param name="userId">Идентификатор пользователя, выполняющего удаление.</param>
    public void Delete(Guid userId)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Задача уже удалена.");

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = userId;
        IncrementRowVersion();
    }

    private void IncrementRowVersion()
    {
        RowVersion = new byte[16];
        RandomNumberGenerator.Fill(RowVersion);
    }
}
