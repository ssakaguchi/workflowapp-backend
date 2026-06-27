using WorkflowApp.Api.Domain.Enums;
using WorkflowApp.Api.DTOs.Applications;

namespace WorkflowApp.Api.Services.Interfaces
{
    public interface IApplicationService
    {
        Task<CreateApplicationResponse> CreateAsync(
            CreateApplicationRequest request,
            int userId,
            CancellationToken cancellationToken);

        Task<ApplicationDetailResponse?> GetDetailAsync(int id, int userId, UserRole currentUserRole, CancellationToken cancellationToken);

        Task<bool> DeleteAsync(int id, string userIdClaim, CancellationToken cancellationToken);

        Task<bool> UpdateAsync(int id, UpdateApplicationRequest request, string userIdClaim, CancellationToken cancellationToken);

        Task<bool> UpdateWorkflowStatusAsync(int id, WorkflowStatus status, int currentUserId, CancellationToken cancellationToken);

        Task<PagedResponse<ApplicationListItemResponse>> GetApplicationsAsync(int page, int pageSize, string? status, int userId, CancellationToken cancellationToken);

        Task<PagedResponse<ApplicationListItemResponse>> GetMyApprovalRequestsAsync(int page, int pageSize, int userId, CancellationToken cancellationToken);

        Task<PagedResponse<ApplicationListItemResponse>> GetAdminApplicationsAsync(int page, int pageSize, CancellationToken cancellationToken);
    }
}
