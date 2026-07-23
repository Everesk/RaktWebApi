using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RaktApi.Domain;

namespace RaktApi.Infrastructure.Data.Interceptors;

/// <summary>
/// Перехватчик EF Core, который автоматически проставляет время создания новым бронированиям.
/// </summary>
public sealed class BookingCreatedAtInterceptor : SaveChangesInterceptor
{
    /// <summary>
    /// Проставляет <see cref="Booking.CreatedAt"/> для новых сущностей перед синхронным сохранением.
    /// </summary>
    /// <param name="eventData">Данные о вызове сохранения.</param>
    /// <param name="result">Текущий результат перехвата.</param>
    /// <returns>Результат без изменений.</returns>
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyCreatedAt(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <summary>
    /// Проставляет <see cref="Booking.CreatedAt"/> для новых сущностей перед асинхронным сохранением.
    /// </summary>
    /// <param name="eventData">Данные о вызове сохранения.</param>
    /// <param name="result">Текущий результат перехвата.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Результат без изменений.</returns>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyCreatedAt(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Заполняет время создания у всех новых бронирований в текущем контексте.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    private static void ApplyCreatedAt(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<Booking>()
                     .Where(entry => entry.State == EntityState.Added))
        {
            entry.Property(x => x.CreatedAt).CurrentValue = now;
        }
    }
}
