using System.ComponentModel.DataAnnotations;

namespace Rakt.BookingsService.Infrastructure;

/// <summary>
/// Настройки фоновой обработки бронирований.
/// </summary>
public sealed class BookingProcessingOptions
{
    /// <summary>
    /// Имя секции настроек.
    /// </summary>
    public const string SectionName = "BookingProcessing";

    /// <summary>
    /// Число неудачных попыток, после которого бронь отклоняется.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "AttemptsLimit должен быть больше 0")]
    public int AttemptsLimit { get; init; } = 3;
}
