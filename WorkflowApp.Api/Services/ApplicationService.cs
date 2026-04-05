using WorkflowApp.Api.Domain.Entities;
using WorkflowApp.Api.DTOs.Applications;
using WorkflowApp.Api.Infrastructure.Data;
using WorkflowApp.Api.Services.Interfaces;

namespace WorkflowApp.Api.Services
{
    public class ApplicationService: IApplicationService
    {
        private readonly AppDbContext _dbContext;

        public ApplicationService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// 非同期で新しい申請エンティティを作成し、データベースに保存します。
        /// </summary>
        /// <returns>作成された申請エンティティの一意の識別子。</returns>
        public async Task<CreateApplicationResponse> CreateAsync(CreateApplicationRequest request,
                                           int userId,
                                           CancellationToken cancellationToken)
        {
            var title = request.Title;
            var content = request.Content;

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("件名は必須です。", nameof(request.Title));
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("申請内容は必須です。", nameof(request.Content));
            }

            var application = new Application
            {
                Title = title,
                Content = content,
                ApplicantUserId = userId,
                CreatedAt = DateTime.UtcNow,
            };


            _dbContext.Applications.Add(application);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new CreateApplicationResponse
            {
                Id = application.Id,
                Title = application.Title,
                Content = application.Content,
                Status = application.Status,
                ApplicantUserId = application.ApplicantUserId,
                CreatedAt = application.CreatedAt
            };
        }
    }
}
