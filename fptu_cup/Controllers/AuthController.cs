using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using fptu_cup.Common.Enums;
using fptu_cup.Data;
using fptu_cup.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace fptu_cup.Controllers
{
    /// <summary>FR01–FR03: đăng ký TeamManager, đăng nhập (JWT), xem thông tin cá nhân, đổi mật khẩu.</summary>
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ApiControllerBase
    {
        private readonly FptuCupDbContext _db;
        private readonly IConfiguration _config;

        public AuthController(FptuCupDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        /// <summary>Vai trò luôn là TeamManager; Admin và Referee không tự đăng ký được.</summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest req)
        {
            var username = req.Username.Trim();
            var email = req.Email.Trim().ToLowerInvariant();

            if (await _db.Users.AnyAsync(u => u.Username == username))
                return Conflict409("Tên đăng nhập đã tồn tại");
            if (await _db.Users.AnyAsync(u => u.Email == email))
                return Conflict409("Email đã tồn tại");

            var user = new User
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                FullName = req.FullName.Trim(),
                Email = email,
                Role = UserRole.TeamManager,
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return Created201(new { user.Id, user.Username, user.FullName, user.Email, role = user.Role }, "Tạo tài khoản thành công");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest req)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == req.Username.Trim());
            if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
                return Error(401, "Sai tên đăng nhập hoặc mật khẩu");
            if (!user.IsActive)
                return Forbidden403("Tài khoản đang bị khóa");

            var minutes = _config.GetValue("Jwt:ExpiresInMinutes", 60);
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("name", user.FullName),
                new Claim("role", user.Role.ToString()),
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(minutes),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            return Success(new
            {
                accessToken = new JwtSecurityTokenHandler().WriteToken(token),
                expiresIn = minutes * 60,
                user = new { user.Id, user.FullName, role = user.Role },
            }, "Đăng nhập thành công");
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var user = await _db.Users.Where(u => u.Id == CurrentUserId)
                .Select(u => new { u.Id, u.Username, u.FullName, u.Email, role = u.Role, u.IsActive })
                .FirstOrDefaultAsync();
            return user == null ? NotFound404() : Success(user);
        }

        [Authorize]
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest req)
        {
            var user = await _db.Users.FindAsync(CurrentUserId);
            if (user == null) return NotFound404();
            if (!BCrypt.Net.BCrypt.Verify(req.OldPassword, user.PasswordHash))
                return BadRequest400("Mật khẩu cũ không đúng");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
            await _db.SaveChangesAsync();
            return Success(null, "Đổi mật khẩu thành công");
        }

        public class RegisterRequest
        {
            [Required, StringLength(50, MinimumLength = 3)] public string Username { get; set; } = "";
            [Required, MinLength(8, ErrorMessage = "Mật khẩu tối thiểu 8 ký tự")] public string Password { get; set; } = "";
            [Required, StringLength(100)] public string FullName { get; set; } = "";
            [Required, EmailAddress, StringLength(100)] public string Email { get; set; } = "";
        }

        public class LoginRequest
        {
            [Required] public string Username { get; set; } = "";
            [Required] public string Password { get; set; } = "";
        }

        public class ChangePasswordRequest
        {
            [Required] public string OldPassword { get; set; } = "";
            [Required, MinLength(8, ErrorMessage = "Mật khẩu mới tối thiểu 8 ký tự")] public string NewPassword { get; set; } = "";
        }
    }
}
