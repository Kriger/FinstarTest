using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace TaskManagement.Infrastructure;

/// <summary>
/// Методы расширения для регистрации OpenTelemetry трейсинга.
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Регистрирует OpenTelemetry TracerProvider с instrumentation для ASP.NET Core и EF Core.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="serviceName">Имя сервиса для трейсов.</param>
    /// <param name="otlpEndpoint">OTLP endpoint (опционально, по умолчанию http://localhost:4317).</param>
    /// <returns>Коллекция сервисов.</returns>
    public static IServiceCollection AddTaskManagementTracing(
        this IServiceCollection services,
        string serviceName,
        string? otlpEndpoint = null)
    {
        services.AddOpenTelemetry()
            .WithTracing(builder => builder
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService(serviceName, serviceVersion: "1.0.0"))
                .AddSource(serviceName)
                .AddAspNetCoreInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint ?? "http://localhost:4317");
                }));

        return services;
    }
}
