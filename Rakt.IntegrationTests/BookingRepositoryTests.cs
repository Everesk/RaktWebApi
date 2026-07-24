using RaktApi.Domain;
using RaktApi.Infrastructure.Repositories;

namespace Rakt.IntegrationTests;

/// <summary>
/// Интеграционные тесты репозитория бронирований на PostgreSQL в Testcontainers.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
[Trait("Category", "Integration")]
public sealed class BookingRepositoryTests(PostgreSqlFixture fixture) : PostgreSqlTestBase(fixture)
{
    /// <summary>Проверяет сохранение нового бронирования.</summary>
    [Fact]
    public async Task AddAsync_PersistsBooking()
    {
        await using var repositories = CreateRepositories();
        var eventEntity = await AddEventAsync(repositories, totalSeats: 2);

        var booking = await AddBookingAsync(repositories, eventEntity.Id);
        var storedBooking = await repositories.Bookings.GetByIdAsync(booking.Id);

        Assert.Equal(eventEntity.Id, booking.EventId);
        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.NotNull(storedBooking);
    }

    /// <summary>Проверяет получение существующей брони и отсутствие результата для неизвестного идентификатора.</summary>
    [Fact]
    public async Task GetByIdAsync_ReturnsBookingOrNull()
    {
        await using var repositories = CreateRepositories();
        var eventEntity = await AddEventAsync(repositories, totalSeats: 1);
        var booking = await AddBookingAsync(repositories, eventEntity.Id);

        var storedBooking = await repositories.Bookings.GetByIdAsync(booking.Id);
        var missingBooking = await repositories.Bookings.GetByIdAsync(Guid.NewGuid());

        Assert.NotNull(storedBooking);
        Assert.Equal(booking.Id, storedBooking.Id);
        Assert.Null(missingBooking);
    }

    /// <summary>Проверяет выборку броней для существующего события.</summary>
    [Fact]
    public async Task GetByEventIdAsync_ReturnsOnlyBookingsForRequestedEvent()
    {
        await using var repositories = CreateRepositories();
        var firstEvent = await AddEventAsync(repositories, totalSeats: 2, title: "Первое");
        var secondEvent = await AddEventAsync(repositories, totalSeats: 1, title: "Второе");
        var firstBooking = await AddBookingAsync(repositories, firstEvent.Id);
        await AddBookingAsync(repositories, secondEvent.Id);

        var bookings = await repositories.Bookings.GetByEventIdAsync(firstEvent.Id);

        Assert.Single(bookings);
        Assert.Equal(firstBooking.Id, bookings.Single().Id);
    }

    /// <summary>Проверяет пустой результат при выборке броней отсутствующего события.</summary>
    [Fact]
    public async Task GetByEventIdAsync_WhenEventDoesNotExist_ReturnsEmptyCollection()
    {
        await using var repositories = CreateRepositories();

        var bookings = await repositories.Bookings.GetByEventIdAsync(Guid.NewGuid());

        Assert.Empty(bookings);
    }

    /// <summary>Проверяет выборку идентификаторов ожидающих обработки броней.</summary>
    [Fact]
    public async Task GetPendingIdsAsync_ReturnsOnlyPendingBookings()
    {
        await using var repositories = CreateRepositories();
        var eventEntity = await AddEventAsync(repositories, totalSeats: 3);
        var pendingBooking = await AddBookingAsync(repositories, eventEntity.Id);

        var pendingIds = await repositories.Bookings.GetPendingIdsAsync();

        Assert.Equal([pendingBooking.Id], pendingIds);
    }

    /// <summary>Проверяет сохранение изменений отслеживаемого бронирования.</summary>
    [Fact]
    public async Task GetForUpdateAsync_AndUpdateAsync_PersistsChanges()
    {
        await using var repositories = CreateRepositories();
        var eventEntity = await AddEventAsync(repositories, totalSeats: 1);
        var booking = await AddBookingAsync(repositories, eventEntity.Id);

        var trackedBooking = await repositories.Bookings.GetForUpdateAsync(booking.Id);
        Assert.NotNull(trackedBooking);
        trackedBooking.Confirm(DateTimeOffset.UtcNow);
        await repositories.Bookings.UpdateAsync();

        var storedBooking = await repositories.Bookings.GetByIdAsync(booking.Id);

        Assert.Equal(BookingStatus.Confirmed, storedBooking!.Status);
        Assert.NotNull(storedBooking.ProcessedAt);
    }

    /// <summary>Создаёт и сохраняет событие для сценария бронирования.</summary>
    private static async Task<Event> AddEventAsync(RepositoryScope repositories, int totalSeats, string title = "Событие")
    {
        var eventEntity = Event.Create(
            title,
            null,
            new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 1, 11, 0, 0, TimeSpan.Zero),
            totalSeats);
        await repositories.Events.AddAsync(eventEntity);
        return eventEntity;
    }

    /// <summary>Создаёт и сохраняет бронирование для подготовки сценария репозитория.</summary>
    private static async Task<Booking> AddBookingAsync(RepositoryScope repositories, Guid eventId)
    {
        var user = User.Create($"user-{Guid.NewGuid():N}", "password-hash");
        await repositories.Users.AddAsync(user);

        var booking = Booking.Create(eventId, user.Id);
        await repositories.Bookings.AddAsync(booking);
        return booking;
    }
}
