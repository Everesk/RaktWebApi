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
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата и время обработки бронирования.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
}
