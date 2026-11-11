using System.ComponentModel.DataAnnotations;
using fptu_cup.Common;
using fptu_cup.Common.Enums;
using fptu_cup.Data;
using fptu_cup.Entities;
using fptu_cup.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace fptu_cup.Controllers
{
    /// <summary>
    /// FR01–FR03: đăng ký TeamManager, đăng nhập (JWT), xem / sửa thông tin cá nhân, đổi mật khẩu;
    /// bổ sung: xác nhận email, quên / đặt lại mật khẩu qua email (token ASP.NET Core Identity).
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ApiControllerBase
    {
        private readonly FptuCupDbContext _db;
        private readonly IConfiguration _config;
        private readonly UserManager<User> _userManager;
        private readonly AccountEmailService _emails;
        private readonly ILogger<AuthController> _logger;

        public AuthController(FptuCupDbContext db, IConfiguration config, UserManager<User> userManager,
            AccountEmailService emails, ILogger<AuthController> logger)
        {
            _db = db;
            _config = config;
            _userManager = userManager;
            _emails = emails;
            _logger = logger;
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
                EmailConfirmed = false, // gửi email xác nhận bằng POST /api/auth/resend-confirmation
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

            var (token, expiresIn) = JwtTokenFactory.Create(_config, user);

            return Success(new
            {
                accessToken = token,
                expiresIn,
                user = new { user.Id, user.FullName, role = user.Role },
            }, "Đăng nhập thành công");
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var user = await _db.Users.Where(u => u.Id == CurrentUserId)
                .Select(u => new { u.Id, u.Username, u.FullName, u.Email, u.EmailConfirmed, role = u.Role, u.IsActive })
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
            user.SecurityStamp = Guid.NewGuid().ToString(); // link đặt lại mật khẩu cũ và phiên ở máy khác hết hiệu lực
            await _db.SaveChangesAsync();
            return Success(null, "Đổi mật khẩu thành công");
        }

        /// <summary>
        /// Sửa hồ sơ: họ tên; đổi email cần nhập mật khẩu hiện tại, email mới phải xác nhận lại (gửi link tới email mới).
        /// </summary>
        [Authorize]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateProfile(UpdateProfileRequest req)
        {
            var user = await _db.Users.FindAsync(CurrentUserId);
            if (user == null) return NotFound404();

            var email = req.Email.Trim().ToLowerInvariant();
            var emailChanged = !string.Equals(email, user.Email, StringComparison.OrdinalIgnoreCase);
            if (emailChanged)
            {
                if (string.IsNullOrEmpty(req.CurrentPassword) || !BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
                    return BadRequest400("Đổi email cần nhập đúng mật khẩu hiện tại");
                if (await _db.Users.AnyAsync(u => u.Email == email && u.Id != user.Id))
                    return Conflict409("Email đã được tài khoản khác sử dụng");
                user.Email = email;
                user.EmailConfirmed = false;
                // Link xác nhận / đặt lại mật khẩu đã gửi tới email trước không được dùng cho email mới.
                user.SecurityStamp = Guid.NewGuid().ToString();
            }
            user.FullName = req.FullName.Trim();
            await _db.SaveChangesAsync();

            var sent = emailChanged && await TrySend(() => _emails.SendConfirmationAsync(user, HttpContext));
            var message = !emailChanged ? "Đã cập nhật hồ sơ"
                : sent ? $"Đã cập nhật hồ sơ. Đã gửi email xác nhận tới {user.Email}"
                : "Đã cập nhật hồ sơ nhưng chưa gửi được email xác nhận, hãy thử gửi lại";
            return Success(new { user.Id, user.Username, user.FullName, user.Email, user.EmailConfirmed, role = user.Role }, message);
        }

        /// <summary>Gửi lại email xác nhận cho tài khoản đang đăng nhập (link hiệu lực 24 giờ).</summary>
        [Authorize]
        [HttpPost("resend-confirmation")]
        public async Task<IActionResult> ResendConfirmation()
        {
            var user = await _db.Users.FindAsync(CurrentUserId);
            if (user == null) return NotFound404();
            if (user.EmailConfirmed) return Conflict409("Email đã được xác nhận");
            return await TrySend(() => _emails.SendConfirmationAsync(user, HttpContext))
                ? Success(null, $"Đã gửi email xác nhận tới {user.Email}")
                : Error(500, "Không gửi được email lúc này, vui lòng thử lại sau");
        }

        /// <summary>
        /// Quên mật khẩu: gửi link đặt lại (trang /reset-password, hiệu lực 30 phút) nếu email thuộc tài khoản đang hoạt động.
        /// Luôn trả 200 cùng một thông báo để không lộ email nào đã đăng ký.
        /// </summary>
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest req)
        {
            var email = req.Email.Trim().ToLowerInvariant();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null && user.IsActive) await TrySend(() => _emails.SendPasswordResetAsync(user, HttpContext));
            return Success(null, "Nếu email thuộc một tài khoản đang hoạt động, link đặt lại mật khẩu đã được gửi");
        }

        /// <summary>Đặt lại mật khẩu bằng userId + token trong email (token dùng một lần, hết hạn sau 30 phút).</summary>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest req)
        {
            var user = await _userManager.FindByIdAsync(req.UserId.ToString());
            if (user == null) return BadRequest400("Link đặt lại mật khẩu không hợp lệ hoặc đã hết hạn");

            var result = await _userManager.ResetPasswordAsync(user, req.Token, req.NewPassword);
            if (!result.Succeeded)
                return BadRequest400(result.Errors.Any(e => e.Code == "InvalidToken")
                    ? "Link đặt lại mật khẩu không hợp lệ hoặc đã hết hạn"
                    : string.Join("; ", result.Errors.Select(e => e.Description)));

            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);
            }
            return Success(null, "Đã đặt lại mật khẩu");
        }

        private async Task<bool> TrySend(Func<Task> send)
        {
            try
            {
                await send();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gửi email thất bại");
                return false;
            }
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

        public class UpdateProfileRequest
        {
            [Required, StringLength(100)] public string FullName { get; set; } = "";
            [Required, EmailAddress(ErrorMessage = "Email không đúng định dạng"), StringLength(100)] public string Email { get; set; } = "";
            /// <summary>Bắt buộc khi đổi email.</summary>
            public string? CurrentPassword { get; set; }
        }

        public class ForgotPasswordRequest
        {
            [Required, EmailAddress(ErrorMessage = "Email không đúng định dạng")] public string Email { get; set; } = "";
        }

        public class ResetPasswordRequest
        {
            public int UserId { get; set; }
            [Required] public string Token { get; set; } = "";
            [Required, MinLength(8, ErrorMessage = "Mật khẩu mới tối thiểu 8 ký tự")] public string NewPassword { get; set; } = "";
        }

        public class ChangePasswordRequest
        {
            [Required] public string OldPassword { get; set; } = "";
            [Required, MinLength(8, ErrorMessage = "Mật khẩu mới tối thiểu 8 ký tự")] public string NewPassword { get; set; } = "";
        }
    }
}
