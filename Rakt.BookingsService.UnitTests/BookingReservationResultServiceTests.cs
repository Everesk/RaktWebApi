using Microsoft.Extensions.Logging.Abstractions;
using Rakt.BookingsService.Application;
using Rakt.BookingsService.Domain;
using Rakt.Contracts.Messaging;
using Xunit;

namespace Rakt.BookingsService.UnitTests;

/// <summary>
/// Проверки обработки ответов сервиса событий на запрос брони.
/// </summary>
[Trait("Category", "Unit")]
public sealed class BookingReservationResultServiceTests
{
    /// <summary>
    /// Успешное резервирование подтверждает ожидающую бронь.
    /// </summary>
    [Fact]
    public async Task HandleAsync_ConfirmsPendingBookingWhenSeatIsReserved()
    {
        var repository = new InMemoryBookingRepository();
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid());
        await repository.AddAsync(booking);
        var service = CreateService(repository);
        var occurredAt = DateTimeOffset.UtcNow;

        await service.HandleAsync(new SeatsReserved(booking.Id, booking.EventId, occurredAt));

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Equal(occurredAt, booking.ProcessedAt);
        Assert.Equal(1, repository.SaveChangesCount);
    }

    /// <summary>
    /// Отказ в резервировании отклоняет ожидающую бронь.
    /// </summary>
    [Fact]
    public async Task HandleAsync_RejectsPendingBookingWhenReservationIsRejected()
    {
        var repository = new InMemoryBookingRepository();
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid());
        await repository.AddAsync(booking);
        var service = CreateService(repository);
        var occurredAt = DateTimeOffset.UtcNow;

        await service.HandleAsync(
            new SeatsReservationRejected(booking.Id, booking.EventId, "Нет свободных мест.", occurredAt));

        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.Equal(occurredAt, booking.ProcessedAt);
        Assert.Equal(1, repository.SaveChangesCount);
    }

    /// <summary>
    /// Поздний результат не должен менять отменённую бронь.
    /// </summary>
    [Fact]
    public async Task HandleAsync_DoesNotChangeCancelledBooking()
    {
        var repository = new InMemoryBookingRepository();
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid());
        booking.Cancel();
        await repository.AddAsync(booking);
        var service = CreateService(repository);

        await service.HandleAsync(new SeatsReserved(booking.Id, booking.EventId, DateTimeOffset.UtcNow));

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Equal(0, repository.SaveChangesCount);
    }

    /// <summary>
    /// Создаёт сервис с отключённым логированием для теста.
    /// </summary>
    private static BookingReservationResultService CreateService(InMemoryBookingRepository repository)
    {
        return new BookingReservationResultService(
            repository,
            NullLogger<BookingReservationResultService>.Instance);
    }
}
