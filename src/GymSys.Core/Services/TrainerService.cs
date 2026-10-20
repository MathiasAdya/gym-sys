using GymSys.Core.Entities;
using GymSys.Core.Interfaces;

namespace GymSys.Core.Services;

/// <summary>FR-10: kelola instruktur (Admin) dan penugasan PT ke member (Admin, Resepsionis).</summary>
public sealed class TrainerService(
    ITrainerRepository trainers,
    IMemberRepository members,
    ICurrentUserService currentUser,
    TimeProvider time) : ITrainerService
{
    /// <summary>Sama dengan panjang kolom Keahlian.</summary>
    public const int MaksPanjangKeahlian = 100;

    private const string HanyaAdmin = "Hanya Admin yang dapat mengelola data instruktur.";
    private const string BukanPetugas = "Hanya Admin dan Resepsionis yang dapat menugaskan instruktur.";

    public async Task<OperationResult> CreateAsync(TrainerInput input, CancellationToken ct = default)
    {
        if (!currentUser.IsInRole(UserRole.Admin)) return OperationResult.Fail(HanyaAdmin);
        if (Validasi(input) is { } error) return OperationResult.Fail(error);
        if (!await AkunTersediaAsync(input.UserId, null, ct))
            return OperationResult.Fail("Akun login sudah dipakai instruktur lain atau bukan akun Instruktur.");

        var trainer = new Trainer { IsActive = true };
        Salin(input, trainer);
        await trainers.AddAsync(trainer, ct);
        return OperationResult.Ok($"Instruktur {trainer.Nama} berhasil ditambahkan.");
    }

    public async Task<OperationResult> UpdateAsync(int trainerId, TrainerInput input, CancellationToken ct = default)
    {
        if (!currentUser.IsInRole(UserRole.Admin)) return OperationResult.Fail(HanyaAdmin);
        if (Validasi(input) is { } error) return OperationResult.Fail(error);

        var trainer = await trainers.GetByIdAsync(trainerId, ct);
        if (trainer is null) return OperationResult.Fail("Instruktur tidak ditemukan.");
        if (!await AkunTersediaAsync(input.UserId, trainerId, ct))
            return OperationResult.Fail("Akun login sudah dipakai instruktur lain atau bukan akun Instruktur.");

        Salin(input, trainer);
        await trainers.UpdateAsync(trainer, ct);
        return OperationResult.Ok("Data instruktur berhasil disimpan.");
    }

    public async Task<OperationResult> SetActiveAsync(int trainerId, bool isActive, CancellationToken ct = default)
    {
        if (!currentUser.IsInRole(UserRole.Admin)) return OperationResult.Fail(HanyaAdmin);
        if (!await trainers.SetActiveAsync(trainerId, isActive, ct))
            return OperationResult.Fail("Instruktur tidak ditemukan.");
        return OperationResult.Ok(isActive ? "Instruktur diaktifkan kembali." : "Instruktur dinonaktifkan.");
    }

    public async Task<OperationResult> AssignAsync(int memberId, int trainerId, CancellationToken ct = default)
    {
        if (!BolehTugaskan()) return OperationResult.Fail(BukanPetugas);

        var member = await members.GetByIdAsync(memberId, ct);
        if (member is null || !member.IsActive)
            return OperationResult.Fail("Member tidak ditemukan atau sudah nonaktif.");

        var trainer = await trainers.GetByIdAsync(trainerId, ct);
        if (trainer is null || !trainer.IsActive)
            return OperationResult.Fail("Instruktur tidak ditemukan atau sudah nonaktif.");

        var current = await trainers.GetActiveAssignmentAsync(memberId, ct);
        if (current?.TrainerId == trainerId)
            return OperationResult.Fail($"{member.NamaLengkap} sudah ditangani {trainer.Nama}.");

        await trainers.AssignAsync(memberId, trainerId, HariIni, ct);
        return OperationResult.Ok($"{member.NamaLengkap} sekarang ditangani {trainer.Nama}.");
    }

    public async Task<OperationResult> EndAssignmentAsync(int memberId, CancellationToken ct = default)
    {
        if (!BolehTugaskan()) return OperationResult.Fail(BukanPetugas);
        if (!await trainers.EndAssignmentAsync(memberId, HariIni, ct))
            return OperationResult.Fail("Member ini tidak punya instruktur aktif.");
        return OperationResult.Ok("Penugasan instruktur diakhiri.");
    }

    private async Task<bool> AkunTersediaAsync(int? userId, int? trainerId, CancellationToken ct)
    {
        if (userId is not { } id) return true;
        var accounts = await trainers.GetAvailableAccountsAsync(trainerId, ct);
        return accounts.Any(a => a.UserId == id);
    }

    private static string? Validasi(TrainerInput input) =>
        MasterDataRules.ValidasiNama(input.Nama)
        ?? (string.IsNullOrWhiteSpace(input.NoTelepon) ? null : MasterDataRules.ValidasiNoTelepon(input.NoTelepon))
        ?? (input.Keahlian?.Trim().Length > MaksPanjangKeahlian
            ? $"Keahlian maksimal {MaksPanjangKeahlian} karakter."
            : null);

    private static void Salin(TrainerInput input, Trainer trainer)
    {
        trainer.Nama = input.Nama.Trim();
        trainer.NoTelepon = string.IsNullOrWhiteSpace(input.NoTelepon) ? null : input.NoTelepon.Trim();
        trainer.Keahlian = string.IsNullOrWhiteSpace(input.Keahlian) ? null : input.Keahlian.Trim();
        trainer.UserId = input.UserId;
    }

    private bool BolehTugaskan() => currentUser.IsInRole(UserRole.Admin, UserRole.Resepsionis);

    private DateOnly HariIni => DateOnly.FromDateTime(time.GetLocalNow().DateTime);
}
