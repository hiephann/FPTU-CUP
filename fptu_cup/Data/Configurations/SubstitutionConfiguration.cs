using fptu_cup.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace fptu_cup.Data.Configurations
{
    public class SubstitutionConfiguration : IEntityTypeConfiguration<Substitution>
    {
        public void Configure(EntityTypeBuilder<Substitution> builder)
        {
            builder.ToTable("Substitutions", t =>
            {
                t.HasCheckConstraint("CK_Substitutions_Minute", "[Minute] BETWEEN 1 AND 60");
                t.HasCheckConstraint("CK_Substitutions_DiffPlayers", "[PlayerOutId] <> [PlayerInId]");
            });

            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.Match)
                .WithMany(m => m.Substitutions)
                .HasForeignKey(x => x.MatchId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Team)
                .WithMany()
                .HasForeignKey(x => x.TeamId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.PlayerOut)
                .WithMany()
                .HasForeignKey(x => x.PlayerOutId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.PlayerIn)
                .WithMany()
                .HasForeignKey(x => x.PlayerInId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
