using GymSys.Core.Interfaces;
using GymSys.Data;
using GymSys.Data.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymSys.Tests;

public class DataServiceCollectionTests
{
    [Fact]
    public void AddGymSysData_BisaResolveSemuaLayanan()
    {
        using var provider = new ServiceCollection()
            .AddGymSysData("Server=unused;Database=GymSysDb")
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        using var db = provider.GetRequiredService<IDbContextFactory<GymSysDbContext>>().CreateDbContext();
        Assert.Equal("GymSysDb", db.Database.GetDbConnection().Database);
        Assert.NotNull(provider.GetRequiredService<SqlConnectionFactory>());
        Assert.NotNull(provider.GetRequiredService<IDashboardQueries>());
        Assert.NotNull(provider.GetRequiredService<IReportQueries>());
        Assert.NotNull(provider.GetRequiredService<IUserRepository>());
        Assert.NotNull(provider.GetRequiredService<ITransactionRepository>());
        Assert.NotNull(provider.GetRequiredService<IMemberRepository>());
        Assert.NotNull(provider.GetRequiredService<IPlanRepository>());
        Assert.NotNull(provider.GetRequiredService<ITrainerRepository>());
    }
}
