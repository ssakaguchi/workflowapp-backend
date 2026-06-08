using WorkflowApp.Api.Domain.Enums;

namespace WorkflowApp.Api.Domain.Entities
{
    /// <summary>
    /// 承認ステップ
    /// </summary>
    public class ApprovalStep
    {
        public int Id { get; set; }

        public int ApplicationId { get; set; }
        public Application Application { get; set; } = null!;

        public int StepOrder { get; set; }

        public int ApproverUserId { get; set; }
        public User ApproverUser { get; set; } = null!;

        public ApprovalStepStatus Status { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public string Comment { get; set; } = string.Empty;
    }
}
