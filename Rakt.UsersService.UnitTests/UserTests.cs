using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Rakt.UsersService.Application;
using Rakt.UsersService.Domain;
using Rakt.UsersService.Domain.Exceptions;
using Rakt.UsersService.Infrastructure;
using Xunit;

namespace Rakt.UsersService.UnitTests;

/// <summary>Проверки доменной модели и сценариев пользователей.</summary>
[Trait("Category", "Unit")]
public sealed class UserTests
{
    /// <summary>Проверяет сохранение хеша при регистрации.</summary>
    [Fact]
    public async Task RegisterAsync_ShouldSaveUserWithHashedPassword()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.RegisterAsync(new RegisterUserCommand("user", "password"));
        var stored = await context.Users.SingleAsync(user => user.Id == result.UserId);

        Assert.Equal("user", stored.Login);
        Assert.NotEqual("password", stored.PasswordHash);
        Assert.Equal(UserRole.User, stored.Role);
    }

    /// <summary>Проверяет сохранение выбранной роли.</summary>
    [Fact]
    public async Task RegisterAsync_ShouldSaveSelectedRole()
    {
        await using var context = CreateContext();
        var result = await CreateService(context).RegisterAsync(new RegisterUserCommand("admin", "password", UserRole.Admin));

        Assert.Equal(UserRole.Admin, (await context.Users.SingleAsync(user => user.Id == result.UserId)).Role);
    }

    /// <summary>Проверяет выпуск токена при корректном входе.</summary>
    [Fact]
    public async Task LoginAsync_ShouldReturnToken_WhenCredentialsAreValid()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        await service.RegisterAsync(new RegisterUserCommand("user", "password"));

        var result = await service.LoginAsync(new LoginCommand("user", "password"));

        Assert.Equal(3, result.Token.Split('.').Length);
    }

    /// <summary>Проверяет запрет повторной регистрации.</summary>
    [Fact]
    public async Task RegisterAsync_ShouldThrowUserAlreadyExistsException_WhenLoginIsTaken()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        await service.RegisterAsync(new RegisterUserCommand("user", "password"));

        await Assert.ThrowsAsync<UserAlreadyExistsException>(() => service.RegisterAsync(new RegisterUserCommand("user", "other")));
    }

    /// <summary>Проверяет запрет входа с неверным паролем.</summary>
    [Fact]
    public async Task LoginAsync_ShouldThrowInvalidCredentialsException_WhenPasswordIsInvalid()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        await service.RegisterAsync(new RegisterUserCommand("user", "password"));

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => service.LoginAsync(new LoginCommand("user", "invalid")));
    }

    private static UsersDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<UsersDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new UsersDbContext(options);
    }

    private static IUserService CreateService(UsersDbContext context)
    {
        var options = Options.Create(new JwtOptions { Secret = "TestJwtSigningSecretKeyMustContainAtLeast32Characters", Issuer = "test", Audience = "test" });
        return new UserService(new UserRepository(context), new PasswordHasher(), new JwtTokenGenerator(options));
    }
}
