using WorkflowApp.Api.Domain.Entities;
using WorkflowApp.Api.Domain.Enums;
using WorkflowApp.Api.Infrastructure.Security;
using WorkflowApp.Api.Tests.Helpers;

namespace WorkflowApp.Api.Tests.Infrastructure.Security
{
    public class JwtTokenServiceTests
    {
        [Fact]
        public void CreateToken_認証トークンが正しく生成されること()
        {
            // Arrange
            var configuration = TestConfigurationFactory.CreateConfiguration();
            var service = new JwtTokenService(configuration);

            var user = new User
            {
                Id = 1,
                LoginId = "testuser",
                DisplayName = "Test User",
                Role = UserRole.Applicant
            };

            // Act
            var result = service.CreateToken(user);

            // Assert
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Token));
            Assert.Equal(user.LoginId, result.LoginId);
            Assert.Equal(user.DisplayName, result.DisplayName);
            Assert.Equal(user.Role.ToString(), result.Role);
            Assert.True(result.ExpiresAt > DateTime.UtcNow);
        }
    }
}
