using fptu_cup.Common.Enums;

namespace fptu_cup.Entities
{
    public class Team
    {
        public int Id { get; set; }
        public int TournamentId { get; set; }
        public int ManagerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Faculty { get; set; }
        public string? LogoUrl { get; set; }
        public string? KitColor { get; set; }
        public TeamStatus Status { get; set; } = TeamStatus.Pending;

        public Tournament? Tournament { get; set; }
        public User? Manager { get; set; }
        public ICollection<Player> Players { get; set; } = new List<Player>();
        public ICollection<Match> HomeMatches { get; set; } = new List<Match>();
        public ICollection<Match> AwayMatches { get; set; } = new List<Match>();
    }
}
