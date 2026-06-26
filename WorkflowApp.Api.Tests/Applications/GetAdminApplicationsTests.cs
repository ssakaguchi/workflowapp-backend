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
    public class GetAdminApplicationsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public GetAdminApplicationsTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task 未認証の場合は401Unauthorizedを返す()
        {
            // Arrange
            await ResetDatabaseAsync();
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/applications/admin?page=1&pageSize=10", cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Adminは全申請を取得できる()
        {
            // Arrange - Adminユーザーで認証されたクライアントを作成
            await ResetDatabaseAsync();
            var client = _factory.CreateClient();
            string token;

            // テストデータのセットアップ
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
                // Adminユーザーの作成
                var adminUser = new User
                {
                    Id = 999,
                    LoginId = "admin01",
                    DisplayName = "テスト管理者",
                    PasswordHash = "dummy-hash",
                    Role = UserRole.Admin,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var applicantUser = new User
                {
                    Id = 1,
                    LoginId = "applicant01",
                    DisplayName = "テスト申請者",
                    PasswordHash = "dummy-hash",
                    Role = UserRole.Applicant,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };


                var loginApproverUser = new User
                {
                    Id = 2,
                    LoginId = "approver01",
                    DisplayName = "テスト承認者",
                    PasswordHash = "dummy-hash",
                    Role = UserRole.Approver,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var otherApproverUser = new User
                {
                    Id = 3,
                    LoginId = "other01",
                    DisplayName = "他申請承認ユーザー",
                    PasswordHash = "dummy-hash",
                    Role = UserRole.Approver,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                dbContext.Users.AddRange(adminUser, applicantUser, loginApproverUser, otherApproverUser);
                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

                // テスト申請の作成
                dbContext.Applications.AddRange(
                    new Application
                    {
                        Title = "出張申請",
                        Content = "東京出張の申請です。",
                        Status = WorkflowStatus.Pending,
                        ApplicantUserId = applicantUser.Id,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                        ApprovalSteps = new List<ApprovalStep>
                        {
                            new ApprovalStep
                            {
                                ApproverUserId = loginApproverUser.Id,
                                StepOrder = 1,
                                Status = ApprovalStepStatus.Pending,
                            }
                        }
                    },
                    new Application
                    {
                        Title = "備品購入申請",
                        Content = "キーボード購入の申請です。",
                        Status = WorkflowStatus.Approved,
                        ApplicantUserId = applicantUser.Id,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                        ApprovalSteps = new List<ApprovalStep>
                        {
                            new ApprovalStep
                            {
                                ApproverUserId = loginApproverUser.Id,
                                StepOrder = 1,
                                Status = ApprovalStepStatus.Approved,
                                ApprovedAt = DateTime.UtcNow.AddMinutes(-3)
                            }
                        }
                    },
                    new Application
                    {
                        Title = "決裁申請書",
                        Content = "決裁申請書の内容です。",
                        Status = WorkflowStatus.Pending,
                        ApplicantUserId = applicantUser.Id,
                        CreatedAt = DateTime.UtcNow,
                        ApprovalSteps = new List<ApprovalStep>
                        {
                            new ApprovalStep
                            {
                                ApproverUserId = otherApproverUser.Id,
                                StepOrder = 1,
                                Status = ApprovalStepStatus.Pending,
                            }
                        }
                    });

                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
                // JWTトークンの生成
                token = jwtTokenService.CreateToken(adminUser).Token;
            }

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act - Adminユーザーで全申請を取得
            var response = await client.GetAsync("/api/applications/admin?page=1&pageSize=10", cancellationToken: TestContext.Current.CancellationToken);

            // Assert - レスポンスの検証
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadFromJsonAsync<PagedResponse<ApplicationListItemResponse>>(cancellationToken: TestContext.Current.CancellationToken);

            responseBody.Should().NotBeNull();
            responseBody.Items.Should().HaveCount(3);
            responseBody.Items.Should().Contain(x => x.Title == "出張申請");
            responseBody.Items.Should().Contain(x => x.Title == "備品購入申請");
            responseBody.Items.Should().Contain(x => x.Title == "決裁申請書");
        }

        [Theory]
        [InlineData(UserRole.Applicant)]
        [InlineData(UserRole.Approver)]
        public async Task Admin以外の場合は403Forbiddenを返す(UserRole role)
        {
            // Arrange - Admin以外のユーザーで認証されたクライアントを作成
            await ResetDatabaseAsync();
            var client = _factory.CreateClient();
            string token;

            // テストデータのセットアップ
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

                var applicantUser = new User
                {
                    Id = 1,
                    LoginId = "applicant01",
                    DisplayName = "テスト申請者",
                    PasswordHash = "dummy-hash",
                    Role = role,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };


                var approverUser = new User
                {
                    Id = 2,
                    LoginId = "approver01",
                    DisplayName = "テスト承認者",
                    PasswordHash = "dummy-hash",
                    Role = UserRole.Approver,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                dbContext.Users.AddRange(applicantUser, approverUser);
                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

                // テスト申請の作成
                dbContext.Applications.AddRange(
                    new Application
                    {
                        Title = "出張申請",
                        Content = "東京出張の申請です。",
                        Status = WorkflowStatus.Pending,
                        ApplicantUserId = applicantUser.Id,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                        ApprovalSteps = new List<ApprovalStep>
                        {
                            new ApprovalStep
                            {
                                ApproverUserId = approverUser.Id,
                                StepOrder = 1,
                                Status = ApprovalStepStatus.Pending,
                            }
                        }
                    });

                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

                // JWTトークンの生成
                token = jwtTokenService.CreateToken(applicantUser).Token;
            }

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act - Adminユーザーで全申請を取得
            var response = await client.GetAsync("/api/applications/admin?page=1&pageSize=10", cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        private async Task ResetDatabaseAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await dbContext.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
            await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        }
    }
}
