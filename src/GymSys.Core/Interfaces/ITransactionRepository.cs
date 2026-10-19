using GymSys.Core.Entities;

namespace GymSys.Core.Interfaces;

/// <summary>Pilihan member di layar transaksi.</summary>
public sealed record MemberLookupDto(int MemberId, string KodeMember, string NamaLengkap, string NoTelepon)
{
    public string Display => $"{KodeMember} · {NamaLengkap} · {NoTelepon}";
}

/// <summary>Pilihan paket di layar transaksi.</summary>
public sealed record PlanLookupDto(int PlanId, string NamaPaket, int DurasiHari, decimal Harga)
{
    public string Display => $"{NamaPaket} ({DurasiHari} hari)";
}

/// <summary>Filter Riwayat Transaksi. Rentang tanggal transaksi INKLUSIF; Page mulai dari 1.</summary>
public sealed record TransactionHistoryQuery(DateOnly From, DateOnly To, StatusTransaksi? Status, int Page, int PageSize);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount);

/// <summary>Satu baris Riwayat Transaksi.</summary>
public sealed record TransactionHistoryRowDto(
    int TransactionId,
    string NoKuitansi,
    DateTime TglTransaksi,
    string KodeMember,
    string NamaMember,
    string NamaPaket,
    DateOnly TglMulaiAktif,
    DateOnly TglBerakhir,
    decimal TotalBayar,
    MetodeBayar MetodeBayar,
    StatusTransaksi Status,
    string? AlasanBatal)
{
    public bool IsLunas => Status == StatusTransaksi.Lunas;
}

/// <summary>Isi kuitansi (PRD Bagian 9, layar Kuitansi).</summary>
public sealed record ReceiptDto(
    int TransactionId,
    string NoKuitansi,
    DateTime TglTransaksi,
    string KodeMember,
    string NamaMember,
    string NamaPaket,
    int DurasiHari,
    DateOnly TglMulaiAktif,
    DateOnly TglBerakhir,
    decimal HargaPaket,
    decimal Diskon,
    decimal TotalBayar,
    MetodeBayar MetodeBayar,
    string NamaKasir,
    StatusTransaksi Status,
    string? AlasanBatal);

/// <summary>
/// Akses data transaksi (Anggota 1). Pencarian member dan daftar paket ada di sini selama
/// IMemberRepository/IPlanRepository (Anggota 3) belum tersedia.
/// </summary>
public interface ITransactionRepository
{
    /// <summary>Member aktif yang nama, kode, atau no. HP-nya mengandung kata kunci.</summary>
    Task<IReadOnlyList<MemberLookupDto>> SearchMembersAsync(string keyword, int take, CancellationToken ct = default);

    Task<IReadOnlyList<PlanLookupDto>> GetActivePlansAsync(CancellationToken ct = default);

    Task<Member?> GetMemberAsync(int memberId, CancellationToken ct = default);

    Task<MembershipPlan?> GetPlanAsync(int planId, CancellationToken ct = default);

    /// <summary>TglBerakhir terbesar dari transaksi Lunas member ini (PRD 7.3), atau null.</summary>
    Task<DateOnly?> GetTglBerakhirTerakhirAsync(int memberId, CancellationToken ct = default);

    /// <summary>
    /// Simpan dalam SATU transaksi database (NFR-05): kunci transaksi member, pastikan TglBerakhir terakhir
    /// masih sama dengan <paramref name="tglBerakhirTerakhirSaatValidasi"/>, isi NoKuitansi berikutnya
    /// untuk tanggal TglTransaksi, lalu simpan. Mengembalikan false (tanpa menyimpan) bila masa aktif
    /// member berubah sejak divalidasi, mis. kasir lain baru saja memperpanjang.
    /// </summary>
    Task<bool> TryAddAsync(Transaction transaction, DateOnly? tglBerakhirTerakhirSaatValidasi, CancellationToken ct = default);

    /// <summary>Urut dari transaksi terbaru.</summary>
    Task<PagedResult<TransactionHistoryRowDto>> GetHistoryAsync(TransactionHistoryQuery query, CancellationToken ct = default);

    Task<ReceiptDto?> GetReceiptAsync(int transactionId, CancellationToken ct = default);

    /// <summary>Ubah status Lunas menjadi Batal (data tidak dihapus). False bila tidak ada atau sudah Batal.</summary>
    Task<bool> TryCancelAsync(int transactionId, string alasan, CancellationToken ct = default);
}
