using System.Net;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkflowApp.Api.Domain.Entities;
using WorkflowApp.Api.Infrastructure.Data;
using WorkflowApp.Api.Tests.Helpers;

namespace WorkflowApp.Api.Tests.Applications
{
    public class DeleteApplicationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public DeleteApplicationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }


        [Fact]
        public async Task 未認証の場合は401Unauthorizedを返す()
        {
            // Arrange
            var client = _factory.CreateClient();
            var applicationId = 1; // 存在しないIDを使用

            // Act
            var response = await client.DeleteAsync($"/api/applications/{applicationId}",
                                                        cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task 自分の申請を削除できる()
        {
            // Arrange
            var client = _factory.CreateClient();
            var userId = 1; // テストユーザーID

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", TestJwtTokenProvider.CreateToken(userId.ToString()));

            int applicationId;

            // 事前に削除対象の申請を作成しておく
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var application = new Application
                {
                    ApplicantUserId = userId,
                    Title = "削除対象タイトル",
                    Content = "削除対象本文",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                dbContext.Applications.Add(application);
                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

                applicationId = application.Id;
            }

            // Act
            var response = await client.DeleteAsync($"/api/applications/{applicationId}", cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var verifyScope = _factory.Services.CreateScope();
            var verifyDbContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

            var deletedApplication = await verifyDbContext.Applications
                .FirstOrDefaultAsync(x => x.Id == applicationId, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Null(deletedApplication);
        }

        [Fact]
        public async Task 他人の申請は削除できず404NotFoundを返す()
        {
            // Arrange
            var client = _factory.CreateClient();
            var userId = 1; // テストユーザーID

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", TestJwtTokenProvider.CreateToken(userId.ToString()));

            int applicationId;

            // 事前に削除対象の申請を作成しておく
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var application = new Application
                {
                    ApplicantUserId = userId + 1, // 他人の申請を作成
                    Title = "削除対象タイトル",
                    Content = "削除対象本文",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                dbContext.Applications.Add(application);
                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

                applicationId = application.Id;
            }

            // Act
            var response = await client.DeleteAsync($"/api/applications/{applicationId}", cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task 存在しない申請を削除しようとすると404NotFoundを返す()
        {
            // Arrange
            var client = _factory.CreateClient();
            var userId = 1; // テストユーザーID

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", TestJwtTokenProvider.CreateToken(userId.ToString()));

            var nonExistentApplicationId = 9999; // 存在しないIDを使用
            
            // Act
            var response = await client.DeleteAsync($"/api/applications/{nonExistentApplicationId}", cancellationToken: TestContext.Current.CancellationToken);
            
            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

    }
}
