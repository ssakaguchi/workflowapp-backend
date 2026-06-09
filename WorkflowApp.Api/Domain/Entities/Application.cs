using WorkflowApp.Api.Domain.Enums;

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
        public WorkflowStatus Status { get; set; } = WorkflowStatus.Pending;
        public int ApplicantUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<ApprovalStep> ApprovalSteps { get; set; } = new();
    }
}
