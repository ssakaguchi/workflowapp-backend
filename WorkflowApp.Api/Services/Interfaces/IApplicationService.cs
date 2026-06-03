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

        Task<List<ApplicationListItemResponse>> GetListAsync(int userId, CancellationToken cancellationToken);

        Task<ApplicationDetailResponse?> GetDetailAsync(int id, int userId, CancellationToken cancellationToken);

        Task<bool> DeleteAsync(int id, string userIdClaim, CancellationToken cancellationToken);

        Task<bool> UpdateAsync(int id, UpdateApplicationRequest request, string userIdClaim, CancellationToken cancellationToken);

        Task<bool> UpdateWorkflowStatusAsync(int id, WorkflowStatus status, int userId, CancellationToken cancellationToken);

        Task<PagedResponse<ApplicationListItemResponse>> GetApplicationsAsync(int page, int pageSize, string? status, int userId, CancellationToken cancellationToken);
    }
}
