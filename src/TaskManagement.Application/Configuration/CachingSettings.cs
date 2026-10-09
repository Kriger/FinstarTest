namespace TaskManagement.Application.Configuration;

/// <summary>
/// Настройки кэширования.
/// </summary>
public class CachingSettings
{
    /// <summary>
    /// Время жизни кэша в секундах.
    /// </summary>
    public int DefaultCacheDurationSeconds { get; set; } = 30;
}
