using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WorkflowApp.Api.Domain.Entities;
using WorkflowApp.Api.Domain.Enums;
using WorkflowApp.Api.Infrastructure.Seeding;
using WorkflowApp.Api.Tests.Helpers;

namespace WorkflowApp.Api.Tests.Infrastructure.Seeding
{
    public class DataSeederTests
    {
        public DataSeederTests() { }

        [Fact]
        public async Task Applicant01が作成されること()
        {
            // Arrange
            var dbContext = TestDbContextFactory.CreateDbContext();
            var passwordHasher = new PasswordHasher<User>();
            var configuration = new ConfigurationBuilder()
                 .AddInMemoryCollection(new Dictionary<string, string?>
                 {
                     ["SeedUsers:0:LoginId"] = "applicant01",
                     ["SeedUsers:0:Password"] = "ChangeMe_Applicant_123!",
                     ["SeedUsers:0:Role"] = "Applicant",
                 })
                 .Build();
            var seeder = new DataSeeder(dbContext, passwordHasher, configuration);

            // Act
            await seeder.SeedAsync(CancellationToken.None);

            // Assert
            var user = await dbContext.Users.SingleAsync(u => u.LoginId == "applicant01",
                                                         cancellationToken: CancellationToken.None);
            Assert.NotNull(user);
            Assert.Equal(UserRole.Applicant, user.Role);

            Assert.False(string.IsNullOrWhiteSpace(user.PasswordHash));
            Assert.NotEqual("ChangeMe_Applicant_123!", user.PasswordHash);

            var verifyResult = passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                "ChangeMe_Applicant_123!");

            Assert.Equal(PasswordVerificationResult.Success, verifyResult);
        }

        [Fact]
        public async Task Approver01が作成されること()
        {
            // Arrange
            var dbContext = TestDbContextFactory.CreateDbContext();
            var passwordHasher = new PasswordHasher<User>();
            var configuration = new ConfigurationBuilder()
                 .AddInMemoryCollection(new Dictionary<string, string?>
                 {
                     ["SeedUsers:1:LoginId"] = "approver01",
                     ["SeedUsers:1:Password"] = "ChangeMe_Approver_123!",
                     ["SeedUsers:1:Role"] = "Approver",
                 })
                 .Build();
            var seeder = new DataSeeder(dbContext, passwordHasher, configuration);

            // Act
            await seeder.SeedAsync(CancellationToken.None);

            // Assert
            var user = await dbContext.Users.SingleAsync(u => u.LoginId == "approver01",
                                                         cancellationToken: CancellationToken.None);
            Assert.NotNull(user);
            Assert.Equal(UserRole.Approver, user.Role);

            Assert.False(string.IsNullOrWhiteSpace(user.PasswordHash));
            Assert.NotEqual("ChangeMe_Approver_123!", user.PasswordHash);

            var verifyResult = passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                "ChangeMe_Approver_123!");

            Assert.Equal(PasswordVerificationResult.Success, verifyResult);
        }

        [Fact]
        public async Task 既に存在する場合は重複作成されないこと()
        {
            // Arrange
            var dbContext = TestDbContextFactory.CreateDbContext();
            var passwordHasher = new PasswordHasher<User>();
            var configuration = new ConfigurationBuilder()
                 .AddInMemoryCollection(new Dictionary<string, string?>
                 {
                     ["SeedUsers:1:LoginId"] = "approver01",
                     ["SeedUsers:1:Password"] = "ChangeMe_Approver_123!",
                     ["SeedUsers:1:Role"] = "Approver",
                 })
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["SeedUsers:0:LoginId"] = "applicant01",
                    ["SeedUsers:0:Password"] = "ChangeMe_Applicant_123!",
                    ["SeedUsers:0:Role"] = "Applicant",
                })
                 .Build();
            var seeder = new DataSeeder(dbContext, passwordHasher, configuration);

            // あらかじめユーザーを作成しておく
            await seeder.SeedAsync(CancellationToken.None);

            // Act
            // もう一度実行する
            await seeder.SeedAsync(CancellationToken.None);

            // Assert
            var applicantCount = dbContext.Users.Count<User>(x => x.LoginId == "applicant01");
            var approverCount = dbContext.Users.Count<User>(x => x.LoginId == "approver01");

            // 重複作成されていないこと
            Assert.Equal(1, applicantCount);
            Assert.Equal(1, approverCount);
        }
    }
}
