using GymSys.Core.Interfaces;

namespace GymSys.App.Fakes;

/// <summary>
/// AuthService PALSU supaya Login bisa dikembangkan tanpa database.
/// Akun demo: admin/admin123, resepsionis/resep123, pt/pt123 (lihat FakeUserStore).
/// Ganti ke AuthService asli (Anggota 1) di App.xaml.cs saat sudah siap.
/// </summary>
public sealed class FakeAuthService : IAuthService
{
    private readonly FakeUserStore _store;

    public FakeAuthService(FakeUserStore store) => _store = store;

    public async Task<AuthResult> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        await Task.Delay(500, ct); // simulasi akses database, supaya state loading terlihat

        var user = _store.FindByUsername(username);
        if (user is null || user.Password != password)
            return AuthResult.Fail("Username atau password salah.");

        if (!user.IsActive)
            return AuthResult.Fail("Akun ini tidak aktif. Hubungi Admin.");

        return AuthResult.Ok(new UserInfo(user.UserId, user.Username, user.NamaLengkap, user.Role));
    }
}
