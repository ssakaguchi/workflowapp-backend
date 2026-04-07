namespace WorkflowApp.Api.DTOs.Applications
{
    public class ApplicationListItemResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
