using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkflowApp.Api.Domain.Entities;
using WorkflowApp.Api.Domain.Enums;
using WorkflowApp.Api.DTOs.Applications;
using WorkflowApp.Api.Infrastructure.Data;
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

        [Fact]
        public async Task ステータスが未入力の場合は400BadRequestを返す()
        {
            var client = _factory.CreateClient();
            var token = TestJwtTokenProvider.CreateToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var applicationId = 1; // 存在しないIDを使用
            var request = new UpdateWorkflowStatusRequest
            {
                Status = ""
            };

            var response = await client.PatchAsJsonAsync($"/api/applications/{applicationId}/status",
                                                         request,
                                                         cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task 自分の申請を更新できる()
        {
            // Arrange
            var client = _factory.CreateClient();
            var userId = 1; // テストユーザーID

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", TestJwtTokenProvider.CreateToken(userId.ToString()));

            int applicationId;

            // 事前に更新対象の申請を作成しておく
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var application = new Application
                {
                    ApplicantUserId = userId,
                    Title = "タイトル",
                    Content = "本文",
                    Status = WorkflowStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };

                dbContext.Applications.Add(application);
                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

                applicationId = application.Id;
            }

            var request = new UpdateWorkflowStatusRequest
            {
                Status = "Approved"
            };

            // Act
            var response = await client.PatchAsJsonAsync($"/api/applications/{applicationId}/status", request, cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var verifyScope = _factory.Services.CreateScope();
            var verifyDbContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

            var updatedApplication = await verifyDbContext.Applications
                .FirstOrDefaultAsync(x => x.Id == applicationId, cancellationToken: TestContext.Current.CancellationToken);

            Assert.NotNull(updatedApplication);
            Assert.Equal("タイトル", updatedApplication!.Title);
            Assert.Equal("本文", updatedApplication.Content);
            Assert.Equal(WorkflowStatus.Approved, updatedApplication.Status);
        }

        [Fact]
        public async Task 他人の申請を更新できない()
        {
            // Arrange
            var client = _factory.CreateClient();

            var userId = 1; // テストユーザーID
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", TestJwtTokenProvider.CreateToken(userId.ToString()));

            int applicationId;

            // 事前に他のユーザーの申請を作成しておく
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var application = new Application
                {
                    ApplicantUserId = 999, // 別のユーザーID
                    Title = "他人の申請タイトル",
                    Content = "他人の申請内容",
                    Status = WorkflowStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                dbContext.Applications.Add(application);
                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
                applicationId = application.Id;
            }

            var request = new UpdateWorkflowStatusRequest
            {
                Status = "Approved"
            };

            // Act
            var response = await client.PatchAsJsonAsync($"/api/applications/{applicationId}/status", request, cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task 存在しない申請を更新できない()
        {
            // Arrange
            var client = _factory.CreateClient();

            var userId = 1; // テストユーザーID
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", TestJwtTokenProvider.CreateToken(userId.ToString()));

            var nonExistentApplicationId = 9999; // 存在しない申請ID

            var request = new UpdateWorkflowStatusRequest
            {
                Status = "Approved"
            };

            // Act
            var response = await client.PatchAsJsonAsync($"/api/applications/{nonExistentApplicationId}/status", request, cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task 無効なステータスの場合は400BadRequestを返す()
        {
            var client = _factory.CreateClient();
            var userId = 1;

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", TestJwtTokenProvider.CreateToken(userId.ToString()));

            int applicationId;

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var application = new Application
                {
                    ApplicantUserId = userId,
                    Title = "タイトル",
                    Content = "本文",
                    Status = WorkflowStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };

                dbContext.Applications.Add(application);
                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

                applicationId = application.Id;
            }

            var request = new UpdateWorkflowStatusRequest
            {
                Status = "InvalidStatus"
            };

            var response = await client.PatchAsJsonAsync(
                $"/api/applications/{applicationId}/status",
                request,
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
