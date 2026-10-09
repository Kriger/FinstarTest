using Microsoft.EntityFrameworkCore;
using Serilog;
using TaskManagement.Api;
using TaskManagement.Api.Filters;
using TaskManagement.Application.Configuration;
using TaskManagement.Application.DTOs;
using TaskManagement.Infrastructure;
using TaskManagement.Infrastructure.Data;
using TaskManagement.Infrastructure.Middleware;
using TaskManagement.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Регистрация трассировки OpenTelemetry
builder.Services.AddTaskManagementTracing(
    "TaskManagement.Api",
    builder.Configuration.GetValue<string>("Otlp:Endpoint")
    ?? "http://localhost:4317");

// Регистрация валидаторов
builder.AddValidators();

// Регистрация инфраструктуры
builder.AddInfrastructure();

// Регистрация проверок работоспособности
builder.AddHealthChecksConfig();

// Регистрация CORS
builder.AddCorsPolicy();

// Регистрация кэширования
builder.AddCaching();

// Регистрация контекста пользователя
builder.AddUserContext();

// Регистрация Swagger
builder.AddSwagger();

// Настройка Serilog с логированием HTTP-запросов
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "TaskManagement.Api")
    .WriteTo.Console()
    .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day)
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)
    .CreateLogger();

builder.Host.UseSerilog();

// Добавление сервисов в контейнер
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Регистрация версионирования API
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = Microsoft.AspNetCore.Mvc.Versioning.ApiVersionReader.Combine(
        new Microsoft.AspNetCore.Mvc.Versioning.UrlSegmentApiVersionReader(),
        new Microsoft.AspNetCore.Mvc.Versioning.HeaderApiVersionReader("X-Api-Version"),
        new Microsoft.AspNetCore.Mvc.Versioning.QueryStringApiVersionReader("v"));
}).AddMvc();

// Настройка DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
    .EnableSensitiveDataLogging(false));

// Настройка RabbitMQ
var rabbitMqConnectionString = builder.Configuration.GetConnectionString("RabbitMq")
    ?? throw new InvalidOperationException(
        "RabbitMQ connection string is not configured. Set 'ConnectionStrings:RabbitMq' in appsettings.");
builder.Services.AddRabbitMq(rabbitMqConnectionString);

// Регистрация фильтров валидации
builder.Services.AddScoped<ValidationActionFilter<CreateTaskDto>>();
builder.Services.AddScoped<ValidationActionFilter<UpdateTaskDto>>();

// Регистрация фоновой службы Outbox
builder.Services.AddHostedService<OutboxBackgroundService>();

// Проверка конфигурации при запуске
builder.Services.PostConfigure<RabbitMqSettings>(settings =>
{
    if (string.IsNullOrEmpty(settings.HostName) && string.IsNullOrEmpty(settings.ConnectionString))
    {
        throw new InvalidOperationException("RabbitMQ connection is not configured.");
    }
});

// Настройка ограничения частоты запросов
var rateLimitConfig = builder.Configuration.GetSection("RateLimiting");
var period = TimeSpan.Parse(rateLimitConfig["Period"] ?? "00:01:00");
var limit = int.Parse(rateLimitConfig["Limit"] ?? "100");
builder.Services.AddSingleton(new RateLimitConfig
{
    Period = period,
    Limit = limit
});

var app = builder.Build();

// Настройка конвейера HTTP-запросов

// Middleware обработки исключений — должен быть первым
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Middleware идемпотентности — предотвращает дублирование POST-запросов
app.UseMiddleware<IdempotencyMiddleware>();

// Middleware ограничения частоты запросов
app.UseMiddleware<RateLimitingMiddleware>();

// Middleware логирования запросов
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    // Настройка Swagger с версионированием API
    app.UseSwagger(options =>
    {
        options.RouteTemplate = "swagger/{documentName}/swagger.json";
    });

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Task Management API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseCors("ApiPolicy");
app.UseHttpsRedirection();

// Применение миграций базы данных
try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
    Log.Information("Database migrations applied successfully");
}
catch (Exception ex)
{
    Log.Warning(ex, "Failed to apply database migrations");
}

app.MapControllers();

// Точка проверки работоспособности
app.MapHealthChecks("/health");

try
{
    Log.Information("Starting TaskManagement.Api host");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
