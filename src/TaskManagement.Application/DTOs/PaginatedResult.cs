namespace TaskManagement.Application.DTOs;

/// <summary>
/// DTO для результатов с пагинацией.
/// </summary>
public class PaginatedResult<T>
{
    /// <summary>
    /// Текущий номер страницы (начиная с 1).
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// Размер страницы.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Общее количество элементов.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Общее количество страниц.
    /// </summary>
    public int TotalPages { get; init; }

    /// <summary>
    /// Наличие следующей страницы.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Наличие предыдущей страницы.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Элементы на текущей странице.
    /// </summary>
    public List<T> Items { get; init; } = new();
}
