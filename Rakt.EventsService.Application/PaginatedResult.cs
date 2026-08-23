namespace Rakt.EventsService.Application;
/// <summary>Результат постраничной выборки.</summary>
public sealed class PaginatedResult<T>
{
    /// <summary>Общее количество элементов.</summary>
    public int TotalCount { get; init; }
    /// <summary>Элементы страницы.</summary>
    public IReadOnlyCollection<T> Items { get; init; } = [];
    /// <summary>Номер страницы.</summary>
    public int Page { get; init; }
    /// <summary>Размер страницы.</summary>
    public int PageSize { get; init; }
    /// <summary>Количество элементов на странице.</summary>
    public int CurrentCount { get; init; }
}
