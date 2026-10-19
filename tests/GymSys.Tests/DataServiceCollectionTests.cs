using GymSys.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymSys.Tests;

public class DataServiceCollectionTests
{
    [Fact]
    public void AddGymSysData_BisaResolveFactory()
    {
        using var provider = new ServiceCollection()
            .AddGymSysData("Server=unused;Database=GymSysDb")
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        using var db = provider.GetRequiredService<IDbContextFactory<GymSysDbContext>>().CreateDbContext();
        Assert.Equal("GymSysDb", db.Database.GetDbConnection().Database);
        Assert.NotNull(provider.GetRequiredService<SqlConnectionFactory>());
    }
}
