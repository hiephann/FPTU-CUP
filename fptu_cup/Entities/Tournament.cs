using fptu_cup.Common.Enums;

namespace fptu_cup.Entities
{
    public class Tournament
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Season { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public int MaxTeams { get; set; }
        /// <summary>1 hoặc 2 lượt (BR17).</summary>
        public int NumberOfLegs { get; set; } = 2;
        public TournamentStatus Status { get; set; } = TournamentStatus.Draft;
        public int CreatedBy { get; set; }

        public User? Creator { get; set; }
        public ICollection<Team> Teams { get; set; } = new List<Team>();
        public ICollection<Match> Matches { get; set; } = new List<Match>();
        public ICollection<Suspension> Suspensions { get; set; } = new List<Suspension>();
    }
}
