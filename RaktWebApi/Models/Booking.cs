namespace RaktWebApi.Models;

/// <summary>
/// Представляет бронирование события.
/// </summary>
public class Booking
{
    /// <summary>
    /// Уникальный идентификатор бронирования.
    /// </summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>
    /// Идентификатор события, к которому относится бронь.
    /// </summary>
    public Guid EventId { get; private set; }

    /// <summary>
    /// Текущий статус бронирования.
    /// </summary>
    public BookingStatus Status { get; private set; } = BookingStatus.Pending;

    /// <summary>
    /// Дата и время создания бронирования.
    /// </summary>
    public DateTime CreatedAt { get; private set; } = DateTime.Now;

    /// <summary>
    /// Дата и время обработки бронирования.
    /// </summary>
    public DateTime? ProcessedAt { get; private set; }

    /// <summary>
    /// Создает новое бронирование для указанного события.
    /// </summary>
    internal Booking(Guid eventId)
    {
        EventId = eventId;
        Status = BookingStatus.Pending;
        CreatedAt = DateTime.Now;
    }

    /// <summary>
    /// Обновляет статус бронирования и фиксирует время обработки.
    /// </summary>
    internal void Confirm(DateTime processedAt)
    {
        Status = BookingStatus.Confirmed;
        ProcessedAt = processedAt;
    }

    /// <summary>
    /// Отклоняет бронирование и фиксирует время обработки.
    /// </summary>
    internal void Reject(DateTime processedAt)
    {
        Status = BookingStatus.Rejected;
        ProcessedAt = processedAt;
    }
}
