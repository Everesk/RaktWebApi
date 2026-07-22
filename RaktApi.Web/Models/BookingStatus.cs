namespace RaktWebApi.Models;

/// <summary>
/// Статус бронирования.
/// </summary>
public enum BookingStatus
{
    /// <summary>
    /// Бронь создана и ожидает обработки.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Бронь подтверждена.
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// Бронь отклонена.
    /// </summary>
    Rejected = 2
}
