using System.Data;
using GymSys.Core.Interfaces;
using Microsoft.Data.SqlClient;

namespace GymSys.Data.Queries;

/// <summary>
/// Query laporan (FR-11) dengan ADO.NET: riwayat transaksi, status member, pendapatan per paket.
/// Total pendapatan hanya menghitung transaksi berstatus 'Lunas'.
/// </summary>
public sealed class ReportQueries : IReportQueries
{
    private readonly SqlConnectionFactory _db;

    public ReportQueries(SqlConnectionFactory db) => _db = db;

    public async Task<IReadOnlyList<TransactionRowDto>> GetTransactionsAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        // Rentang inklusif: dari awal hari 'from' sampai akhir hari 'to' (batas atas eksklusif = to + 1 hari).
        const string sql = """
            SELECT t.NoKuitansi, t.TglTransaksi, m.NamaLengkap, p.NamaPaket,
                   t.TotalBayar, t.MetodeBayar, t.Status
            FROM Transactions t
            JOIN Members m ON m.MemberId = t.MemberId
            JOIN MembershipPlans p ON p.PlanId = t.PlanId
            WHERE t.TglTransaksi >= @dari AND t.TglTransaksi < @sampai
            ORDER BY t.TglTransaksi DESC, t.TransactionId DESC;
            """;

        var rows = new List<TransactionRowDto>();

        await using var connection = await _db.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        AddRange(command, from, to);
        await using var reader = await command.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            rows.Add(new TransactionRowDto(
                NoKuitansi: reader.GetString(0),
                TglTransaksi: reader.GetDateTime(1),
                NamaMember: reader.GetString(2),
                NamaPaket: reader.GetString(3),
                TotalBayar: reader.GetDecimal(4),
                MetodeBayar: reader.GetString(5),
                Status: reader.GetString(6)));
        }

        return rows;
    }

    public async Task<IReadOnlyList<MemberStatusRowDto>> GetMemberStatusAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT KodeMember, NamaLengkap, NoTelepon, TglBerakhirTerakhir, Status
            FROM vw_MemberStatus
            ORDER BY NamaLengkap;
            """;

        var rows = new List<MemberStatusRowDto>();

        await using var connection = await _db.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            rows.Add(new MemberStatusRowDto(
                KodeMember: reader.GetString(0),
                NamaLengkap: reader.GetString(1),
                NoTelepon: reader.GetString(2),
                TglBerakhir: reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                Status: reader.GetString(4)));
        }

        return rows;
    }

    public async Task<IReadOnlyList<RevenueByPlanDto>> GetRevenueByPlanAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        const string sql = """
            SELECT p.NamaPaket, COUNT(*) AS JumlahTransaksi, SUM(t.TotalBayar) AS TotalPendapatan
            FROM Transactions t
            JOIN MembershipPlans p ON p.PlanId = t.PlanId
            WHERE t.Status = 'Lunas'
              AND t.TglTransaksi >= @dari AND t.TglTransaksi < @sampai
            GROUP BY p.NamaPaket
            ORDER BY SUM(t.TotalBayar) DESC;
            """;

        var rows = new List<RevenueByPlanDto>();

        await using var connection = await _db.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        AddRange(command, from, to);
        await using var reader = await command.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            rows.Add(new RevenueByPlanDto(
                NamaPaket: reader.GetString(0),
                JumlahTransaksi: reader.GetInt32(1),
                TotalPendapatan: reader.GetDecimal(2)));
        }

        return rows;
    }

    private static void AddRange(SqlCommand command, DateTime from, DateTime to)
    {
        command.Parameters.Add("@dari", SqlDbType.DateTime2).Value = from.Date;
        command.Parameters.Add("@sampai", SqlDbType.DateTime2).Value = to.Date.AddDays(1);
    }
}
