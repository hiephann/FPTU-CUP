using fptu_cup.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace fptu_cup.Data.Configurations
{
    public class SuspensionConfiguration : IEntityTypeConfiguration<Suspension>
    {
        public void Configure(EntityTypeBuilder<Suspension> builder)
        {
            builder.ToTable("Suspensions", t => t.HasCheckConstraint(
                "CK_Suspensions_Remaining", "[MatchesRemaining] >= 0"));

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Reason).HasMaxLength(100).IsRequired();

            builder.HasOne(x => x.Player)
                .WithMany(p => p.Suspensions)
                .HasForeignKey(x => x.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Tournament)
                .WithMany(t => t.Suspensions)
                .HasForeignKey(x => x.TournamentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
