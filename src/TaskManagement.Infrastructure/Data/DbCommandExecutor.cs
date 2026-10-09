using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Serilog;

namespace TaskManagement.Infrastructure.Data;

/// <summary>
/// Исключение, выбрасываемое при срабатывании Circuit Breaker для БД.
/// </summary>
public class DatabaseCircuitBreakerOpenException : Exception
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="DatabaseCircuitBreakerOpenException"/>.
    /// </summary>
    /// <param name="message">Сообщение об ошибке.</param>
    public DatabaseCircuitBreakerOpenException(string message) : base(message)
    {
    }
}

/// <summary>
/// Исключение, выбрасываемое при превышении таймаута БД.
/// </summary>
public class DatabaseTimeoutException : Exception
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="DatabaseTimeoutException"/>.
    /// </summary>
    /// <param name="message">Сообщение об ошибке.</param>
    public DatabaseTimeoutException(string message) : base(message)
    {
    }
}

/// <summary>
/// Исполнитель команд БД с Circuit Breaker и Retry.
/// Оберчивает EF Core операции политикой устойчивости.
/// </summary>
public class DbCommandExecutor
{
    private readonly AppDbContext _context;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly AsyncCircuitBreakerPolicy _circuitBreakerPolicy;
    private readonly AsyncTimeoutPolicy _timeoutPolicy;
    private static readonly Serilog.ILogger _logger = Log.ForContext<DbCommandExecutor>();

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="DbCommandExecutor"/>.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    public DbCommandExecutor(AppDbContext context)
    {
        _context = context;

        // Retry: до 3 попыток с экспоненциальным backoff (1с, 2с, 4с)
        _retryPolicy = Policy
            .Handle<DbUpdateException>()
            .Or<TimeoutException>()
            .Or<OperationCanceledException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

        // Circuit Breaker: открывает цепь после 5 последовательных ошибок на 30 секунд
        _circuitBreakerPolicy = Policy
            .Handle<DbUpdateException>()
            .Or<TimeoutException>()
            .Or<OperationCanceledException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30));

        // Timeout: 10 секунд на операцию
        _timeoutPolicy = Policy.TimeoutAsync(TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// Выполняет операцию над БД с применением всех политик.
    /// </summary>
    /// <param name="operation">Асинхронная операция.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат операции.</returns>
    public async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        var policyWrap = _circuitBreakerPolicy.WrapAsync(_retryPolicy).WrapAsync(_timeoutPolicy);

        try
        {
            await policyWrap.ExecuteAsync(operation);
        }
        catch (BrokenCircuitException)
        {
            throw new DatabaseCircuitBreakerOpenException(
                "База данных временно недоступна. Пожалуйста, попробуйте позже.");
        }
        catch (TimeoutRejectedException)
        {
            throw new DatabaseTimeoutException(
                "Превышено время ожидания операции с базой данных.");
        }
    }

    /// <summary>
    /// Выполняет операцию над БД с применением всех политик и возвращает результат.
    /// </summary>
    /// <typeparam name="T">Тип результата операции.</typeparam>
    /// <param name="operation">Асинхронная операция.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат операции.</returns>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        var policyWrap = _circuitBreakerPolicy.WrapAsync(_retryPolicy).WrapAsync(_timeoutPolicy);

        try
        {
            return await policyWrap.ExecuteAsync(operation);
        }
        catch (BrokenCircuitException)
        {
            throw new DatabaseCircuitBreakerOpenException(
                "База данных временно недоступна. Пожалуйста, попробуйте позже.");
        }
        catch (TimeoutRejectedException)
        {
            throw new DatabaseTimeoutException(
                "Превышено время ожидания операции с базой данных.");
        }
    }

    /// <summary>
    /// Сохраняет изменения в БД с применением политик.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Количество сохранённых изменений.</returns>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await ExecuteAsync(() => _context.SaveChangesAsync(cancellationToken), cancellationToken);
            return 0;
        }
        catch (DatabaseCircuitBreakerOpenException)
        {
            throw;
        }
        catch (DatabaseTimeoutException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Ошибка при сохранении изменений в БД");
            throw;
        }
    }
}
