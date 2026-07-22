namespace RaktApi.Application.Ports;

/// <summary>
/// Описывает результат попытки подтвердить бронирование.
/// </summary>
public enum BookingConfirmationResult
{
    /// <summary>Бронирование не найдено.</summary>
    NotFound,

    /// <summary>Связанное событие не найдено, бронирование отклонено.</summary>
    EventNotFound,

    /// <summary>Бронирование успешно подтверждено.</summary>
    Confirmed
}
