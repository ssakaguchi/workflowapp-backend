namespace WorkflowApp.Api.DTOs.Auth
{
    public class MeResponse
    {
        public int UserId { get; init; }
        public string LoginId { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
