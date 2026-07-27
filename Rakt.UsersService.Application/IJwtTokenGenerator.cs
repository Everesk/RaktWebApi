using Rakt.UsersService.Domain;
namespace Rakt.UsersService.Application;
/// <summary>Порт выпуска JWT.</summary>
public interface IJwtTokenGenerator { string Generate(User user); }
