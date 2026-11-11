using fptu_cup.Common.Enums;
using fptu_cup.Data;
using fptu_cup.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace fptu_cup.Identity
{
    /// <summary>
    /// ASP.NET Core Identity dùng thẳng bảng Users có sẵn (không tạo bảng AspNetUsers):
    /// UserManager/SignInManager đọc ghi người dùng qua store này, vai trò lấy từ cột Role.
    /// </summary>
    public class AppUserStore : IUserPasswordStore<User>, IUserEmailStore<User>, IUserRoleStore<User>, IUserSecurityStampStore<User>
    {
        private readonly FptuCupDbContext _db;

        public AppUserStore(FptuCupDbContext db) => _db = db;

        public void Dispose() { }

        // ---------------------------------------------------------------- IUserStore

        public Task<string> GetUserIdAsync(User user, CancellationToken ct) => Task.FromResult(user.Id.ToString());
        public Task<string?> GetUserNameAsync(User user, CancellationToken ct) => Task.FromResult<string?>(user.Username);

        public Task SetUserNameAsync(User user, string? userName, CancellationToken ct)
        {
            user.Username = userName ?? "";
            return Task.CompletedTask;
        }

        // Bảng Users không có cột Normalized*: SQL Server so sánh không phân biệt hoa thường nên dùng UPPER để khớp.
        public Task<string?> GetNormalizedUserNameAsync(User user, CancellationToken ct) => Task.FromResult<string?>(user.Username.ToUpperInvariant());
        public Task SetNormalizedUserNameAsync(User user, string? normalizedName, CancellationToken ct) => Task.CompletedTask;

        public async Task<IdentityResult> CreateAsync(User user, CancellationToken ct)
        {
            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> UpdateAsync(User user, CancellationToken ct)
        {
            _db.Users.Update(user);
            await _db.SaveChangesAsync(ct);
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(User user, CancellationToken ct)
        {
            _db.Users.Remove(user);
            await _db.SaveChangesAsync(ct);
            return IdentityResult.Success;
        }

        public Task<User?> FindByIdAsync(string userId, CancellationToken ct) =>
            int.TryParse(userId, out var id) ? _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct) : Task.FromResult<User?>(null);

        public Task<User?> FindByNameAsync(string normalizedUserName, CancellationToken ct) =>
            _db.Users.FirstOrDefaultAsync(u => u.Username.ToUpper() == normalizedUserName, ct);

        // ---------------------------------------------------------------- IUserPasswordStore (hash BCrypt, xem BcryptPasswordHasher)

        public Task SetPasswordHashAsync(User user, string? passwordHash, CancellationToken ct)
        {
            user.PasswordHash = passwordHash ?? "";
            return Task.CompletedTask;
        }

        public Task<string?> GetPasswordHashAsync(User user, CancellationToken ct) => Task.FromResult<string?>(user.PasswordHash);
        public Task<bool> HasPasswordAsync(User user, CancellationToken ct) => Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));

        // ---------------------------------------------------------------- IUserEmailStore (Email là duy nhất)

        public Task SetEmailAsync(User user, string? email, CancellationToken ct)
        {
            user.Email = email ?? "";
            return Task.CompletedTask;
        }

        public Task<string?> GetEmailAsync(User user, CancellationToken ct) => Task.FromResult<string?>(user.Email);
        public Task<bool> GetEmailConfirmedAsync(User user, CancellationToken ct) => Task.FromResult(user.EmailConfirmed);

        public Task SetEmailConfirmedAsync(User user, bool confirmed, CancellationToken ct)
        {
            user.EmailConfirmed = confirmed;
            return Task.CompletedTask;
        }

        // ---------------------------------------------------------------- IUserSecurityStampStore
        // Đổi khi đổi/đặt lại mật khẩu: token đặt lại mật khẩu cũ và cookie đăng nhập cũ hết hiệu lực.

        public Task SetSecurityStampAsync(User user, string stamp, CancellationToken ct)
        {
            user.SecurityStamp = stamp;
            return Task.CompletedTask;
        }

        public Task<string?> GetSecurityStampAsync(User user, CancellationToken ct) =>
            Task.FromResult<string?>(user.SecurityStamp ??= Guid.NewGuid().ToString());

        public Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken ct) =>
            _db.Users.FirstOrDefaultAsync(u => u.Email.ToUpper() == normalizedEmail, ct);

        public Task<string?> GetNormalizedEmailAsync(User user, CancellationToken ct) => Task.FromResult<string?>(user.Email.ToUpperInvariant());
        public Task SetNormalizedEmailAsync(User user, string? normalizedEmail, CancellationToken ct) => Task.CompletedTask;

        // ---------------------------------------------------------------- IUserRoleStore (mỗi người dùng đúng một vai trò)

        public Task AddToRoleAsync(User user, string roleName, CancellationToken ct)
        {
            user.Role = Enum.Parse<UserRole>(roleName, ignoreCase: true);
            return Task.CompletedTask;
        }

        public Task RemoveFromRoleAsync(User user, string roleName, CancellationToken ct) => Task.CompletedTask;

        public Task<IList<string>> GetRolesAsync(User user, CancellationToken ct) => Task.FromResult<IList<string>>(new[] { user.Role.ToString() });

        public Task<bool> IsInRoleAsync(User user, string roleName, CancellationToken ct) =>
            Task.FromResult(string.Equals(user.Role.ToString(), roleName, StringComparison.OrdinalIgnoreCase));

        public async Task<IList<User>> GetUsersInRoleAsync(string roleName, CancellationToken ct)
        {
            var role = Enum.Parse<UserRole>(roleName, ignoreCase: true);
            return await _db.Users.Where(u => u.Role == role).ToListAsync(ct);
        }
    }
}
