using GymSys.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymSys.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> b)
    {
        b.ToTable("Transactions", t =>
        {
            t.HasCheckConstraint("CK_Trx_MetodeBayar", "[MetodeBayar] IN ('Tunai','Transfer')");
            t.HasCheckConstraint("CK_Trx_Status", "[Status] IN ('Lunas','Batal')");
            t.HasCheckConstraint("CK_Trx_Tanggal", "[TglBerakhir] >= [TglMulaiAktif]");
            t.HasCheckConstraint("CK_Trx_Diskon", "[Diskon] <= [HargaPaket]");
        });
        b.HasKey(x => x.TransactionId);
        b.HasIndex(x => x.NoKuitansi).IsUnique();
        b.HasIndex(x => new { x.MemberId, x.Status, x.TglBerakhir }).HasDatabaseName("IX_Trx_Member");
        // Riwayat Transaksi, laporan per periode, dan pendapatan dashboard memfilter rentang TglTransaksi (NFR-02).
        b.HasIndex(x => x.TglTransaksi).IncludeProperties(x => new { x.Status, x.TotalBayar }).HasDatabaseName("IX_Trx_Tgl");
        b.Property(x => x.NoKuitansi).HasMaxLength(20);
        b.Property(x => x.TglTransaksi).HasDefaultValueSql("SYSDATETIME()");
        b.Property(x => x.Diskon).HasDefaultValue(0m);
        b.Property(x => x.MetodeBayar).HasConversion<string>().HasMaxLength(10);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(10).HasDefaultValue(StatusTransaksi.Lunas);
        b.Property(x => x.AlasanBatal).HasMaxLength(255);

        b.HasOne(x => x.Member).WithMany(m => m.Transactions).HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Plan).WithMany().HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Kasir).WithMany().HasForeignKey(x => x.UserIdKasir).OnDelete(DeleteBehavior.Restrict);
    }
}
