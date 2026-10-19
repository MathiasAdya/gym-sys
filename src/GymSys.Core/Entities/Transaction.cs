namespace GymSys.Core.Entities;

public class Transaction
{
    public int TransactionId { get; set; }
    public string NoKuitansi { get; set; } = "";
    public int MemberId { get; set; }
    public int PlanId { get; set; }
    public int UserIdKasir { get; set; }
    public DateTime TglTransaksi { get; set; }
    public DateOnly TglMulaiAktif { get; set; }
    public DateOnly TglBerakhir { get; set; }

    /// <summary>Salinan harga paket saat transaksi, agar perubahan harga tidak mengubah riwayat.</summary>
    public decimal HargaPaket { get; set; }

    public decimal Diskon { get; set; }
    public decimal TotalBayar { get; set; }
    public MetodeBayar MetodeBayar { get; set; }
    public StatusTransaksi Status { get; set; } = StatusTransaksi.Lunas;
    public string? AlasanBatal { get; set; }

    public Member? Member { get; set; }
    public MembershipPlan? Plan { get; set; }
    public User? Kasir { get; set; }
}
