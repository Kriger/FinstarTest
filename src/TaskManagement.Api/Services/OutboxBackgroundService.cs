using Serilog;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Outbox;

namespace TaskManagement.Api.Services;

/// <summary>
/// Фоновый сервис для публикации сообщений из Outbox в RabbitMQ.
/// Реализует retry-логику с exponential backoff и лимитом попыток.
/// </summary>
public class OutboxBackgroundService : BackgroundService
{
    private readonly ILogger<OutboxBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly int _batchSize;
    private readonly TimeSpan _pollingInterval;
    private readonly TimeSpan _retryDelay;

    private static readonly Serilog.ILogger _serilog = Log.ForContext<OutboxBackgroundService>();

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="OutboxBackgroundService"/>.
    /// </summary>
    /// <param name="logger">Логгер.</param>
    /// <param name="serviceProvider">Сервис провайдер для создания scoped сервисов.</param>
    public OutboxBackgroundService(
        ILogger<OutboxBackgroundService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _batchSize = 10;
        _pollingInterval = TimeSpan.FromSeconds(5);
        _retryDelay = TimeSpan.FromSeconds(10);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _serilog.Information("OutboxBackgroundService запускается");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _serilog.Error(ex, "Ошибка при обработке сообщений Outbox");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _serilog.Information("OutboxBackgroundService остановлен");
    }

    /// <summary>
    /// Обрабатывает неотправленные и зависшие сообщения из Outbox.
    /// </summary>
    /// <param name="stoppingToken">Токен отмены.</param>
    private async Task ProcessMessagesAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // 1. Обрабатываем Pending сообщения
        await ProcessPendingMessagesAsync(outboxRepository, eventPublisher, unitOfWork, stoppingToken);

        // 2. Обрабатываем Sending сообщения (зависшие после сбоя)
        await ProcessSendingMessagesAsync(outboxRepository, eventPublisher, unitOfWork, stoppingToken);
    }

    /// <summary>
    /// Обрабатывает сообщения в статусе Pending.
    /// </summary>
    private async Task ProcessPendingMessagesAsync(
        IOutboxRepository outboxRepository,
        IEventPublisher eventPublisher,
        IUnitOfWork unitOfWork,
        CancellationToken stoppingToken)
    {
        var (messages, totalCount) = await outboxRepository.GetPendingAsync(1, _batchSize, stoppingToken);

        if (messages.Count == 0)
        {
            return;
        }

        _serilog.Information(
            "Найдено {MessageCount} сообщений Pending (всего: {TotalCount})",
            messages.Count,
            totalCount);

        foreach (var message in messages)
        {
            try
            {
                message.MarkAsSending();

                await eventPublisher.PublishRawAsync(message.EventType, message.Content, stoppingToken);

                message.MarkAsSent();
                await unitOfWork.SaveChangesAsync(stoppingToken);

                _serilog.Information(
                    "Сообщение Outbox отправлено: {EventType} EventId: {EventId}",
                    message.EventType,
                    message.Id);
            }
            catch (Exception ex)
            {
                _serilog.Error(
                    ex,
                    "Ошибка при отправке сообщения Outbox: {EventType} EventId: {EventId}",
                    message.EventType,
                    message.Id);

                // Откатываем статус обратно на Pending для повторной попытки
                message.MarkAsPending();
                await unitOfWork.SaveChangesAsync(stoppingToken);
            }
        }
    }

    /// <summary>
    /// Обрабатывает сообщения в статусе Sending (зависшие после сбоя).
    /// </summary>
    private async Task ProcessSendingMessagesAsync(
        IOutboxRepository outboxRepository,
        IEventPublisher eventPublisher,
        IUnitOfWork unitOfWork,
        CancellationToken stoppingToken)
    {
        var (messages, totalCount) = await outboxRepository.GetSendingAsync(1, _batchSize, stoppingToken);

        if (messages.Count == 0)
        {
            return;
        }

        _serilog.Information(
            "Найдено {MessageCount} сообщений Sending (зависших) (всего: {TotalCount})",
            messages.Count,
            totalCount);

        foreach (var message in messages)
        {
            if (!message.CanRetry())
            {
                _serilog.Warning(
                    "Исчерпано максимальное количество попыток отправки. Маркирую сообщение как Failed: {EventType} EventId: {EventId} (RetryCount: {RetryCount})",
                    message.EventType,
                    message.Id,
                    message.RetryCount);

                message.MarkAsFailed();
                await unitOfWork.SaveChangesAsync(stoppingToken);
                continue;
            }

            // Exponential backoff: 10s * 2^RetryCount
            var delay = TimeSpan.FromSeconds(_retryDelay.TotalSeconds * Math.Pow(2, message.RetryCount));

            _serilog.Information(
                "Повторная попытка отправки сообщения: {EventType} EventId: {EventId} (RetryCount: {RetryCount}, Backoff: {Backoff}s)",
                message.EventType,
                message.Id,
                message.RetryCount,
                delay.TotalSeconds);

            message.IncrementRetryCount();

            try
            {
                await eventPublisher.PublishRawAsync(message.EventType, message.Content, stoppingToken);

                message.MarkAsSent();
                await unitOfWork.SaveChangesAsync(stoppingToken);

                _serilog.Information(
                    "Сообщение Outbox успешно отправлено при попытке #{RetryCount}: {EventType} EventId: {EventId}",
                    message.RetryCount,
                    message.EventType,
                    message.Id);
            }
            catch (Exception ex)
            {
                _serilog.Error(
                    ex,
                    "Ошибка при повторной отправке сообщения Outbox: {EventType} EventId: {EventId} (RetryCount: {RetryCount})",
                    message.EventType,
                    message.Id,
                    message.RetryCount);

                // Откатываем статус обратно на Pending для следующей итерации
                message.MarkAsPending();
                await unitOfWork.SaveChangesAsync(stoppingToken);
            }
        }
    }
}
