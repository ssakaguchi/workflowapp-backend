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
    public class UpdateWorkflowStatusTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public UpdateWorkflowStatusTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task 未認証の場合は401Unauthorizedを返す()
        {
            // Arrange
            var client = _factory.CreateClient();

            var applicationId = 1; // 存在しないIDを使用
            var request = new UpdateWorkflowStatusRequest
            {
                Status = WorkflowStatus.Approved.ToString()
            };

            // Act
            var response = await client.PatchAsJsonAsync($"/api/applications/{applicationId}/status", request, cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("")]
        [InlineData("InvalidStatus")]
        public async Task 無効なステータスの場合は400BadRequestを返す(string invalidStatus)
        {
            // Arrange
            var client = _factory.CreateClient();

            int applicationId;
            string token;

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

                // テストユーザーの作成
                var loginUser = await CreateUserAsync(dbContext, "approver01", "テスト承認者", UserRole.Approver);

                // テスト申請の作成
                var application = await CreateApplicationAsync(dbContext, "出張申請", "大阪出張", 999, WorkflowStatus.Pending);

                applicationId = application.Id;
                token = jwtTokenService.CreateToken(loginUser).Token;
            }

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await PatchStatusAsync(client, applicationId, invalidStatus);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task 存在しない申請IDを指定した場合_404NotFoundを返す()
        {
            // Arrange
            var client = _factory.CreateClient();

            string token;

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

                // テストユーザーの作成
                var loginUser = await CreateUserAsync(dbContext, "approver01", "テスト承認者", UserRole.Approver);

                token = jwtTokenService.CreateToken(loginUser).Token;
            }

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            const int applicationId = 9999; // 存在しない申請ID

            // Act
            var response = await PatchStatusAsync(client, applicationId, WorkflowStatus.Approved.ToString());

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Applicantがステータス更新しようとした場合_403Forbiddenを返すこと()
        {
            // Arrange
            var client = _factory.CreateClient();

            int applicationId;
            string token;

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

                // テストユーザーの作成
                var loginUser = await CreateUserAsync(dbContext, "applicant01", "テスト申請者", UserRole.Applicant);

                // テスト申請の作成
                var application = await CreateApplicationAsync(dbContext, "出張申請", "大阪出張", loginUser.Id, WorkflowStatus.Pending);

                applicationId = application.Id;
                token = jwtTokenService.CreateToken(loginUser).Token;
            }

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await PatchStatusAsync(client, applicationId, WorkflowStatus.Approved.ToString());

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Approverがステータス更新した場合_成功を返しDBのステータスが更新されること()
        {
            // Arrange
            var client = _factory.CreateClient();

            int applicationId;
            string token;

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

                // テストユーザーの作成
                var loginUser = await CreateUserAsync(dbContext, "approver01", "テスト承認者", UserRole.Approver);

                // テスト申請の作成
                var application = await CreateApplicationAsync(dbContext, "出張申請", "大阪出張", 999, WorkflowStatus.Pending);

                applicationId = application.Id;
                token = jwtTokenService.CreateToken(loginUser).Token;
            }

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await PatchStatusAsync(client, applicationId, WorkflowStatus.Approved.ToString());

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var verifyScope = _factory.Services.CreateScope();
            var verifyDbContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

            var updatedApplication = await verifyDbContext.Applications
                .FirstOrDefaultAsync(x => x.Id == applicationId, cancellationToken: TestContext.Current.CancellationToken);

            Assert.NotNull(updatedApplication);
            Assert.Equal(WorkflowStatus.Approved, updatedApplication.Status);
        }

        #region ユーティリティメソッド

        /// <summary>
        /// テストユーザーを作成するユーティリティメソッド
        /// </summary>
        /// <param name="dbContext">データベースコンテキスト</param>
        /// <param name="loginId">ログインID</param>
        /// <param name="displayName">表示名</param>
        /// <param name="role">ユーザーの役割</param>
        /// <returns>作成されたユーザー</returns>
        private async Task<User> CreateUserAsync(AppDbContext dbContext, string loginId, string displayName, UserRole role)
        {
            var loginUser = new User
            {
                LoginId = loginId,
                DisplayName = displayName,
                PasswordHash = Guid.NewGuid().ToString(),
                Role = role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.Users.Add(loginUser);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            return loginUser;
        }

        /// <summary>
        /// テスト用の申請を作成するユーティリティメソッド
        /// </summary>
        /// <param name="dbContext">データベースコンテキスト</param>
        /// <param name="title">申請のタイトル</param>
        /// <param name="content">申請の内容</param>
        /// <param name="applicantUserId">申請者のユーザーID</param>
        /// <param name="status">申請のステータス</param>
        /// <returns>作成された申請オブジェクト</returns>
        private static async Task<Application> CreateApplicationAsync(AppDbContext dbContext,
                                                                      string title,
                                                                      string content,
                                                                      int applicantUserId,
                                                                      WorkflowStatus status)
        {
            var application = new Application
            {
                Title = title,
                Content = content,
                ApplicantUserId = applicantUserId,
                Status = status
            };

            dbContext.Applications.Add(application);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            return application;
        }

        /// <summary>
        /// 申請のステータスを更新するためのユーティリティメソッド
        /// </summary>
        /// <param name="client">HTTPクライアント</param>
        /// <param name="applicationId">申請ID</param>
        /// <param name="status">更新するステータス</param>
        /// <returns>HTTPレスポンスメッセージ</returns>
        private static async Task<HttpResponseMessage> PatchStatusAsync(HttpClient client, int applicationId, string status)
        {
            var request = new UpdateWorkflowStatusRequest
            {
                Status = status,
            };

            return await client.PatchAsJsonAsync(
                $"/api/applications/{applicationId}/status",
                request,
                TestContext.Current.CancellationToken);
        }

        #endregion
    }
}
