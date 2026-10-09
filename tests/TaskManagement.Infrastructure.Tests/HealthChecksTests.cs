using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TaskManagement.Infrastructure.HealthChecks;
using Xunit;

namespace TaskManagement.Infrastructure.Tests;

/// <summary>
/// Модульные тесты для проверок состояния инфраструктуры.
/// </summary>
public class HealthChecksTests
{
    [Fact]
    public async Task DatabaseHealthCheck_ShouldReturnResult_WhenConnectionStringProvided()
    {
        // Arrange
        var healthCheck = new DatabaseHealthCheck("Host=localhost;Port=5432;Database=test;Username=test;Password=test");

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task RabbitMqHealthCheck_ShouldReturnResult_WhenConnectionStringProvided()
    {
        // Arrange
        var healthCheck = new RabbitMqHealthCheck("amqp://guest:guest@localhost:5672");

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }
}
