using System.ComponentModel.DataAnnotations;
using fptu_cup.Common;
using fptu_cup.Common.Enums;
using fptu_cup.Data;
using fptu_cup.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace fptu_cup.Controllers
{
    /// <summary>FR04, FR16: Admin tạo tài khoản trọng tài, khóa/mở tài khoản, xem danh sách trọng tài.</summary>
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class UsersController : ApiControllerBase
    {
        private readonly FptuCupDbContext _db;

        public UsersController(FptuCupDbContext db) => _db = db;

        [HttpGet("api/users")]
        public async Task<IActionResult> GetAll([FromQuery] PagedQuery query, UserRole? role)
        {
            var q = _db.Users.AsQueryable();
            if (role != null) q = q.Where(u => u.Role == role);
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = query.Search.Trim();
                q = q.Where(u => u.Username.Contains(s) || u.FullName.Contains(s) || u.Email.Contains(s));
            }

            var total = await q.CountAsync();
            var items = await q.OrderBy(u => u.Id).Skip(query.Skip).Take(query.PageSize)
                .Select(u => new { u.Id, u.Username, u.FullName, u.Email, role = u.Role, u.IsActive, u.CreatedAt })
                .ToListAsync();
            return SuccessPaged(items, total, query);
        }

        /// <summary>Danh sách trọng tài đang hoạt động (dùng khi phân công trận).</summary>
        [HttpGet("api/referees")]
        public async Task<IActionResult> GetReferees()
        {
            var items = await _db.Users.Where(u => u.Role == UserRole.Referee && u.IsActive)
                .OrderBy(u => u.FullName)
                .Select(u => new { u.Id, u.FullName, u.Email })
                .ToListAsync();
            return Success(items);
        }

        /// <summary>Tạo tài khoản: mặc định Referee; Admin chọn được mọi vai trò (Admin, Referee, TeamManager).</summary>
        [HttpPost("api/users")]
        public async Task<IActionResult> CreateReferee(CreateRefereeRequest req)
        {
            var username = req.Username.Trim();
            var email = req.Email.Trim().ToLowerInvariant();
            if (await _db.Users.AnyAsync(u => u.Username == username)) return Conflict409("Tên đăng nhập đã tồn tại");
            if (await _db.Users.AnyAsync(u => u.Email == email)) return Conflict409("Email đã tồn tại");

            var user = new User
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                FullName = req.FullName.Trim(),
                Email = email,
                Role = req.Role ?? UserRole.Referee,
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return Created201(new { user.Id, user.Username, user.FullName, user.Email, role = user.Role }, "Tạo tài khoản thành công");
        }

        /// <summary>Admin đặt lại mật khẩu cho bất kỳ tài khoản nào (không cần mật khẩu cũ).</summary>
        [HttpPut("api/users/{id:int}/password")]
        public async Task<IActionResult> ResetPassword(int id, ResetPasswordRequest req)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound404("Không tìm thấy tài khoản");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
            user.SecurityStamp = Guid.NewGuid().ToString(); // phiên đăng nhập cũ của người đó hết hiệu lực
            await _db.SaveChangesAsync();
            return Success(new { user.Id, user.Username }, "Đã đặt lại mật khẩu");
        }

        [HttpPatch("api/users/{id:int}/status")]
        public async Task<IActionResult> SetStatus(int id, SetStatusRequest req)
        {
            if (id == CurrentUserId) return Conflict409("Không thể tự khóa tài khoản của mình");
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound404("Không tìm thấy tài khoản");

            user.IsActive = req.IsActive;
            await _db.SaveChangesAsync();
            return Success(new { user.Id, user.Username, user.IsActive }, req.IsActive ? "Đã mở khóa tài khoản" : "Đã khóa tài khoản");
        }

        public class CreateRefereeRequest
        {
            [Required, StringLength(50, MinimumLength = 3)] public string Username { get; set; } = "";
            [Required, MinLength(8)] public string Password { get; set; } = "";
            [Required, StringLength(100)] public string FullName { get; set; } = "";
            [Required, EmailAddress, StringLength(100)] public string Email { get; set; } = "";
            /// <summary>Bỏ trống = Referee.</summary>
            public UserRole? Role { get; set; }
        }

        public class ResetPasswordRequest
        {
            [Required, MinLength(8, ErrorMessage = "Mật khẩu mới tối thiểu 8 ký tự")] public string NewPassword { get; set; } = "";
        }

        public class SetStatusRequest
        {
            public bool IsActive { get; set; }
        }
    }
}
