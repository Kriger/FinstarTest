namespace TaskManagement.Application.Interfaces;

/// <summary>
/// Интерфейс для управления единицей работы.
/// Обеспечивает атомарное сохранение изменений во всех репозиториях.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Сохраняет все изменения во всех репозиториях в одной транзакции.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Количество сохранённых изменений.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
