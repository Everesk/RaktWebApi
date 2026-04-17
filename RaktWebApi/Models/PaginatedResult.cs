namespace RaktWebApi.Models;

/// <summary>
/// Результат постраничного вывода данных.
/// </summary>
/// <typeparam name="T">Тип элементов выборки.</typeparam>
public class PaginatedResult<T>
{
    /// <summary>
    /// Общее количество элементов, удовлетворяющих условиям запроса.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Элементы текущей страницы.
    /// </summary>
    public IReadOnlyCollection<T> Items { get; set; } = [];

    /// <summary>
    /// Номер текущей страницы.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Запрошенный размер страницы.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Количество элементов на текущей странице.
    /// </summary>
    public int CurrentCount { get; set; }
}