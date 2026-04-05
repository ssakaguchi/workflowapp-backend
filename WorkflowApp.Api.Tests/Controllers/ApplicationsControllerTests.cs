using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using WorkflowApp.Api.Controllers;
using WorkflowApp.Api.DTOs.Applications;
using WorkflowApp.Api.Services.Interfaces;

namespace WorkflowApp.Api.Tests.Controllers
{
    public class ApplicationsControllerTests
    {
        [Fact]
        public async Task Create_NameIdentifierクレームが存在する場合はCreatedAtActionを返すこと()
        {
            // Arrange
            var service = Substitute.For<IApplicationService>();
            service.CreateAsync(Arg.Any<CreateApplicationRequest>(), 1, Arg.Any<CancellationToken>())
                .Returns(new CreateApplicationResponse { Id = 10 });

            var controller = new ApplicationsController(service);

            var claim = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1")
            };

            var identity = new ClaimsIdentity(claim, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };

            var request = new CreateApplicationRequest
            {
                Title = "出張申請",
                Content = "申請内容"
            };

            // Act
            var result = await controller.Create(request, CancellationToken.None);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal("Create", createdAtActionResult.ActionName);
        }
    }
}
