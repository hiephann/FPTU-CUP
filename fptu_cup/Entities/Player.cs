using fptu_cup.Common.Enums;

namespace fptu_cup.Entities
{
    public class Player
    {
        public int Id { get; set; }
        public int TeamId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int ShirtNumber { get; set; }
        public PlayerPosition Position { get; set; }
        public DateOnly? DateOfBirth { get; set; }

        public Team? Team { get; set; }
        public ICollection<MatchLineup> Lineups { get; set; } = new List<MatchLineup>();
        public ICollection<Goal> Goals { get; set; } = new List<Goal>();
        public ICollection<Card> Cards { get; set; } = new List<Card>();
        public ICollection<Suspension> Suspensions { get; set; } = new List<Suspension>();
    }
}
