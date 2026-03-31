using System.ComponentModel.DataAnnotations;

namespace RaktWebApi.Models;

/// <summary>
/// Базовый DTO события.
/// </summary>
public class EventBaseDto : IValidatableObject
{
    /// <summary>
    /// Заголовок события.
    /// </summary>
    [Required(ErrorMessage = "Заголовок обязателен")]
    [MaxLength(200, ErrorMessage = "Максимальная длина заголовка — 200 символов")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Описание события.
    /// </summary>
    [MaxLength(1000, ErrorMessage = "Максимальная длина описания — 1000 символов")]
    public string? Description { get; set; }

    /// <summary>
    /// Дата и время начала события.
    /// </summary>
    [Required(ErrorMessage = "Дата начала обязательна")]
    public DateTime? StartAt { get; set; }

    /// <summary>
    /// Дата и время окончания события.
    /// </summary>
    [Required(ErrorMessage = "Дата окончания обязательна")]
    public DateTime? EndAt { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartAt.HasValue && EndAt.HasValue && EndAt <= StartAt)
        {
            yield return new ValidationResult(
                "Дата окончания должна быть больше даты начала",
                [nameof(EndAt)]);
        }
    }
}

/// <summary>
/// DTO для создания события. Пока ничем не отличается от базового. 
/// </summary>
public class CreateEventDto : EventBaseDto
{
}

/// <summary>
/// DTO для обновления события. Пока ничем не отличается от базового (т.к. обновление по частям нам вроде не нужно).
/// </summary>
public class UpdateEventDto : EventBaseDto
{
}