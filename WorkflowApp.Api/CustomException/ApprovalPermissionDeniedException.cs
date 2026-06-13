namespace WorkflowApp.Api.CustomException
{
    public class ApprovalPermissionDeniedException : Exception
    {
        public ApprovalPermissionDeniedException()
            : base("この申請を承認または却下する権限がありません。")
        {
        }
    }
}
