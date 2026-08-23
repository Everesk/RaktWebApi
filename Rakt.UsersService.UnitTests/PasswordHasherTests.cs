using Rakt.UsersService.Infrastructure;
using Xunit;
namespace Rakt.UsersService.UnitTests;
/// <summary>Проверки хеширования паролей.</summary>
[Trait("Category", "Unit")]
public sealed class PasswordHasherTests
{
    [Fact]
    public void Hash_ShouldReturnSha256Hash() => Assert.Equal("5E884898DA28047151D0E56F8DC6292773603D0D6AABBDD62A11EF721D1542D8", new PasswordHasher().Hash("password"));
    [Fact]
    public void Verify_ShouldReturnTrue_WhenPasswordMatchesHash() { var hasher = new PasswordHasher(); Assert.True(hasher.Verify("password", hasher.Hash("password"))); }
    [Fact]
    public void Verify_ShouldReturnFalse_WhenPasswordDoesNotMatchHash() { var hasher = new PasswordHasher(); Assert.False(hasher.Verify("other", hasher.Hash("password"))); }
}
