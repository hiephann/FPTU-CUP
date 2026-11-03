using fptu_cup.Common.Enums;

namespace fptu_cup.Entities
{
    public class Match
    {
        public int Id { get; set; }
        public int TournamentId { get; set; }
        public int Round { get; set; }
        public int HomeTeamId { get; set; }
        public int AwayTeamId { get; set; }
        public int? VenueId { get; set; }
        public int? RefereeId { get; set; }
        public DateTime KickoffTime { get; set; }
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public MatchStatus Status { get; set; } = MatchStatus.Scheduled;
        /// <summary>Người sửa gần nhất – BR15.</summary>
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        /// <summary>Đội bị xử thua do không nộp đội hình trước hạn (BR23); null nếu không có.</summary>
        public int? ForfeitTeamId { get; set; }
        /// <summary>Lý do xử thua, hoặc lý do hủy trận khi cả hai đội đều không nộp đội hình (BR23).</summary>
        public string? ForfeitReason { get; set; }

        public Tournament? Tournament { get; set; }
        public Team? ForfeitTeam { get; set; }
        public Team? HomeTeam { get; set; }
        public Team? AwayTeam { get; set; }
        public Venue? Venue { get; set; }
        public User? Referee { get; set; }
        public User? Updater { get; set; }
        public ICollection<MatchLineup> Lineups { get; set; } = new List<MatchLineup>();
        public ICollection<Substitution> Substitutions { get; set; } = new List<Substitution>();
        public ICollection<Goal> Goals { get; set; } = new List<Goal>();
        public ICollection<Card> Cards { get; set; } = new List<Card>();
    }
}
