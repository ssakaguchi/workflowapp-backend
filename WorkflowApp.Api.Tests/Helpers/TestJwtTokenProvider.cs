using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace WorkflowApp.Api.Tests.Helpers
{
    public static class TestJwtTokenProvider
    {
        // テスト用のシークレットキー、発行者、対象を定義（Program.csで設定しているものと合わせること）
        private const string SecretKey = "THIS_IS_DEVELOPMENT_ONLY_SECRET_KEY_12345";
        private const string Issuer = "WorkflowApp.Api";
        private const string Audience = "WorkflowApp.Client";

        /// <summary>
        /// テスト用のJWTトークンを生成します。ユーザーIDを指定することができます。
        /// </summary>
        /// <param name="userId">ユーザーID</param>
        /// <returns>JWTトークン</returns>
        public static string CreateToken(string userId = "1")
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim("displayName", "Test User"),
                new Claim(ClaimTypes.Role, "User")
            };

            // シークレットキーを使用して署名資格情報を作成
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));

            // HMAC SHA256アルゴリズムを使用して署名資格情報を作成
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // JWTトークンを作成
            var token = new JwtSecurityToken(
                issuer: Issuer,
                audience: Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
             );

            // トークンを文字列に変換して返す
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
