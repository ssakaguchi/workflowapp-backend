using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkflowApp.Api.Domain.Entities;
using WorkflowApp.Api.Domain.Enums;
using WorkflowApp.Api.DTOs.Users;
using WorkflowApp.Api.Infrastructure.Data;
using WorkflowApp.Api.Tests.Helpers;

namespace WorkflowApp.Api.Tests.Controllers
{
    public class UsersApiTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public UsersApiTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;

            // 各テストの前にテスト用DBのユーザーデータをクリーンアップする
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Users.RemoveRange(dbContext.Users);
                dbContext.SaveChanges();
            }
        }


        [Fact]
        public async Task GetApproversAsync_トークンなしの場合は401を返すこと()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/users/approvers", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }


        [Fact]
        public async Task GetApproversAsync_トークンに不正な値が含まれる場合は401を返すこと()
        {
            // Arrange
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid_token");

            // Act
            var response = await client.GetAsync("/api/users/approvers", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetApproversAsync_有効なトークンの場合は承認者情報を返すこと()
        {
            // Arrange
            var client = _factory.CreateClient();

            // 一般ユーザーを登録してログインし、有効なトークンを取得する
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
                registerRequest.loginId,
                password = "Password123"
            };

            var loginResponse = await client.PostAsJsonAsync("/api/auth/login",
                                                             loginRequest,
                                                             cancellationToken: TestContext.Current.CancellationToken);

            var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(loginResult);
            Assert.False(string.IsNullOrEmpty(loginResult.Token));

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.Token);

            // 承認者ユーザーをテスト用DBに登録する
            int approverUserId;
            var approverDisplayName = "Approver User";

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var approver = new User
                {
                    LoginId = $"approver_{Guid.NewGuid():N}",
                    DisplayName = approverDisplayName,
                    PasswordHash = "dummy-password-hash",
                    Role = UserRole.Approver,
                    IsActive = true
                };

                dbContext.Users.Add(approver);
                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

                approverUserId = approver.Id;
            }

            // Act
            var response = await client.GetAsync("/api/users/approvers", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var approvers = await response.Content.ReadFromJsonAsync<List<GetApproverResponse>>(
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.NotNull(approvers);
            Assert.True(approvers.Count > 0);
            Assert.Equal(approverUserId, approvers[0].UserId);
            Assert.Equal(approverDisplayName, approvers[0].DisplayName);
        }

        [Fact]
        public async Task GetApproversAsync_承認者が存在しない場合はNotFoundを返すこと()
        {
            // Arrange
            var client = _factory.CreateClient();

            // 一般ユーザーを登録してログインし、有効なトークンを取得する
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
                registerRequest.loginId,
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
            var response = await client.GetAsync("/api/users/approvers", TestContext.Current.CancellationToken);
            
            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        private sealed class LoginResponse
        {
            public string Token { get; set; } = default!;
        }
    }
}
