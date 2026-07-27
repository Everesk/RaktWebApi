using Rakt.UsersService.Domain;
using Xunit;
namespace Rakt.UsersService.UnitTests;
/// <summary>Проверки доменной модели пользователей.</summary>
public sealed class UserTests
{
    /// <summary>Пользователь создаётся с указанной ролью.</summary>
    [Fact]
    public void Create_SetsRole() => Assert.Equal(UserRole.Admin, User.Create("admin", "hash", UserRole.Admin).Role);
}
