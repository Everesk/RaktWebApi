using Microsoft.EntityFrameworkCore;
using Rakt.EventsService.Application;
using Rakt.EventsService.Infrastructure;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<EventsDbContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("EventsDatabase")));
builder.Services.AddScoped<IEventRepository, EventRepository>(); builder.Services.AddScoped<EventSeatsService>();
var app = builder.Build();
app.MapGet("/health", () => Results.Ok());
app.Run();
public partial class Program { }
