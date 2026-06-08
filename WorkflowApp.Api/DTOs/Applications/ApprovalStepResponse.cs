namespace WorkflowApp.Api.DTOs.Applications
{
    public class ApprovalStepResponse
    {
        public int Id { get; set; }
        public int StepOrder { get; set; }
        public int ApproverUserId { get; set; }
        public string ApproverName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? ApprovedAt { get; set; }
        public string Comment { get; set; } = string.Empty;
    }
}
