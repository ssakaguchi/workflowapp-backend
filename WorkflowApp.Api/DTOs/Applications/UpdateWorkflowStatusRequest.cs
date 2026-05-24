namespace WorkflowApp.Api.DTOs.Applications
{
    public class UpdateWorkflowStatusRequest
    {
        /// <summary>
        /// ワークフローのステータスを表す文字列。(ex."Pending", "Approved", "Rejected" etc.)
        /// </summary>
        public string Status { get; set; } = string.Empty;
    }
}
