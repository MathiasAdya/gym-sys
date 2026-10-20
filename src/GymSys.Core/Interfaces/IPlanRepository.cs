using GymSys.Core.Entities;

namespace GymSys.Core.Interfaces;

/// <summary>Satu baris daftar paket. IsUsed = sudah dipakai minimal satu transaksi.</summary>
public sealed record PlanRowDto(
    int PlanId,
    string NamaPaket,
    int DurasiHari,
    decimal Harga,
    string? Deskripsi,
    bool IsActive,
    bool IsUsed);

/// <summary>Akses data paket membership (Anggota 3). Penulisan data lewat IPlanService.</summary>
public interface IPlanRepository
{
    /// <summary>Semua paket termasuk yang nonaktif, urut durasi.</summary>
    Task<IReadOnlyList<PlanRowDto>> GetAllAsync(CancellationToken ct = default);

    Task<MembershipPlan?> GetByIdAsync(int planId, CancellationToken ct = default);

    /// <summary>Nama paket wajib unik (FR-09). excludePlanId diisi saat mengubah paket.</summary>
    Task<bool> IsNamaTakenAsync(string namaPaket, int? excludePlanId = null, CancellationToken ct = default);

    Task<bool> IsUsedAsync(int planId, CancellationToken ct = default);

    Task AddAsync(MembershipPlan plan, CancellationToken ct = default);

    Task UpdateAsync(MembershipPlan plan, CancellationToken ct = default);

    /// <summary>False bila paket tidak ada.</summary>
    Task<bool> SetActiveAsync(int planId, bool isActive, CancellationToken ct = default);

    /// <summary>Hapus permanen hanya bila belum pernah dipakai transaksi (FR-09). False bila sudah dipakai atau tidak ada.</summary>
    Task<bool> TryDeleteAsync(int planId, CancellationToken ct = default);
}
