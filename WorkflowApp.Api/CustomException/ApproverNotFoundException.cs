namespace WorkflowApp.Api.CustomException
{
    public class ApproverNotFoundException : Exception
    {
        public ApproverNotFoundException()
            : base("承認者が未登録のため、申請を作成できません。")
        {
        }
    }
}
