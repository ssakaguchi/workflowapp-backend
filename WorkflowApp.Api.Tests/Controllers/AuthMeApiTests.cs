using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WorkflowApp.Api.Tests.Controllers
{
    public class AuthMeApiTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public AuthMeApiTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }


        [Fact]
        public async Task MeAsync_トークンなしの場合は401を返すこと()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/auth/me", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task MeAsync_有効なトークンの場合はユーザー情報を返すこと()
        {
            // Arrange
            var client = _factory.CreateClient();

            var registerRequest = new
            {
                loginId = $"testuser_{Guid.NewGuid():N}",
                displayName = "Test User",
                password = "Password123"
            };

            var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerRequest, cancellationToken: TestContext.Current.CancellationToken);
            registerResponse.EnsureSuccessStatusCode();

            var loginRequest = new
            {
                loginId = registerRequest.loginId,
                password = "Password123"
            };

            var loginResponse = await client.PostAsJsonAsync("/api/auth/login",
                                                             loginRequest,
                                                             cancellationToken: TestContext.Current.CancellationToken);

            var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(loginResult);
            Assert.False(string.IsNullOrEmpty(loginResult.Token));

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.Token);

            // Act
            var response = await client.GetAsync("/api/auth/me", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var me = await response.Content.ReadFromJsonAsync<MeResponseDto>(
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.NotNull(me);
            Assert.True(me.UserId > 0);
            Assert.Equal(registerRequest.loginId, me.LoginId);
            Assert.Equal(registerRequest.displayName, me.DisplayName);
        }


        private sealed class LoginResponse
        {
            public string Token { get; set; } = default!;
        }

        private sealed class MeResponseDto
        {
            public int UserId { get; set; }
            public string LoginId { get; set; } = default!;
            public string DisplayName { get; set; } = default!;
        }

        [Fact]
        public async Task MeAsync_トークンに不正な値が含まれる場合は401を返すこと()
        {
            // Arrange
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid_token");

            // Act
            var response = await client.GetAsync("/api/auth/me", TestContext.Current.CancellationToken);
            
            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
