namespace fptu_cup.Entities
{
    /// <summary>Thay người – lưu riêng, không sửa MatchLineups (BR20, BR21).</summary>
    public class Substitution
    {
        public int Id { get; set; }
        public int MatchId { get; set; }
        public int TeamId { get; set; }
        public int PlayerOutId { get; set; }
        public int PlayerInId { get; set; }
        public int Minute { get; set; }

        public Match? Match { get; set; }
        public Team? Team { get; set; }
        public Player? PlayerOut { get; set; }
        public Player? PlayerIn { get; set; }
    }
}
