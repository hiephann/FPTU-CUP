using fptu_cup.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace fptu_cup.Data.Configurations
{
    public class CardConfiguration : IEntityTypeConfiguration<Card>
    {
        public void Configure(EntityTypeBuilder<Card> builder)
        {
            builder.ToTable("Cards", t =>
            {
                t.HasCheckConstraint("CK_Cards_Type", "[CardType] IN (N'Yellow', N'Red')");
                t.HasCheckConstraint("CK_Cards_Minute", "[Minute] BETWEEN 1 AND 60");
            });

            builder.HasKey(x => x.Id);
            builder.Property(x => x.CardType).HasConversion<string>().HasMaxLength(10).IsRequired();

            builder.HasOne(x => x.Match)
                .WithMany(m => m.Cards)
                .HasForeignKey(x => x.MatchId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Player)
                .WithMany(p => p.Cards)
                .HasForeignKey(x => x.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
