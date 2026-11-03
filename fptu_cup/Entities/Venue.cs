namespace fptu_cup.Entities
{
    public class Venue
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public int? Capacity { get; set; }

        public ICollection<Match> Matches { get; set; } = new List<Match>();
    }
}
