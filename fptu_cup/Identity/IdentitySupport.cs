using System.Security.Claims;
using fptu_cup.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace fptu_cup.Identity
{
    /// <summary>Identity băm/kiểm tra mật khẩu bằng BCrypt để dùng chung dữ liệu với API (/api/auth).</summary>
    public class BcryptPasswordHasher : IPasswordHasher<User>
    {
        public string HashPassword(User user, string password) => BCrypt.Net.BCrypt.HashPassword(password);

        public PasswordVerificationResult VerifyHashedPassword(User user, string hashedPassword, string providedPassword)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword)
                    ? PasswordVerificationResult.Success
                    : PasswordVerificationResult.Failed;
            }
            catch (BCrypt.Net.SaltParseException)
            {
                return PasswordVerificationResult.Failed;
            }
        }
    }

    /// <summary>Cookie đăng nhập của giao diện MVC chứa id, username, họ tên và vai trò (dùng cho [Authorize(Roles = ...)]).</summary>
    public class AppClaimsPrincipalFactory : UserClaimsPrincipalFactory<User>
    {
        public const string FullNameClaim = "full_name";

        public AppClaimsPrincipalFactory(UserManager<User> userManager, IOptions<IdentityOptions> options)
            : base(userManager, options) { }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
        {
            var identity = await base.GenerateClaimsAsync(user);
            identity.AddClaim(new Claim(Options.ClaimsIdentity.RoleClaimType, user.Role.ToString()));
            identity.AddClaim(new Claim(FullNameClaim, user.FullName));
            return identity;
        }
    }

    /// <summary>Thông báo lỗi Identity bằng tiếng Việt (hiện trong form đăng ký).</summary>
    public class VietnameseIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError DuplicateUserName(string userName) => new() { Code = nameof(DuplicateUserName), Description = "Tên đăng nhập đã tồn tại" };
        public override IdentityError DuplicateEmail(string email) => new() { Code = nameof(DuplicateEmail), Description = "Email đã tồn tại" };
        public override IdentityError InvalidUserName(string? userName) => new() { Code = nameof(InvalidUserName), Description = "Tên đăng nhập chỉ gồm chữ không dấu, số và các ký tự - . _ @ +" };
        public override IdentityError InvalidEmail(string? email) => new() { Code = nameof(InvalidEmail), Description = "Email không đúng định dạng" };
        public override IdentityError PasswordTooShort(int length) => new() { Code = nameof(PasswordTooShort), Description = $"Mật khẩu tối thiểu {length} ký tự" };
    }

    public static class ClaimsPrincipalExtensions
    {
        public static string? FullName(this ClaimsPrincipal user) =>
            user.FindFirst(AppClaimsPrincipalFactory.FullNameClaim)?.Value ?? user.Identity?.Name;

        public static string? Role(this ClaimsPrincipal user) => user.FindFirst(ClaimTypes.Role)?.Value;
    }
}
