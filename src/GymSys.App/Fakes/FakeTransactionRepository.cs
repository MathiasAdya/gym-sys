using GymSys.Core.Entities;
using GymSys.Core.Interfaces;
using GymSys.Core.Services;

namespace GymSys.App.Fakes;

/// <summary>Data transaksi PALSU di memori untuk mode tanpa database. TransactionService tetap yang asli.</summary>
public sealed class FakeTransactionRepository : ITransactionRepository
{
    private readonly object _lock = new();

    private readonly List<Member> _members = new()
    {
        new Member { MemberId = 1, KodeMember = "M0001", NamaLengkap = "Andi Pratama", NoTelepon = "081234560001" },
        new Member { MemberId = 2, KodeMember = "M0002", NamaLengkap = "Siti Rahmawati", NoTelepon = "081234560002" },
        new Member { MemberId = 3, KodeMember = "M0003", NamaLengkap = "Budi Santoso", NoTelepon = "081234560003" },
        new Member { MemberId = 4, KodeMember = "M0004", NamaLengkap = "Dewi Lestari", NoTelepon = "081234560004" },
        new Member { MemberId = 5, KodeMember = "M0005", NamaLengkap = "Rizky Ramadhan", NoTelepon = "081234560005" },
    };

    private readonly List<MembershipPlan> _plans = new()
    {
        new MembershipPlan { PlanId = 1, NamaPaket = "Bronze 1 Bulan", DurasiHari = 30, Harga = 250_000m, IsActive = true },
        new MembershipPlan { PlanId = 2, NamaPaket = "Silver 3 Bulan", DurasiHari = 90, Harga = 675_000m, IsActive = true },
        new MembershipPlan { PlanId = 3, NamaPaket = "Gold 12 Bulan", DurasiHari = 365, Harga = 2_400_000m, IsActive = true },
    };

    private readonly List<Transaction> _transactions = new();

    private readonly FakeUserStore _users;

    public FakeTransactionRepository(FakeUserStore users)
    {
        _users = users;
        // Andi masih aktif 10 hari lagi, supaya alur perpanjangan bisa dicoba.
        var mulai = DateOnly.FromDateTime(DateTime.Today).AddDays(-19);
        _transactions.Add(new Transaction
        {
            TransactionId = 1, NoKuitansi = $"INV-{mulai:yyyyMMdd}-0001", MemberId = 1, PlanId = 1, UserIdKasir = 2,
            TglTransaksi = mulai.ToDateTime(new TimeOnly(9, 0)), TglMulaiAktif = mulai,
            TglBerakhir = MembershipCalculator.HitungTglBerakhir(mulai, 30),
            HargaPaket = 250_000m, TotalBayar = 250_000m, MetodeBayar = MetodeBayar.Tunai,
        });
    }

    public Task<IReadOnlyList<MemberLookupDto>> SearchMembersAsync(string keyword, int take, CancellationToken ct = default)
    {
        keyword = keyword.Trim();
        lock (_lock)
            return Task.FromResult<IReadOnlyList<MemberLookupDto>>(_members
                .Where(m => m.IsActive && (m.NamaLengkap.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                                           || m.KodeMember.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                                           || m.NoTelepon.Contains(keyword)))
                .OrderBy(m => m.NamaLengkap)
                .Take(take)
                .Select(m => new MemberLookupDto(m.MemberId, m.KodeMember, m.NamaLengkap, m.NoTelepon))
                .ToList());
    }

    public Task<IReadOnlyList<PlanLookupDto>> GetActivePlansAsync(CancellationToken ct = default)
    {
        lock (_lock)
            return Task.FromResult<IReadOnlyList<PlanLookupDto>>(_plans
                .Where(p => p.IsActive)
                .Select(p => new PlanLookupDto(p.PlanId, p.NamaPaket, p.DurasiHari, p.Harga))
                .ToList());
    }

    public Task<Member?> GetMemberAsync(int memberId, CancellationToken ct = default)
    {
        lock (_lock)
            return Task.FromResult(_members.SingleOrDefault(m => m.MemberId == memberId));
    }

