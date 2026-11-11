using fptu_cup.Common.Enums;

namespace fptu_cup.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool IsActive { get; set; } = true;

        /// <summary>Email đã được xác nhận qua link gửi về hộp thư (tài khoản seed / Admin tạo mặc định là đã xác nhận).</summary>
        public bool EmailConfirmed { get; set; } = true;

        /// <summary>Đổi mỗi khi đổi/đặt lại mật khẩu → link đặt lại mật khẩu và phiên đăng nhập cũ hết hiệu lực (ASP.NET Core Identity).</summary>
        public string? SecurityStamp { get; set; } = Guid.NewGuid().ToString();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Tournament> Tournaments { get; set; } = new List<Tournament>();
        public ICollection<Team> Teams { get; set; } = new List<Team>();
        public ICollection<Match> RefereedMatches { get; set; } = new List<Match>();
    }
}
