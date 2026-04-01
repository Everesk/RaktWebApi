using RaktWebApi.Services;
using RaktWebApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddStandardConfiguration();
builder.Services.AddSingleton<IEventService, EventService>(); // Как синглтон, ведь события хранятся в памяти

var app = builder.Build();

app.UseStandardConfiguration();

app.Run();
