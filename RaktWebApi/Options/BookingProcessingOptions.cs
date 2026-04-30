using System.ComponentModel.DataAnnotations;

namespace RaktWebApi.Options;

/// <summary>
/// Настройки фоновой обработки бронирований.
/// </summary>
public sealed class BookingProcessingOptions
{
    /// <summary>
    /// Имя секции конфигурации.
    /// </summary>
    public const string SectionName = "BookingProcessing";

    /// <summary>
    /// Максимальное число неуспешных попыток обработки, после которого бронь отклоняется.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "AttemptsLimit должен быть больше 0")]
    public int AttemptsLimit { get; set; } = 3;
}