    public Task<MembershipPlan?> GetPlanAsync(int planId, CancellationToken ct = default)
    {
        lock (_lock)
            return Task.FromResult(_plans.SingleOrDefault(p => p.PlanId == planId));
    }

    public Task<DateOnly?> GetTglBerakhirTerakhirAsync(int memberId, CancellationToken ct = default)
    {
        lock (_lock)
            return Task.FromResult(TglBerakhirTerakhir(memberId));
    }

    public Task<bool> TryAddAsync(Transaction transaction, DateOnly? tglBerakhirTerakhirSaatValidasi, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (TglBerakhirTerakhir(transaction.MemberId) != tglBerakhirTerakhirSaatValidasi)
                return Task.FromResult(false);

            var tanggal = DateOnly.FromDateTime(transaction.TglTransaksi);
            var awalan = MembershipCalculator.AwalanKuitansi(tanggal);
            var terakhir = _transactions.Select(t => t.NoKuitansi).Where(n => n.StartsWith(awalan, StringComparison.Ordinal)).Max();
            transaction.NoKuitansi = MembershipCalculator.NoKuitansiBerikutnya(tanggal, terakhir);
            transaction.TransactionId = _transactions.Max(t => t.TransactionId) + 1;
            _transactions.Add(transaction);
            return Task.FromResult(true);
        }
    }

    public Task<PagedResult<TransactionHistoryRowDto>> GetHistoryAsync(TransactionHistoryQuery query, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var rows = _transactions
                .Where(t => DateOnly.FromDateTime(t.TglTransaksi) >= query.From && DateOnly.FromDateTime(t.TglTransaksi) <= query.To)
                .Where(t => query.Status is null || t.Status == query.Status)
                .OrderByDescending(t => t.TglTransaksi).ThenByDescending(t => t.TransactionId)
                .ToList();
            var items = rows
                .Skip((Math.Max(1, query.Page) - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(t =>
                {
                    var m = _members.Single(x => x.MemberId == t.MemberId);
                    return new TransactionHistoryRowDto(t.TransactionId, t.NoKuitansi, t.TglTransaksi, m.KodeMember, m.NamaLengkap,
                        _plans.Single(p => p.PlanId == t.PlanId).NamaPaket, t.TglMulaiAktif, t.TglBerakhir, t.TotalBayar,
                        t.MetodeBayar, t.Status, t.AlasanBatal);
                })
                .ToList();
            return Task.FromResult(new PagedResult<TransactionHistoryRowDto>(items, rows.Count));
        }
    }

    public Task<ReceiptDto?> GetReceiptAsync(int transactionId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var t = _transactions.SingleOrDefault(x => x.TransactionId == transactionId);
            if (t is null) return Task.FromResult<ReceiptDto?>(null);
            var m = _members.Single(x => x.MemberId == t.MemberId);
            var p = _plans.Single(x => x.PlanId == t.PlanId);
            return Task.FromResult<ReceiptDto?>(new ReceiptDto(t.TransactionId, t.NoKuitansi, t.TglTransaksi, m.KodeMember,
                m.NamaLengkap, p.NamaPaket, p.DurasiHari, t.TglMulaiAktif, t.TglBerakhir, t.HargaPaket, t.Diskon, t.TotalBayar,
                t.MetodeBayar, _users.FindById(t.UserIdKasir)?.NamaLengkap ?? "-", t.Status, t.AlasanBatal));
        }
    }

    public Task<bool> TryCancelAsync(int transactionId, string alasan, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var t = _transactions.SingleOrDefault(x => x.TransactionId == transactionId && x.Status == StatusTransaksi.Lunas);
            if (t is null) return Task.FromResult(false);
            t.Status = StatusTransaksi.Batal;
            t.AlasanBatal = alasan;
            return Task.FromResult(true);
        }
    }

    private DateOnly? TglBerakhirTerakhir(int memberId) =>
        MembershipCalculator.TglBerakhirTerakhir(_transactions.Where(t => t.MemberId == memberId));
}
