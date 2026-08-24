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
        var administratorToken = await CreateUserTokenAsync(role: 1);
        var userToken = await CreateUserTokenAsync(role: 0);
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
    /// Не допускает подтверждения большего числа броней, чем мест на событии.
    /// </summary>
    [Fact]
    public async Task ConcurrentBookings_DoNotOverbookEvent()
    {
        var administratorToken = await CreateUserTokenAsync(role: 1);
        var eventId = await CreateEventAsync(administratorToken, totalSeats: 2);
        var userTokens = new List<string>();

        for (var index = 0; index < 5; index++)
        {
            userTokens.Add(await CreateUserTokenAsync(role: 0));
        }

        var bookingIds = await Task.WhenAll(
            userTokens.Select(userToken => CreateBookingAsync(eventId, userToken)));

        await WaitUntilAsync(
            async () =>
            {
                var statuses = await Task.WhenAll(
                    bookingIds.Select(bookingId => GetBookingStatusAsync(bookingId, userTokens[Array.IndexOf(bookingIds, bookingId)])));

                return statuses.All(status => status is 1 or 2);
            },
            "Не все конкурентные брони получили итоговый статус.");

        var finalStatuses = await Task.WhenAll(
            bookingIds.Select(bookingId => GetBookingStatusAsync(bookingId, userTokens[Array.IndexOf(bookingIds, bookingId)])));

        Assert.Equal(2, finalStatuses.Count(status => status == 1));
        Assert.Equal(3, finalStatuses.Count(status => status == 2));
        Assert.Equal(0, await GetAvailableSeatsAsync(eventId));
    }

    /// <summary>
    /// Возвращает место после отмены, даже если отмена поступила в Events раньше запроса брони.
    /// </summary>
    [Fact]
    public async Task ImmediateCancellation_DoesNotKeepEventSeatReserved()
    {
        var administratorToken = await CreateUserTokenAsync(role: 1);
        var userToken = await CreateUserTokenAsync(role: 0);
        var eventId = await CreateEventAsync(administratorToken, totalSeats: 1);
        var bookingId = await CreateBookingAsync(eventId, userToken);

        using (var client = CreateAuthorizedClient(fixture.BookingsBaseAddress, userToken))
        {
            using var response = await client.DeleteAsync($"bookings/{bookingId}");
            response.EnsureSuccessStatusCode();
        }

        await WaitUntilAsync(
            async () => await GetAvailableSeatsAsync(eventId) == 1,
            "Сервис событий удерживает место отменённой брони.");

        Assert.Equal(3, await GetBookingStatusAsync(bookingId, userToken));
    }

    /// <summary>
    /// Проверяет полный путь кеша события через HTTP API, Redis и PostgreSQL.
    /// </summary>
    [Fact]
    public async Task EventCache_UpdatesAfterWriteAndIsRemovedAfterDelete()
    {
        var administratorToken = await CreateUserTokenAsync(role: 1);
        var eventId = await CreateEventAsync(administratorToken);

        using (var client = new HttpClient { BaseAddress = fixture.EventsBaseAddress })
        {
            using var response = await client.GetAsync($"events/{eventId}");
            response.EnsureSuccessStatusCode();
            var eventInfo = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal("E2E-событие", eventInfo.GetProperty("title").GetString());
        }

        await WaitUntilAsync(
            async () => await HasCachedEventTitleAsync(eventId, "E2E-событие"),
            "Созданное событие не появилось в Redis.");

        using (var client = new HttpClient { BaseAddress = fixture.EventsBaseAddress })
        {
            using var response = await client.GetAsync("events/top");
            response.EnsureSuccessStatusCode();
        }

        await WaitUntilAsync(
            async () => await fixture.GetRedisValueAsync("events:top10") is not null,
            "Рейтинг событий не появился в Redis.");

        using (var client = CreateAuthorizedClient(fixture.EventsBaseAddress, administratorToken))
        {
            var startAt = DateTimeOffset.UtcNow.AddDays(2);
            using var response = await client.PutAsJsonAsync($"events/{eventId}", new
            {
                Title = "E2E-событие обновлено",
                Description = "Проверка обновления кеша",
                StartAt = startAt,
                EndAt = startAt.AddHours(2)
            });
            response.EnsureSuccessStatusCode();
        }

        await WaitUntilAsync(
            async () => await HasCachedEventTitleAsync(eventId, "E2E-событие обновлено"),
            "Redis не получил обновлённые данные события.");

        using (var client = new HttpClient { BaseAddress = fixture.EventsBaseAddress })
        {
            using var response = await client.GetAsync($"events/{eventId}");
            response.EnsureSuccessStatusCode();
            var eventInfo = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal("E2E-событие обновлено", eventInfo.GetProperty("title").GetString());
        }

        using (var client = CreateAuthorizedClient(fixture.EventsBaseAddress, administratorToken))
        {
            using var response = await client.DeleteAsync($"events/{eventId}");
            response.EnsureSuccessStatusCode();
        }

        await WaitUntilAsync(
            async () => await fixture.GetRedisValueAsync($"event:{eventId}") is null,
            "Ключ удалённого события остался в Redis.");
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
    /// Регистрирует уникального тестового пользователя и возвращает его JWT-токен.
    /// </summary>
    private Task<string> CreateUserTokenAsync(int role)
    {
        return RegisterAndLoginAsync(
            fixture.UsersBaseAddress,
            $"e2e-{Guid.NewGuid():N}",
            "Password123!",
            role);
    }

    /// <summary>
    /// Создаёт событие от имени администратора.
    /// </summary>
    private async Task<Guid> CreateEventAsync(string administratorToken, int totalSeats = 2)
    {
        using var client = CreateAuthorizedClient(fixture.EventsBaseAddress, administratorToken);
        using var response = await client.PostAsJsonAsync("events", new
        {
            Title = "E2E-событие",
            Description = "Проверка полного цикла",
            StartAt = DateTimeOffset.UtcNow.AddDays(1),
            EndAt = DateTimeOffset.UtcNow.AddDays(1).AddHours(2),
            TotalSeats = totalSeats
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
    /// Проверяет, что Redis содержит событие с ожидаемым заголовком.
    /// </summary>
    private async Task<bool> HasCachedEventTitleAsync(Guid eventId, string expectedTitle)
    {
        var cachedValue = await fixture.GetRedisValueAsync($"event:{eventId}");
        if (cachedValue is null)
        {
            return false;
        }

        using var document = JsonDocument.Parse(cachedValue);
        return document.RootElement.GetProperty("Title").GetString() == expectedTitle;
    }

    /// <summary>
    /// Возвращает числовой статус брони из API броней.
    /// </summary>
    private async Task<int> GetBookingStatusAsync(Guid bookingId, string userToken)
    {
        using var client = CreateAuthorizedClient(fixture.BookingsBaseAddress, userToken);
        using var response = await client.GetAsync($"bookings/{bookingId}");
        response.EnsureSuccessStatusCode();
        var booking = await response.Content.ReadFromJsonAsync<JsonElement>();

        return booking.GetProperty("status").GetInt32();
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
