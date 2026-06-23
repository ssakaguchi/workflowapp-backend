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
    public class GetMyApprovalRequestsAsyncTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public GetMyApprovalRequestsAsyncTests(CustomWebApplicationFactory factory)
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
            var response = await client.GetAsync("/api/applications/my-approval-requests/", cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task 自分が承認者のPending状態の申請だけが取得できる()
        {
            // Arrange
            await ResetDatabaseAsync();
            var client = _factory.CreateClient();

            string token;

            // テストデータのセットアップ
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

                // テストユーザーの作成
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

                dbContext.Users.AddRange(applicantUser, loginApproverUser, otherApproverUser);
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
                        Title = "他承認ユーザーの申請",
                        Content = "これは取得対象外です。",
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

                token = jwtTokenService.CreateToken(loginApproverUser).Token;
            }

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await client.GetAsync("/api/applications/my-approval-requests/", cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseBody = await response.Content.ReadFromJsonAsync<PagedResponse<ApplicationListItemResponse>>(
                cancellationToken: TestContext.Current.CancellationToken);

            responseBody.Should().NotBeNull();
            responseBody.Items.Should().HaveCount(1);

            responseBody.Items.Select(x => x.Title)
                .Should()
                .BeEquivalentTo(new[] { "出張申請" });

            responseBody.Items.Should().OnlyContain(
                x => x.Title == "出張申請");
        }


        [Fact]
        public async Task Applicantロールの場合は403を返す()
        {
            // Arrange
            await ResetDatabaseAsync();
            var client = _factory.CreateClient();
            string token;

            // テストデータのセットアップ
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

                // テストユーザーの作成
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


                var loginApplicantUser = new User
                {
                    Id = 2,
                    LoginId = "applicant02",
                    DisplayName = "テスト申請者2",
                    PasswordHash = "dummy-hash",
                    Role = UserRole.Applicant,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                dbContext.Users.AddRange(applicantUser, loginApplicantUser);
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
                                ApproverUserId = loginApplicantUser.Id,
                                StepOrder = 1,
                                Status = ApprovalStepStatus.Pending,
                            }
                        }
                    });

                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

                token = jwtTokenService.CreateToken(loginApplicantUser).Token;
            }

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await client.GetAsync("/api/applications/my-approval-requests/", cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ApplicationStatusがApprovedの場合はApprovalStepがPendingでも取得されない()
        {
            // Arrange
            await ResetDatabaseAsync();
            var client = _factory.CreateClient();
            string token;

            // テストデータのセットアップ
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
                // テストユーザーの作成
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

                dbContext.Users.AddRange(applicantUser, loginApproverUser);

                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

                // テスト申請の作成
                dbContext.Applications.AddRange(
                    new Application
                    {
                        Title = "出張申請",
                        Content = "東京出張の申請です。",
                        Status = WorkflowStatus.Approved, // ApplicationStatusがApproved
                        ApplicantUserId = applicantUser.Id,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                        ApprovalSteps = new List<ApprovalStep>
                        {
                            new ApprovalStep
                            {
                                ApproverUserId = loginApproverUser.Id,
                                StepOrder = 1,
                                Status = ApprovalStepStatus.Pending, // ApprovalStepはPending
                            }
                        }
                    });

                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
                token = jwtTokenService.CreateToken(loginApproverUser).Token;
            }

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await client.GetAsync("/api/applications/my-approval-requests/", cancellationToken: TestContext.Current.CancellationToken);

            var responseBody = await response.Content.ReadFromJsonAsync<PagedResponse<ApplicationListItemResponse>>(
                cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // ApplicationStatusがApprovedのため、取得されないことを確認
            responseBody.Should().NotBeNull();
            responseBody.Items.Should().HaveCount(0);
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
