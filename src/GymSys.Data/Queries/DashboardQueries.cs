using System.Globalization;
using GymSys.Core.Interfaces;
using Microsoft.Data.SqlClient;

namespace GymSys.Data.Queries;

/// <summary>
/// Query agregat dashboard (FR-02) dengan ADO.NET. Semua query terparameter dan async (NFR-01, NFR-03).
/// Status member diambil dari view vw_MemberStatus supaya angkanya sama persis dengan laporan.
/// </summary>
public sealed class DashboardQueries : IDashboardQueries
{
    private static readonly CultureInfo Id = CultureInfo.GetCultureInfo("id-ID");
    private readonly SqlConnectionFactory _db;

    public DashboardQueries(SqlConnectionFactory db) => _db = db;

    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken ct = default)
    {
        const string sql = """
            DECLARE @awalBulan DATE = DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1);
            DECLARE @awalBulanDepan DATE = DATEADD(MONTH, 1, @awalBulan);

            SELECT
                (SELECT COUNT(*) FROM vw_MemberStatus
                  WHERE Status IN ('Aktif', 'Akan Habis'))                         AS TotalMemberAktif,
                (SELECT COUNT(*) FROM Members
                  WHERE TglBergabung >= @awalBulan AND TglBergabung < @awalBulanDepan) AS MemberBaruBulanIni,
                (SELECT COUNT(*) FROM vw_MemberStatus
                  WHERE Status = 'Akan Habis')                                     AS AkanHabis7Hari,
                (SELECT ISNULL(SUM(TotalBayar), 0) FROM Transactions
                  WHERE Status = 'Lunas'
                    AND TglTransaksi >= @awalBulan AND TglTransaksi < @awalBulanDepan) AS PendapatanBulanIni;
            """;

        await using var connection = await _db.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(ct);

        if (!await reader.ReadAsync(ct))
            return new DashboardSummary(0, 0, 0, 0m);

        return new DashboardSummary(
            TotalMemberAktif: reader.GetInt32(0),
            MemberBaruBulanIni: reader.GetInt32(1),
            AkanHabis7Hari: reader.GetInt32(2),
            PendapatanBulanIni: reader.GetDecimal(3));
    }

    public async Task<IReadOnlyList<MonthlyCount>> GetRegistrationTrendAsync(int months, CancellationToken ct = default)
    {
        if (months < 1)
            throw new ArgumentOutOfRangeException(nameof(months));

        const string sql = """
            SELECT YEAR(TglBergabung) AS Tahun, MONTH(TglBergabung) AS Bulan, COUNT(*) AS Jumlah
            FROM Members
            WHERE TglBergabung >= @dari
            GROUP BY YEAR(TglBergabung), MONTH(TglBergabung);
            """;

        var thisMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var from = thisMonth.AddMonths(-(months - 1));

        var counts = new Dictionary<(int Year, int Month), int>();

        await using (var connection = await _db.OpenAsync(ct))
        await using (var command = new SqlCommand(sql, connection))
        {
            command.Parameters.Add("@dari", System.Data.SqlDbType.Date).Value = from;
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                counts[(reader.GetInt32(0), reader.GetInt32(1))] = reader.GetInt32(2);
        }

        // Bulan tanpa pendaftaran tetap ditampilkan dengan nilai 0 supaya grafik selalu 12 batang.
        var result = new List<MonthlyCount>(months);
        for (var i = 0; i < months; i++)
        {
            var month = from.AddMonths(i);
            counts.TryGetValue((month.Year, month.Month), out var count);
            result.Add(new MonthlyCount(month.ToString("MMM", Id), count));
        }

        return result;
    }

    public async Task<IReadOnlyList<ExpiringMemberDto>> GetExpiringMembersAsync(int withinDays, CancellationToken ct = default)
    {
        // Sisa < withinDays selaras dengan definisi "Akan Habis" di vw_MemberStatus (hari ini s.d. 6 hari lagi untuk 7).
        const string sql = """
            SELECT v.KodeMember, v.NamaLengkap, v.NoTelepon, v.TglBerakhirTerakhir,
                   DATEDIFF(DAY, CAST(GETDATE() AS DATE), v.TglBerakhirTerakhir) AS SisaHari
            FROM vw_MemberStatus v
            WHERE v.Status = 'Akan Habis'
              AND DATEDIFF(DAY, CAST(GETDATE() AS DATE), v.TglBerakhirTerakhir) < @hari
            ORDER BY v.TglBerakhirTerakhir ASC, v.NamaLengkap ASC;
            """;

        var rows = new List<ExpiringMemberDto>();

        await using var connection = await _db.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@hari", System.Data.SqlDbType.Int).Value = withinDays;
        await using var reader = await command.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            rows.Add(new ExpiringMemberDto(
                KodeMember: reader.GetString(0),
                NamaLengkap: reader.GetString(1),
                NoTelepon: reader.GetString(2),
                TglBerakhir: reader.GetDateTime(3),
                SisaHari: reader.GetInt32(4)));
        }

        return rows;
    }
}
