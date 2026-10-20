using GymSys.Core.Entities;
using GymSys.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GymSys.Data.Repositories;

/// <summary>IPlanRepository dengan EF Core (Anggota 3).</summary>
public sealed class PlanRepository(IDbContextFactory<GymSysDbContext> factory) : IPlanRepository
{
    public async Task<IReadOnlyList<PlanRowDto>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.MembershipPlans.AsNoTracking()
            .OrderBy(p => p.DurasiHari).ThenBy(p => p.NamaPaket)
            .Select(p => new PlanRowDto(
                p.PlanId, p.NamaPaket, p.DurasiHari, p.Harga, p.Deskripsi, p.IsActive,
                db.Transactions.Any(t => t.PlanId == p.PlanId)))
            .ToListAsync(ct);
    }

    public async Task<MembershipPlan?> GetByIdAsync(int planId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.MembershipPlans.AsNoTracking().SingleOrDefaultAsync(p => p.PlanId == planId, ct);
    }

    public async Task<bool> IsNamaTakenAsync(string namaPaket, int? excludePlanId = null, CancellationToken ct = default)
    {
        var nama = namaPaket.Trim();
        var exclude = excludePlanId ?? 0;
        await using var db = await factory.CreateDbContextAsync(ct);
        // Collation default SQL Server tidak membedakan huruf besar/kecil.
        return await db.MembershipPlans.AnyAsync(p => p.NamaPaket == nama && p.PlanId != exclude, ct);
    }

    public async Task<bool> IsUsedAsync(int planId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Transactions.AnyAsync(t => t.PlanId == planId, ct);
    }

    public async Task AddAsync(MembershipPlan plan, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        db.MembershipPlans.Add(plan);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(MembershipPlan plan, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var existing = await db.MembershipPlans.SingleOrDefaultAsync(p => p.PlanId == plan.PlanId, ct)
                       ?? throw new InvalidOperationException($"Paket {plan.PlanId} tidak ditemukan.");

        existing.NamaPaket = plan.NamaPaket;
        existing.DurasiHari = plan.DurasiHari;
        existing.Harga = plan.Harga;
        existing.Deskripsi = plan.Deskripsi;
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> SetActiveAsync(int planId, bool isActive, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var changed = await db.MembershipPlans
            .Where(p => p.PlanId == planId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, isActive), ct);
        return changed == 1;
    }

    public async Task<bool> TryDeleteAsync(int planId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (await db.Transactions.AnyAsync(t => t.PlanId == planId, ct))
            return false;

        // Foreign key di Transactions tetap menolak bila ada transaksi yang masuk di antara cek dan hapus.
        var deleted = await db.MembershipPlans
            .Where(p => p.PlanId == planId)
            .ExecuteDeleteAsync(ct);
        return deleted == 1;
    }
}
