using GymSys.Core.Entities;
using GymSys.Core.Services;
using GymSys.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GymSys.Tests;

public class SeedDataTests
{
    private static GymSysDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<GymSysDbContext>().UseSqlServer("Server=unused").Options);

    private static IModel DesignModel(DbContext db) => db.GetService<IDesignTimeModel>().Model;

    [Fact]
    public void AdminSeed_BisaLoginDenganPasswordAwal_DanWajibGanti()
    {
        using var db = CreateContext();
        var admin = (User)DesignModel(db).FindEntityType(typeof(User))!.GetSeedData().Single()
            .Aggregate(new User(), (u, kv) => { typeof(User).GetProperty(kv.Key)!.SetValue(u, kv.Value); return u; });

        Assert.Equal(SeedData.AdminUsername, admin.Username);
        Assert.Equal(UserRole.Admin, admin.Role);
        Assert.True(admin.MustChangePassword);
        Assert.True(PasswordHasher.Verify(SeedData.AdminInitialPassword, admin.PasswordSalt, admin.PasswordHash));
    }

    [Fact]
    public void PaketSeed_SesuaiPrd()
    {
        using var db = CreateContext();
        var durasi = DesignModel(db).FindEntityType(typeof(MembershipPlan))!.GetSeedData()
            .Select(p => (int)p[nameof(MembershipPlan.DurasiHari)]!).Order();

        Assert.Equal([30, 90, 365], durasi);
    }
}
