namespace GymSys.Core.Interfaces;

// KONTRAK USULAN untuk halaman Laporan (FR11).
// Implementasi asli: ADO.NET di GymSys.Data/Queries/ReportQueries.cs (SQL terparameter, async).
// Aturan: total pendapatan = jumlah TotalBayar transaksi berstatus "Lunas" pada periode.

public sealed record TransactionRowDto(
    string NoKuitansi,
    DateTime TglTransaksi,
    string NamaMember,
    string NamaPaket,
    decimal TotalBayar,
    string MetodeBayar,
    string Status);   // "Lunas" atau "Batal"

public sealed record MemberStatusRowDto(
    string KodeMember,
    string NamaLengkap,
    string NoTelepon,
    DateTime? TglBerakhir,
    string Status);   // Aktif | Akan Habis | Kedaluwarsa | Belum Berlangganan | Nonaktif

public sealed record RevenueByPlanDto(string NamaPaket, int JumlahTransaksi, decimal TotalPendapatan);

public interface IReportQueries
{
    /// <summary>Rentang tanggal INKLUSIF (from..to). Urut dari transaksi terbaru.</summary>
    Task<IReadOnlyList<TransactionRowDto>> GetTransactionsAsync(DateTime from, DateTime to, CancellationToken ct = default);

    Task<IReadOnlyList<MemberStatusRowDto>> GetMemberStatusAsync(CancellationToken ct = default);

    /// <summary>Hanya transaksi Lunas, dikelompokkan per paket.</summary>
    Task<IReadOnlyList<RevenueByPlanDto>> GetRevenueByPlanAsync(DateTime from, DateTime to, CancellationToken ct = default);
}
