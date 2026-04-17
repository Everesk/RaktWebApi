using System.ComponentModel.DataAnnotations;

namespace RaktWebApi.Models;

/// <summary>
/// Параметры запроса для фильтрации и пагинации событий.
/// </summary>
public class EventQueryDto
{
    /// <summary>
    /// Фильтр по заголовку события.
    /// Поиск выполняется по частичному совпадению без учета регистра.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Фильтр по времени начала события.
    /// Возвращаются события, начинающиеся не раньше указанного значения.
    /// </summary>
    public DateTime? From { get; set; }

    /// <summary>
    /// Фильтр по времени окончания события.
    /// Возвращаются события, заканчивающиеся не позже указанного значения.
    /// </summary>
    public DateTime? To { get; set; }

    /// <summary>
    /// Номер страницы (опционально).
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page должен быть больше 0.")]
    public int? Page { get; set; }

    /// <summary>
    /// Количество элементов на странице (опционально).
    /// </summary>
    [Range(1, 100, ErrorMessage = "PageSize должен быть в диапазоне от 1 до 100.")]
    public int? PageSize { get; set; }
}