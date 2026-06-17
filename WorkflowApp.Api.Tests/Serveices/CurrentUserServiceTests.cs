using System.Security.Claims;
using WorkflowApp.Api.Services;

namespace WorkflowApp.Api.Tests.Services
{
    public class CurrentUserServiceTests
    {
        // System Under Test (テスト対象のインスタンス)
        private readonly CurrentUserService _sut = new();

        [Fact]
        public void GetCurrentUser_NameIdentifierが存在しない場合はnullを返すこと()
        {
            // Arrange
            var user = CreateClaimsPrincipal(
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim("displayName", "Test User"),
                new Claim(ClaimTypes.Role, "User"));

            // Act
            var result = _sut.GetCurrentUser(user);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetCurrentUser_Nameが存在しない場合はnullを返すこと()
        {
            // Arrange
            var user = CreateClaimsPrincipal(
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim("displayName", "Test User"),
                new Claim(ClaimTypes.Role, "User"));

            // Act
            var result = _sut.GetCurrentUser(user);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetCurrentUser_Roleが存在しない場合はnullを返すこと()
        {
            // Arrange
            var user = CreateClaimsPrincipal(
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim("displayName", "Test User")
                // Roleクレームを意図的に省略
                );

            // Act
            var result = _sut.GetCurrentUser(user);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetCurrentUser_NameIdentifierが数値でない場合はnullを返すこと()
        {
            // Arrange
            var user = CreateClaimsPrincipal(
                new Claim(ClaimTypes.NameIdentifier, "abc"),
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim("displayName", "Test User"),
                new Claim(ClaimTypes.Role, "User"));

            // Act
            var result = _sut.GetCurrentUser(user);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetCurrentUser_DisplayNameが存在しない場合は空文字で返すこと()
        {
            // Arrange
            var user = CreateClaimsPrincipal(
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim("displayName", string.Empty),
                new Claim(ClaimTypes.Role, "User")
                );

            // Act
            var result = _sut.GetCurrentUser(user);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.UserId);
            Assert.Equal("testuser", result.LoginId);
            Assert.Equal(string.Empty, result.DisplayName);
            Assert.Equal("User", result.Role);
        }

        [Fact]
        public void GetCurrentUser_必要なClaimが揃っている場合はユーザー情報を返すこと()
        {
            // Arrange
            var user = CreateClaimsPrincipal(
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim("displayName", "Test User"),
                new Claim(ClaimTypes.Role, "User"));

            // Act
            var result = _sut.GetCurrentUser(user);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.UserId);
            Assert.Equal("testuser", result.LoginId);
            Assert.Equal("Test User", result.DisplayName);
            Assert.Equal("User", result.Role);
        }

        /// <summary>
        /// 指定したクレームを持つClaimsPrincipalを生成する
        /// </summary>
        /// <remarks>
        /// 認証タイプは "TestAuthType" を使用（テスト・モック用途）
        /// </remarks>
        /// <param name="claims">設定するクレーム（空可）。</param>
        /// <returns>ClaimsPrincipal インスタンス</returns>
        private static ClaimsPrincipal CreateClaimsPrincipal(params Claim[] claims)
        {
            var identity = new ClaimsIdentity(claims, authenticationType: "TestAuthType");
            return new ClaimsPrincipal(identity);
        }
    }
}
