using GymSys.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymSys.Data.Configurations;

public class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> b)
    {
        b.ToTable("Members", t => t.HasCheckConstraint("CK_Members_JenisKelamin", "[JenisKelamin] IN ('L','P')"));
        b.HasKey(x => x.MemberId);
        b.HasIndex(x => x.KodeMember).IsUnique();
        b.HasIndex(x => x.NoTelepon).IsUnique();
        b.HasIndex(x => x.NamaLengkap).HasDatabaseName("IX_Members_Nama");
        b.Property(x => x.KodeMember).HasMaxLength(20);
        b.Property(x => x.NamaLengkap).HasMaxLength(100);
        b.Property(x => x.NoTelepon).HasMaxLength(15);
        b.Property(x => x.Email).HasMaxLength(100);
        b.Property(x => x.Alamat).HasMaxLength(255);
        b.Property(x => x.JenisKelamin).HasMaxLength(1).IsFixedLength().IsUnicode(false);
        b.Property(x => x.KontakDarurat).HasMaxLength(100);
        b.Property(x => x.CatatanKesehatan).HasMaxLength(1000);
        b.Property(x => x.TglBergabung).HasDefaultValueSql("CAST(GETDATE() AS DATE)");
        b.Property(x => x.IsActive).HasDefaultValue(true).HasSentinel(true);
    }
}
