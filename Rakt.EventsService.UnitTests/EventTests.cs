using Rakt.EventsService.Domain;
using Xunit;
namespace Rakt.EventsService.UnitTests;
/// <summary>Проверки доменной модели событий.</summary>
public sealed class EventTests
{
    /// <summary>Нельзя зарезервировать больше мест, чем есть.</summary>
    [Fact]
    public void TryReserveSeat_StopsAtCapacity()
    {
        var entity = Event.Create("Встреча", DateTimeOffset.UtcNow, 1);
        Assert.True(entity.TryReserveSeat());
        Assert.False(entity.TryReserveSeat());
    }
}
