using fptu_cup.Common.Enums;

namespace fptu_cup.Entities
{
    public class Card
    {
        public int Id { get; set; }
        public int MatchId { get; set; }
        public int PlayerId { get; set; }
        public CardType CardType { get; set; }
        public int Minute { get; set; }

        public Match? Match { get; set; }
        public Player? Player { get; set; }
    }
}
