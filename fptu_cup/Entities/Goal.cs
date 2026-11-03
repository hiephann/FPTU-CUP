namespace fptu_cup.Entities
{
    public class Goal
    {
        public int Id { get; set; }
        public int MatchId { get; set; }
        public int PlayerId { get; set; }
        /// <summary>Đội được tính bàn thắng (đội đối phương nếu là phản lưới nhà).</summary>
        public int TeamId { get; set; }
        public int Minute { get; set; }
        public bool IsOwnGoal { get; set; }

        public Match? Match { get; set; }
        public Player? Player { get; set; }
        public Team? Team { get; set; }
    }
}
