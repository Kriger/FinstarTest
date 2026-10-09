namespace TaskManagement.Application.Configuration;

/// <summary>
/// Общие настройки подключения к RabbitMQ.
/// </summary>
public class RabbitMqSettings
{
    /// <summary>
    /// Имя хоста RabbitMQ.
    /// </summary>
    public string HostName { get; set; } = "localhost";

    /// <summary>
    /// Порт RabbitMQ.
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Виртуальный хост.
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Имя пользователя для аутентификации.
    /// </summary>
    public string UserName { get; set; } = "guest";

    /// <summary>
    /// Пароль для аутентификации.
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// Альтернативная строка подключения.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Имя очереди для потребителя.
    /// </summary>
    public string QueueName { get; set; } = "task_events";
}
