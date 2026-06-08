namespace WorkflowApp.Api.DTOs.Applications
{
    public class ApplicationDetailResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int ApplicantUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ApprovalStepResponse> ApprovalSteps { get; set; } = new List<ApprovalStepResponse>();
    }
}
