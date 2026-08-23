using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Rakt.UsersService.Application;
using Rakt.UsersService.Domain;
namespace Rakt.UsersService.Infrastructure;
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
