using GymSys.Core.Entities;
using GymSys.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GymSys.Data.Repositories;

/// <summary>ITrainerRepository dengan EF Core (Anggota 3): data instruktur dan penugasan PT (FR-10).</summary>
public sealed class TrainerRepository(IDbContextFactory<GymSysDbContext> factory) : ITrainerRepository
{
    public async Task<IReadOnlyList<TrainerRowDto>> GetAllAsync(bool includeInactive, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var trainers = db.Trainers.AsNoTracking();
        if (!includeInactive)
            trainers = trainers.Where(t => t.IsActive);

        return await trainers
            .OrderBy(t => t.Nama)
            .Select(t => new TrainerRowDto(
                t.TrainerId, t.Nama, t.NoTelepon, t.Keahlian, t.IsActive, t.UserId,
                t.User != null ? t.User.Username : null,
                t.Assignments.Count(a => a.IsActive)))
            .ToListAsync(ct);
    }

    public async Task<Trainer?> GetByIdAsync(int trainerId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Trainers.AsNoTracking().SingleOrDefaultAsync(t => t.TrainerId == trainerId, ct);
    }

    public async Task<Trainer?> GetByUserIdAsync(int userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Trainers.AsNoTracking().SingleOrDefaultAsync(t => t.UserId == userId, ct);
    }

    public async Task<IReadOnlyList<InstrukturAccountDto>> GetAvailableAccountsAsync(int? includeTrainerId = null, CancellationToken ct = default)
    {
        var include = includeTrainerId ?? 0;
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Users.AsNoTracking()
            .Where(u => u.Role == UserRole.Instruktur
                        && u.IsActive
                        && !db.Trainers.Any(t => t.UserId == u.UserId && t.TrainerId != include))
            .OrderBy(u => u.Username)
            .Select(u => new InstrukturAccountDto(u.UserId, u.Username, u.NamaLengkap))
            .ToListAsync(ct);
    }

    public async Task AddAsync(Trainer trainer, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        db.Trainers.Add(trainer);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Trainer trainer, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var existing = await db.Trainers.SingleOrDefaultAsync(t => t.TrainerId == trainer.TrainerId, ct)
                       ?? throw new InvalidOperationException($"Instruktur {trainer.TrainerId} tidak ditemukan.");

        existing.Nama = trainer.Nama;
        existing.NoTelepon = trainer.NoTelepon;
        existing.Keahlian = trainer.Keahlian;
        existing.UserId = trainer.UserId;
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> SetActiveAsync(int trainerId, bool isActive, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var changed = await db.Trainers
            .Where(t => t.TrainerId == trainerId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsActive, isActive), ct);
        return changed == 1;
    }

    public async Task<PTAssignmentDto?> GetActiveAssignmentAsync(int memberId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.PTAssignments.AsNoTracking()
            .Where(a => a.IsActive && a.MemberId == memberId)
            .Select(a => new PTAssignmentDto(a.AssignmentId, a.MemberId, a.TrainerId, a.Trainer!.Nama, a.TglMulai))
            .FirstOrDefaultAsync(ct);
    }

    public async Task AssignAsync(int memberId, int trainerId, DateOnly tglMulai, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Akhiri penugasan aktif lama dulu: index UX_PT_MemberAktif hanya mengizinkan satu PT aktif per member.
        await db.PTAssignments
            .Where(a => a.IsActive && a.MemberId == memberId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.IsActive, false)
                .SetProperty(a => a.TglSelesai, tglMulai), ct);

        db.PTAssignments.Add(new PTAssignment
        {
            MemberId = memberId,
            TrainerId = trainerId,
            TglMulai = tglMulai,
            IsActive = true,
        });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task<bool> EndAssignmentAsync(int memberId, DateOnly tglSelesai, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var changed = await db.PTAssignments
            .Where(a => a.IsActive && a.MemberId == memberId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.IsActive, false)
                .SetProperty(a => a.TglSelesai, tglSelesai), ct);
        return changed > 0;
    }
}
