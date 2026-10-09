namespace TaskManagement.Api;

/// <summary>
/// Конфигурация ограничения частоты запросов.
/// </summary>
public class RateLimitConfig
{
    /// <summary>
    /// Период времени для ограничения.
    /// </summary>
    public TimeSpan Period { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Максимальное количество запросов за период.
    /// </summary>
    public int Limit { get; set; } = 100;
}
