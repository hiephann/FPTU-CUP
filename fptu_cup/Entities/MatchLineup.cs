namespace fptu_cup.Entities
{
    /// <summary>Đội hình nộp cho một trận; khóa khi trận InProgress (BR19, BR20).</summary>
    public class MatchLineup
    {
        public int Id { get; set; }
        public int MatchId { get; set; }
        public int TeamId { get; set; }
        public int PlayerId { get; set; }
        public bool IsStarter { get; set; }

        public Match? Match { get; set; }
        public Team? Team { get; set; }
        public Player? Player { get; set; }
    }
}
