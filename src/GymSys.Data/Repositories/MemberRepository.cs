using GymSys.Core.Entities;
using GymSys.Core.Interfaces;
using GymSys.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace GymSys.Data.Repositories;

/// <summary>
/// IMemberRepository dengan EF Core (Anggota 3).
/// Filter status dihitung di SQL dengan aturan yang sama dengan MembershipCalculator dan vw_MemberStatus (PRD 7.3).
/// </summary>
public sealed class MemberRepository(IDbContextFactory<GymSysDbContext> factory) : IMemberRepository
{
    public async Task<PagedResult<MemberListRowDto>> SearchAsync(MemberListQuery query, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var members = db.Members.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            members = members.Where(m => m.NamaLengkap.Contains(keyword)
                                         || m.KodeMember.Contains(keyword)
                                         || m.NoTelepon.Contains(keyword));
        }
        if (query.TrainerId is { } trainerId)
            members = members.Where(m => db.PTAssignments.Any(a => a.IsActive && a.MemberId == m.MemberId && a.TrainerId == trainerId));

        var rows = members.Select(m => new
        {
            m.MemberId,
            m.KodeMember,
            m.NamaLengkap,
            m.NoTelepon,
            m.IsActive,
            Berakhir = db.Transactions
                .Where(t => t.MemberId == m.MemberId && t.Status == StatusTransaksi.Lunas)
                .Max(t => (DateOnly?)t.TglBerakhir),
        });

        // Aktif bila berakhir >= hari ini + 7; Akan Habis bila hari ini <= berakhir < hari ini + 7.
        var hariIni = query.HariIni;
        var batasAkanHabis = hariIni.AddDays(MembershipCalculator.HariAkanHabis);
        rows = query.Status switch
        {
            null => rows.Where(r => r.IsActive),
            MemberStatus.Nonaktif => rows.Where(r => !r.IsActive),
            MemberStatus.BelumBerlangganan => rows.Where(r => r.IsActive && r.Berakhir == null),
            MemberStatus.Kedaluwarsa => rows.Where(r => r.IsActive && r.Berakhir < hariIni),
            MemberStatus.AkanHabis => rows.Where(r => r.IsActive && r.Berakhir >= hariIni && r.Berakhir < batasAkanHabis),
            MemberStatus.Aktif => rows.Where(r => r.IsActive && r.Berakhir >= batasAkanHabis),
            _ => rows,
        };

        var total = await rows.CountAsync(ct);
        var page = Math.Max(1, query.Page);
        var items = await rows
            .OrderBy(r => r.NamaLengkap).ThenBy(r => r.MemberId)
            .Skip((page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var result = items
            .Select(r => new MemberListRowDto(r.MemberId, r.KodeMember, r.NamaLengkap, r.NoTelepon, r.Berakhir,
                MembershipCalculator.HitungStatus(r.IsActive, r.Berakhir, hariIni)))
            .ToList();
        return new PagedResult<MemberListRowDto>(result, total);
    }

    public async Task<Member?> GetByIdAsync(int memberId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Members.AsNoTracking().SingleOrDefaultAsync(m => m.MemberId == memberId, ct);
    }

    public async Task<bool> IsNoTeleponTakenAsync(string noTelepon, int? excludeMemberId = null, CancellationToken ct = default)
    {
        var exclude = excludeMemberId ?? 0;
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Members.AnyAsync(m => m.NoTelepon == noTelepon && m.MemberId != exclude, ct);
    }

    public async Task<Member> AddAsync(Member member, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // UPDLOCK + HOLDLOCK: dua resepsionis yang mendaftarkan member bersamaan menunggu bergiliran,
        // sehingga tidak mendapat Kode Member yang sama (pola yang sama dengan nomor kuitansi).
        var tahun = member.TglBergabung.Year;
        var pola = MasterDataRules.AwalanKodeMember(tahun) + "%";
        var kodeTerakhir = await db.Database.SqlQuery<string>(
                $"SELECT MAX(KodeMember) AS Value FROM Members WITH (UPDLOCK, HOLDLOCK) WHERE KodeMember LIKE {pola}")
            .SingleAsync(ct);
        member.KodeMember = MasterDataRules.KodeMemberBerikutnya(tahun, kodeTerakhir);

        db.Members.Add(member);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return member;
    }

    public async Task UpdateAsync(Member member, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var existing = await db.Members.SingleOrDefaultAsync(m => m.MemberId == member.MemberId, ct)
                       ?? throw new InvalidOperationException($"Member {member.MemberId} tidak ditemukan.");

        // KodeMember, TglBergabung, dan IsActive sengaja tidak diubah di sini.
        existing.NamaLengkap = member.NamaLengkap;
        existing.NoTelepon = member.NoTelepon;
        existing.Email = member.Email;
        existing.Alamat = member.Alamat;
        existing.TglLahir = member.TglLahir;
        existing.JenisKelamin = member.JenisKelamin;
        existing.KontakDarurat = member.KontakDarurat;
        existing.CatatanKesehatan = member.CatatanKesehatan;
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> SetActiveAsync(int memberId, bool isActive, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var changed = await db.Members
            .Where(m => m.MemberId == memberId)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsActive, isActive), ct);
        return changed == 1;
    }

    public async Task<IReadOnlyList<TransactionHistoryRowDto>> GetTransactionsAsync(int memberId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Transactions.AsNoTracking()
            .Where(t => t.MemberId == memberId)
            .OrderByDescending(t => t.TglTransaksi).ThenByDescending(t => t.TransactionId)
            .Select(t => new TransactionHistoryRowDto(
                t.TransactionId, t.NoKuitansi, t.TglTransaksi, t.Member!.KodeMember, t.Member.NamaLengkap, t.Plan!.NamaPaket,
                t.TglMulaiAktif, t.TglBerakhir, t.TotalBayar, t.MetodeBayar, t.Status, t.AlasanBatal))
            .ToListAsync(ct);
    }
}
