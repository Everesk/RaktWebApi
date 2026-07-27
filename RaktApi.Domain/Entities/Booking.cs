using RaktApi.Domain.Exceptions;

namespace RaktApi.Domain;

/// <summary>
/// Представляет бронирование события.
/// </summary>
public class Booking
{
    /// <summary>
    /// Навигационная ссылка на связанное событие.
    /// </summary>
    public Event Event { get; private set; } = null!;

    /// <summary>
    /// Навигационная ссылка на пользователя, создавшего бронирование.
    /// </summary>
    public User User { get; private set; } = null!;

    /// <summary>
    /// Уникальный идентификатор бронирования.
    /// </summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>
    /// Идентификатор события, к которому относится бронь.
    /// </summary>
    public Guid EventId { get; private set; }

    /// <summary>
    /// Идентификатор пользователя, создавшего бронирование.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Текущий статус бронирования.
    /// </summary>
    public BookingStatus Status { get; private set; }

    /// <summary>
    /// Дата и время создания бронирования.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Дата и время обработки бронирования.
    /// </summary>
    public DateTimeOffset? ProcessedAt { get; private set; }

    /// <summary>
    /// Создает новое бронирование для указанного события.
    /// </summary>
    internal Booking(Guid eventId, Guid userId)
    {
        EventId = eventId;
        UserId = userId;
        Status = BookingStatus.Pending;
    }

    /// <summary>
    /// Создаёт новое бронирование для указанного события.
    /// </summary>
    /// <param name="eventId">Идентификатор события.</param>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <returns>Новое бронирование в статусе ожидания обработки.</returns>
    public static Booking Create(Guid eventId, Guid userId) => new(eventId, userId);

    /// <summary>
    /// Приватный конструктор для материализации сущности.
    /// </summary>
    private Booking()
    {
    }

    /// <summary>
    /// Обновляет статус бронирования и фиксирует время обработки.
    /// </summary>
    public void Confirm(DateTimeOffset processedAt)
    {
        Status = BookingStatus.Confirmed;
        ProcessedAt = processedAt;
    }

    /// <summary>
    /// Отклоняет бронирование и фиксирует время обработки.
    /// </summary>
    public void Reject(DateTimeOffset processedAt)
    {
        Status = BookingStatus.Rejected;
        ProcessedAt = processedAt;
    }

    /// <summary>
    /// Отменяет бронирование.
    /// </summary>
    /// <exception cref="BookingAlreadyCancelledException">Бронирование уже было отменено.</exception>
    public void Cancel()
    {
        if (Status == BookingStatus.Cancelled)
        {
            throw new BookingAlreadyCancelledException("Бронирование уже отменено.");
        }

        Status = BookingStatus.Cancelled;
    }
}
