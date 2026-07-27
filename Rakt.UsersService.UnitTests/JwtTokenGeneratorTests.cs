using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Rakt.UsersService.Domain;
using Rakt.UsersService.Infrastructure;
using Xunit;
namespace Rakt.UsersService.UnitTests;
/// <summary>Проверки генератора JWT.</summary>
public sealed class JwtTokenGeneratorTests
{
    [Fact]
    public void Generate_ShouldCreateTokenWithUserClaims()
    {
        var options = CreateOptions();
        var user = User.Create("admin", "hash", UserRole.Admin);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(new JwtTokenGenerator(Options.Create(options)).Generate(user));
        Assert.Equal(options.Issuer, token.Issuer);
        Assert.Equal(options.Audience, token.Audiences.Single());
        Assert.Contains(token.Claims, claim => claim.Type == JwtRegisteredClaimNames.Sub && claim.Value == user.Id.ToString());
        Assert.Contains(token.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == UserRole.Admin.ToString());
    }
    [Fact]
    public void Generate_ShouldProduceTokenWithValidSignature()
    {
        var options = CreateOptions();
        var user = User.Create("admin", "hash", UserRole.Admin);
        var parameters = new TokenValidationParameters { ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Secret)), ValidateIssuer = true, ValidIssuer = options.Issuer, ValidateAudience = true, ValidAudience = options.Audience, ValidateLifetime = true, ClockSkew = TimeSpan.Zero };
        var principal = new JwtSecurityTokenHandler { MapInboundClaims = false }.ValidateToken(new JwtTokenGenerator(Options.Create(options)).Generate(user), parameters, out _);
        Assert.Equal(user.Id.ToString(), principal.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
    }
    private static JwtOptions CreateOptions() => new() { Secret = "ДаЯХранюЭтотСекретныйСекретПрямоВГите", Issuer = "test-issuer", Audience = "test-audience", LifetimeMinutes = 30 };
}
