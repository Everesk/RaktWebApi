namespace RaktWebApi.Models.DTO;

/// <summary>
/// DTO для бронирования.
/// </summary>
public class BookingDto
{
    /// <summary>
    /// Уникальный идентификатор бронирования.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор события, к которому относится бронь.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Текущий статус бронирования.
    /// </summary>
    public BookingStatus Status { get; set; }

    /// <summary>
    /// Дата и время создания бронирования.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Дата и время обработки бронирования.
    /// </summary>
    public DateTimeOffset? ProcessedAt { get; set; }
}
