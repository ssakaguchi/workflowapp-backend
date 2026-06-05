using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using WorkflowApp.Api.Domain.Entities;
using WorkflowApp.Api.DTOs.Auth;
using WorkflowApp.Api.Services.Interfaces;

namespace WorkflowApp.Api.Infrastructure.Security
{
    /// <summary>
    /// JWTトークンの生成を担当するサービスクラス
    /// </summary>
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration configuration;

        public JwtTokenService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        /// <summary>
        /// トークンを生成するメソッド。ユーザ情報をもとにJWTトークンを作成し、認証レスポンスとして返す。
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public AuthResponse CreateToken(User user)
        {
            // JWTの発行者、対象、シークレットキー、有効期限を設定
            var issuer = configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException("Jwt:Issuerが設定されていません");

            var audience = configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException("Jwt:Audienceが設定されていません");

            var secretKey = configuration["Jwt:SecretKey"]
                ?? throw new InvalidOperationException("Jwt:SecretKeyが設定されていません");

            var expireMinutes = int.TryParse(configuration["Jwt:ExpireMinutes"], out var minutes) ? minutes : 60;

            var expiresAt = DateTime.UtcNow.AddMinutes(expireMinutes);

            // ユーザのクレームを作成
            var claims = new List<Claim>
            {
                new (ClaimTypes.NameIdentifier, user.Id.ToString()),
                new (JwtRegisteredClaimNames.Sub, user.LoginId),
                new (ClaimTypes.Name, user.LoginId),
                new (ClaimTypes.Role, user.Role.ToString()),
                new ("displayName", user.DisplayName),
            };

            // シークレットキーを使用して署名用のセキュリティキーを作成
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            // 署名のためのクレデンシャルを作成
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // JWTトークンを作成
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            // トークンを文字列に変換
            var tokenValue = new JwtSecurityTokenHandler().WriteToken(token);

            // 認証レスポンスを返す
            return new AuthResponse
            {
                Token = tokenValue,
                LoginId = user.LoginId,
                DisplayName = user.DisplayName,
                Role = user.Role.ToString(),
                ExpiresAt = expiresAt
            };
        }
    }
}
