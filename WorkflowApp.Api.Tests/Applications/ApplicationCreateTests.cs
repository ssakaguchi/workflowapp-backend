using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkflowApp.Api.Domain.Entities;
using WorkflowApp.Api.Domain.Enums;
using WorkflowApp.Api.DTOs.Applications;
using WorkflowApp.Api.Infrastructure.Data;
using WorkflowApp.Api.Services.Interfaces;
using WorkflowApp.Api.Tests.Helpers;

namespace WorkflowApp.Api.Tests.Applications
{
    public class ApplicationCreateTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ApplicationCreateTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;

            // 各テストの前にDBをクリーンアップ
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            dbContext.Applications.RemoveRange(dbContext.Applications);
            dbContext.Users.RemoveRange(dbContext.Users);
            dbContext.SaveChanges();
        }


        [Fact]
        public async Task Post_認証済みユーザーが有効な申請を送信した場合_201Createdを返しDBに保存されること()
        {
            // Arrange
            var client = _factory.CreateClient();

            int userId;
            int approverId;
            string token;

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

                // テストユーザーの作成
                var user = new User
                {
                    LoginId = $"applicant-{Guid.NewGuid()}",
                    DisplayName = "テスト申請者",
                    PasswordHash = "dummy-hash",
                    Role = UserRole.Applicant,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // テスト承認者の作成
                var approver = new User
                {
                    LoginId = $"approver-{Guid.NewGuid()}",
                    DisplayName = "テスト承認者",
                    PasswordHash = "dummy-hash",
                    Role = UserRole.Approver,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                dbContext.Users.Add(user);
                dbContext.Users.Add(approver);
                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

                userId = user.Id;
                approverId = approver.Id;

                token = jwtTokenService.CreateToken(user).Token;
            }

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var request = new
            {
                Title = "テスト申請",
                Content = "これはテストの申請です。",
                ApproverUserId = approverId
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/applications",
                                                        request,
                                                        cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var responseBody = await response.Content.ReadFromJsonAsync<CreateApplicationResponse>(cancellationToken: TestContext.Current.CancellationToken);

            responseBody.Should().NotBeNull();
            responseBody!.Title.Should().Be(request.Title);
            responseBody.Content.Should().Be(request.Content);

            using var verfyScope = _factory.Services.CreateScope();
            var verifyDbContext = verfyScope.ServiceProvider.GetRequiredService<AppDbContext>();

            var savedApplication = verifyDbContext.Applications
                .Include(a => a.ApprovalSteps)
                .Single();

            savedApplication.Title.Should().Be(request.Title);
            savedApplication.Content.Should().Be(request.Content);
            savedApplication.ApplicantUserId.Should().Be(userId);
            savedApplication.Status.Should().Be(WorkflowStatus.Pending);


            savedApplication.ApprovalSteps.Should().HaveCount(1);

            var approvalStep = savedApplication.ApprovalSteps.Single();
            approvalStep.StepOrder.Should().Be(1);
            approvalStep.Status.Should().Be(ApprovalStepStatus.Pending);
            approvalStep.ApproverUserId.Should().Be(approverId);
        }

        [Fact]
        public async Task Post_認証されていないユーザーが申請を送信した場合_401Unauthorizedを返すこと()
        {
            // Arrange
            var client = _factory.CreateClient();
            var request = new
            {
                Title = "テスト申請",
                Content = "これはテストの申請です。",
                ApproverUserId = 1
            };
            // Act
            var response = await client.PostAsJsonAsync("/api/applications",
                                                        request,
                                                        cancellationToken: TestContext.Current.CancellationToken);
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Post_Title未入力の場合_400BadRequestを返すこと()
        {
            // Arrange
            var client = _factory.CreateClient();
            string token = await this.CreateAccessTokenAsync();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var request = new
            {
                Title = "", // タイトルが空
                Content = "これはテストの申請です。",
                ApproverUserId = 1
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/applications",
                                                        request,
                                                        cancellationToken: TestContext.Current.CancellationToken);
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // DBに保存されていないことを確認
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Applications.Should().BeEmpty();
        }



        [Fact]
        public async Task Post_Content未入力の場合_400BadRequestを返すこと()
        {
            // Arrange
            var client = _factory.CreateClient();
            string token = await this.CreateAccessTokenAsync();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var request = new
            {
                Title = "テスト申請",
                Content = "", // コンテンツが空
                ApproverUserId = 1
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/applications",
                                                        request,
                                                        cancellationToken: TestContext.Current.CancellationToken);
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // DBに保存されていないことを確認
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Applications.Should().BeEmpty();
        }


        [Fact]
        public async Task Post_Titleが100文字を超える場合_400BadRequestを返すこと()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = await CreateAccessTokenAsync();

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var request = new
            {
                Title = new string('あ', 101),   // タイトルが101文字
                Content = "4月10日の東京出張について申請します。",
                ApproverUserId = 1
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/applications",
                                                        request,
                                                        cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // DBに保存されていないことを確認
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Applications.Should().BeEmpty();
        }

        [Fact]
        public async Task Post_Contentが2000文字を超える場合_400BadRequestを返すこと()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = await CreateAccessTokenAsync();

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var request = new
            {
                Title = "出張申請",
                Content = new string('あ', 2001), // コンテンツが2001文字
                ApproverUserId = 1
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/applications",
                                                        request,
                                                        TestContext.Current.CancellationToken);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // DBに保存されていないことを確認
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Applications.Should().BeEmpty();
        }

        [Fact]
        public async Task Post_承認者IDが未指定の場合_400BadRequestを返すこと()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = await CreateAccessTokenAsync();

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var request = new
            {
                Title = "出張申請",
                Content = "4月10日の東京出張について申請します。"
                // ApproverIDを未指定
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/applications",
                                                        request,
                                                        cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // DBに保存されていないことを確認
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Applications.Should().BeEmpty();
        }


        /// <summary>
        /// トークンを作成するヘルパーメソッド
        /// </summary>
        /// <returns></returns>
        private async Task<string> CreateAccessTokenAsync()
        {
            int userId;
            string token;
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
                // テストユーザーの作成
                var user = new User
                {
                    LoginId = $"applicant-{Guid.NewGuid()}",
                    DisplayName = "テスト申請者",
                    PasswordHash = "dummy-hash",
                    Role = UserRole.Applicant,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                dbContext.Users.Add(user);
                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
                userId = user.Id;
                token = jwtTokenService.CreateToken(user).Token;
            }

            return token;
        }
    }
}
