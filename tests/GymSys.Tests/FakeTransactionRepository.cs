using GymSys.Core.Entities;
using GymSys.Core.Interfaces;
using GymSys.Core.Services;

namespace GymSys.Tests;

/// <summary>Repository transaksi di memori untuk menguji TransactionService.</summary>
internal sealed class FakeTransactionRepository : ITransactionRepository
{
    public List<Member> Members { get; } = [];
    public List<MembershipPlan> Plans { get; } = [];
    public List<Transaction> Transactions { get; } = [];

    /// <summary>Simulasi kasir lain menyimpan transaksi di antara validasi dan penyimpanan.</summary>
    public Action? BeforeAdd { get; set; }

    public Task<IReadOnlyList<MemberLookupDto>> SearchMembersAsync(string keyword, int take, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<MemberLookupDto>>(Members
            .Where(m => m.IsActive && m.NamaLengkap.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .Take(take)
            .Select(m => new MemberLookupDto(m.MemberId, m.KodeMember, m.NamaLengkap, m.NoTelepon))
            .ToList());

    public Task<IReadOnlyList<PlanLookupDto>> GetActivePlansAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<PlanLookupDto>>(Plans
            .Where(p => p.IsActive)
            .Select(p => new PlanLookupDto(p.PlanId, p.NamaPaket, p.DurasiHari, p.Harga))
            .ToList());

    public Task<Member?> GetMemberAsync(int memberId, CancellationToken ct = default) =>
        Task.FromResult(Members.SingleOrDefault(m => m.MemberId == memberId));

    public Task<MembershipPlan?> GetPlanAsync(int planId, CancellationToken ct = default) =>
        Task.FromResult(Plans.SingleOrDefault(p => p.PlanId == planId));

    public Task<DateOnly?> GetTglBerakhirTerakhirAsync(int memberId, CancellationToken ct = default) =>
        Task.FromResult(MembershipCalculator.TglBerakhirTerakhir(Transactions.Where(t => t.MemberId == memberId)));

    public async Task<bool> TryAddAsync(Transaction transaction, DateOnly? tglBerakhirTerakhirSaatValidasi, CancellationToken ct = default)
    {
        BeforeAdd?.Invoke();
        if (await GetTglBerakhirTerakhirAsync(transaction.MemberId, ct) != tglBerakhirTerakhirSaatValidasi)
            return false;

        var tanggal = DateOnly.FromDateTime(transaction.TglTransaksi);
        var awalan = MembershipCalculator.AwalanKuitansi(tanggal);
        var terakhir = Transactions.Select(t => t.NoKuitansi).Where(n => n.StartsWith(awalan)).Max();
        transaction.NoKuitansi = MembershipCalculator.NoKuitansiBerikutnya(tanggal, terakhir);
        transaction.TransactionId = Transactions.Count + 1;
        Transactions.Add(transaction);
        return true;
    }

    // Query layar (riwayat, kuitansi) tidak dipakai TransactionService.
    public Task<PagedResult<TransactionHistoryRowDto>> GetHistoryAsync(TransactionHistoryQuery query, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<ReceiptDto?> GetReceiptAsync(int transactionId, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<bool> TryCancelAsync(int transactionId, string alasan, CancellationToken ct = default)
    {
        var t = Transactions.SingleOrDefault(x => x.TransactionId == transactionId && x.Status == StatusTransaksi.Lunas);
        if (t is null) return Task.FromResult(false);
        t.Status = StatusTransaksi.Batal;
        t.AlasanBatal = alasan;
        return Task.FromResult(true);
    }
}
