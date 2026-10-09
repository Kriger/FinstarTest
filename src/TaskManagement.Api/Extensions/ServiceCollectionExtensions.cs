using Microsoft.OpenApi.Models;
using TaskManagement.Api.Services;
using TaskManagement.Api.Swagger;
using TaskManagement.Application.Configuration;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Mapping;
using TaskManagement.Application.Services;
using TaskManagement.Infrastructure.Data;
using TaskManagement.Infrastructure.HealthChecks;
using TaskManagement.Infrastructure.Idempotency;
using TaskManagement.Infrastructure.Repositories;
using FluentValidation;

namespace TaskManagement.Api;

/// <summary>
/// Методы расширения для регистрации сервисов приложения.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует FluentValidation валидаторы.
    /// </summary>
    public static WebApplicationBuilder AddValidators(this WebApplicationBuilder builder)
    {
        builder.Services.AddValidatorsFromAssembly(
            typeof(TaskManagement.Application.Validation.CreateTaskValidator).Assembly);
        return builder;
    }

    /// <summary>
    /// Регистрирует Swagger с поддержкой версионирования API.
    /// </summary>
    public static WebApplicationBuilder AddSwagger(this WebApplicationBuilder builder)
    {
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Task Management API",
                Version = "v1",
                Description = "REST API для управления задачами с интеграцией RabbitMQ"
            });
            options.SchemaFilter<SwaggerExamples>();
        });
        return builder;
    }

    /// <summary>
    /// Регистрирует инфраструктуру (репозитории, сервисы, мапперы).
    /// </summary>
    public static WebApplicationBuilder AddInfrastructure(this WebApplicationBuilder builder)
    {
        // DbCommandExecutor с Circuit Breaker для всех операций БД
        builder.Services.AddScoped<DbCommandExecutor>();
        builder.Services.AddScoped<IUnitOfWork, Infrastructure.Data.UnitOfWork>();
        builder.Services.AddScoped<ITaskRepository, TaskRepository>();
        builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
        builder.Services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
        builder.Services.AddScoped<ITaskService, TaskService>();
        builder.Services.AddScoped<ITaskMapper, TaskMapper>();
        return builder;
    }

    /// <summary>
    /// Регистрирует health checks.
    /// </summary>
    public static WebApplicationBuilder AddHealthChecksConfig(this WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "db" })
            .AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: new[] { "mq" });
        return builder;
    }

    /// <summary>
    /// Регистрирует CORS политику.
    /// </summary>
    public static WebApplicationBuilder AddCorsPolicy(this WebApplicationBuilder builder)
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:3000", "http://localhost:5173" };

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("ApiPolicy", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .WithExposedHeaders("X-Total-Count", "X-Page-Number");
            });
        });
        return builder;
    }

    /// <summary>
    /// Регистрирует кэширование (включая Distributed Cache для идемпотентности).
    /// </summary>
    public static WebApplicationBuilder AddCaching(this WebApplicationBuilder builder)
    {
        // Use in-memory cache as fallback when Redis is not available
        builder.Services.AddDistributedMemoryCache();
        builder.Services.Configure<CachingSettings>(builder.Configuration.GetSection("Caching"));
        return builder;
    }

    /// <summary>
    /// Регистрирует контекст пользователя.
    /// </summary>
    public static WebApplicationBuilder AddUserContext(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IUserContext, HttpContextUserContext>();
        return builder;
    }
}
