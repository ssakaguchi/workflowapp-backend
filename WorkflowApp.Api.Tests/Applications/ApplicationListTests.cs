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
    public class ApplicationListTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ApplicationListTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Get_認証済みユーザーの場合_自分の申請一覧を返すこと()
        {
            // Arrange
            var client = _factory.CreateClient();

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
                    DisplayName = "テスト申請者",
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

                // テスト申請の作成
                dbContext.Applications.AddRange(
                    new Application
                    {
                        Title = "出張申請",
                        Content = "東京出張の申請です。",
                        Status = WorkflowStatus.Pending,
                        ApplicantUserId = loginUser.Id,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-10)
                    },
                    new Application
                    {
                        Title = "備品購入申請",
                        Content = "キーボード購入の申請です。",
                        Status = WorkflowStatus.Approved,
                        ApplicantUserId = loginUser.Id,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-5)
                    },
                    new Application
                    {
                        Title = "他ユーザーの申請",
                        Content = "これは取得対象外です。",
                        Status = WorkflowStatus.Pending,
                        ApplicantUserId = otherUser.Id,
                        CreatedAt = DateTime.UtcNow
                    });

                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

                token = jwtTokenService.CreateToken(loginUser).Token;
            }

            client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await client.GetAsync("/api/applications/list", TestContext.Current.CancellationToken);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var responseBody = await response.Content.ReadFromJsonAsync<List<ApplicationListItemResponse>>(
                cancellationToken: TestContext.Current.CancellationToken);

            responseBody.Should().NotBeNull();
            responseBody.Should().HaveCount(2);

            responseBody.Select(x => x.Title)
                .Should()
                .BeEquivalentTo(new[] { "出張申請", "備品購入申請" });

            responseBody.Should().OnlyContain(
                x => x.Title == "出張申請" || x.Title == "備品購入申請");

        }

        [Fact]
        public async Task Get_未認証の場合_401Unauthorizedを返すこと()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/applications", TestContext.Current.CancellationToken);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
