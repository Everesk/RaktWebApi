using System.ComponentModel.DataAnnotations;

namespace RaktWebApi.Models;

/// <summary>
/// DTO для создания события.
/// </summary>
public class CreateEventDto : IValidatableObject
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
    public DateTime StartAt { get; set; }

    /// <summary>
    /// Дата и время окончания события.
    /// </summary>
    [Required(ErrorMessage = "Дата окончания обязательна")]
    public DateTime EndAt { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndAt <= StartAt)
        {
            yield return new ValidationResult(
                "Дата окончания должна быть больше даты начала",
                [nameof(EndAt)]);
        }
    }
}

/// <summary>
/// DTO для обновления события.
/// </summary>
public class UpdateEventDto : IValidatableObject
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
    public DateTime StartAt { get; set; }

    /// <summary>
    /// Дата и время окончания события.
    /// </summary>
    [Required(ErrorMessage = "Дата окончания обязательна")]
    public DateTime EndAt { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndAt <= StartAt)
        {
            yield return new ValidationResult(
                "Дата окончания должна быть больше даты начала",
                [nameof(EndAt)]);
        }
    }
}