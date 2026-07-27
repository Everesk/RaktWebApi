using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Rakt.UsersService.Application;
using Rakt.UsersService.Domain;
namespace Rakt.UsersService.Infrastructure;
/// <summary>Контекст изолированной базы данных пользователей.</summary>
public sealed class UsersDbContext(DbContextOptions<UsersDbContext> options) : DbContext(options)
{
    /// <summary>Пользователи сервиса.</summary>
    public DbSet<User> Users => Set<User>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<User>(e =>
        {
            e.ToTable("users"); e.HasKey(x => x.Id); e.Property(x => x.Login).HasMaxLength(100).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(64).IsRequired(); e.Property(x => x.Role).HasConversion<string>(); e.HasIndex(x => x.Login).IsUnique();
        });
    }
}

/// <summary>Создаёт контекст пользователей для инструментов EF Core.</summary>
public sealed class UsersDbContextFactory : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<UsersDbContext>
{
    public UsersDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<UsersDbContext>().UseNpgsql("Host=localhost;Database=rakt_users;Username=postgres;Password=postgres").Options);
}
/// <summary>Репозиторий пользователей EF Core.</summary>
public sealed class UserRepository(UsersDbContext db) : IUserRepository
{
    public Task<User?> FindByLoginAsync(string login, CancellationToken ct = default) => db.Users.SingleOrDefaultAsync(x => x.Login == login, ct);
    public async Task AddAsync(User user, CancellationToken ct = default) { await db.Users.AddAsync(user, ct); await db.SaveChangesAsync(ct); }
}
/// <summary>Настройки JWT сервиса пользователей.</summary>
public sealed class JwtOptions
{
    /// <summary>Секрет подписи.</summary>
    public string Secret { get; init; } = null!;
    /// <summary>Издатель токена.</summary>
    public string Issuer { get; init; } = null!;
    /// <summary>Получатель токена.</summary>
    public string Audience { get; init; } = null!;
    /// <summary>Время жизни в минутах.</summary>
    public int LifetimeMinutes { get; init; } = 60;
}
/// <summary>Хеширует пароли алгоритмом SHA-256.</summary>
public sealed class PasswordHasher : IPasswordHasher
{
    public string Hash(string password) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
    public bool Verify(string password, string hash) => CryptographicOperations.FixedTimeEquals(Convert.FromHexString(Hash(password)), Convert.FromHexString(hash));
}
/// <summary>Выпускает подписанные JWT-токены.</summary>
public sealed class JwtTokenGenerator(IOptions<JwtOptions> options) : IJwtTokenGenerator
{
    public string Generate(User user)
    {
        var value = options.Value;
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(value.Secret)), SecurityAlgorithms.HmacSha256);
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), new Claim(ClaimTypes.Role, user.Role.ToString()) };
        var token = new JwtSecurityToken(value.Issuer, value.Audience, claims, expires: DateTime.UtcNow.AddMinutes(value.LifetimeMinutes), signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
