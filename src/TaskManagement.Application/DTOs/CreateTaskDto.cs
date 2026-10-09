namespace TaskManagement.Application.DTOs;

/// <summary>
/// DTO для создания новой задачи.
/// </summary>
public class CreateTaskDto
{
    /// <summary>
    /// Заголовок задачи (обязательный, макс. 200 символов).
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Описание задачи.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
