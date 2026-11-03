using fptu_cup.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace fptu_cup.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users", t => t.HasCheckConstraint(
                "CK_Users_Role", "[Role] IN (N'Admin', N'Referee', N'TeamManager')"));

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Username).HasMaxLength(50).IsRequired();
            builder.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            builder.Property(x => x.FullName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Email).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(x => x.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(x => x.Username).IsUnique().HasDatabaseName("UQ_Users_Username");
            builder.HasIndex(x => x.Email).IsUnique().HasDatabaseName("UQ_Users_Email");
        }
    }
}
