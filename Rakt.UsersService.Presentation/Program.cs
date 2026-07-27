using Microsoft.EntityFrameworkCore;
using Rakt.UsersService.Application;
using Rakt.UsersService.Infrastructure;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<UsersDbContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("UsersDatabase")));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<IUserRepository, UserRepository>(); builder.Services.AddScoped<IPasswordHasher, PasswordHasher>(); builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>(); builder.Services.AddScoped<IUserService, UserService>();
var app = builder.Build();
app.MapPost("/api/auth/register", (RegisterUserCommand command, IUserService service, CancellationToken ct) => service.RegisterAsync(command, ct));
app.MapPost("/api/auth/login", (LoginCommand command, IUserService service, CancellationToken ct) => service.LoginAsync(command, ct));
app.Run();
public partial class Program { }
