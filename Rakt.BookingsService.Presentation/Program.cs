using Microsoft.EntityFrameworkCore;
using Rakt.BookingsService.Application;
using Rakt.BookingsService.Infrastructure;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<BookingsDbContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("BookingsDatabase")));
builder.Services.AddScoped<IBookingRepository, BookingRepository>(); builder.Services.AddScoped<BookingService>();
var app = builder.Build();
app.MapGet("/health", () => Results.Ok());
app.Run();
public partial class Program { }
