using GymSys.Core.Entities;
using GymSys.Core.Interfaces;
using GymSys.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace GymSys.Data.Repositories;

public sealed class TransactionRepository(IDbContextFactory<GymSysDbContext> factory) : ITransactionRepository
{
    public async Task<IReadOnlyList<MemberLookupDto>> SearchMembersAsync(string keyword, int take, CancellationToken ct = default)
    {
        keyword = keyword.Trim();
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Members.AsNoTracking()
            .Where(m => m.IsActive
                        && (m.NamaLengkap.Contains(keyword) || m.KodeMember.Contains(keyword) || m.NoTelepon.Contains(keyword)))
            .OrderBy(m => m.NamaLengkap)
            .Take(take)
            .Select(m => new MemberLookupDto(m.MemberId, m.KodeMember, m.NamaLengkap, m.NoTelepon))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PlanLookupDto>> GetActivePlansAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.MembershipPlans.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.DurasiHari)
            .Select(p => new PlanLookupDto(p.PlanId, p.NamaPaket, p.DurasiHari, p.Harga))
            .ToListAsync(ct);
    }

    public async Task<Member?> GetMemberAsync(int memberId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Members.AsNoTracking().SingleOrDefaultAsync(m => m.MemberId == memberId, ct);
    }

    public async Task<MembershipPlan?> GetPlanAsync(int planId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.MembershipPlans.AsNoTracking().SingleOrDefaultAsync(p => p.PlanId == planId, ct);
    }

    public async Task<DateOnly?> GetTglBerakhirTerakhirAsync(int memberId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Transactions
            .Where(t => t.MemberId == memberId && t.Status == StatusTransaksi.Lunas)
            .MaxAsync(t => (DateOnly?)t.TglBerakhir, ct);
    }

    public async Task<bool> TryAddAsync(Transaction transaction, DateOnly? tglBerakhirTerakhirSaatValidasi, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // UPDLOCK + HOLDLOCK mengunci rentang baris sampai commit: dua kasir yang memperpanjang member yang sama,
        // atau mencetak kuitansi di hari yang sama, menunggu bergiliran dan tidak mendapat nilai yang sama.
        var berakhirSekarang = await db.Database.SqlQuery<DateOnly?>(
                $"SELECT MAX(TglBerakhir) AS Value FROM Transactions WITH (UPDLOCK, HOLDLOCK) WHERE MemberId = {transaction.MemberId} AND Status = 'Lunas'")
            .SingleAsync(ct);
        if (berakhirSekarang != tglBerakhirTerakhirSaatValidasi)
            return false;

        var tanggal = DateOnly.FromDateTime(transaction.TglTransaksi);
        var pola = MembershipCalculator.AwalanKuitansi(tanggal) + "%";
        var nomorTerakhir = await db.Database.SqlQuery<string>(
                $"SELECT MAX(NoKuitansi) AS Value FROM Transactions WITH (UPDLOCK, HOLDLOCK) WHERE NoKuitansi LIKE {pola}")
            .SingleAsync(ct);
        transaction.NoKuitansi = MembershipCalculator.NoKuitansiBerikutnya(tanggal, nomorTerakhir);

        db.Transactions.Add(transaction);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }
}
