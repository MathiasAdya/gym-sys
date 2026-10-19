using GymSys.Core.Entities;
using GymSys.Core.Interfaces;
using GymSys.Core.Services;

namespace GymSys.Tests;

public class AccountServiceTests
{
    private const string Password = "Rahasia#1";

    private readonly FakeUserRepository _users = new();
    private readonly CurrentUserService _currentUser = new();
    private readonly AccountService _accounts;
    private readonly User _admin;
    private readonly User _resep;

    public AccountServiceTests()
    {
        _accounts = new AccountService(_users, _currentUser);
        _admin = BuatUser(1, "admin", UserRole.Admin);
        _resep = BuatUser(2, "resep", UserRole.Resepsionis);
    }

    [Fact]
    public async Task ChangePassword_Berhasil_HashBaruDanMustChangeDihapus()
    {
        _resep.MustChangePassword = true;
        SignIn(_resep);

        var result = await _accounts.ChangePasswordAsync(2, Password, "PasswordBaru1");

        Assert.True(result.Success);
        Assert.False(_resep.MustChangePassword);
        Assert.False(_currentUser.User!.MustChangePassword);
        Assert.True(PasswordHasher.Verify("PasswordBaru1", _resep.PasswordSalt, _resep.PasswordHash));
    }

    [Theory]
    [InlineData("salah", "PasswordBaru1")]
    [InlineData(Password, "pendek")]
    [InlineData(Password, Password)]
    public async Task ChangePassword_TidakValid_PasswordTetap(string lama, string baru)
    {
        SignIn(_resep);

        Assert.False((await _accounts.ChangePasswordAsync(2, lama, baru)).Success);
        Assert.True(PasswordHasher.Verify(Password, _resep.PasswordSalt, _resep.PasswordHash));
    }

    [Fact]
    public async Task ChangePassword_AkunOrangLain_Ditolak()
    {
        SignIn(_admin);

        Assert.False((await _accounts.ChangePasswordAsync(2, Password, "PasswordBaru1")).Success);
    }

    [Fact]
    public async Task KelolaStaf_BukanAdmin_Ditolak()
    {
        SignIn(_resep);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _accounts.GetStaffAsync());
        Assert.False((await _accounts.AddStaffAsync("baru", "Baru", UserRole.Resepsionis, Password)).Success);
        Assert.False((await _accounts.SetActiveAsync(2, 1, false)).Success);
        Assert.True(_admin.IsActive);
    }

    [Fact]
    public async Task AddStaff_Admin_TersimpanDenganHashDanWajibGantiPassword()
    {
        SignIn(_admin);

        var result = await _accounts.AddStaffAsync(" pt ", "Bima", UserRole.Instruktur, Password);

        Assert.True(result.Success);
        var pt = Assert.Single(_users.Users, u => u.Username == "pt");
        Assert.True(PasswordHasher.Verify(Password, pt.PasswordSalt, pt.PasswordHash));
        Assert.True(pt.MustChangePassword);
        Assert.Equal(3, (await _accounts.GetStaffAsync()).Count);
    }

    [Fact]
    public async Task AddStaff_UsernameDipakai_Gagal()
    {
        SignIn(_admin);

        Assert.False((await _accounts.AddStaffAsync("RESEP", "Lain", UserRole.Resepsionis, Password)).Success);
    }

    [Fact]
    public async Task SetActive_Admin_BisaNonaktifkanOrangLainTapiBukanDiriSendiri()
    {
        SignIn(_admin);

        Assert.False((await _accounts.SetActiveAsync(1, 1, false)).Success);
        Assert.True(_admin.IsActive);

        Assert.True((await _accounts.SetActiveAsync(1, 2, false)).Success);
        Assert.False(_resep.IsActive);
    }

    private User BuatUser(int id, string username, UserRole role)
    {
        var salt = PasswordHasher.NewSalt();
        var user = new User { UserId = id, Username = username, NamaLengkap = username, Role = role, PasswordSalt = salt, PasswordHash = PasswordHasher.Hash(Password, salt) };
        _users.Seed(user);
        return user;
    }

    private void SignIn(User user) =>
        ((ICurrentUserService)_currentUser).SignIn(new UserInfo(user.UserId, user.Username, user.NamaLengkap, user.Role, user.MustChangePassword));
}
