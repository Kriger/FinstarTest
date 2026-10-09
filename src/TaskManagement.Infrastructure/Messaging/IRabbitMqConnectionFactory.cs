using RabbitMQ.Client;

namespace TaskManagement.Infrastructure.Messaging;

/// <summary>
/// Интерфейс фабрики для создания соединений и каналов RabbitMQ.
/// </summary>
public interface IRabbitMqConnectionFactory : IDisposable
{
    /// <summary>
    /// Создаёт подключение к RabbitMQ.
    /// </summary>
    IConnection CreateConnection();

    /// <summary>
    /// Создаёт канал в рамках подключения.
    /// </summary>
    /// <param name="connection">Подключение для создания канала.</param>
    IModel CreateChannel(IConnection connection);

    /// <summary>
    /// Объявляет очередь для событий задач.
    /// </summary>
    /// <param name="channel">Канал для объявления очереди.</param>
    void DeclareTaskEventsQueue(IModel channel);
}
