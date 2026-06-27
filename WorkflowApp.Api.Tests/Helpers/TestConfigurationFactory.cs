using Microsoft.Extensions.Configuration;

namespace WorkflowApp.Api.Tests.Helpers
{
    public static class TestConfigurationFactory
    {
        public static IConfiguration CreateConfiguration()
        {
            var settings = new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "WorkflowApp",
                ["Jwt:SecretKey"] = "DEVELOPMENT_SECRET_KEY_1234567890",
                ["Jwt:Audience"] = "WorkflowApp.Client",
                ["Jwt:ExpirationMinutes"] = "60"
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();
        }
    }
}
