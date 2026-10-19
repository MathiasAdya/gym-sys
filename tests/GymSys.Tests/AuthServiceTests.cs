using GymSys.Core.Entities;
using GymSys.Core.Services;

namespace GymSys.Tests;

public class AuthServiceTests
{
    private const string Password = "Rahasia#1";

    private readonly FakeUserRepository _users = new();
    private readonly FakeTime _time = new();
    private readonly AuthService _auth;
    private readonly User _user;

    public AuthServiceTests()
    {
        _auth = new AuthService(_users, _time);
        var salt = PasswordHasher.NewSalt();
        _user = new User { UserId = 2, Username = "resep", NamaLengkap = "Rina", Role = UserRole.Resepsionis, PasswordSalt = salt, PasswordHash = PasswordHasher.Hash(Password, salt) };
        _users.Seed(_user);
    }

    [Fact]
    public async Task Login_Benar_KembalikanUserInfo()
    {
        var result = await _auth.LoginAsync(" resep ", Password);

        Assert.True(result.Success);
        Assert.Equal(new(2, "resep", "Rina", UserRole.Resepsionis), result.User);
    }

    [Fact]
    public async Task Login_UsernameTidakAda_Gagal()
    {
        var result = await _auth.LoginAsync("siapa", Password);

        Assert.False(result.Success);
        Assert.Null(result.User);
    }

    [Fact]
    public async Task Login_SalahEmpatKali_BelumTerkunci_LaluBenarResetCounter()
    {
        for (var i = 0; i < 4; i++) await _auth.LoginAsync("resep", "salah");
        Assert.Equal(4, _user.FailedLoginCount);
        Assert.Null(_user.LockedUntil);

        Assert.True((await _auth.LoginAsync("resep", Password)).Success);
        Assert.Equal(0, _user.FailedLoginCount);
    }

    [Fact]
    public async Task Login_SalahLimaKali_Terkunci5Menit()
    {
        for (var i = 0; i < 5; i++) await _auth.LoginAsync("resep", "salah");

        Assert.Equal(_time.Now.AddMinutes(5), _user.LockedUntil);
        var saatTerkunci = await _auth.LoginAsync("resep", Password);
        Assert.False(saatTerkunci.Success);
        Assert.Contains("terkunci", saatTerkunci.Message);

        _time.Advance(TimeSpan.FromMinutes(4.9));
        Assert.False((await _auth.LoginAsync("resep", Password)).Success);

        _time.Advance(TimeSpan.FromMinutes(0.2));
        Assert.True((await _auth.LoginAsync("resep", Password)).Success);
        Assert.Null(_user.LockedUntil);
    }

    [Fact]
    public async Task Login_AkunNonaktif_Gagal()
    {
        _user.IsActive = false;

        var result = await _auth.LoginAsync("resep", Password);

        Assert.False(result.Success);
        Assert.Contains("dinonaktifkan", result.Message);
    }

    private sealed class FakeTime : TimeProvider
    {
        public DateTime Now { get; private set; } = new(2026, 11, 10, 8, 0, 0, DateTimeKind.Utc);

        public void Advance(TimeSpan by) => Now += by;

        public override DateTimeOffset GetUtcNow() => new(Now);
    }
}
