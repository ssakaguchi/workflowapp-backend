namespace WorkflowApp.Api.Domain.Enums
{
    public enum WorkflowStatus
    {
        Pending = 0,    // 申請が提出された状態
        Approved = 1,   // 申請が承認された状態
        Rejected = 2,   // 申請が拒否された状態
        Remanded = 3    // 申請が差し戻された状態
    }
}
