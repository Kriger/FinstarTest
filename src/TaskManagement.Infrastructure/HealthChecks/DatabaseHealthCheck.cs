using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using RabbitMQ.Client;

namespace TaskManagement.Infrastructure.HealthChecks;

/// <summary>
/// Проверка доступности PostgreSQL базы данных.
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly string _connectionString;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="DatabaseHealthCheck"/>.
    /// </summary>
    /// <param name="configuration">Конфигурация приложения.</param>
    public DatabaseHealthCheck(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection not configured.");
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return HealthCheckResult.Healthy("Подключение к базе данных успешно.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Подключение к базе данных не удалось.", ex);
        }
    }
}

/// <summary>
/// Проверка доступности RabbitMQ.
/// </summary>
public class RabbitMqHealthCheck : IHealthCheck
{
    private readonly string _connectionString;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="RabbitMqHealthCheck"/>.
    /// </summary>
    /// <param name="configuration">Конфигурация приложения.</param>
    public RabbitMqHealthCheck(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("RabbitMq")
            ?? throw new InvalidOperationException("RabbitMq connection string not configured.");
    }

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = new ConnectionFactory()
            {
                Uri = new Uri(_connectionString),
                RequestedHeartbeat = TimeSpan.FromSeconds(10)
            };

            using var connection = factory.CreateConnection();
            return Task.FromResult(
                HealthCheckResult.Healthy("Подключение к RabbitMQ успешно."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy("Подключение к RabbitMQ не удалось.", ex));
        }
    }
}
