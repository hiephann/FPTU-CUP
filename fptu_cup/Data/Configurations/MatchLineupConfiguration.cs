using fptu_cup.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace fptu_cup.Data.Configurations
{
    public class MatchLineupConfiguration : IEntityTypeConfiguration<MatchLineup>
    {
        public void Configure(EntityTypeBuilder<MatchLineup> builder)
        {
            builder.ToTable("MatchLineups");

            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.Match)
                .WithMany(m => m.Lineups)
                .HasForeignKey(x => x.MatchId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Team)
                .WithMany()
                .HasForeignKey(x => x.TeamId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Player)
                .WithMany(p => p.Lineups)
                .HasForeignKey(x => x.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.MatchId, x.PlayerId }).IsUnique().HasDatabaseName("UQ_MatchLineups_Match_Player");
        }
    }
}
