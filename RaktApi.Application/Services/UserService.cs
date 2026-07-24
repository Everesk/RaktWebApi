using RaktApi.Application.DTO;
using RaktApi.Application.Ports;
using RaktApi.Domain;
using RaktApi.Domain.Exceptions;

namespace RaktApi.Application.Services;

/// <summary>Реализует регистрацию и аутентификацию пользователей.</summary>
public sealed class UserService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator) : IUserService
{
    /// <inheritdoc />
    public async Task<User> RegisterAsync(RegisterUserDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        cancellationToken.ThrowIfCancellationRequested();

        if (await userRepository.ExistsByLoginAsync(dto.Login, cancellationToken))
        {
            throw new UserAlreadyExistsException($"Пользователь с логином '{dto.Login}' уже существует.");
        }

        var user = User.Create(dto.Login, passwordHasher.Hash(dto.Password), dto.Role);
        await userRepository.AddAsync(user, cancellationToken);
        return user;
    }

    /// <inheritdoc />
    public async Task<AuthenticationDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userRepository.GetByLoginAsync(dto.Login, cancellationToken);
        if (user is null || !passwordHasher.Verify(dto.Password, user.PasswordHash))
        {
            throw new InvalidCredentialsException("Неверный логин или пароль.");
        }

        return new AuthenticationDto { Token = jwtTokenGenerator.Generate(user) };
    }
}
