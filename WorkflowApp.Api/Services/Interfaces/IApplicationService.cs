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
    }
}
