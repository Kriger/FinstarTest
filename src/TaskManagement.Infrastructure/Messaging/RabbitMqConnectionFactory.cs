using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using RabbitMQ.Client;

namespace TaskManagement.Infrastructure.Messaging;

/// <summary>
/// Фабрика соединений RabbitMQ с политиками повторных попыток и Circuit Breaker.
/// </summary>
public class RabbitMqConnectionFactory : IRabbitMqConnectionFactory, IDisposable
{
    private readonly string _connectionString;
    private readonly ILogger<RabbitMqConnectionFactory> _logger;
    private readonly ResiliencePipeline _pipeline;
    private readonly string _queueName;
    private bool _disposed;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="RabbitMqConnectionFactory"/>.
    /// </summary>
    /// <param name="connectionString">Строка подключения RabbitMQ.</param>
    /// <param name="logger">Логгер.</param>
    /// <param name="queueName">Имя очереди (по умолчанию: task_events).</param>
    public RabbitMqConnectionFactory(
        string connectionString,
        ILogger<RabbitMqConnectionFactory> logger,
        string queueName = "task_events")
    {
        _connectionString = connectionString;
        _logger = logger;
        _queueName = queueName;

        // Настройка политики повторных попыток: 3 попытки с экспоненциальной задержкой
        var retryStrategy = new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = new PredicateBuilder().Handle<Exception>(),
            OnRetry = (args) =>
            {
                _logger.LogWarning(
                    "Публикация в RabbitMQ не удалась (попытка {Attempt}/{Max}): {Message}",
                    args.AttemptNumber + 1,
                    3,
                    args.Outcome.Exception?.Message);
                return default;
            }
        };

        // Настройка Circuit Breaker: открывает цепь после 50% ошибок при >= 20 запросах
        // на 30 секунд, затем позволяет один пробный запрос (half-open)
        var circuitBreakerStrategy = new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            MinimumThroughput = 20,
            BreakDuration = TimeSpan.FromSeconds(30),
            ShouldHandle = new PredicateBuilder().Handle<Exception>(),
            OnClosed = (args) =>
            {
                _logger.LogInformation("Circuit Breaker закрыт: RabbitMQ доступен");
                return default;
            },
            OnOpened = (args) =>
            {
                _logger.LogError(
                    "Circuit Breaker открыт: RabbitMQ недоступен. Ожидание {BreakDuration} сек.",
                    args.BreakDuration.TotalSeconds);
                return default;
            },
            OnHalfOpened = (args) =>
            {
                _logger.LogInformation("Circuit Breaker: half-open — пробный запрос к RabbitMQ");
                return default;
            }
        };

        // Комбинируем Retry + Circuit Breaker: сначала retry, потом circuit breaker
        _pipeline = new ResiliencePipelineBuilder()
            .AddRetry(retryStrategy)
            .AddCircuitBreaker(circuitBreakerStrategy)
            .Build();
    }

    /// <inheritdoc />
    public IConnection CreateConnection()
    {
        var factory = new ConnectionFactory()
        {
            Uri = new Uri(_connectionString),
            RequestedHeartbeat = TimeSpan.FromSeconds(10)
        };

        return factory.CreateConnection();
    }

    /// <inheritdoc />
    public IModel CreateChannel(IConnection connection)
    {
        return connection.CreateModel();
    }

    /// <inheritdoc />
    public void DeclareTaskEventsQueue(IModel channel)
    {
        const string exchangeName = "task_events";

        // Объявляем exchange типа direct
        channel.ExchangeDeclare(
            exchange: exchangeName,
            type: "direct",
            durable: true,
            autoDelete: false);

        // Объявляем очередь
        channel.QueueDeclare(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false);

        // Связываем очередь с exchange для каждого типа событий
        channel.QueueBind(
            queue: _queueName,
            exchange: exchangeName,
            routingKey: "task_created");
        channel.QueueBind(
            queue: _queueName,
            exchange: exchangeName,
            routingKey: "task_updated");
        channel.QueueBind(
            queue: _queueName,
            exchange: exchangeName,
            routingKey: "task_deleted");
    }

    /// <summary>
    /// Выполняет публикацию с повторными попытками и Circuit Breaker.
    /// </summary>
    /// <param name="action">Действие для выполнения.</param>
    public void ExecuteWithRetry(Action action)
    {
        _pipeline.Execute(action);
    }

    /// <summary>
    /// Выполняет асинхронную публикацию с повторными попытками и Circuit Breaker.
    /// </summary>
    /// <param name="action">Асинхронное действие для выполнения.</param>
    public async Task ExecuteWithRetryAsync(Func<Task> action)
    {
        await _pipeline.ExecuteAsync(async _ => await action());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // ResiliencePipeline не требует явного disposal
    }
}
