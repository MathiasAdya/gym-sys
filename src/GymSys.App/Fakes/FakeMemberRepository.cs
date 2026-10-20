using GymSys.Core.Entities;
using GymSys.Core.Interfaces;
using GymSys.Core.Services;

namespace GymSys.App.Fakes;

/// <summary>IMemberRepository PALSU di atas FakeMasterDataStore. Diganti MemberRepository (EF Core) nanti.</summary>
public sealed class FakeMemberRepository(FakeMasterDataStore store) : IMemberRepository
{
    public async Task<PagedResult<MemberListRowDto>> SearchAsync(MemberListQuery query, CancellationToken ct = default)
    {
        await Task.Delay(150, ct); // simulasi akses database, supaya LoadingOverlay terlihat

        var keyword = query.Keyword?.Trim() ?? "";
        lock (store.Lock)
        {
            var rows = store.Members
                .Where(m => keyword.Length == 0
                            || m.NamaLengkap.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                            || m.KodeMember.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                            || m.NoTelepon.Contains(keyword))
                .Where(m => query.TrainerId is not { } trainerId
                            || store.Assignments.Any(a => a.IsActive && a.MemberId == m.MemberId && a.TrainerId == trainerId))
                .Select(m =>
                {
                    var berakhir = store.TglBerakhirTerakhir(m.MemberId);
                    return new MemberListRowDto(m.MemberId, m.KodeMember, m.NamaLengkap, m.NoTelepon, berakhir,
                        MembershipCalculator.HitungStatus(m.IsActive, berakhir, query.HariIni));
                })
                .Where(r => query.Status is { } status ? r.Status == status : r.Status != MemberStatus.Nonaktif)
                .OrderBy(r => r.NamaLengkap)
                .ToList();

            var page = Math.Max(1, query.Page);
            var items = rows.Skip((page - 1) * query.PageSize).Take(query.PageSize).ToList();
            return new PagedResult<MemberListRowDto>(items, rows.Count);
        }
    }

    public Task<Member?> GetByIdAsync(int memberId, CancellationToken ct = default)
    {
        lock (store.Lock)
        {
            var member = store.Members.FirstOrDefault(m => m.MemberId == memberId);
            return Task.FromResult(member is null ? null : FakeMasterDataStore.Copy(member));
        }
    }

    public Task<bool> IsNoTeleponTakenAsync(string noTelepon, int? excludeMemberId = null, CancellationToken ct = default)
    {
        lock (store.Lock)
            return Task.FromResult(store.Members.Any(m => m.NoTelepon == noTelepon && m.MemberId != excludeMemberId));
    }

    public Task<Member> AddAsync(Member member, CancellationToken ct = default)
    {
        lock (store.Lock)
        {
            var saved = FakeMasterDataStore.Copy(member);
            saved.MemberId = store.NextMemberId();
            saved.KodeMember = store.KodeBerikutnya(member.TglBergabung.Year);
            store.Members.Add(saved);
            return Task.FromResult(FakeMasterDataStore.Copy(saved));
        }
    }

    public Task UpdateAsync(Member member, CancellationToken ct = default)
    {
        lock (store.Lock)
        {
            var existing = store.Members.First(m => m.MemberId == member.MemberId);
            existing.NamaLengkap = member.NamaLengkap;
            existing.NoTelepon = member.NoTelepon;
            existing.Email = member.Email;
            existing.Alamat = member.Alamat;
            existing.TglLahir = member.TglLahir;
            existing.JenisKelamin = member.JenisKelamin;
            existing.KontakDarurat = member.KontakDarurat;
            existing.CatatanKesehatan = member.CatatanKesehatan;
        }
        return Task.CompletedTask;
    }

    public Task<bool> SetActiveAsync(int memberId, bool isActive, CancellationToken ct = default)
    {
        lock (store.Lock)
        {
            var member = store.Members.FirstOrDefault(m => m.MemberId == memberId);
            if (member is null) return Task.FromResult(false);
            member.IsActive = isActive;
            return Task.FromResult(true);
        }
    }

    public Task<IReadOnlyList<TransactionHistoryRowDto>> GetTransactionsAsync(int memberId, CancellationToken ct = default)
    {
        lock (store.Lock)
        {
            var member = store.Members.FirstOrDefault(m => m.MemberId == memberId);
            if (member is null)
                return Task.FromResult<IReadOnlyList<TransactionHistoryRowDto>>(Array.Empty<TransactionHistoryRowDto>());

            var rows = store.Transactions
                .Where(t => t.MemberId == memberId)
                .OrderByDescending(t => t.TglTransaksi)
                .Select(t => new TransactionHistoryRowDto(
                    t.TransactionId, t.NoKuitansi, t.TglTransaksi, member.KodeMember, member.NamaLengkap,
                    store.Plans.FirstOrDefault(p => p.PlanId == t.PlanId)?.NamaPaket ?? "-",
                    t.TglMulaiAktif, t.TglBerakhir, t.TotalBayar, t.MetodeBayar, t.Status, t.AlasanBatal))
                .ToList();
            return Task.FromResult<IReadOnlyList<TransactionHistoryRowDto>>(rows);
        }
    }
}
