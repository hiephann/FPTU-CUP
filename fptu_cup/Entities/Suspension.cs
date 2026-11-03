namespace fptu_cup.Entities
{
    /// <summary>Án treo giò sinh tự động khi trận kết thúc (BR14).</summary>
    public class Suspension
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public int TournamentId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int MatchesRemaining { get; set; }

        public Player? Player { get; set; }
        public Tournament? Tournament { get; set; }
    }
}
