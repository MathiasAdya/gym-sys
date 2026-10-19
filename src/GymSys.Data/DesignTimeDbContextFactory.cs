using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GymSys.Data;

/// <summary>
/// Dipakai hanya oleh `dotnet ef` (migrations). Connection string diambil dari env var
/// GYMSYS_CONNECTION, atau default instance SQL Server lokal.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<GymSysDbContext>
{
    public GymSysDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("GYMSYS_CONNECTION")
            ?? "Server=localhost;Database=GymSysDb;Trusted_Connection=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<GymSysDbContext>().UseSqlServer(connectionString).Options;
        return new GymSysDbContext(options);
    }
}
