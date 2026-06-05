using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WorkflowApp.Api.Domain.Entities;
using WorkflowApp.Api.Infrastructure.Data;
using WorkflowApp.Api.DTOs.Auth;
using WorkflowApp.Api.Services.Interfaces;
using WorkflowApp.Api.Domain.Enums;

namespace WorkflowApp.Api.Services
{
    /// <summary>
    /// ユーザー認証および登録機能を提供する
    /// </summary>
    /// <remarks>
    /// このサービスはユーザーの登録と認証を担当し、JWTトークンの発行も行います。
    /// </remarks>
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _dbContext;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public AuthService(AppDbContext dbContext, IJwtTokenService jwtTokenService)
        {
            _dbContext = dbContext;
            _jwtTokenService = jwtTokenService;
        }

        /// <summary>
        /// 非同期操作として、新しいユーザーを登録します。
        /// </summary>
        /// <param name="request">登録するユーザーの情報を含むリクエスト</param>
        /// <param name="cancellationToken">操作のキャンセルを通知するためのトークン</param>
        /// <returns>登録処理の非同期操作を表すタスク。</returns>
        /// <exception cref="InvalidOperationException">同じLoginIdのユーザーが既に存在する場合にスローされます。</exception>
        public async Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
        {
            var exists = await _dbContext.Users
            .AnyAsync(x => x.LoginId == request.LoginId, cancellationToken);

            if (exists)
            {
                throw new InvalidOperationException("同じLoginIdのユーザーが既に存在します。");
            }

            var user = new User
            {
                LoginId = request.LoginId,
                DisplayName = request.DisplayName,
                Role = UserRole.Applicant,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// ログイン情報をもとにユーザーを認証し、成功時に認証情報を返す
        /// </summary>
        /// <remarks>
        /// 認証成功時に古いパスワードハッシュは自動更新されます。
        /// ユーザーが存在しない・無効・パスワード不一致の場合はnullを返します。
        /// </remarks>
        /// <param name="request">ログインIDとパスワードを含むリクエスト（null不可）</param>
        /// <param name="cancellationToken">非同期処理のキャンセル用トークン</param>
        /// <returns>成功時は認証情報、失敗時は null</returns>
        public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            // ユーザーの取得
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(
                    x => x.LoginId == request.LoginId && x.IsActive,
                    cancellationToken);

            if (user is null)
            {
                return null;
            }

            // パスワードの検証
            var verifyResult = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password);

            if (verifyResult == PasswordVerificationResult.Failed)
            {
                return null;
            }

            // パスワードのハッシュアルゴリズムが古い場合は再ハッシュして保存
            if (verifyResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
                user.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return _jwtTokenService.CreateToken(user);
        }
    }
}
