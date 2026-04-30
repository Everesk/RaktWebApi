using System.ComponentModel.DataAnnotations;

namespace RaktWebApi.Models.DTO;

/// <summary>
/// Параметры запроса для фильтрации и пагинации событий.
/// </summary>
public class EventQueryDto : IValidatableObject
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
    public DateTimeOffset? From { get; set; }

    /// <summary>
    /// Фильтр по времени окончания события.
    /// Возвращаются события, заканчивающиеся не позже указанного значения.
    /// </summary>
    public DateTimeOffset? To { get; set; }

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

    /// <summary>
    /// Дополнительная валидация - проверка, что диапазон дат не перевернут.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (From.HasValue && To.HasValue && From > To)
        {
            yield return new ValidationResult(
                "Дата начала не может быть позже даты окончания",
                [nameof(From), nameof(To)]);
        }
    }
}
