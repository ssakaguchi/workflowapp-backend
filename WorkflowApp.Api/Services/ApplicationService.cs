using Microsoft.EntityFrameworkCore;
using WorkflowApp.Api.CustomException;
using WorkflowApp.Api.Domain.Entities;
using WorkflowApp.Api.Domain.Enums;
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

            var approver = await _dbContext.Users
                .Where(u => u.Id == request.ApproverUserId
                         && u.Role == UserRole.Approver
                         && u.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (approver is null)
            {
                throw new ApproverNotFoundException();
            }

            var application = new Application
            {
                Title = title,
                Content = content,
                ApplicantUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ApprovalSteps = new List<ApprovalStep>
                {
                    new ApprovalStep
                    {
                        StepOrder = 1,
                        ApproverUserId = approver.Id,
                        Status = ApprovalStepStatus.Pending,
                        Comment = content,
                    }
                 }
            };


            _dbContext.Applications.Add(application);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new CreateApplicationResponse
            {
                Id = application.Id,
                Title = application.Title,
                Content = application.Content,
                Status = application.Status.ToString(),
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
                    Status = x.Status.ToString(),
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
                .Where(a =>
                    a.Id == applicationId &&
                    // 申請者本人または承認者であれば、申請の詳細を取得できるようにします。
                    (
                        a.ApplicantUserId == userId ||
                        a.ApprovalSteps.Any(s => s.ApproverUserId == userId)
                    ))
                    // 申請の詳細を取得する際に、関連する承認ステップも一緒に取得します。
                    .Select(a => new ApplicationDetailResponse
                    {
                        Id = a.Id,
                        Title = a.Title,
                        Content = a.Content,
                        Status = a.Status.ToString(),
                        ApplicantUserId = a.ApplicantUserId,
                        CreatedAt = a.CreatedAt,
                        ApprovalSteps = a.ApprovalSteps
                            .OrderBy(s => s.StepOrder)
                            .Select(s => new ApprovalStepResponse
                            {
                                Id = s.Id,
                                StepOrder = s.StepOrder,
                                ApproverUserId = s.ApproverUserId,
                                ApproverName = s.ApproverUser.DisplayName,
                                Status = s.Status.ToString(),
                                ApprovedAt = s.ApprovedAt,
                                Comment = s.Comment
                            })
                            .ToList()
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
            await _dbContext.SaveChangesAsync(cancellationToken);

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

        /// <summary>
        /// ワークフローのステータスを非同期で更新します。
        /// 承認操作はコントローラー側の認可により、承認者のみ実行できます。
        /// </summary>
        /// <param name="id">更新する申請のID</param>
        /// <param name="status">更新するステータスの情報</param>
        /// <param name="currentUserId">現在のユーザーID</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>更新が成功したかどうか</returns>
        public async Task<bool> UpdateWorkflowStatusAsync(int id,
                                                          WorkflowStatus status,
                                                          int currentUserId,
                                                          CancellationToken cancellationToken)
        {
            var application = await _dbContext.Applications
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (application == null)
            {
                return false;
            }

            if (status is not (WorkflowStatus.Approved or WorkflowStatus.Rejected))
            {
                throw new InvalidOperationException("承認または却下のみ指定できます。");
            }

            var pendingStep = await _dbContext.ApprovalSteps
                .SingleOrDefaultAsync(x =>
                    x.ApplicationId == id &&
                    x.Status == ApprovalStepStatus.Pending,
                    cancellationToken);

            if (pendingStep is null)
            {
                throw new InvalidOperationException("承認待ちのワークフローが存在しません。");
            }

            if (pendingStep.ApproverUserId != currentUserId)
            {
                throw new ApprovalPermissionDeniedException();
            }

            application.Status = status;

            var now = DateTime.UtcNow;

            application.UpdatedAt = now;

            pendingStep.Status = status == WorkflowStatus.Approved
                ? ApprovalStepStatus.Approved
                : ApprovalStepStatus.Rejected;

            pendingStep.ApprovedAt = now;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return true;
        }

        /// <summary>
        /// 申請の一覧をページネーション付きで取得します。
        /// </summary>
        /// <param name="page">取得するページ番号</param>
        /// <param name="pageSize">1ページあたりの件数</param>
        /// <param name="status">フィルタリングするステータス（省略可能）</param>
        /// <param name="userId">フィルタリングするユーザーID</param>    
        /// <param name="cancellationToken">キャンセルトークン</param>
        /// <returns>ページネーションされた申請の一覧</returns>
        public async Task<PagedResponse<ApplicationListItemResponse>> GetApplicationsAsync(int page, int pageSize, string? status, int userId, CancellationToken cancellationToken)
        {
            // クエリの初期化
            var query = _dbContext.Applications
                .Where(x => x.ApplicantUserId == userId ||
                            x.ApprovalSteps.Any(s => s.ApproverUserId.Equals(userId)))
                .AsQueryable();

            // 無効なステータスが指定された場合は、フィルタリングを行わない
            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<WorkflowStatus>(status.Trim(), ignoreCase: true, out var parsedStatus))
            {
                query = query.Where(x => x.Status == parsedStatus);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            // クエリにページネーションとソートを適用し、必要なフィールドのみを選択してリストを取得
            var items = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ApplicationListItemResponse
                {
                    Id = x.Id,
                    Title = x.Title,
                    Status = x.Status.ToString(),
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return new PagedResponse<ApplicationListItemResponse>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }


        /// <summary>
        /// 承認者が自分に割り当てられた申請の一覧をページネーション付きで取得します。
        /// </summary>
        /// <param name="page">取得するページ番号</param>
        /// <param name="pageSize">1ページあたりの件数</param>
        /// <param name="userId">フィルタリングするユーザーID</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        /// <returns>ページネーションされた承認リクエストの一覧</returns>
        public async Task<PagedResponse<ApplicationListItemResponse>> GetMyApprovalRequestsAsync(int page, int pageSize, int userId, CancellationToken cancellationToken)
        {
            // クエリの初期化
            var query = _dbContext.Applications
                .AsNoTracking()
                .Where(a => 
                  a.Status == WorkflowStatus.Pending && 
                  a.ApprovalSteps.Any(s =>
                      s.ApproverUserId == userId &&
                      s.Status == ApprovalStepStatus.Pending));

            var totalCount = await query.CountAsync(cancellationToken);

            // クエリにページネーションとソートを適用し、必要なフィールドのみを選択してリストを取得
            var items = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ApplicationListItemResponse
                {
                    Id = x.Id,
                    Title = x.Title,
                    Status = x.Status.ToString(),
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return new PagedResponse<ApplicationListItemResponse>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }
    }
}
