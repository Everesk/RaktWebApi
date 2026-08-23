namespace Rakt.BookingsService.Domain;
/// <summary>Состояние обработки бронирования.</summary>
public enum BookingStatus
{
    Pending,
    Confirmed,
    Rejected,
    Cancelled
}
