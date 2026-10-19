using GymSys.Core.Entities;

namespace GymSys.Core.Interfaces;

// KONTRAK USULAN (PRD Bagian 12.2): dipakai Login (Anggota 2), diimplementasikan AuthService (Anggota 1).
// Kalau Anggota 1 sudah punya versi sendiri, pakai punya dia dan sesuaikan LoginViewModel.

public sealed record UserInfo(int UserId, string Username, string NamaLengkap, UserRole Role);

public sealed record AuthResult(bool Success, string? Message, UserInfo? User)
{
    public static AuthResult Ok(UserInfo user) => new(true, null, user);
    public static AuthResult Fail(string message) => new(false, message, null);
}

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string username, string password, CancellationToken ct = default);
}
