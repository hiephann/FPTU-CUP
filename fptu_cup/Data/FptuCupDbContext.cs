using fptu_cup.Entities;
using Microsoft.EntityFrameworkCore;

namespace fptu_cup.Data
{
    public class FptuCupDbContext : DbContext
    {
        public FptuCupDbContext(DbContextOptions<FptuCupDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Tournament> Tournaments => Set<Tournament>();
        public DbSet<Team> Teams => Set<Team>();
        public DbSet<Player> Players => Set<Player>();
        public DbSet<Venue> Venues => Set<Venue>();
        public DbSet<Match> Matches => Set<Match>();
        public DbSet<MatchLineup> MatchLineups => Set<MatchLineup>();
        public DbSet<Substitution> Substitutions => Set<Substitution>();
        public DbSet<Goal> Goals => Set<Goal>();
        public DbSet<Card> Cards => Set<Card>();
        public DbSet<Suspension> Suspensions => Set<Suspension>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(FptuCupDbContext).Assembly);
        }
    }
}
