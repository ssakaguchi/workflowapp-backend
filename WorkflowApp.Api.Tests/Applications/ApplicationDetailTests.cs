using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WorkflowApp.Api.Domain.Entities;
using WorkflowApp.Api.Domain.Enums;
using WorkflowApp.Api.DTOs.Applications;
using WorkflowApp.Api.Infrastructure.Data;
using WorkflowApp.Api.Services.Interfaces;
using WorkflowApp.Api.Tests.Helpers;

namespace WorkflowApp.Api.Tests.Applications
{
    public class ApplicationDetailTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ApplicationDetailTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Get_認証済みユーザーの場合_自分の申請の詳細を返すこと()
        {
            // Arrange
            var client = _factory.CreateClient();

            int applicationId = 1; // テスト用の申請ID
            string token;


            // テストデータのセットアップ
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

                // テストユーザーの作成
                var loginUser = new User
                {
                    LoginId = "applicant01",
                    DisplayName = "ログインユーザー",
                    PasswordHash = "dummy-hash",
                    Role = UserRole.Applicant,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                dbContext.Users.Add(loginUser);
                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

                // テスト申請の作成 
                var application = new Application
                {
                    Title = "テスト申請",
                    Content = "これはテスト申請の説明です。",
                    Status = WorkflowStatus.Pending,
                    ApplicantUserId = loginUser.Id,
                    CreatedAt = DateTime.UtcNow
                };

                dbContext.Applications.Add(application);
                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

                applicationId = application.Id;

                // JWTトークンの生成
                token = jwtTokenService.CreateToken(loginUser).Token;
            }

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await client.GetAsync($"/api/applications/{applicationId}", TestContext.Current.CancellationToken);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);


            var responseBody = await response.Content.ReadFromJsonAsync<ApplicationDetailResponse>(cancellationToken: TestContext.Current.CancellationToken);

