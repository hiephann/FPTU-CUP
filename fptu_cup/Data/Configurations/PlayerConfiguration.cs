using fptu_cup.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace fptu_cup.Data.Configurations
{
    public class PlayerConfiguration : IEntityTypeConfiguration<Player>
    {
        public void Configure(EntityTypeBuilder<Player> builder)
        {
            builder.ToTable("Players", t =>
            {
                t.HasCheckConstraint("CK_Players_Shirt", "[ShirtNumber] BETWEEN 1 AND 99");
                t.HasCheckConstraint("CK_Players_Position", "[Position] IN ('GK', 'DF', 'MF', 'FW')");
            });

            builder.HasKey(x => x.Id);
            builder.Property(x => x.StudentCode).HasMaxLength(20).IsUnicode(false).IsRequired();
            builder.Property(x => x.FullName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Position).HasConversion<string>().HasMaxLength(2).IsUnicode(false).IsRequired();
            builder.Property(x => x.DateOfBirth).HasColumnType("date");

            // Số lượng 7–14 cầu thủ/đội (BR01) và MSSV duy nhất trong giải (BR02) kiểm tra ở Service.
            builder.HasOne(x => x.Team)
                .WithMany(t => t.Players)
                .HasForeignKey(x => x.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.TeamId, x.ShirtNumber }).IsUnique().HasDatabaseName("UQ_Players_Team_Shirt"); // BR03
            builder.HasIndex(x => x.StudentCode).HasDatabaseName("IX_Players_StudentCode");
        }
    }
}
