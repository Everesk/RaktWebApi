namespace Rakt.EventsService.Application;
/// <summary>Сервис управления местами события.</summary>
public sealed class EventSeatsService(IEventRepository events)
{
    /// <summary>Резервирует место и возвращает результат операции.</summary>
    public async Task<bool> ReserveAsync(Guid eventId, CancellationToken ct = default) { var entity = await events.GetAsync(eventId, ct) ?? throw new KeyNotFoundException("Событие не найдено."); var result = entity.TryReserveSeat(); await events.SaveChangesAsync(ct); return result; }
    /// <summary>Возвращает место при отмене брони.</summary>
    public async Task ReleaseAsync(Guid eventId, CancellationToken ct = default) { var entity = await events.GetAsync(eventId, ct) ?? throw new KeyNotFoundException("Событие не найдено."); entity.ReleaseSeat(); await events.SaveChangesAsync(ct); }
}
