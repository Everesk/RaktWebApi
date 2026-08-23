using Rakt.EventsService.Domain;
using Rakt.EventsService.Domain.Exceptions;
namespace Rakt.EventsService.Application;
/// <summary>Реализует CRUD-сценарии сервиса событий.</summary>
public sealed class EventService(IEventRepository events) : IEventService
{
    /// <inheritdoc />
    public async Task<PaginatedResult<EventInfoDto>> GetAllAsync(EventQueryDto query, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var result = await events.GetAllAsync(query, ct);
        return new PaginatedResult<EventInfoDto> { TotalCount = result.TotalCount, Items = result.Items.Select(ToDto).ToList(), Page = result.Page, PageSize = result.PageSize, CurrentCount = result.CurrentCount };
    }
    /// <inheritdoc />
    public async Task<EventInfoDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var entity = await events.GetAsync(id, ct) ?? throw new NotFoundException($"Событие с идентификатором '{id}' не найдено.");
        return ToDto(entity);
    }
    /// <inheritdoc />
    public async Task<EventInfoDto> CreateAsync(CreateEventDto dto, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var entity = Event.Create(dto.Title, dto.Description, dto.StartAt!.Value, dto.EndAt!.Value, dto.TotalSeats!.Value);
        await events.AddAsync(entity, ct);
        await events.SaveChangesAsync(ct);
        return ToDto(entity);
    }
    /// <inheritdoc />
    public async Task UpdateAsync(Guid id, UpdateEventDto dto, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var entity = await events.GetForUpdateAsync(id, ct) ?? throw new NotFoundException($"Событие с идентификатором '{id}' не найдено.");
        entity.Update(dto.Title, dto.Description, dto.StartAt!.Value, dto.EndAt!.Value);
        await events.SaveChangesAsync(ct);
    }
    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var entity = await events.GetForUpdateAsync(id, ct) ?? throw new NotFoundException($"Событие с идентификатором '{id}' не найдено.");
        await events.DeleteAsync(entity, ct);
        await events.SaveChangesAsync(ct);
    }
    private static EventInfoDto ToDto(Event entity) => new() { Id = entity.Id, Title = entity.Title, Description = entity.Description, StartAt = entity.StartAt, EndAt = entity.EndAt, TotalSeats = entity.TotalSeats, AvailableSeats = entity.AvailableSeats, IsFull = entity.IsFull };
}
