using System.Globalization;
using GymSys.Core.Interfaces;

namespace GymSys.App.Fakes;

/// <summary>Data dashboard PALSU. Ganti dengan DashboardQueries (ADO.NET) saat database siap.</summary>
public sealed class FakeDashboardQueries : IDashboardQueries
{
    private static readonly CultureInfo Id = CultureInfo.GetCultureInfo("id-ID");

    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken ct = default)
    {
        await Task.Delay(300, ct);
        return new DashboardSummary(
            TotalMemberAktif: 142,
            MemberBaruBulanIni: 18,
            AkanHabis7Hari: 6,
            PendapatanBulanIni: 18_450_000m);
    }

    public async Task<IReadOnlyList<MonthlyCount>> GetRegistrationTrendAsync(int months, CancellationToken ct = default)
    {
        await Task.Delay(300, ct);

        int[] sample = { 8, 11, 9, 14, 12, 17, 15, 19, 16, 21, 18, 18 };
        var result = new List<MonthlyCount>();
        for (var i = 0; i < months; i++)
        {
            var date = DateTime.Today.AddMonths(i - (months - 1));
            result.Add(new MonthlyCount(date.ToString("MMM", Id), sample[i % sample.Length]));
        }

        return result;
    }

    public async Task<IReadOnlyList<ExpiringMemberDto>> GetExpiringMembersAsync(int withinDays, CancellationToken ct = default)
    {
        await Task.Delay(300, ct);

        var today = DateTime.Today;
        (string Kode, string Nama, string Hp, int Sisa)[] raw =
        {
            ("GYM2026-0007", "Andi Pratama", "081234567801", 0),
            ("GYM2026-0013", "Siti Rahmawati", "081234567802", 1),
            ("GYM2026-0021", "Budi Santoso", "081234567803", 2),
            ("GYM2026-0034", "Dewi Lestari", "081234567804", 3),
            ("GYM2026-0042", "Rizky Ramadhan", "081234567805", 5),
            ("GYM2026-0058", "Maya Putri", "081234567806", 7),
        };

        return raw
            .Where(r => r.Sisa <= withinDays)
            .OrderBy(r => r.Sisa)
            .Select(r => new ExpiringMemberDto(r.Kode, r.Nama, r.Hp, today.AddDays(r.Sisa), r.Sisa))
            .ToList();
    }
}
