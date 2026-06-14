using Microsoft.EntityFrameworkCore;
using WorkflowApp.Api.Domain.Enums;
using WorkflowApp.Api.DTOs.Users;
using WorkflowApp.Api.Infrastructure.Data;
using WorkflowApp.Api.Services.Interfaces;

namespace WorkflowApp.Api.Services
{
    public class UserService: IUserService
    {
        private readonly AppDbContext _dbContext;

        public UserService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// 承認者の一覧を非同期で取得します。
        /// </summary>
        /// <param name="cancellationToken">キャンセルトークン</param>
        /// <returns>承認者の情報</returns>
        public async Task<IReadOnlyCollection<GetApproverResponse>> GetApproversAsync(CancellationToken cancellationToken)
        {
            return await _dbContext.Users
                .Where(u => u.Role == UserRole.Approver && u.IsActive)
                .Select(u => new GetApproverResponse
                {
                    UserId = u.Id,
                    DisplayName = u.DisplayName
                })
                .ToListAsync(cancellationToken);
        }
    }
}
