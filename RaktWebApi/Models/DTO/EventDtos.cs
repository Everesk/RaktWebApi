using System.ComponentModel.DataAnnotations;

namespace RaktWebApi.Models.DTO;

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
    public DateTimeOffset? StartAt { get; set; }

    /// <summary>
    /// Дата и время окончания события.
    /// </summary>
    [Required(ErrorMessage = "Дата окончания обязательна")]
    public DateTimeOffset? EndAt { get; set; }


    /// <summary>
    /// Дополнительная валидация - проверка что время начала не позже времени конца
    /// </summary>
    /// <param name="validationContext"></param>
    /// <returns></returns>
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
/// DTO для создания события.
/// </summary>
public class CreateEventDto : EventBaseDto
{
    /// <summary>
    /// Общее количество мест на событии.
    /// </summary>
    [Required(ErrorMessage = "Количество мест обязательно")]
    [Range(1, int.MaxValue, ErrorMessage = "Количество мест должно быть больше нуля")]
    public int? TotalSeats { get; set; }
}

/// <summary>
/// DTO с информацией о событии.
/// </summary>
public class EventInfoDto : EventBaseDto
{
    /// <summary>
    /// Уникальный идентификатор события.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Общее количество мест на событии.
    /// </summary>
    public int TotalSeats { get; set; }

    /// <summary>
    /// Текущее количество свободных мест.
    /// </summary>
    public int AvailableSeats { get; set; }

    /// <summary>
    /// Признак того, что свободных мест на событии больше нет.
    /// </summary>
    public bool IsFull { get; set; }
}

/// <summary>
/// DTO для обновления события. Пока ничем не отличается от базового (т.к. обновление по частям нам вроде не нужно).
/// </summary>
public class UpdateEventDto : EventBaseDto
{
}
