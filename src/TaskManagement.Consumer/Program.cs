using Serilog;
using TaskManagement.Application.Configuration;
using TaskManagement.Consumer.Services;
using TaskManagement.Infrastructure;
using TaskManagement.Infrastructure.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .TaskManagementLogger("consumer")
    .CreateLogger();

// Configure RabbitMQ settings from appsettings.json
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));

// Register OpenTelemetry tracing
builder.Services.AddTaskManagementTracing("TaskManagement.Consumer");

// Add hosted service
builder.Services.AddHostedService<EventLoggerService>();

var host = builder.Build();

try
{
    Log.Information("Сервис Consumer запускается");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Работа хоста неожиданно завершена");
}
finally
{
    Log.CloseAndFlush();
}
