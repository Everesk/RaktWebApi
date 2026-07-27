using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RaktApi.Domain;
using RaktApi.Infrastructure.Options;
using RaktApi.Infrastructure.Services;

namespace Rakt.Tests.Services;

/// <summary>
/// Набор тестов для <see cref="JwtTokenGenerator"/>.
/// </summary>
[Trait("Category", "Unit")]
public class JwtTokenGeneratorTests
{
    /// <summary>
    /// Проверяет формирование токена с данными пользователя и параметрами конфигурации.
    /// </summary>
    [Fact]
    public void Generate_ShouldCreateTokenWithUserClaims()
    {
        // Arrange
        var options = Options.Create(new JwtOptions
        {
            Secret = "ДаЯХранюЭтотСекретныйСекретПрямоВГите",
            Issuer = "test-issuer",
            Audience = "test-audience",
            LifetimeMinutes = 30
        });
        var tokenGenerator = new JwtTokenGenerator(options);
        var user = User.Create("admin", "password-hash", UserRole.Admin);

        // Act
        var tokenValue = tokenGenerator.Generate(user);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenValue);

        // Assert
        token.Issuer.Should().Be("test-issuer");
        token.Audiences.Should().ContainSingle().Which.Should().Be("test-audience");
        token.Claims.Should().Contain(claim => claim.Type == JwtRegisteredClaimNames.Sub && claim.Value == user.Id.ToString());
        token.Claims.Should().Contain(claim => claim.Type == JwtRegisteredClaimNames.UniqueName && claim.Value == user.Login);
        token.Claims.Should().Contain(claim => claim.Type == ClaimTypes.Role && claim.Value == UserRole.Admin.ToString());
    }

    /// <summary>
    /// Проверяет подпись, срок действия, издателя и аудиторию сформированного токена.
    /// </summary>
    [Fact]
    public void Generate_ShouldProduceTokenWithValidSignature()
    {
        // Arrange
        var options = new JwtOptions
        {
            Secret = "ДаЯХранюЭтотСекретныйСекретПрямоВГите",
            Issuer = "test-issuer",
            Audience = "test-audience",
            LifetimeMinutes = 30
        };
        var tokenGenerator = new JwtTokenGenerator(Options.Create(options));
        var user = User.Create("admin", "password-hash", UserRole.Admin);
        var tokenHandler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Secret)),
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // Act
        var tokenValue = tokenGenerator.Generate(user);
        var principal = tokenHandler.ValidateToken(tokenValue, validationParameters, out var validatedToken);

        // Assert
        validatedToken.Should().BeOfType<JwtSecurityToken>();
        principal.FindFirst(JwtRegisteredClaimNames.Sub)!.Value.Should().Be(user.Id.ToString());
        principal.FindFirst(ClaimTypes.Role)!.Value.Should().Be(UserRole.Admin.ToString());
    }
}