            responseBody.Should().NotBeNull();
            responseBody.Id.Should().Be(applicationId);
            responseBody.Title.Should().Be("テスト申請");
            responseBody.Content.Should().Be("これはテスト申請の説明です。");
            responseBody.Status.Should().Be("Pending");

        }

        [Fact]
        public async Task Get_認証済みユーザーが他人の申請を指定した場合_404NotFoundを返すこと()
        {
            // Arrange
            var client = _factory.CreateClient();

            int otherUsersApplicationId;
            string token;

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

                var loginUser = new User
                {
                    LoginId = "applicant01",
                    DisplayName = "ログインユーザー",
                    PasswordHash = "dummy-hash",
                    Role = UserRole.Applicant,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var otherUser = new User
                {
                    LoginId = "other01",
                    DisplayName = "他ユーザー",
                    PasswordHash = "dummy-hash",
                    Role = UserRole.Applicant,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                dbContext.Users.AddRange(loginUser, otherUser);
                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

                var otherUsersApplication = new Application
                {
                    Title = "他ユーザーの申請",
                    Content = "これは取得不可です。",
                    Status = WorkflowStatus.Pending,
                    ApplicantUserId = otherUser.Id,
                    CreatedAt = DateTime.UtcNow
                };

                dbContext.Applications.Add(otherUsersApplication);
                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

                otherUsersApplicationId = otherUsersApplication.Id;
                token = jwtTokenService.CreateToken(loginUser).Token;
            }

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await client.GetAsync($"/api/applications/{otherUsersApplicationId}", TestContext.Current.CancellationToken);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Get_未認証の場合_401Unauthorizedを返すこと()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/applications/1", TestContext.Current.CancellationToken);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        }

        [Fact]
        public async Task Adminは他人の申請詳細を取得できること()
        {
            // Arrange - 管理者ユーザーを作成し、他人の申請を作成しておく
            var client = _factory.CreateClient();
            int otherUsersApplicationId;
            string token;

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
                var adminUser = new User
                {
                    LoginId = "admin01",
                    DisplayName = "管理者ユーザー",
                    PasswordHash = "dummy-hash",
                    Role = UserRole.Admin,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var otherUser = new User
                {
                    LoginId = "other01",
                    DisplayName = "他ユーザー",
                    PasswordHash = "dummy-hash",
                    Role = UserRole.Applicant,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                dbContext.Users.AddRange(adminUser, otherUser);
                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
                var otherUsersApplication = new Application
                {
                    Title = "他ユーザーの申請",
                    Content = "これは管理者が取得可能です。",
                    Status = WorkflowStatus.Pending,
                    ApplicantUserId = otherUser.Id,
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.Applications.Add(otherUsersApplication);
                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
                otherUsersApplicationId = otherUsersApplication.Id;
                token = jwtTokenService.CreateToken(adminUser).Token;
            }

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Act - 管理者ユーザーで他人の申請詳細を取得
            var response = await client.GetAsync($"/api/applications/{otherUsersApplicationId}", TestContext.Current.CancellationToken);

            // Assert - 管理者ユーザーは他人の申請詳細を取得できること
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseBody =
                await response.Content.ReadFromJsonAsync<ApplicationDetailResponse>(cancellationToken: TestContext.Current.CancellationToken);
            responseBody.Should().NotBeNull();
            responseBody.Id.Should().Be(otherUsersApplicationId);
            responseBody.Title.Should().Be("他ユーザーの申請");
        }

        [Fact]
        public async Task Applicantは他人の申請詳細を取得できないこと()
        {
            // Arrange - 他人の申請を作成しておく
            var client = _factory.CreateClient();
            int otherUsersApplicationId;
            string token;
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
                var loginUser = new User
                {
                    LoginId = "applicant01",
                    DisplayName = "ログインユーザー",
                    PasswordHash = "dummy-hash",
                    Role = UserRole.Applicant,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                var otherUser = new User
                {
                    LoginId = "other01",
                    DisplayName = "他ユーザー",
                    PasswordHash = "dummy-hash",
                    Role = UserRole.Applicant,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                dbContext.Users.AddRange(loginUser, otherUser);
                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
                var otherUsersApplication = new Application
                {
                    Title = "他ユーザーの申請",
                    Content = "これは取得不可です。",
                    Status = WorkflowStatus.Pending,
                    ApplicantUserId = otherUser.Id,
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.Applications.Add(otherUsersApplication);
                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
                otherUsersApplicationId = otherUsersApplication.Id;
                token = jwtTokenService.CreateToken(loginUser).Token;
            }

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act - 他人の申請詳細を取得しようとする
            var response =
                await client.GetAsync($"/api/applications/{otherUsersApplicationId}", TestContext.Current.CancellationToken);

            // Assert - 他人の申請詳細は取得できないこと
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Approverは自分に回付されていない申請詳細を取得できないこと()
        {
            // Arrange - 他人の申請を作成しておく
            var client = _factory.CreateClient();
            int otherUsersApplicationId;
            string token;
            
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
                var approverUser = new User
                {
                    LoginId = "approver01",
                    DisplayName = "承認者ユーザー",
                    PasswordHash = "dummy-hash",
                    Role = UserRole.Approver,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                var otherUser = new User
                {
                    LoginId = "other01",
                    DisplayName = "他ユーザー",
                    PasswordHash = "dummy-hash",
                    Role = UserRole.Applicant,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                dbContext.Users.AddRange(approverUser, otherUser);
                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
                var otherUsersApplication = new Application
                {
                    Title = "他ユーザーの申請",
                    Content = "これは取得不可です。",
                    Status = WorkflowStatus.Pending,
                    ApplicantUserId = otherUser.Id,
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.Applications.Add(otherUsersApplication);
                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
                otherUsersApplicationId = otherUsersApplication.Id;
                token = jwtTokenService.CreateToken(approverUser).Token;
            }
            
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act - 他人の申請詳細を取得しようとする
            var response = await client.GetAsync($"/api/applications/{otherUsersApplicationId}", TestContext.Current.CancellationToken);

            // Assert - 他人の申請詳細は取得できないこと
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
