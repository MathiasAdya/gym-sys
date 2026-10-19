using GymSys.Core.Entities;
using GymSys.Core.Interfaces;

namespace GymSys.Core.Services;

/// <summary>
/// FR-01: login PBKDF2, salah 5x berturut-turut mengunci akun 5 menit. LockedUntil disimpan dalam UTC.
/// Hasilnya di-SignIn ke ICurrentUserService oleh LoginViewModel.
/// </summary>
public sealed class AuthService(IUserRepository users, TimeProvider time) : IAuthService
{
    public const int MaksGagalLogin = 5;
    public static readonly TimeSpan LamaKunci = TimeSpan.FromMinutes(5);

    private const string SalahLogin = "Username atau password salah.";

    public async Task<AuthResult> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        var user = await users.GetByUsernameAsync(username.Trim(), ct);
        if (user is null) return AuthResult.Fail(SalahLogin);

        var now = time.GetUtcNow().UtcDateTime;
        if (user.LockedUntil > now) return AuthResult.Fail(PesanTerkunci(user.LockedUntil.Value - now));

        if (!PasswordHasher.Verify(password, user.PasswordSalt, user.PasswordHash))
        {
            user.FailedLoginCount++;
            var terkunci = user.FailedLoginCount >= MaksGagalLogin;
            if (terkunci)
            {
                user.FailedLoginCount = 0;
                user.LockedUntil = now + LamaKunci;
            }
            await users.UpdateAsync(user, ct);
            return AuthResult.Fail(terkunci ? PesanTerkunci(LamaKunci) : SalahLogin);
        }

        if (!user.IsActive) return AuthResult.Fail("Akun ini sudah dinonaktifkan. Hubungi Admin.");

        if (user.FailedLoginCount != 0 || user.LockedUntil is not null)
        {
            user.FailedLoginCount = 0;
            user.LockedUntil = null;
            await users.UpdateAsync(user, ct);
        }

        return AuthResult.Ok(new UserInfo(user.UserId, user.Username, user.NamaLengkap, user.Role, user.MustChangePassword));
    }

    private static string PesanTerkunci(TimeSpan sisa) =>
        $"Akun terkunci karena {MaksGagalLogin}x salah password. Coba lagi dalam {Math.Ceiling(sisa.TotalMinutes)} menit.";
}
