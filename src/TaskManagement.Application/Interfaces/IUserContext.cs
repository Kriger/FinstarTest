namespace TaskManagement.Application.Interfaces;

/// <summary>
/// Контекст текущего пользователя.
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// Уникальный идентификатор пользователя.
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// Имя пользователя.
    /// </summary>
    string UserName { get; }

    /// <summary>
    /// Является ли пользователь аутентифицированным.
    /// </summary>
    bool IsAuthenticated { get; }
}
