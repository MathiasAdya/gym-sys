using GymSys.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymSys.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users", t => t.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Admin','Resepsionis','Instruktur')"));
        b.HasKey(x => x.UserId);
        b.HasIndex(x => x.Username).IsUnique();
        b.Property(x => x.Username).HasMaxLength(50);
        b.Property(x => x.PasswordHash).HasMaxLength(32);
        b.Property(x => x.PasswordSalt).HasMaxLength(16);
        b.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.NamaLengkap).HasMaxLength(100);
        b.Property(x => x.IsActive).HasDefaultValue(true).HasSentinel(true);
        b.Property(x => x.Theme).HasMaxLength(10).HasDefaultValue("Dark");
        b.Property(x => x.FailedLoginCount).HasDefaultValue(0);
        b.Property(x => x.MustChangePassword).HasDefaultValue(false);
    }
}
