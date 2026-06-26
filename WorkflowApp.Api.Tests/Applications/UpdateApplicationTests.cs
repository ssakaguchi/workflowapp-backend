using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkflowApp.Api.Domain.Entities;
using WorkflowApp.Api.DTOs.Applications;
using WorkflowApp.Api.Infrastructure.Data;
using WorkflowApp.Api.Tests.Helpers;

namespace WorkflowApp.Api.Tests.Applications
{
    public class UpdateApplicationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public UpdateApplicationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task 未認証の場合は401Unauthorizedを返す()
        {
            // Arrange
            var client = _factory.CreateClient();

            var applicationId = 1; // 存在しないIDを使用
            var request = new UpdateApplicationRequest
            {
                Title = "更新された申請タイトル",
                Content = "更新された申請内容"
            };

            // Act
            var response = await client.PutAsJsonAsync($"/api/applications/{applicationId}",
                                                       request,
                                                       cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Title未入力の場合は400BadRequestを返す()
        {
            var client = _factory.CreateClient();
            var token = TestJwtTokenProvider.CreateToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var applicationId = 1; // 存在しないIDを使用
            var request = new UpdateApplicationRequest
            {
                Title = "",
                Content = "更新後本文"
            };

            var response = await client.PutAsJsonAsync($"/api/applications/{applicationId}",
                                                       request,
                                                       cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Content未入力の場合は400BadRequestを返す()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwtTokenProvider.CreateToken());

            var applicationId = 1; // 存在しないIDを使用
            var request = new UpdateApplicationRequest
            {
                Title = "更新後タイトル",
                Content = ""
            };

            var response = await client.PutAsJsonAsync($"/api/applications/{applicationId}", request, cancellationToken: TestContext.Current.CancellationToken);

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
                    Title = "更新前タイトル",
                    Content = "更新前本文",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                dbContext.Applications.Add(application);
                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

                applicationId = application.Id;
            }

            var request = new UpdateApplicationRequest
            {
                Title = "更新後タイトル",
                Content = "更新後本文"
            };

            // Act
            var response = await client.PutAsJsonAsync($"/api/applications/{applicationId}", request, cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var verifyScope = _factory.Services.CreateScope();
            var verifyDbContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

            var updatedApplication = await verifyDbContext.Applications
                .FirstOrDefaultAsync(x => x.Id == applicationId, cancellationToken: TestContext.Current.CancellationToken);

            Assert.NotNull(updatedApplication);
            Assert.Equal("更新後タイトル", updatedApplication!.Title);
            Assert.Equal("更新後本文", updatedApplication.Content);
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
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                dbContext.Applications.Add(application);
                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
                applicationId = application.Id;
            }

            var request = new UpdateApplicationRequest
            {
                Title = "更新後タイトル",
                Content = "更新後本文"
            };

            // Act
            var response = await client.PutAsJsonAsync($"/api/applications/{applicationId}", request, cancellationToken: TestContext.Current.CancellationToken);

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

            var request = new UpdateApplicationRequest
            {
                Title = "更新後タイトル",
                Content = "更新後本文"
            };

            // Act
            var response = await client.PutAsJsonAsync($"/api/applications/{nonExistentApplicationId}", request, cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Adminが申請を更新しようとした場合は403Forbiddenを返すこと()
        {
            // Arrange - Adminユーザーとして認証されたクライアントを作成
            var client = _factory.CreateClient();
            var adminUserId = 2; // 管理者ユーザーID

            client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", TestJwtTokenProvider.CreateToken(adminUserId.ToString(), "Admin"));

            int applicationId;

            // 事前に他のユーザーの申請を作成しておく
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var application = new Application
                {
                    ApplicantUserId = 1, // 別のユーザーID
                    Title = "他人の申請タイトル",
                    Content = "他人の申請内容",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                dbContext.Applications.Add(application);
                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
                applicationId = application.Id;
            }
            var request = new UpdateApplicationRequest
            {
                Title = "更新後タイトル",
                Content = "更新後本文"
            };

            // Act
            var response = await client.PutAsJsonAsync($"/api/applications/{applicationId}", request, cancellationToken: TestContext.Current.CancellationToken);

            // Assert - Adminユーザーは申請の更新が許可されていないため、403 Forbiddenを返すことを確認
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
