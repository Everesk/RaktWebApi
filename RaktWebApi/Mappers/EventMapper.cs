using RaktWebApi.Models;

namespace RaktWebApi.Mappers;

public static class EventMapper
{
    /// <summary>
    /// Преобразует CreateEventDto в Event.
    /// </summary>
    public static Event CreateFromDto(this CreateEventDto dto)
        => new Event(dto.Title, dto.Description, dto.StartAt, dto.EndAt);

    /// <summary>
    /// Обновляет Event из UpdateEventDto.
    /// </summary>
    public static void UpdateFromDto(this Event entity, UpdateEventDto dto)
        => entity.Update(dto.Title, dto.Description, dto.StartAt, dto.EndAt);
}