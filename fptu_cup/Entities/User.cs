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
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Tournament> Tournaments { get; set; } = new List<Tournament>();
        public ICollection<Team> Teams { get; set; } = new List<Team>();
        public ICollection<Match> RefereedMatches { get; set; } = new List<Match>();
    }
}
