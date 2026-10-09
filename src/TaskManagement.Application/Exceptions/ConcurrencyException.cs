namespace TaskManagement.Application.Exceptions;

/// <summary>
/// Исключение, возникающее при конфликте оптимистичной блокировки.
/// </summary>
public class ConcurrencyException : Exception
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="ConcurrencyException"/>.
    /// </summary>
    /// <param name="message">Сообщение об ошибке.</param>
    public ConcurrencyException(string message)
        : base(message)
    {
    }
}
