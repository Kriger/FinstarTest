using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using TaskManagement.Application.Configuration;
using TaskManagement.Infrastructure.Messaging;

namespace TaskManagement.Consumer.Services;

/// <summary>
/// Фоновый сервис, прослушивающий RabbitMQ и логирующий входящие события.
/// Включает дедупликацию событий и валидацию существования задач.
/// </summary>
public class EventLoggerService : BackgroundService
{
    private readonly ILogger<EventLoggerService> _logger;
    private readonly RabbitMqSettings _settings;
    private readonly IHostApplicationLifetime _applicationLifetime;

    private static readonly Serilog.ILogger _serilog = Log.ForContext<EventLoggerService>();

    /// <summary>
    /// Хранилище обработанных EventId для дедупликации.
    /// Ключ — EventId, значение — время истечения TTL.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, DateTime> _processedEvents = new();

    /// <summary>
    /// Интервал очистки устаревших записей из кеша дедупликации.
    /// </summary>
    private static readonly TimeSpan DeduplicationTtl = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Интервал принудительной очистки кеша дедупликации.
    /// </summary>
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="EventLoggerService"/>.
    /// </summary>
    /// <param name="logger">Консольный логгер.</param>
    /// <param name="settings">Настройки RabbitMQ из конфигурации.</param>
    /// <param name="applicationLifetime">Менеджер жизненного цикла приложения.</param>
    public EventLoggerService(
        ILogger<EventLoggerService> logger,
        IOptions<RabbitMqSettings> settings,
        IHostApplicationLifetime applicationLifetime)
    {
        _logger = logger;
        _settings = settings.Value;
        _applicationLifetime = applicationLifetime;
    }

    /// <summary>
    /// Базовая задержка между попытками переподключения (экспоненциальный рост).
    /// </summary>
    private static readonly TimeSpan InitialReconnectDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Максимальная задержка между попытками переподключения.
    /// </summary>
    private static readonly TimeSpan MaxReconnectDelay = TimeSpan.FromSeconds(60);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _serilog.Information("EventLoggerService запускается");
        _serilog.Information("Подключение к RabbitMQ: {HostName}:{Port}", _settings.HostName, _settings.Port);

