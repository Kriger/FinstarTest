using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Events;

namespace TaskManagement.Infrastructure.Messaging;

/// <summary>
/// Реализация IEventPublisher на основе RabbitMQ с повторными попытками Polly.
/// Создаёт новый канал для каждой операции публикации для корректной обработки сбоев соединения.
/// </summary>
public class RabbitMqEventPublisher : IEventPublisher
{
    private readonly IConnection _connection;
    private readonly RabbitMqConnectionFactory _factory;
    private readonly ILogger<RabbitMqEventPublisher> _logger;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="RabbitMqEventPublisher"/>.
    /// </summary>
    /// <param name="connection">Подключение RabbitMQ.</param>
    /// <param name="factory">Фабрика соединений RabbitMQ.</param>
    /// <param name="logger">Логгер.</param>
    public RabbitMqEventPublisher(
        IConnection connection,
        RabbitMqConnectionFactory factory,
        ILogger<RabbitMqEventPublisher> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : TaskEvent
    {
        var routingKey = GetRoutingKey(@event);
        await _factory.ExecuteWithRetryAsync(() => PublishAsyncInternal(routingKey, @event));
    }

    /// <inheritdoc />
    public async Task PublishRawAsync(string eventType, string content, CancellationToken cancellationToken = default)
    {
        var routingKey = GetRoutingKey(eventType);
        await _factory.ExecuteWithRetryAsync(() => PublishRawAsyncInternal(routingKey, content, cancellationToken));
    }

    private static string GetRoutingKey(TaskEvent @event)
    {
        return @event switch
        {
            TaskCreatedEvent => "task_created",
            TaskUpdatedEvent => "task_updated",
            TaskDeletedEvent => "task_deleted",
            _ => throw new ArgumentOutOfRangeException(nameof(@event), "Неизвестный тип события.")
        };
    }

    private static string GetRoutingKey(string eventType)
    {
        return eventType switch
        {
            "TaskCreatedEvent" => "task_created",
            "TaskUpdatedEvent" => "task_updated",
            "TaskDeletedEvent" => "task_deleted",
            _ => throw new ArgumentOutOfRangeException(nameof(eventType), "Неизвестный тип события.")
        };
    }

    private async Task PublishAsyncInternal(string routingKey, TaskEvent @event)
    {
        // Создаём новый канал для каждой публикации — это гарантирует корректную обработку сбоев соединения
        using var channel = _connection.CreateModel();

        var json = JsonSerializer.Serialize(@event);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";

        channel.BasicPublish(
            exchange: "task_events",
            routingKey: routingKey,
            basicProperties: properties,
            body: body);

        _logger?.LogInformation(
            "Событие опубликовано: {RoutingKey}, TaskId: {TaskId}",
            routingKey,
            @event.TaskId);
    }

    private async Task PublishRawAsyncInternal(string routingKey, string content, CancellationToken cancellationToken)
    {
        // Создаём новый канал для каждой публикации — это гарантирует корректную обработку сбоев соединения
        using var channel = _connection.CreateModel();

        var body = Encoding.UTF8.GetBytes(content);

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";

        channel.BasicPublish(
            exchange: "task_events",
            routingKey: routingKey,
            basicProperties: properties,
            body: body);

        _logger?.LogInformation(
            "Сырое событие опубликовано: {RoutingKey}",
            routingKey);
    }
}
