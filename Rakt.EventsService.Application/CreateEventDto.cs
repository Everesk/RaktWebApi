using System.ComponentModel.DataAnnotations;
namespace Rakt.EventsService.Application;
/// <summary>Данные создания события.</summary>
public sealed class CreateEventDto : IValidatableObject
{
    /// <summary>Заголовок события.</summary>
    [Required(ErrorMessage = "Заголовок обязателен")]
    [MaxLength(200, ErrorMessage = "Максимальная длина заголовка — 200 символов")]
    public string Title { get; set; } = string.Empty;
    /// <summary>Описание события.</summary>
    public string? Description { get; set; }
    /// <summary>Время начала.</summary>
    [Required(ErrorMessage = "Дата начала обязательна")]
    public DateTimeOffset? StartAt { get; set; }
    /// <summary>Время окончания.</summary>
    [Required(ErrorMessage = "Дата окончания обязательна")]
    public DateTimeOffset? EndAt { get; set; }
    /// <summary>Количество мест.</summary>
    [Required(ErrorMessage = "Количество мест обязательно")]
    [Range(1, int.MaxValue, ErrorMessage = "Количество мест должно быть больше нуля")]
    public int? TotalSeats { get; set; }
    /// <summary>Проверяет порядок временных границ.</summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartAt.HasValue && EndAt.HasValue && EndAt <= StartAt)
        {
            yield return new ValidationResult("Дата окончания должна быть больше даты начала", [nameof(EndAt)]);
        }
    }
}
