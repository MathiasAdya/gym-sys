using Microsoft.Data.SqlClient;

namespace GymSys.Data.Queries;

/// <summary>
/// Membuat koneksi SQL Server dari connection string (NFR-04: string berasal dari appsettings, bukan dari kode).
/// Dipakai oleh query ADO.NET (DashboardQueries, ReportQueries).
/// </summary>
public sealed class SqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string kosong.", nameof(connectionString));
        _connectionString = connectionString;
    }

    /// <summary>Koneksi sudah terbuka (OpenAsync). Pemanggil wajib membungkusnya dengan await using.</summary>
    public async Task<SqlConnection> OpenAsync(CancellationToken ct = default)
    {
        var connection = new SqlConnection(_connectionString);
        try
        {
            await connection.OpenAsync(ct);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
