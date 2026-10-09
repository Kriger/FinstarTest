using Serilog;
using Serilog.Events;

namespace TaskManagement.Infrastructure.Logging;

/// <summary>
/// Расширения для настройки логгирования TaskManagement-сервисов.
/// </summary>
public static class LoggerConfigurationExtensions
{
    /// <summary>
    /// Настраивает логгер для сервисов TaskManagement с указанным префиксом имени файла логов.
    /// </summary>
    /// <param name="loggerConfiguration">Конфигуратор Serilog.</param>
    /// <param name="logFilePrefix">Префикс имени файла логов (например, "producer" или "consumer").</param>
    /// <returns>Конфигуратор логгера.</returns>
    public static LoggerConfiguration TaskManagementLogger(
        this LoggerConfiguration loggerConfiguration,
        string logFilePrefix)
    {
        return loggerConfiguration
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File($"logs/{logFilePrefix}-.log", rollingInterval: RollingInterval.Day)
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Information);
    }
}
