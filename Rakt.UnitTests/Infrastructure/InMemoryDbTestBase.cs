using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaktApi.Infrastructure.Data;
using RaktApi.Domain;

namespace Rakt.Tests.Infrastructure;

/// <summary>
/// Базовый класс для тестов, работающих с InMemory EF Core и DI-контейнером.
/// </summary>
public abstract class InMemoryDbTestBase : IDisposable
{
    private readonly string _dbName = Guid.NewGuid().ToString();
    private ServiceProvider? _serviceProvider;
    private IServiceScope? _testScope;

    /// <summary>
    /// Имя уникальной InMemory-базы для текущего тестового экземпляра.
    /// </summary>
    protected string DatabaseName => _dbName;

    /// <summary>
    /// Настраивает дополнительные сервисы для текущего тестового контейнера.
    /// </summary>
    /// <param name="services">Коллекция сервисов DI.</param>
    protected abstract void ConfigureServices(IServiceCollection services);

    /// <summary>
    /// Настраивает DbContext для текущего тестового контейнера.
    /// </summary>
    /// <param name="services">Коллекция сервисов DI.</param>
    protected virtual void ConfigureDbContext(IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(_dbName));
    }

    /// <summary>
    /// Возвращает настроенный root-контейнер для тестов.
    /// </summary>
    protected IServiceProvider ServiceProvider
    {
        get
        {
            EnsureTestScope();
            return _testScope!.ServiceProvider;
        }
    }

    /// <summary>
    /// Возвращает сервис из основного тестового scope.
    /// </summary>
    /// <typeparam name="TService">Тип сервиса.</typeparam>
    /// <returns>Экземпляр сервиса.</returns>
    protected TService GetRequiredService<TService>()
        where TService : notnull
    {
        EnsureTestScope();
        return _testScope!.ServiceProvider.GetRequiredService<TService>();
    }

    /// <summary>
    /// Создает новый scope поверх тестового контейнера.
    /// </summary>
    /// <returns>Новый DI scope.</returns>
    protected IServiceScope CreateScope()
    {
        EnsureServiceProvider();
        return _serviceProvider!.CreateScope();
    }

    /// <summary>
    /// Создает scoped-объект сервиса вместе с его scope.
    /// </summary>
    /// <typeparam name="TService">Тип сервиса.</typeparam>
    /// <returns>Обертка со scope и сервисом.</returns>
    protected ScopedService<TService> CreateScopedService<TService>()
        where TService : notnull
    {
        var scope = CreateScope();
        return new ScopedService<TService>(scope);
    }

    /// <summary>
    /// Создает и сохраняет тестовое событие в InMemory-базе.
    /// </summary>
    /// <param name="totalSeats">Общее число мест.</param>
    /// <param name="title">Название события.</param>
    /// <param name="startAt">Дата и время начала события.</param>
    /// <returns>Созданное событие.</returns>
    protected async Task<Event> SeedEventAsync(
        int totalSeats = 10,
        string title = "Тестовое событие",
        DateTimeOffset? startAt = null)
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var eventStartAt = startAt ?? DateTimeOffset.UtcNow.AddDays(1);
        var eventEntity = new Event(
            title: title,
            description: null,
            startAt: eventStartAt,
            endAt: eventStartAt.AddHours(1),
            totalSeats: totalSeats);

        await context.Events.AddAsync(eventEntity);
        await context.SaveChangesAsync();

        return eventEntity;
    }

    /// <summary>
    /// Создает UTC-время для тестовых сценариев.
    /// </summary>
    /// <param name="year">Год.</param>
    /// <param name="month">Месяц.</param>
    /// <param name="day">День.</param>
    /// <param name="hour">Час.</param>
    /// <param name="minute">Минута.</param>
    /// <param name="second">Секунда.</param>
    /// <returns>Дата и время в UTC.</returns>
    protected static DateTimeOffset Utc(int year, int month, int day, int hour, int minute, int second)
    {
        return new DateTimeOffset(year, month, day, hour, minute, second, TimeSpan.Zero);
    }

    /// <summary>
    /// Освобождает ресурсы тестового контейнера.
    /// </summary>
    public void Dispose()
    {
        _testScope?.Dispose();
        _serviceProvider?.Dispose();
    }

    /// <summary>
    /// Создает root-контейнер один раз для текущего тестового экземпляра.
    /// </summary>
    private void EnsureServiceProvider()
    {
        if (_serviceProvider is not null)
        {
            return;
        }

        var services = new ServiceCollection();
        ConfigureDbContext(services);
        ConfigureServices(services);

        _serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Создает основной scope теста один раз.
    /// </summary>
    private void EnsureTestScope()
    {
        EnsureServiceProvider();

        if (_testScope is not null)
        {
            return;
        }

        _testScope = _serviceProvider!.CreateScope();
    }

    /// <summary>
    /// Обертка для scoped-сервиса и связанного с ним scope.
    /// </summary>
    /// <typeparam name="TService">Тип сервиса.</typeparam>
    protected sealed class ScopedService<TService> : IDisposable
        where TService : notnull
    {
        /// <summary>
        /// Создает обертку для scoped-сервиса.
        /// </summary>
        /// <param name="scope">Активный scope DI.</param>
        public ScopedService(IServiceScope scope)
        {
            Scope = scope;
            Service = scope.ServiceProvider.GetRequiredService<TService>();
        }

        /// <summary>
        /// Активный scope DI.
        /// </summary>
        public IServiceScope Scope { get; }

        /// <summary>
        /// Экземпляр сервиса из scope.
        /// </summary>
        public TService Service { get; }

        /// <summary>
        /// Освобождает scope.
        /// </summary>
        public void Dispose()
        {
            Scope.Dispose();
        }
    }
}
