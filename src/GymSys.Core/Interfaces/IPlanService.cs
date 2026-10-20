namespace GymSys.Core.Interfaces;

/// <summary>Isi form paket (FR-09).</summary>
public sealed record PlanInput(string NamaPaket, int DurasiHari, decimal Harga, string? Deskripsi);

/// <summary>Penulisan data paket (FR-09). Hanya Admin; peran dicek di sini.</summary>
public interface IPlanService
{
    Task<OperationResult> CreateAsync(PlanInput input, CancellationToken ct = default);

    Task<OperationResult> UpdateAsync(int planId, PlanInput input, CancellationToken ct = default);

    Task<OperationResult> SetActiveAsync(int planId, bool isActive, CancellationToken ct = default);

    /// <summary>Paket yang sudah dipakai transaksi tidak bisa dihapus, hanya dinonaktifkan.</summary>
    Task<OperationResult> DeleteAsync(int planId, CancellationToken ct = default);
}
