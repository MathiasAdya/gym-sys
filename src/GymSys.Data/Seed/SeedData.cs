using GymSys.Core.Entities;
using GymSys.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace GymSys.Data;

public static class SeedData
{
    public const string AdminUsername = "admin";

    /// <summary>Password awal admin; wajib diganti saat login pertama (MustChangePassword).</summary>
    public const string AdminInitialPassword = "Admin#123";

    // Salt tetap agar migration deterministik; hash berubah begitu admin mengganti password.
    private static readonly byte[] AdminSalt =
        [0x47, 0x59, 0x4D, 0x2D, 0x53, 0x59, 0x53, 0x2D, 0x61, 0x64, 0x6D, 0x69, 0x6E, 0x2D, 0x76, 0x32];

    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasData(new User
        {
            UserId = 1,
            Username = AdminUsername,
            PasswordSalt = AdminSalt,
            PasswordHash = PasswordHasher.Hash(AdminInitialPassword, AdminSalt),
            Role = UserRole.Admin,
            NamaLengkap = "Administrator",
            IsActive = true,
            Theme = "Dark",
            MustChangePassword = true,
        });

        modelBuilder.Entity<MembershipPlan>().HasData(
            new MembershipPlan { PlanId = 1, NamaPaket = "Bronze 1 Bulan", DurasiHari = 30, Harga = 250_000m, Deskripsi = "Akses gym 30 hari", IsActive = true },
            new MembershipPlan { PlanId = 2, NamaPaket = "Silver 3 Bulan", DurasiHari = 90, Harga = 675_000m, Deskripsi = "Akses gym 90 hari", IsActive = true },
            new MembershipPlan { PlanId = 3, NamaPaket = "Gold 12 Bulan", DurasiHari = 365, Harga = 2_400_000m, Deskripsi = "Akses gym 365 hari", IsActive = true });
    }
}
