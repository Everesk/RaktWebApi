using RaktWebApi.Models;
using RaktWebApi.Models.DTO;

namespace RaktWebApi.Mappers;

/// <summary>
/// Методы расширения для создания Event из DTO
/// </summary>

public static class EventMapper
{
    /// <summary>
    /// Преобразует CreateEventDto в Event.
    /// </summary>
    public static Event CreateFromDto(this CreateEventDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (!dto.StartAt.HasValue)
            throw new ArgumentException("Не указано время начала события", nameof(dto));

        if (!dto.EndAt.HasValue)
            throw new ArgumentException("Не указано время окончания события", nameof(dto));

        if (!dto.TotalSeats.HasValue)
            throw new ArgumentException("Не указано количество мест на событии", nameof(dto));

        return Event.Create(dto.Title, dto.Description, dto.StartAt.Value, dto.EndAt.Value, dto.TotalSeats.Value);
    }

    /// <summary>
    /// Преобразует Event в EventInfoDto.
    /// </summary>
    public static EventInfoDto ToInfoDto(this Event entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new EventInfoDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            StartAt = entity.StartAt,
            EndAt = entity.EndAt,
            TotalSeats = entity.TotalSeats,
            AvailableSeats = entity.AvailableSeats,
            IsFull = entity.IsFull
        };
    }

    /// <summary>
    /// Обновляет Event из UpdateEventDto.
    /// </summary>
    public static void UpdateFromDto(this Event entity, UpdateEventDto dto)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(dto);

        if (!dto.StartAt.HasValue)
            throw new ArgumentException("Не указано время начала события", nameof(dto));

        if (!dto.EndAt.HasValue)
            throw new ArgumentException("Не указано время окончания события", nameof(dto));

        entity.Update(dto.Title, dto.Description, dto.StartAt.Value, dto.EndAt.Value);
    }
}