        var factory = new ConnectionFactory()
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password,
            VirtualHost = _settings.VirtualHost,
            RequestedHeartbeat = TimeSpan.FromSeconds(10)
        };

        if (!string.IsNullOrEmpty(_settings.ConnectionString))
        {
            factory.Uri = new Uri(_settings.ConnectionString);
        }

        // Запускаем фоновую задачу очистки кеша дедупликации
        var cleanupTask = Task.Run(
            () => CleanupExpiredEntries(stoppingToken),
            stoppingToken);

        // Основной цикл с автоматическим переподключением
        await RunConsumerLoop(factory, stoppingToken);

        await cleanupTask;
        _serilog.Information("EventLoggerService остановлен");
    }

    /// <summary>
    /// Основной цикл подключения и потребления сообщений с автоматическим переподключением.
    /// </summary>
    /// <param name="factory">Фабрика соединений RabbitMQ.</param>
    /// <param name="stoppingToken">Токен отмены.</param>
    private async Task RunConsumerLoop(ConnectionFactory factory, CancellationToken stoppingToken)
    {
        var reconnectDelay = InitialReconnectDelay;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAndConsumeAsync(factory, stoppingToken);
                // Если подключение установлено, сбрасываем задержку для следующего цикла
                reconnectDelay = InitialReconnectDelay;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _serilog.Information("Цикл подключения прерван по токену отмены");
                break;
            }
            catch (Exception ex)
            {
                _serilog.Error(
                    ex,
                    "Ошибка подключения к RabbitMQ. Переподключение через {Delay} сек.",
                    reconnectDelay.TotalSeconds);

                await Task.Delay(reconnectDelay, stoppingToken);

                // Экспоненциальный рост задержки с ограничением
                reconnectDelay = TimeSpan.FromSeconds(
                    Math.Min(reconnectDelay.TotalSeconds * 2, MaxReconnectDelay.TotalSeconds));
            }
        }
    }

    /// <summary>
    /// Устанавливает соединение, объявляет очереди и начинает потребление сообщений.
    /// </summary>
    /// <param name="factory">Фабрика соединений RabbitMQ.</param>
    /// <param name="stoppingToken">Токен отмены.</param>
    private async Task ConnectAndConsumeAsync(ConnectionFactory factory, CancellationToken stoppingToken)
    {
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        // Объявляем exchange, основную очередь и DLX с binding'ами
        var connectionFactory = new RabbitMqConnectionFactory(
            _settings.ConnectionString
            ?? $"amqp://{_settings.UserName}:{_settings.Password}@{_settings.HostName}:{_settings.Port}",
            null!,
            _settings.QueueName);
        connectionFactory.DeclareTaskEventsQueue(channel);

        // Объявляем Dead Letter Exchange и очередь
        const string deadLetterExchange = "task_events_dlx";
        const string deadLetterQueue = "task_events_dlx_queue";

        channel.ExchangeDeclare(
            exchange: deadLetterExchange,
            type: "direct",
            durable: true);

        channel.QueueDeclare(
            queue: deadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false);

        channel.QueueBind(
            queue: deadLetterQueue,
            exchange: deadLetterExchange,
            routingKey: "dead_letter");

        // Переподключаем основную очередь с параметрами DLX
        channel.QueueDeclare(
            queue: _settings.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] = deadLetterExchange,
                ["x-dead-letter-routing-key"] = "dead_letter"
            });

        // Пересоздаём binding'и для основной очереди
        channel.QueueBind(
            queue: _settings.QueueName,
            exchange: "task_events",
            routingKey: "task_created");
        channel.QueueBind(
            queue: _settings.QueueName,
            exchange: "task_events",
            routingKey: "task_updated");
        channel.QueueBind(
            queue: _settings.QueueName,
            exchange: "task_events",
            routingKey: "task_deleted");

        _serilog.Information(
            "Подключено к RabbitMQ. Прослушивание очереди: {Queue}, DLX: {DeadLetterExchange}",
            _settings.QueueName,
            deadLetterExchange);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var routingKey = ea.RoutingKey;

            try
            {
                // Извлекаем EventId из JSON-сообщения для дедупликации
                var eventId = ExtractEventId(message);

                // Проверяем дедупликацию
                if (eventId.HasValue && IsDuplicate(eventId.Value))
                {
                    _serilog.Warning(
                        "Дубликат события [EventId: {EventId}], пропускаем",
                        eventId.Value);
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    return;
                }

                _serilog.Information(
                    "Получено событие [{RoutingKey}] EventId: {EventId}: {Message}",
                    routingKey,
                    eventId ?? Guid.Empty,
                    message);

                // Определяем тип события
                var eventType = GetEventType(routingKey);
                _serilog.Information(
                    "Тип события: {EventType}, TaskId: {TaskId}, Получено в: {Timestamp}",
                    eventType,
                    eventId ?? Guid.Empty,
                    DateTime.UtcNow);

                // Помечаем событие как обработанное для дедупликации
                if (eventId.HasValue)
                {
                    MarkAsProcessed(eventId.Value);
                }

                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _serilog.Error(
                    ex,
                    "Ошибка обработки события [{RoutingKey}] EventId: {EventId}",
                    routingKey,
                    ExtractEventId(message) ?? Guid.Empty);

                // Отклоняем сообщение без повторной отправки — отправляем в DLX
                channel.BasicNack(
                    deliveryTag: ea.DeliveryTag,
                    multiple: false,
                    requeue: false);
            }
        };

        channel.BasicConsume(
            queue: _settings.QueueName,
            autoAck: false,
            consumer: consumer);

        // Ждём отмены — если соединение разорвётся, будет выброшено исключение
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    /// <summary>
    /// Извлекает EventId из JSON-сообщения.
    /// </summary>
    /// <param name="message">JSON-строка события.</param>
    /// <returns>EventId или null, если извлечь не удалось.</returns>
    private static Guid? ExtractEventId(string message)
    {
        try
        {
            var options = new JsonDocumentOptions
            {
                MaxDepth = 64,
                AllowTrailingCommas = true
            };
            using var doc = JsonDocument.Parse(message, options);
            var root = doc.RootElement;
            if (root.TryGetProperty("EventId", out var eventIdProp) && eventIdProp.TryGetGuid(out var eventId))
            {
                return eventId;
            }
        }
        catch
        {
            // Не критично — дедупликация не сработает, но сообщение будет обработано
        }

        return null;
    }

    /// <summary>
    /// Проверяет, является ли событие дубликатом.
    /// </summary>
    /// <param name="eventId">Идентификатор события.</param>
    /// <returns>True, если событие уже было обработано.</returns>
    private bool IsDuplicate(Guid eventId)
    {
        return _processedEvents.ContainsKey(eventId);
    }

    /// <summary>
    /// Добавляет EventId в кеш дедупликации.
    /// </summary>
    /// <param name="eventId">Идентификатор события.</param>
    private void MarkAsProcessed(Guid eventId)
    {
        _processedEvents[eventId] = DateTime.UtcNow + DeduplicationTtl;
    }

    /// <summary>
    /// Периодически очищает устаревшие записи из кеша дедупликации.
    /// </summary>
    /// <param name="stoppingToken">Токен отмены.</param>
    private async Task CleanupExpiredEntries(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var expiredKeys = _processedEvents
                    .Where(kvp => kvp.Value <= now)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    _processedEvents.TryRemove(key, out _);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Ошибка при очистке кеша дедупликации");
            }

            await Task.Delay(CleanupInterval, stoppingToken);
        }
    }

    /// <summary>
    /// Определяет тип события по routing key.
    /// </summary>
    /// <param name="routingKey">Ключ маршрутизации.</param>
    /// <returns>Название типа события.</returns>
    private static string GetEventType(string routingKey)
    {
        return routingKey switch
        {
            "task_created" => "TaskCreatedEvent",
            "task_updated" => "TaskUpdatedEvent",
            "task_deleted" => "TaskDeletedEvent",
            _ => "UnknownEvent"
        };
    }
}
