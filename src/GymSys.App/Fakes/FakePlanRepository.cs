using GymSys.Core.Entities;
using GymSys.Core.Interfaces;

namespace GymSys.App.Fakes;

/// <summary>IPlanRepository PALSU di atas FakeMasterDataStore. Diganti PlanRepository (EF Core) nanti.</summary>
public sealed class FakePlanRepository(FakeMasterDataStore store) : IPlanRepository
{
    public Task<IReadOnlyList<PlanRowDto>> GetAllAsync(CancellationToken ct = default)
    {
        lock (store.Lock)
        {
            var rows = store.Plans
                .OrderBy(p => p.DurasiHari).ThenBy(p => p.NamaPaket)
                .Select(p => new PlanRowDto(p.PlanId, p.NamaPaket, p.DurasiHari, p.Harga, p.Deskripsi, p.IsActive,
                    store.Transactions.Any(t => t.PlanId == p.PlanId)))
                .ToList();
            return Task.FromResult<IReadOnlyList<PlanRowDto>>(rows);
        }
    }

    public Task<MembershipPlan?> GetByIdAsync(int planId, CancellationToken ct = default)
    {
        lock (store.Lock)
        {
            var plan = store.Plans.FirstOrDefault(p => p.PlanId == planId);
            return Task.FromResult(plan is null ? null : FakeMasterDataStore.Copy(plan));
        }
    }

    public Task<bool> IsNamaTakenAsync(string namaPaket, int? excludePlanId = null, CancellationToken ct = default)
    {
        lock (store.Lock)
            return Task.FromResult(store.Plans.Any(p =>
                string.Equals(p.NamaPaket, namaPaket.Trim(), StringComparison.OrdinalIgnoreCase) && p.PlanId != excludePlanId));
    }

    public Task<bool> IsUsedAsync(int planId, CancellationToken ct = default)
    {
        lock (store.Lock)
            return Task.FromResult(store.Transactions.Any(t => t.PlanId == planId));
    }

    public Task AddAsync(MembershipPlan plan, CancellationToken ct = default)
    {
        lock (store.Lock)
        {
            var saved = FakeMasterDataStore.Copy(plan);
            saved.PlanId = store.NextPlanId();
            plan.PlanId = saved.PlanId;
            store.Plans.Add(saved);
        }
        return Task.CompletedTask;
    }

    public Task UpdateAsync(MembershipPlan plan, CancellationToken ct = default)
    {
        lock (store.Lock)
        {
            var existing = store.Plans.First(p => p.PlanId == plan.PlanId);
            existing.NamaPaket = plan.NamaPaket;
            existing.DurasiHari = plan.DurasiHari;
            existing.Harga = plan.Harga;
            existing.Deskripsi = plan.Deskripsi;
        }
        return Task.CompletedTask;
    }

    public Task<bool> SetActiveAsync(int planId, bool isActive, CancellationToken ct = default)
    {
        lock (store.Lock)
        {
            var plan = store.Plans.FirstOrDefault(p => p.PlanId == planId);
            if (plan is null) return Task.FromResult(false);
            plan.IsActive = isActive;
            return Task.FromResult(true);
        }
    }

    public Task<bool> TryDeleteAsync(int planId, CancellationToken ct = default)
    {
        lock (store.Lock)
        {
            if (store.Transactions.Any(t => t.PlanId == planId))
                return Task.FromResult(false);
            return Task.FromResult(store.Plans.RemoveAll(p => p.PlanId == planId) > 0);
        }
    }
}
