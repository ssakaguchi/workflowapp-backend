namespace WorkflowApp.Api.CustomException
{
    public class ApprovalPermissionDeniedException : Exception
    {
        public ApprovalPermissionDeniedException()
            : base("この申請を承認する権限がありません。")
        {
        }
    }
}
