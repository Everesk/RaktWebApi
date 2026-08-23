using Microsoft.EntityFrameworkCore;
using Rakt.UsersService.Application;
using Rakt.UsersService.Domain;
using Rakt.UsersService.Domain.Exceptions;
using Rakt.UsersService.Infrastructure;
using Xunit;

namespace Rakt.UsersService.UnitTests;

/// <summary>
/// Проверки прикладных сценариев регистрации и входа пользователей.
/// </summary>
[Trait("Category", "Unit")]
public sealed class UserServiceTests
{
    /// <summary>
    /// Регистрация сохраняет хеш пароля и выбранную роль.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_SavesHashedPasswordAndSelectedRole()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.RegisterAsync(
            new RegisterUserCommand("user", "Password123!", UserRole.Admin));

        var user = await context.Users.SingleAsync();
        Assert.Equal(result.UserId, user.Id);
        Assert.Equal(UserRole.Admin, user.Role);
        Assert.NotEqual("Password123!", user.PasswordHash);
    }

    /// <summary>
    /// Вход с правильным паролем возвращает токен пользователя.
    /// </summary>
    [Fact]
    public async Task LoginAsync_ReturnsTokenWhenCredentialsAreValid()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var registration = await service.RegisterAsync(new RegisterUserCommand("user", "Password123!"));

        var login = await service.LoginAsync(new LoginCommand("user", "Password123!"));

        Assert.Equal(registration.UserId, login.UserId);
        Assert.Equal($"token-{login.UserId}", login.Token);
    }

    /// <summary>
    /// Повторная регистрация логина запрещена.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_ThrowsWhenLoginIsTaken()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        await service.RegisterAsync(new RegisterUserCommand("user", "Password123!"));

        await Assert.ThrowsAsync<UserAlreadyExistsException>(
            () => service.RegisterAsync(new RegisterUserCommand("user", "Password123!")));
    }

    /// <summary>
    /// Вход с неправильным паролем запрещён.
    /// </summary>
    [Fact]
    public async Task LoginAsync_ThrowsWhenPasswordIsInvalid()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        await service.RegisterAsync(new RegisterUserCommand("user", "Password123!"));

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => service.LoginAsync(new LoginCommand("user", "WrongPassword!")));
    }

    /// <summary>
    /// Создаёт изолированный контекст пользователей в памяти.
    /// </summary>
    private static UsersDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new UsersDbContext(options);
    }

    /// <summary>
    /// Создаёт сценарий пользователей с реальными репозиторием и хешированием.
    /// </summary>
    private static UserService CreateService(UsersDbContext context)
    {
        return new UserService(
            new UserRepository(context),
            new PasswordHasher(),
            new TestJwtTokenGenerator());
    }
}
