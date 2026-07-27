namespace Rakt.Contracts.Messaging;
/// <summary>Имена Kafka-топиков, используемые контрактами бронирований.</summary>
public static class BookingTopics
{
    /// <summary>Топик запросов создания брони.</summary>
    public const string Requested = "booking-requested";

    /// <summary>Топик запросов отмены брони.</summary>
    public const string Cancelled = "booking-cancelled";

    /// <summary>Топик с подтверждёнными бронями.</summary>
    public const string Confirmed = "booking-confirmed";

    /// <summary>Топик успешных резервирований мест сервисом событий.</summary>
    public const string SeatsReserved = "seats-reserved";

    /// <summary>Топик отказов в резервировании мест сервисом событий.</summary>
    public const string SeatsReservationRejected = "seats-reservation-rejected";
}
