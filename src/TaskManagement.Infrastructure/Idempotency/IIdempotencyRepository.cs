namespace TaskManagement.Infrastructure.Idempotency;

/// <summary>
/// Интерфейс репозитория для хранения записей идемпотентности.
/// </summary>
public interface IIdempotencyRepository
{
    /// <summary>
    /// Получает запись по ключу идемпотентности.
    /// </summary>
    /// <param name="key">Ключ идемпотентности.</param>
    /// <returns>Запись, если найдена; иначе null.</returns>
    Task<IdempotencyRecord?> GetByKeyAsync(string key);

    /// <summary>
    /// Сохраняет новую запись идемпотентности.
    /// </summary>
    /// <param name="record">Запись для сохранения.</param>
    /// <returns>Task.</returns>
    Task SaveAsync(IdempotencyRecord record);

    /// <summary>
    /// Обновляет существующую запись (помечает как обработанную).
    /// </summary>
    /// <param name="record">Запись для обновления.</param>
    /// <returns>Task.</returns>
    Task UpdateAsync(IdempotencyRecord record);

    /// <summary>
    /// Удаляет устаревшие записи идемпотентности.
    /// </summary>
    /// <returns>Количество удалённых записей.</returns>
    Task<int> DeleteExpiredAsync();
}
