using GymSys.Core.Entities;
using GymSys.Core.Services;

namespace GymSys.Core.Interfaces;

/// <summary>
/// Filter Daftar Member (FR-04, FR-05). Page mulai dari 1.
/// Status null = semua status kecuali Nonaktif (member nonaktif tidak muncul di daftar default).
/// TrainerId terisi = hanya member yang sedang ditugaskan ke PT itu (FR-10, "Member Saya").
/// </summary>
public sealed record MemberListQuery(
    string? Keyword,
    MemberStatus? Status,
    DateOnly HariIni,
    int Page,
    int PageSize,
    int? TrainerId = null);

/// <summary>Satu baris Daftar Member. Status dihitung dengan aturan yang sama dengan vw_MemberStatus (PRD 7.3).</summary>
public sealed record MemberListRowDto(
    int MemberId,
    string KodeMember,
    string NamaLengkap,
    string NoTelepon,
    DateOnly? TglBerakhirTerakhir,
    MemberStatus Status)
{
    public string StatusLabel => Status.ToLabel();
}

/// <summary>Akses data member (Anggota 3). Penulisan data lewat IMemberService, bukan langsung dari ViewModel.</summary>
public interface IMemberRepository
{
    /// <summary>Cari berdasarkan kode, nama, atau no. HP (FR-05). Urut nama.</summary>
    Task<PagedResult<MemberListRowDto>> SearchAsync(MemberListQuery query, CancellationToken ct = default);

    Task<Member?> GetByIdAsync(int memberId, CancellationToken ct = default);

    /// <summary>No. HP wajib unik (FR-03). excludeMemberId diisi saat mengubah member.</summary>
    Task<bool> IsNoTeleponTakenAsync(string noTelepon, int? excludeMemberId = null, CancellationToken ct = default);

    /// <summary>
    /// Simpan member baru dalam SATU transaksi database (NFR-05): kunci kode terakhir pada tahun
    /// TglBergabung, isi KodeMember berikutnya lewat MasterDataRules.KodeMemberBerikutnya, lalu simpan.
    /// Mengembalikan member yang MemberId dan KodeMember-nya sudah terisi.
    /// </summary>
    Task<Member> AddAsync(Member member, CancellationToken ct = default);

    /// <summary>Ubah data profil. KodeMember, TglBergabung, dan IsActive tidak ikut diubah.</summary>
    Task UpdateAsync(Member member, CancellationToken ct = default);

    /// <summary>Nonaktifkan (soft delete) atau aktifkan kembali (FR-04). False bila member tidak ada.</summary>
    Task<bool> SetActiveAsync(int memberId, bool isActive, CancellationToken ct = default);

    /// <summary>Riwayat transaksi satu member untuk layar Detail Member, urut dari yang terbaru.</summary>
    Task<IReadOnlyList<TransactionHistoryRowDto>> GetTransactionsAsync(int memberId, CancellationToken ct = default);
}
