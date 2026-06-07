using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WorkflowApp.Api.Domain.Entities;
using WorkflowApp.Api.Domain.Enums;
using WorkflowApp.Api.Infrastructure.Data;

namespace WorkflowApp.Api.Infrastructure.Seeding
{

    public class SeedUser
    {
        public string LoginId { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    /// <summary>
    /// 開発確認用データ作成クラス
    /// </summary>
    public class DataSeeder
    {
        private readonly AppDbContext _dbContext;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IConfiguration _configuration;

        public DataSeeder(AppDbContext dbContext,
            IPasswordHasher<User> passwordHasher, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
        }


        /// <summary>
        /// 開発確認用のユーザーデータを作成する
        /// </summary>
        /// <returns></returns>
        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            var seedUsers = _configuration.GetSection("SeedUsers").Get<List<SeedUser>>() ?? [];

            if (!seedUsers.Any()) { return; }

            foreach (var seedUser in seedUsers)
            {
                if (Enum.TryParse<UserRole>(seedUser.Role, out var role))
                {
                    await SeedUserAsync(seedUser.LoginId, seedUser.Password, role, cancellationToken);
                }
            }

                await _dbContext.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// ユーザーデータを作成する
        /// </summary>
        /// <param name="loginId">ログインID</param>
        /// <param name="password">パスワード</param>
        /// <param name="role">ユーザーの役割</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        /// <returns></returns>
        private async Task SeedUserAsync(string loginId, string password, UserRole role, CancellationToken cancellationToken)
        {
            var exists = await _dbContext.Users.AnyAsync(u => u.LoginId == loginId,
                                                         cancellationToken: cancellationToken);

            // 既に同じログインIDのユーザーが存在する場合は作成しない
            if (exists) { return; }

            var user = new User
            {
                LoginId = loginId,
                DisplayName = loginId,
                Role = role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, password);

            await _dbContext.Users.AddAsync(user, cancellationToken);
        }
    }
}
