using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Rakt.E2ETests;

/// <summary>
/// Проверяет сквозной жизненный цикл брони через Users, Events, Bookings и Kafka.
/// </summary>
[Trait("Category", "E2E")]
public sealed class BookingLifecycleE2eTests(MicroservicesE2eFixture fixture)
    : IClassFixture<MicroservicesE2eFixture>
{
    /// <summary>
    /// Создаёт событие, подтверждает бронь, уменьшает места и возвращает место при отмене.
    /// </summary>
    [Fact]
    public async Task BookingLifecycle_ConfirmsBookingReservesAndReleasesEventSeat()
    {
        var administratorToken = await RegisterAndLoginAsync(
            fixture.UsersBaseAddress,
            "e2e-admin",
            "Password123!",
            role: 1);
        var userToken = await RegisterAndLoginAsync(
            fixture.UsersBaseAddress,
            "e2e-user",
            "Password123!",
            role: 0);
        var eventId = await CreateEventAsync(administratorToken);

        var bookingId = await CreateBookingAsync(eventId, userToken);

        await WaitUntilAsync(
            async () =>
            {
                using var client = CreateAuthorizedClient(fixture.BookingsBaseAddress, userToken);
                using var response = await client.GetAsync($"bookings/{bookingId}");
                var booking = await response.Content.ReadFromJsonAsync<JsonElement>();

                return response.IsSuccessStatusCode
                    && booking.GetProperty("status").GetInt32() == 1;
            },
            "Бронь не была подтверждена в ожидаемый срок.");

        await WaitUntilAsync(
            async () => await GetAvailableSeatsAsync(eventId) == 1,
            "Сервис событий не уменьшил число свободных мест.");

        using (var client = CreateAuthorizedClient(fixture.BookingsBaseAddress, userToken))
        {
            using var response = await client.DeleteAsync($"bookings/{bookingId}");
            response.EnsureSuccessStatusCode();
        }

        await WaitUntilAsync(
            async () => await GetAvailableSeatsAsync(eventId) == 2,
            "Сервис событий не вернул место после отмены брони.");
    }

    /// <summary>
    /// Регистрирует пользователя и получает его JWT-токен через публичный API.
    /// </summary>
    private static async Task<string> RegisterAndLoginAsync(
        Uri baseAddress,
        string login,
        string password,
        int role)
    {
        using var client = new HttpClient
        {
            BaseAddress = baseAddress
        };
        using var registerResponse = await client.PostAsJsonAsync("auth/register", new
        {
            Login = login,
            Password = password,
            Role = role
        });
        registerResponse.EnsureSuccessStatusCode();

        using var loginResponse = await client.PostAsJsonAsync("auth/login", new
        {
            Login = login,
            Password = password
        });
        loginResponse.EnsureSuccessStatusCode();
        var authentication = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();

        return authentication.GetProperty("token").GetString()
            ?? throw new InvalidOperationException("Сервис пользователей не вернул JWT-токен.");
    }

    /// <summary>
    /// Создаёт событие от имени администратора.
    /// </summary>
    private async Task<Guid> CreateEventAsync(string administratorToken)
    {
        using var client = CreateAuthorizedClient(fixture.EventsBaseAddress, administratorToken);
        using var response = await client.PostAsJsonAsync("events", new
        {
            Title = "E2E-событие",
            Description = "Проверка полного цикла",
            StartAt = DateTimeOffset.UtcNow.AddDays(1),
            EndAt = DateTimeOffset.UtcNow.AddDays(1).AddHours(2),
            TotalSeats = 2
        });
        response.EnsureSuccessStatusCode();
        var eventInfo = await response.Content.ReadFromJsonAsync<JsonElement>();

        return eventInfo.GetProperty("id").GetGuid();
    }

    /// <summary>
    /// Создаёт бронь от имени обычного пользователя.
    /// </summary>
    private async Task<Guid> CreateBookingAsync(Guid eventId, string userToken)
    {
        using var client = CreateAuthorizedClient(fixture.BookingsBaseAddress, userToken);
        using var response = await client.PostAsJsonAsync("bookings", new
        {
            EventId = eventId
        });
        response.EnsureSuccessStatusCode();
        var booking = await response.Content.ReadFromJsonAsync<JsonElement>();

        return booking.GetProperty("id").GetGuid();
    }

    /// <summary>
    /// Возвращает текущее число свободных мест события.
    /// </summary>
    private async Task<int> GetAvailableSeatsAsync(Guid eventId)
    {
        using var client = new HttpClient
        {
            BaseAddress = fixture.EventsBaseAddress
        };
        using var response = await client.GetAsync($"events/{eventId}");
        response.EnsureSuccessStatusCode();
        var eventInfo = await response.Content.ReadFromJsonAsync<JsonElement>();

        return eventInfo.GetProperty("availableSeats").GetInt32();
    }

    /// <summary>
    /// Создаёт HTTP-клиент с JWT в заголовке Authorization.
    /// </summary>
    private static HttpClient CreateAuthorizedClient(Uri baseAddress, string token)
    {
        var client = new HttpClient
        {
            BaseAddress = baseAddress
        };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    /// <summary>
    /// Ожидает выполнения асинхронного условия с ограничением времени.
    /// </summary>
    private static async Task WaitUntilAsync(Func<Task<bool>> condition, string errorMessage)
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        while (!cancellationTokenSource.IsCancellationRequested)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationTokenSource.Token);
        }

        throw new TimeoutException(errorMessage);
    }
}
