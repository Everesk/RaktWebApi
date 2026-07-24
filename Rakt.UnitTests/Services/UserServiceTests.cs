using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RaktApi.Application.DTO;
using RaktApi.Application.Ports;
using RaktApi.Application.Services;
using RaktApi.Domain;
using RaktApi.Domain.Exceptions;
using RaktApi.Infrastructure.Data;
using RaktApi.Infrastructure.Options;
using RaktApi.Infrastructure.Repositories;
using RaktApi.Infrastructure.Services;
using Rakt.Tests.Infrastructure;

namespace Rakt.Tests.Services;

/// <summary>
/// Набор тестов для сервиса <see cref="UserService"/>.
/// </summary>
public class UserServiceTests : InMemoryDbTestBase
{
    /// <summary>
    /// Регистрирует зависимости сценариев работы с пользователями.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserService, UserService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator>(_ => new JwtTokenGenerator(Options.Create(new JwtOptions
        {
            Secret = "TestJwtSigningSecretKeyMustContainAtLeast32Characters",
            Issuer = "test-issuer",
            Audience = "test-audience",
            LifetimeMinutes = 60
        })));
    }

    /// <summary>
    /// Проверяет сохранение пользователя с хешем пароля при регистрации.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_ShouldSaveUserWithHashedPassword()
    {
        // Arrange
        using var serviceScope = CreateScopedService<IUserService>();
        var dto = new RegisterUserDto { Login = "user", Password = "password" };

        // Act
        var user = await serviceScope.Service.RegisterAsync(dto);

        // Assert
        user.Role.Should().Be(UserRole.User);
        user.PasswordHash.Should().NotBe(dto.Password);

        using var verificationScope = CreateScope();
        var context = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var storedUser = await context.Users.AsNoTracking().SingleAsync(x => x.Id == user.Id);
        storedUser.Login.Should().Be(dto.Login);
        storedUser.PasswordHash.Should().Be(new PasswordHasher().Hash(dto.Password));
    }

    /// <summary>
    /// Проверяет возврат токена при корректных учетных данных.
    /// </summary>
    [Fact]
    public async Task LoginAsync_ShouldReturnToken_WhenCredentialsAreValid()
    {
        // Arrange
        using var serviceScope = CreateScopedService<IUserService>();
        await serviceScope.Service.RegisterAsync(new RegisterUserDto { Login = "user", Password = "password" });

        // Act
        var authentication = await serviceScope.Service.LoginAsync(new LoginDto { Login = "user", Password = "password" });

        // Assert
        authentication.Token.Should().NotBeNullOrWhiteSpace();
        authentication.Token.Split('.').Should().HaveCount(3);
    }

    /// <summary>
    /// Проверяет запрет повторной регистрации с тем же логином.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_ShouldThrowUserAlreadyExistsException_WhenLoginIsTaken()
    {
        // Arrange
        using var serviceScope = CreateScopedService<IUserService>();
        await serviceScope.Service.RegisterAsync(new RegisterUserDto { Login = "user", Password = "password" });

        // Act
        Func<Task> act = async () => await serviceScope.Service.RegisterAsync(
            new RegisterUserDto { Login = "user", Password = "another-password" });

        // Assert
        await act.Should().ThrowAsync<UserAlreadyExistsException>();
    }

    /// <summary>
    /// Проверяет запрет входа с неверным паролем.
    /// </summary>
    [Fact]
    public async Task LoginAsync_ShouldThrowInvalidCredentialsException_WhenPasswordIsInvalid()
    {
        // Arrange
        using var serviceScope = CreateScopedService<IUserService>();
        await serviceScope.Service.RegisterAsync(new RegisterUserDto { Login = "user", Password = "password" });

        // Act
        Func<Task> act = async () => await serviceScope.Service.LoginAsync(
            new LoginDto { Login = "user", Password = "invalid-password" });

        // Assert
        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }
}
