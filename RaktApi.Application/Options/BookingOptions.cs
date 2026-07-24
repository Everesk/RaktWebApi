using System.ComponentModel.DataAnnotations;

namespace RaktApi.Application.Options;

/// <summary>
/// Настройки правил создания бронирований.
/// </summary>
public sealed class BookingOptions
{
    /// <summary>
    /// Имя секции конфигурации.
    /// </summary>
    public const string SectionName = "Booking";

    /// <summary>
    /// Максимально допустимое количество активных бронирований одного пользователя.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int MaxActiveBookings { get; set; } = 10;
}
