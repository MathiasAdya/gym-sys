using GymSys.Core.Interfaces;
using GymSys.Data.Queries;
using GymSys.Data.Repositories;
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
        services.AddSingleton<IDashboardQueries, DashboardQueries>();
        services.AddSingleton<IReportQueries, ReportQueries>();
        services.AddSingleton<IUserRepository, UserRepository>();
        services.AddSingleton<ITransactionRepository, TransactionRepository>();

        // Anggota 3: Member, Paket, Instruktur
        services.AddSingleton<IMemberRepository, MemberRepository>();
        services.AddSingleton<IPlanRepository, PlanRepository>();
        services.AddSingleton<ITrainerRepository, TrainerRepository>();
        return services;
    }
}
