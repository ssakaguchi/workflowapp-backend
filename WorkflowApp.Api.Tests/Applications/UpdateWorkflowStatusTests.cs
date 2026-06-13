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
        [InlineData("Pending")]
        [InlineData("Remanded")]
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

                // 承認ステップの作成
                var approvalSteps = CreateApprovalSteps(loginUser.Id, stepOrder: 1, status: ApprovalStepStatus.Pending);

                // テスト申請の作成
                var application = await CreateApplicationAsync(dbContext, "出張申請", "大阪出張", 999, WorkflowStatus.Pending, approvalSteps);

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

                // 承認ステップの作成
                var approvalSteps = CreateApprovalSteps(loginUser.Id, stepOrder: 1, status: ApprovalStepStatus.Pending);

                // テスト申請の作成
                var application = await CreateApplicationAsync(dbContext, "出張申請", "大阪出張", loginUser.Id, WorkflowStatus.Pending, approvalSteps);

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

                // 承認ステップの作成
                var approvalSteps = CreateApprovalSteps(loginUser.Id, stepOrder: 1, status: ApprovalStepStatus.Pending);

                // テスト申請の作成
                var application = await CreateApplicationAsync(dbContext, "出張申請", "大阪出張", 999, WorkflowStatus.Pending, approvalSteps);

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

            // 承認ステップのステータスも更新されていることを確認
            var updatedStep = await verifyDbContext.ApprovalSteps
                .SingleAsync(x => x.ApplicationId == applicationId, TestContext.Current.CancellationToken);

            updatedStep.Status.Should().Be(ApprovalStepStatus.Approved);
            updatedStep.ApprovedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task 承認待ちのワークフローが存在しない場合_400BadRequestを返す()
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

                // 承認ステップの作成 - 承認待ちのステップが存在しない状態を作るため、Rejectedに設定
                var approvalSteps = CreateApprovalSteps(loginUser.Id, stepOrder: 1, status: ApprovalStepStatus.Rejected);

                // テスト申請の作成
                var application = await CreateApplicationAsync(dbContext, "出張申請", "大阪出張", 999, WorkflowStatus.Pending, approvalSteps);

                applicationId = application.Id;
                token = jwtTokenService.CreateToken(loginUser).Token;
            }

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await PatchStatusAsync(client, applicationId, WorkflowStatus.Approved.ToString());

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }


        [Fact]
        public async Task 承認要求があった申請を承認する権限がないユーザーの場合_403Forbiddenを返す()
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
                var loginUser = await CreateUserAsync(dbContext, $"approver-{Guid.NewGuid()}", "ログイン承認者", UserRole.Approver);
                var assignedApprover = await CreateUserAsync(dbContext, $"approver-{Guid.NewGuid()}", "担当承認者", UserRole.Approver);

                // 承認ステップの作成 - 承認要求はあるが、承認者が異なるユーザーを設定
                var approvalSteps = CreateApprovalSteps(assignedApprover.Id, stepOrder: 1, status: ApprovalStepStatus.Pending);

                // テスト申請の作成
                var application = await CreateApplicationAsync(dbContext, "出張申請", "大阪出張", 999, WorkflowStatus.Pending, approvalSteps);

                applicationId = application.Id;
                token = jwtTokenService.CreateToken(loginUser).Token;
            }

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await PatchStatusAsync(client, applicationId, WorkflowStatus.Approved.ToString());

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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
        /// <param name="approvalStep">承認ステップのリスト</param>
        /// <returns>作成された申請オブジェクト</returns>
        private static async Task<Application> CreateApplicationAsync(AppDbContext dbContext,
                                                                      string title,
                                                                      string content,
                                                                      int applicantUserId,
                                                                      WorkflowStatus status,
                                                                      List<ApprovalStep> approvalStep)
        {
            var application = new Application
            {
                Title = title,
                Content = content,
                ApplicantUserId = applicantUserId,
                Status = status,
                ApprovalSteps = approvalStep
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

        /// <summary>
        /// 承認ステップのリストを作成するユーティリティメソッド
        /// </summary>
        /// <param name="approverUserId">承認者のユーザーID</param>
        /// <param name="stepOrder">ステップの順序</param>
        /// <param name="status">承認ステップのステータス</param>
        /// <returns>作成された承認ステップのリスト</returns>
        private static List<ApprovalStep> CreateApprovalSteps(int approverUserId, int stepOrder, ApprovalStepStatus status)
        {
            return new List<ApprovalStep>
                {
                    new ApprovalStep
                    {
                        StepOrder = stepOrder,
                        ApproverUserId = approverUserId,
                        Status = status,
                    }
                };
        }

        #endregion
    }
}
