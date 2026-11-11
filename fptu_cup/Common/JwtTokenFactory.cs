using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using fptu_cup.Entities;
using Microsoft.IdentityModel.Tokens;

namespace fptu_cup.Common
{
    /// <summary>Sinh JWT (claim sub, name, role; hết hạn sau Jwt:ExpiresInMinutes) – dùng cho /api/auth/login và form đăng nhập MVC.</summary>
    public static class JwtTokenFactory
    {
        public static (string Token, int ExpiresInSeconds) Create(IConfiguration config, User user)
        {
            var minutes = config.GetValue("Jwt:ExpiresInMinutes", 60);
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("name", user.FullName),
                new Claim("role", user.Role.ToString()),
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
            var token = new JwtSecurityToken(
                issuer: config["Jwt:Issuer"],
                audience: config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(minutes),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
            return (new JwtSecurityTokenHandler().WriteToken(token), minutes * 60);
        }
    }
}
