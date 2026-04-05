namespace WorkflowApp.Api.Domain.Entities
{
    /// <summary>
    /// ワークフローの申請
    /// </summary>
    public class Application
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public int ApplicantUserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
