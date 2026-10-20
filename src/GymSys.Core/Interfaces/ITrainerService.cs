namespace GymSys.Core.Interfaces;

/// <summary>Isi form instruktur. UserId null bila belum dihubungkan ke akun login.</summary>
public sealed record TrainerInput(string Nama, string? NoTelepon, string? Keahlian, int? UserId);

/// <summary>
/// FR-10. Kelola data instruktur: hanya Admin.
/// Tugaskan dan akhiri penugasan PT: Admin dan Resepsionis. Peran dicek di sini.
/// </summary>
public interface ITrainerService
{
    Task<OperationResult> CreateAsync(TrainerInput input, CancellationToken ct = default);

    Task<OperationResult> UpdateAsync(int trainerId, TrainerInput input, CancellationToken ct = default);

    Task<OperationResult> SetActiveAsync(int trainerId, bool isActive, CancellationToken ct = default);

    /// <summary>Tugaskan PT ke member; penugasan aktif lama otomatis diakhiri (satu member maksimal satu PT aktif).</summary>
    Task<OperationResult> AssignAsync(int memberId, int trainerId, CancellationToken ct = default);

    Task<OperationResult> EndAssignmentAsync(int memberId, CancellationToken ct = default);
}
