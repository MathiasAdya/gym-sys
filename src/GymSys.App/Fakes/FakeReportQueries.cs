using GymSys.Core.Interfaces;

namespace GymSys.App.Fakes;

/// <summary>Data laporan PALSU (dibuat deterministik). Ganti dengan ReportQueries (ADO.NET) saat database siap.</summary>
public sealed class FakeReportQueries : IReportQueries
{
    private static readonly string[] Names =
    {
        "Andi Pratama", "Siti Rahmawati", "Budi Santoso", "Dewi Lestari", "Rizky Ramadhan", "Maya Putri",
        "Fajar Nugroho", "Intan Permata", "Hendra Wijaya", "Lina Marlina", "Agus Setiawan", "Nadia Safitri"
    };

    private static readonly (string Name, decimal Price)[] Plans =
    {
        ("Bronze 1 Bulan", 250_000m),
        ("Silver 3 Bulan", 650_000m),
        ("Gold 12 Bulan", 2_200_000m),
    };

    private readonly List<TransactionRowDto> _transactions = BuildTransactions();

    private static List<TransactionRowDto> BuildTransactions()
    {
        var list = new List<TransactionRowDto>();
        for (var i = 0; i < 48; i++)
        {
            var date = DateTime.Today.AddDays(-(i * 2 + i % 3)).AddHours(9 + i % 8);
            var plan = Plans[i % Plans.Length];
            var discount = i % 7 == 0 ? 50_000m : 0m;
            list.Add(new TransactionRowDto(
                NoKuitansi: $"INV-{date:yyyyMMdd}-{i + 1:0000}",
                TglTransaksi: date,
                NamaMember: Names[i % Names.Length],
                NamaPaket: plan.Name,
                TotalBayar: plan.Price - discount,
                MetodeBayar: i % 2 == 0 ? "Tunai" : "Transfer",
                Status: i % 11 == 0 ? "Batal" : "Lunas"));
        }

        return list;
    }

    private IEnumerable<TransactionRowDto> InRange(DateTime from, DateTime to)
        => _transactions.Where(t => t.TglTransaksi.Date >= from.Date && t.TglTransaksi.Date <= to.Date);

    public async Task<IReadOnlyList<TransactionRowDto>> GetTransactionsAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await Task.Delay(350, ct);
        return InRange(from, to).OrderByDescending(t => t.TglTransaksi).ToList();
    }

    public async Task<IReadOnlyList<MemberStatusRowDto>> GetMemberStatusAsync(CancellationToken ct = default)
    {
        await Task.Delay(350, ct);

        var today = DateTime.Today;
        var list = new List<MemberStatusRowDto>();
        for (var i = 0; i < 24; i++)
        {
            var kind = i % 6;
            DateTime? end;
            string status;
            switch (kind)
            {
                case 0: end = today.AddDays(20 + i); status = "Aktif"; break;
                case 1: end = today.AddDays(1 + i % 6); status = "Akan Habis"; break;
                case 2: end = today.AddDays(-(3 + i)); status = "Kedaluwarsa"; break;
                case 3: end = null; status = "Belum Berlangganan"; break;
                case 4: end = today.AddDays(45); status = "Aktif"; break;
                default: end = today.AddDays(-60); status = "Nonaktif"; break;
            }

            list.Add(new MemberStatusRowDto(
                $"GYM2026-{i + 1:0000}",
                Names[i % Names.Length] + (i >= Names.Length ? " Jr." : ""),
                $"0812345{67000 + i}",
                end,
                status));
        }

        return list;
    }

    public async Task<IReadOnlyList<RevenueByPlanDto>> GetRevenueByPlanAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await Task.Delay(350, ct);
        return InRange(from, to)
            .Where(t => t.Status == "Lunas")
            .GroupBy(t => t.NamaPaket)
            .Select(g => new RevenueByPlanDto(g.Key, g.Count(), g.Sum(t => t.TotalBayar)))
            .OrderByDescending(r => r.TotalPendapatan)
            .ToList();
    }
}
