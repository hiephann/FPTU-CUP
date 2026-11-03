using fptu_cup.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace fptu_cup.Data.Configurations
{
    public class TeamConfiguration : IEntityTypeConfiguration<Team>
    {
        public void Configure(EntityTypeBuilder<Team> builder)
        {
            builder.ToTable("Teams", t => t.HasCheckConstraint(
                "CK_Teams_Status", "[Status] IN (N'Pending', N'Approved', N'Rejected')"));

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Faculty).HasMaxLength(100);
            builder.Property(x => x.LogoUrl).HasMaxLength(255);
            builder.Property(x => x.KitColor).HasMaxLength(30);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

            builder.HasOne(x => x.Tournament)
                .WithMany(t => t.Teams)
                .HasForeignKey(x => x.TournamentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Manager)
                .WithMany(u => u.Teams)
                .HasForeignKey(x => x.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TournamentId, x.Name }).IsUnique().HasDatabaseName("UQ_Teams_Tournament_Name");
        }
    }
}
