using WorkflowApp.Api.DTOs.Users;

namespace WorkflowApp.Api.Services.Interfaces
{
    public interface IUserService
    {
        Task<IReadOnlyCollection<GetApproverResponse>> GetApproversAsync(CancellationToken cancellationToken);
    }
}
