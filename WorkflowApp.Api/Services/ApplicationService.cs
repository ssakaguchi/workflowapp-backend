using Microsoft.EntityFrameworkCore;
using WorkflowApp.Api.Domain.Entities;
using WorkflowApp.Api.DTOs.Applications;
using WorkflowApp.Api.Infrastructure.Data;
using WorkflowApp.Api.Services.Interfaces;

namespace WorkflowApp.Api.Services
{
    public class ApplicationService : IApplicationService
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

        /// <summary>
        /// 申請のリストを非同期で取得します。ユーザーIDに基づいて、申請者が作成した申請のみを返します。
        /// </summary>
        /// <param name="userId">申請者のユーザーID</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>申請のリスト</returns>
        public async Task<List<ApplicationListItemResponse>> GetListAsync(
            int userId,
            CancellationToken cancellationToken)
        {
            return await _dbContext.Applications
                .Where(x => x.ApplicantUserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new ApplicationListItemResponse
                {
                    Id = x.Id,
                    Title = x.Title,
                    Status = x.Status,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 申請の詳細を非同期で取得します。
        /// </summary>
        /// <param name="applicationId">取得する申請のID</param>
        /// <param name="userId">申請者のユーザーID</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>申請の詳細情報</returns>
        public async Task<ApplicationDetailResponse?> GetDetailAsync(int applicationId,
                                                               int userId,
                                                               CancellationToken cancellationToken)
        {
            return await _dbContext.Applications
                .Where(x => x.Id == applicationId && x.ApplicantUserId == userId)
                .Select(x => new ApplicationDetailResponse
                {
                    Id = x.Id,
                    Title = x.Title,
                    Content = x.Content,
                    Status = x.Status,
                    ApplicantUserId = x.ApplicantUserId,
                    CreatedAt = x.CreatedAt
                })
                // 必ず1件か0件の結果が返ることを期待しているため、SingleOrDefaultAsyncを使用しています。
                .SingleOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// 申請を非同期で削除します。削除は申請者本人のみが行えるように、ユーザーIDを確認します。
        /// </summary>
        /// <param name="id">削除する申請のID</param>
        /// <param name="userIdClaim">申請者のユーザーID</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>削除が成功したかどうか</returns>
        public async Task<bool> DeleteAsync(int id,
                                            string userIdClaim,
                                            CancellationToken cancellationToken)
        {
            var application = await _dbContext.Applications
                .FirstOrDefaultAsync(x => x.Id == id
                && x.ApplicantUserId.ToString() == userIdClaim, cancellationToken);

            if (application is null)
            {
                return false;
            }

            _dbContext.Applications.Remove(application);
            await _dbContext.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// 申請を非同期で更新します。更新は申請者本人のみが行えるように、ユーザーIDを確認します。
        /// </summary>
        /// <param name="id">更新する申請のID</param>
        /// <param name="request">更新する申請の情報</param>
        /// <param name="userIdClaim">申請者のユーザーID</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>更新が成功したかどうか</returns>
        public async Task<bool> UpdateAsync(int id, UpdateApplicationRequest request, string userIdClaim, CancellationToken cancellationToken)
        {
            var application = await _dbContext.Applications.FirstOrDefaultAsync(x => x.Id == id && x.ApplicantUserId.ToString() == userIdClaim,
                                                                                cancellationToken);
            if (application == null)
            {
                return false;
            }

            application.Title = request.Title;
            application.Content = request.Content;
            application.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
