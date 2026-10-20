using GymSys.Core.Entities;

namespace GymSys.Core.Interfaces;

/// <summary>Satu baris daftar instruktur. Username null bila belum terhubung ke akun login.</summary>
public sealed record TrainerRowDto(
    int TrainerId,
    string Nama,
    string? NoTelepon,
    string? Keahlian,
    bool IsActive,
    int? UserId,
    string? Username,
    int JumlahMemberAktif);

/// <summary>Penugasan PT yang sedang aktif untuk satu member.</summary>
public sealed record PTAssignmentDto(
    int AssignmentId,
    int MemberId,
    int TrainerId,
    string NamaTrainer,
    DateOnly TglMulai);

/// <summary>Akun login berperan Instruktur yang bisa dihubungkan ke data instruktur.</summary>
public sealed record InstrukturAccountDto(int UserId, string Username, string NamaLengkap)
{
    public string Display => $"{Username} · {NamaLengkap}";
}

/// <summary>Akses data instruktur dan penugasan PT (Anggota 3). Penulisan data lewat ITrainerService.</summary>
public interface ITrainerRepository
{
    Task<IReadOnlyList<TrainerRowDto>> GetAllAsync(bool includeInactive, CancellationToken ct = default);

    Task<Trainer?> GetByIdAsync(int trainerId, CancellationToken ct = default);

    /// <summary>Instruktur milik akun yang sedang login, untuk filter "Member Saya".</summary>
    Task<Trainer?> GetByUserIdAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// Akun berperan Instruktur yang belum terhubung ke instruktur mana pun.
    /// includeTrainerId: akun milik instruktur itu tetap disertakan (saat mengubah data instruktur).
    /// </summary>
    Task<IReadOnlyList<InstrukturAccountDto>> GetAvailableAccountsAsync(int? includeTrainerId = null, CancellationToken ct = default);

    Task AddAsync(Trainer trainer, CancellationToken ct = default);

    Task UpdateAsync(Trainer trainer, CancellationToken ct = default);

    /// <summary>False bila instruktur tidak ada.</summary>
    Task<bool> SetActiveAsync(int trainerId, bool isActive, CancellationToken ct = default);

    Task<PTAssignmentDto?> GetActiveAssignmentAsync(int memberId, CancellationToken ct = default);

    /// <summary>
    /// SATU transaksi database (FR-10, satu member maksimal satu PT aktif): akhiri penugasan aktif lama
    /// (TglSelesai = tglMulai, IsActive = 0), lalu buat penugasan baru.
    /// </summary>
    Task AssignAsync(int memberId, int trainerId, DateOnly tglMulai, CancellationToken ct = default);

    /// <summary>Akhiri penugasan aktif member. False bila tidak ada penugasan aktif.</summary>
    Task<bool> EndAssignmentAsync(int memberId, DateOnly tglSelesai, CancellationToken ct = default);
}
