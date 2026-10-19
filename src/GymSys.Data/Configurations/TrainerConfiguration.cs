using GymSys.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymSys.Data.Configurations;

public class TrainerConfiguration : IEntityTypeConfiguration<Trainer>
{
    public void Configure(EntityTypeBuilder<Trainer> b)
    {
        b.ToTable("Trainers");
        b.HasKey(x => x.TrainerId);
        // Unik hanya bila terisi (EF menambah filter IS NOT NULL), sehingga banyak instruktur boleh tanpa akun.
        b.HasIndex(x => x.UserId).IsUnique();
        b.Property(x => x.Nama).HasMaxLength(100);
        b.Property(x => x.NoTelepon).HasMaxLength(15);
        b.Property(x => x.Keahlian).HasMaxLength(100);
        b.Property(x => x.IsActive).HasDefaultValue(true).HasSentinel(true);

        b.HasOne(x => x.User).WithOne(u => u.Trainer).HasForeignKey<Trainer>(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
