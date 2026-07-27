using System.ComponentModel.DataAnnotations;
namespace Rakt.EventsService.Application;
/// <summary>Параметры фильтрации и пагинации событий.</summary>
public sealed class EventQueryDto : IValidatableObject
{
    /// <summary>Фильтр по заголовку.</summary>
    public string? Title { get; set; }
    /// <summary>Нижняя граница времени начала.</summary>
    public DateTimeOffset? From { get; set; }
    /// <summary>Верхняя граница времени окончания.</summary>
    public DateTimeOffset? To { get; set; }
    /// <summary>Номер страницы.</summary>
    public int? Page { get; set; }
    /// <summary>Размер страницы.</summary>
    public int? PageSize { get; set; }
    /// <summary>Проверяет порядок границ диапазона.</summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (From.HasValue && To.HasValue && From > To)
        {
            yield return new ValidationResult("Дата начала не может быть позже даты окончания", [nameof(From), nameof(To)]);
        }
    }
}
