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
    public class ApplicationListSearchTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ApplicationListSearchTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetApplications_filters_by_title_case_insensitively_and_keeps_status_filter()
        {
            await ResetDatabaseAsync();
            var client = _factory.CreateClient();
            string token;

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
                var applicant = CreateUser("applicant01", "Mika Tanaka", UserRole.Applicant);

                dbContext.Users.Add(applicant);
                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

                dbContext.Applications.AddRange(
                    new Application
                    {
                        Title = "Travel request",
                        Content = "Travel details",
                        Status = WorkflowStatus.Pending,
                        ApplicantUserId = applicant.Id,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-1)
                    },
                    new Application
                    {
                        Title = "Equipment request",
                        Content = "Equipment details",
                        Status = WorkflowStatus.Pending,
                        ApplicantUserId = applicant.Id,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Application
                    {
                        Title = "Travel advance",
                        Content = "Approved travel details",
                        Status = WorkflowStatus.Approved,
                        ApplicantUserId = applicant.Id,
                        CreatedAt = DateTime.UtcNow.AddMinutes(1)
                    },
                    new Application
                    {
                        Title = "ＴＲＡＶＥＬ request",
                        Content = "Full-width travel details",
                        Status = WorkflowStatus.Pending,
                        ApplicantUserId = applicant.Id,
                        CreatedAt = DateTime.UtcNow.AddMinutes(2)
                    });

                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
                token = jwtTokenService.CreateToken(applicant).Token;
            }

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync(
                "/api/applications?status=Pending&searchTerm=%20TRAVEL%20",
                TestContext.Current.CancellationToken);

            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadFromJsonAsync<PagedResponse<ApplicationListItemResponse>>(
                cancellationToken: TestContext.Current.CancellationToken);

            responseBody.Should().NotBeNull();
            responseBody.TotalCount.Should().Be(1);
            responseBody.Items.Should().ContainSingle()
                .Which.Title.Should().Be("Travel request");

            var emptySearchResponse = await client.GetAsync(
                "/api/applications?searchTerm=%20%20",
                TestContext.Current.CancellationToken);
            var emptySearchBody = await emptySearchResponse.Content.ReadFromJsonAsync<PagedResponse<ApplicationListItemResponse>>(
                cancellationToken: TestContext.Current.CancellationToken);

            emptySearchBody.Should().NotBeNull();
            emptySearchBody.TotalCount.Should().Be(4);
        }

        [Fact]
        public async Task GetMyApprovalRequests_filters_by_applicant_name()
        {
            await ResetDatabaseAsync();
            var client = _factory.CreateClient();
            string token;

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
                var applicant = CreateUser("applicant01", "Mika Tanaka", UserRole.Applicant);
                var otherApplicant = CreateUser("applicant02", "Yuki Sato", UserRole.Applicant);
                var approver = CreateUser("approver01", "Approver", UserRole.Approver);

                dbContext.Users.AddRange(applicant, otherApplicant, approver);
                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

                dbContext.Applications.AddRange(
                    new Application
                    {
                        Title = "Travel request",
                        Content = "Travel details",
                        Status = WorkflowStatus.Pending,
                        ApplicantUserId = applicant.Id,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-1),
                        ApprovalSteps =
                        [
                            new ApprovalStep
                            {
                                ApproverUserId = approver.Id,
                                StepOrder = 1,
                                Status = ApprovalStepStatus.Pending
                            }
                        ]
                    },
                    new Application
                    {
                        Title = "Equipment request",
                        Content = "Equipment details",
                        Status = WorkflowStatus.Pending,
                        ApplicantUserId = otherApplicant.Id,
                        CreatedAt = DateTime.UtcNow,
                        ApprovalSteps =
                        [
                            new ApprovalStep
                            {
                                ApproverUserId = approver.Id,
                                StepOrder = 1,
                                Status = ApprovalStepStatus.Pending
                            }
                        ]
                    });

                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
                token = jwtTokenService.CreateToken(approver).Token;
            }

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync(
                "/api/applications/my-approval-requests?searchTerm=MIKA",
                TestContext.Current.CancellationToken);
            var responseBody = await response.Content.ReadFromJsonAsync<PagedResponse<ApplicationListItemResponse>>(
                cancellationToken: TestContext.Current.CancellationToken);

            response.EnsureSuccessStatusCode();
            responseBody.Should().NotBeNull();
            responseBody.TotalCount.Should().Be(1);
            responseBody.Items.Should().ContainSingle()
                .Which.ApplicantDisplayName.Should().Be("Mika Tanaka");
        }

        [Fact]
        public async Task GetAdminApplications_filters_before_paging_and_counts_matches()
        {
            await ResetDatabaseAsync();
            var client = _factory.CreateClient();
            string token;

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
                var admin = CreateUser("admin01", "Admin", UserRole.Admin);
                var applicant = CreateUser("applicant01", "Applicant", UserRole.Applicant);

                dbContext.Users.AddRange(admin, applicant);
                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

                dbContext.Applications.AddRange(
                    new Application
                    {
                        Title = "Match One",
                        Content = "First matching request",
                        Status = WorkflowStatus.Pending,
                        ApplicantUserId = applicant.Id,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-2)
                    },
                    new Application
                    {
                        Title = "Match Two",
                        Content = "Second matching request",
                        Status = WorkflowStatus.Pending,
                        ApplicantUserId = applicant.Id,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-1)
                    },
                    new Application
                    {
                        Title = "Other request",
                        Content = "Non-matching request",
                        Status = WorkflowStatus.Pending,
                        ApplicantUserId = applicant.Id,
                        CreatedAt = DateTime.UtcNow
                    });

                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
                token = jwtTokenService.CreateToken(admin).Token;
            }

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync(
                "/api/applications/admin?page=2&pageSize=1&searchTerm=MATCH",
                TestContext.Current.CancellationToken);
            var responseBody = await response.Content.ReadFromJsonAsync<PagedResponse<ApplicationListItemResponse>>(
                cancellationToken: TestContext.Current.CancellationToken);

            response.EnsureSuccessStatusCode();
            responseBody.Should().NotBeNull();
            responseBody.TotalCount.Should().Be(2);
            responseBody.TotalPages.Should().Be(2);
            responseBody.Items.Should().ContainSingle()
                .Which.Title.Should().Be("Match One");
        }

        private async Task ResetDatabaseAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await dbContext.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
            await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        }

        private static User CreateUser(string loginId, string displayName, UserRole role) => new()
        {
            LoginId = loginId,
            DisplayName = displayName,
            PasswordHash = "dummy-hash",
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}