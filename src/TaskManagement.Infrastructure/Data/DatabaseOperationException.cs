namespace TaskManagement.Infrastructure.Data;

/// <summary>
/// Исключение, выбрасываемое при ошибке выполнения операции с БД.
/// </summary>
public class DatabaseOperationException : Exception
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="DatabaseOperationException"/>.
    /// </summary>
    /// <param name="message">Сообщение об ошибке.</param>
    public DatabaseOperationException(string message) : base(message)
    {
    }
}
