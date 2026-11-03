using fptu_cup.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace fptu_cup.Data.Configurations
{
    public class MatchConfiguration : IEntityTypeConfiguration<Match>
    {
        public void Configure(EntityTypeBuilder<Match> builder)
        {
            builder.ToTable("Matches", t =>
            {
                t.HasCheckConstraint("CK_Matches_Round", "[Round] >= 1");
                t.HasCheckConstraint("CK_Matches_DiffTeams", "[HomeTeamId] <> [AwayTeamId]"); // BR06
                t.HasCheckConstraint("CK_Matches_Status",
                    "[Status] IN (N'Scheduled', N'InProgress', N'Finished', N'Postponed', N'Cancelled')");
            });

            builder.HasKey(x => x.Id);
            builder.Property(x => x.KickoffTime).HasColumnType("datetime2");
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

            builder.HasOne(x => x.Tournament)
                .WithMany(t => t.Matches)
                .HasForeignKey(x => x.TournamentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.HomeTeam)
                .WithMany(t => t.HomeMatches)
                .HasForeignKey(x => x.HomeTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.AwayTeam)
                .WithMany(t => t.AwayMatches)
                .HasForeignKey(x => x.AwayTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Venue)
                .WithMany(v => v.Matches)
                .HasForeignKey(x => x.VenueId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Referee)
                .WithMany(u => u.RefereedMatches)
                .HasForeignKey(x => x.RefereeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.ForfeitReason).HasMaxLength(200);
            builder.HasOne(x => x.ForfeitTeam)
                .WithMany()
                .HasForeignKey(x => x.ForfeitTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Updater)
                .WithMany()
                .HasForeignKey(x => x.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TournamentId, x.Round }).HasDatabaseName("IX_Matches_Tournament_Round");
            builder.HasIndex(x => x.KickoffTime).HasDatabaseName("IX_Matches_KickoffTime");
        }
    }
}
