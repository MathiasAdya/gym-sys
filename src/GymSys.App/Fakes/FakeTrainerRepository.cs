using GymSys.Core.Entities;
using GymSys.Core.Interfaces;

namespace GymSys.App.Fakes;

/// <summary>ITrainerRepository PALSU di atas FakeMasterDataStore. Diganti TrainerRepository (EF Core) nanti.</summary>
public sealed class FakeTrainerRepository(FakeMasterDataStore store) : ITrainerRepository
{
    public Task<IReadOnlyList<TrainerRowDto>> GetAllAsync(bool includeInactive, CancellationToken ct = default)
    {
        lock (store.Lock)
        {
            var rows = store.Trainers
                .Where(t => includeInactive || t.IsActive)
                .OrderBy(t => t.Nama)
                .Select(t => new TrainerRowDto(
                    t.TrainerId, t.Nama, t.NoTelepon, t.Keahlian, t.IsActive, t.UserId,
                    store.InstrukturAccounts.Where(a => a.UserId == t.UserId).Select(a => a.Username).FirstOrDefault(),
                    store.Assignments.Count(a => a.IsActive && a.TrainerId == t.TrainerId)))
                .ToList();
            return Task.FromResult<IReadOnlyList<TrainerRowDto>>(rows);
        }
    }

    public Task<Trainer?> GetByIdAsync(int trainerId, CancellationToken ct = default)
    {
        lock (store.Lock)
        {
            var trainer = store.Trainers.FirstOrDefault(t => t.TrainerId == trainerId);
            return Task.FromResult(trainer is null ? null : FakeMasterDataStore.Copy(trainer));
        }
    }

    public Task<Trainer?> GetByUserIdAsync(int userId, CancellationToken ct = default)
    {
        lock (store.Lock)
        {
            var trainer = store.Trainers.FirstOrDefault(t => t.UserId == userId);
            return Task.FromResult(trainer is null ? null : FakeMasterDataStore.Copy(trainer));
        }
    }

    public Task<IReadOnlyList<InstrukturAccountDto>> GetAvailableAccountsAsync(int? includeTrainerId = null, CancellationToken ct = default)
    {
        lock (store.Lock)
        {
            var dipakai = store.Trainers
                .Where(t => t.UserId is not null && t.TrainerId != includeTrainerId)
                .Select(t => t.UserId!.Value)
                .ToHashSet();
            var rows = store.InstrukturAccounts
                .Where(a => !dipakai.Contains(a.UserId))
                .Select(a => new InstrukturAccountDto(a.UserId, a.Username, a.NamaLengkap))
                .ToList();
            return Task.FromResult<IReadOnlyList<InstrukturAccountDto>>(rows);
        }
    }

    public Task AddAsync(Trainer trainer, CancellationToken ct = default)
    {
        lock (store.Lock)
        {
            var saved = FakeMasterDataStore.Copy(trainer);
            saved.TrainerId = store.NextTrainerId();
            trainer.TrainerId = saved.TrainerId;
            store.Trainers.Add(saved);
        }
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Trainer trainer, CancellationToken ct = default)
    {
        lock (store.Lock)
        {
            var existing = store.Trainers.First(t => t.TrainerId == trainer.TrainerId);
            existing.Nama = trainer.Nama;
            existing.NoTelepon = trainer.NoTelepon;
            existing.Keahlian = trainer.Keahlian;
            existing.UserId = trainer.UserId;
        }
        return Task.CompletedTask;
    }

    public Task<bool> SetActiveAsync(int trainerId, bool isActive, CancellationToken ct = default)
    {
        lock (store.Lock)
        {
            var trainer = store.Trainers.FirstOrDefault(t => t.TrainerId == trainerId);
            if (trainer is null) return Task.FromResult(false);
            trainer.IsActive = isActive;
            return Task.FromResult(true);
        }
    }

    public Task<PTAssignmentDto?> GetActiveAssignmentAsync(int memberId, CancellationToken ct = default)
    {
        lock (store.Lock)
        {
            var dto = store.Assignments
                .Where(a => a.IsActive && a.MemberId == memberId)
                .Select(a => new PTAssignmentDto(a.AssignmentId, a.MemberId, a.TrainerId,
                    store.Trainers.FirstOrDefault(t => t.TrainerId == a.TrainerId)?.Nama ?? "-", a.TglMulai))
                .FirstOrDefault();
            return Task.FromResult(dto);
        }
    }

    public Task AssignAsync(int memberId, int trainerId, DateOnly tglMulai, CancellationToken ct = default)
    {
        lock (store.Lock)
        {
            foreach (var lama in store.Assignments.Where(a => a.IsActive && a.MemberId == memberId))
            {
                lama.IsActive = false;
                lama.TglSelesai = tglMulai;
            }
            store.Assignments.Add(new PTAssignment
            {
                AssignmentId = store.NextAssignmentId(),
                MemberId = memberId,
                TrainerId = trainerId,
                TglMulai = tglMulai,
            });
        }
        return Task.CompletedTask;
    }

    public Task<bool> EndAssignmentAsync(int memberId, DateOnly tglSelesai, CancellationToken ct = default)
    {
        lock (store.Lock)
        {
            var aktif = store.Assignments.FirstOrDefault(a => a.IsActive && a.MemberId == memberId);
            if (aktif is null) return Task.FromResult(false);
            aktif.IsActive = false;
            aktif.TglSelesai = tglSelesai;
            return Task.FromResult(true);
        }
    }
}
