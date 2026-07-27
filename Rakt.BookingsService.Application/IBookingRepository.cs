using Rakt.BookingsService.Domain;
namespace Rakt.BookingsService.Application;
/// <summary>Порт хранения броней.</summary>
public interface IBookingRepository { Task<Booking?> GetAsync(Guid id, CancellationToken ct = default); Task AddAsync(Booking booking, CancellationToken ct = default); Task SaveChangesAsync(CancellationToken ct = default); }
