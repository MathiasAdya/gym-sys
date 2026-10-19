using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymSys.Data;

public static class DataServiceCollectionExtensions
{
    /// <summary>
    /// Registrasi semua layanan GymSys.Data. Repository dan query baru didaftarkan di sini, bukan di App.
    /// Pakai IDbContextFactory (bukan DbContext langsung): ViewModel WPF hidup lama, jadi buat
    /// DbContext pendek per operasi: <c>await using var db = await factory.CreateDbContextAsync();</c>
    /// </summary>
    public static IServiceCollection AddGymSysData(this IServiceCollection services, string connectionString)
    {
        services.AddDbContextFactory<GymSysDbContext>(o => o.UseSqlServer(connectionString));
        services.AddSingleton(new SqlConnectionFactory(connectionString));
        return services;
    }
}

/// <summary>Untuk query ADO.NET (Dashboard/Laporan). Selalu pakai parameter, jangan gabung string.</summary>
public sealed class SqlConnectionFactory(string connectionString)
{
    public async Task<SqlConnection> OpenAsync(CancellationToken ct = default)
    {
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }
}
