using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Rakt.BookingsService.Domain;

namespace Rakt.BookingsService.Infrastructure;

/// <summary>
/// Устанавливает время создания для новых броней перед сохранением.
/// </summary>
public sealed class BookingCreatedAtInterceptor : SaveChangesInterceptor
{
    /// <summary>
    /// Заполняет время создания перед синхронным сохранением.
    /// </summary>
    /// <param name="eventData">Данные о сохранении контекста.</param>
    /// <param name="result">Текущий результат перехвата.</param>
    /// <returns>Результат без изменений.</returns>
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyCreatedAt(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    /// <summary>
    /// Заполняет время создания перед асинхронным сохранением.
    /// </summary>
    /// <param name="eventData">Данные о сохранении контекста.</param>
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
    /// Проставляет время создания у новых отслеживаемых броней.
    /// </summary>
    private static void ApplyCreatedAt(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var createdAt = DateTimeOffset.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<Booking>())
        {
            if (entry.State != EntityState.Added)
            {
                continue;
            }

            entry.Property(booking => booking.CreatedAt).CurrentValue = createdAt;
        }
    }
}
