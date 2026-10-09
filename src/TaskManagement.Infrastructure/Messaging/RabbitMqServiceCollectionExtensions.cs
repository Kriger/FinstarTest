using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using TaskManagement.Application.Interfaces;
using TaskManagement.Infrastructure.Messaging;

namespace TaskManagement.Infrastructure;

/// <summary>
/// Методы расширения для регистрации RabbitMQ.
/// Использует пул соединений для оптимизации производительности.
/// </summary>
public static class RabbitMqServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует RabbitMQ-компоненты в DI-контейнере.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="connectionString">Строка подключения RabbitMQ.</param>
    /// <returns>Коллекция сервисов.</returns>
    public static IServiceCollection AddRabbitMq(
        this IServiceCollection services,
        string connectionString)
    {
        // Singleton factory — создаётся один раз при запуске приложения
        services.AddSingleton(sp =>
        {
            var logger = sp.GetService<ILogger<RabbitMqConnectionFactory>>();
            return new RabbitMqConnectionFactory(connectionString, logger!);
        });

        // Singleton connection — переиспользуется всеми запросами
        services.AddSingleton(sp =>
        {
            var connectionFactory = sp.GetRequiredService<RabbitMqConnectionFactory>();
            var connection = connectionFactory.CreateConnection();

            // Подписка на событие восстановления соединения
            connection.ConnectionShutdown += (sender, args) =>
            {
                var logger = sp.GetService<ILogger<IConnection>>();
                logger?.LogWarning("RabbitMQ connection shut down by {Initiator}", args.Initiator);
            };

            return connection;
        });

        // Scoped event publisher — создаёт новый канал для каждой публикации
        // через IConnection, что гарантирует корректную обработку сбоев соединения
        services.AddScoped<IEventPublisher>(sp =>
        {
            var connection = sp.GetRequiredService<IConnection>();
            var connectionFactory = sp.GetRequiredService<RabbitMqConnectionFactory>();
            var logger = sp.GetService<ILogger<RabbitMqEventPublisher>>();
            return new RabbitMqEventPublisher(connection, connectionFactory, logger!);
        });

        return services;
    }
}
