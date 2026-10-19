using GymSys.Core.Entities;

namespace GymSys.Core.Interfaces;

// KONTRAK USULAN untuk halaman Pengaturan (FR12). Implementasi asli: Anggota 1 (UserRepository + hash PBKDF2).

public sealed record OperationResult(bool Success, string Message)
{
    public static OperationResult Ok(string message = "") => new(true, message);
    public static OperationResult Fail(string message) => new(false, message);
}

public sealed record StaffAccountDto(int UserId, string Username, string NamaLengkap, UserRole Role, bool IsActive);

public interface IAccountService
{
    Task<OperationResult> ChangePasswordAsync(int userId, string oldPassword, string newPassword, CancellationToken ct = default);

    /// <summary>Hanya Admin. Service asli WAJIB memeriksa peran, bukan hanya menyembunyikan menu.</summary>
    Task<IReadOnlyList<StaffAccountDto>> GetStaffAsync(CancellationToken ct = default);

    Task<OperationResult> AddStaffAsync(string username, string namaLengkap, UserRole role, string initialPassword, CancellationToken ct = default);

    /// <summary>Aktifkan/nonaktifkan akun. Admin tidak boleh menonaktifkan akunnya sendiri (FR12).</summary>
    Task<OperationResult> SetActiveAsync(int actingUserId, int targetUserId, bool isActive, CancellationToken ct = default);
}
