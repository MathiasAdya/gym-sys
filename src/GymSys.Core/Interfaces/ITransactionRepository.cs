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
}
