using fptu_cup.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace fptu_cup.Data.Configurations
{
    public class TournamentConfiguration : IEntityTypeConfiguration<Tournament>
    {
        public void Configure(EntityTypeBuilder<Tournament> builder)
        {
            builder.ToTable("Tournaments", t =>
            {
                t.HasCheckConstraint("CK_Tournaments_MaxTeams", "[MaxTeams] >= 2");
                t.HasCheckConstraint("CK_Tournaments_Legs", "[NumberOfLegs] IN (1, 2)");
                t.HasCheckConstraint("CK_Tournaments_Status",
                    "[Status] IN (N'Draft', N'Registration', N'Ongoing', N'Finished')");
                t.HasCheckConstraint("CK_Tournaments_Dates", "[EndDate] >= [StartDate]");
            });

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
            builder.Property(x => x.Season).HasMaxLength(20).IsRequired();
            builder.Property(x => x.StartDate).HasColumnType("date");
            builder.Property(x => x.EndDate).HasColumnType("date");
            builder.Property(x => x.NumberOfLegs).HasColumnType("tinyint").HasConversion<byte>();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

            builder.HasOne(x => x.Creator)
                .WithMany(u => u.Tournaments)
                .HasForeignKey(x => x.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
