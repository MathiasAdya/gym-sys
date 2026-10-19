using GymSys.Core.Entities;
using GymSys.Core.Interfaces;

namespace GymSys.Core.Services;

/// <summary>FR12: ganti password sendiri, dan kelola akun staf (hanya Admin, dicek di sini, bukan hanya di menu).</summary>
public sealed class AccountService(IUserRepository users, ICurrentUserService currentUser) : IAccountService
{
    public const int MinPanjangPassword = 8;

    private const string HanyaAdmin = "Hanya Admin yang dapat mengelola akun staf.";

    public async Task<OperationResult> ChangePasswordAsync(int userId, string oldPassword, string newPassword, CancellationToken ct = default)
    {
        if (currentUser.User?.UserId != userId)
            return OperationResult.Fail("Hanya dapat mengganti password akun sendiri.");

        var user = await users.GetByIdAsync(userId, ct);
        if (user is null) return OperationResult.Fail("Akun tidak ditemukan.");
        if (!PasswordHasher.Verify(oldPassword, user.PasswordSalt, user.PasswordHash))
            return OperationResult.Fail("Password lama salah.");
        if (PesanPasswordTidakValid(newPassword) is { } error) return OperationResult.Fail(error);
        if (newPassword == oldPassword)
            return OperationResult.Fail("Password baru harus berbeda dari password lama.");

        user.PasswordSalt = PasswordHasher.NewSalt();
        user.PasswordHash = PasswordHasher.Hash(newPassword, user.PasswordSalt);
        user.MustChangePassword = false;
        await users.UpdateAsync(user, ct);
        currentUser.SignIn(currentUser.User with { MustChangePassword = false });
        return OperationResult.Ok("Password berhasil diganti.");
    }

    public async Task<IReadOnlyList<StaffAccountDto>> GetStaffAsync(CancellationToken ct = default)
    {
        if (!currentUser.IsInRole(UserRole.Admin)) throw new UnauthorizedAccessException(HanyaAdmin);

        return (await users.GetAllAsync(ct))
            .Select(u => new StaffAccountDto(u.UserId, u.Username, u.NamaLengkap, u.Role, u.IsActive))
            .ToList();
    }

    public async Task<OperationResult> AddStaffAsync(string username, string namaLengkap, UserRole role, string initialPassword, CancellationToken ct = default)
    {
        if (!currentUser.IsInRole(UserRole.Admin)) return OperationResult.Fail(HanyaAdmin);

        username = username.Trim();
        namaLengkap = namaLengkap.Trim();
        if (username.Length == 0 || namaLengkap.Length == 0)
            return OperationResult.Fail("Username dan nama lengkap wajib diisi.");
        if (PesanPasswordTidakValid(initialPassword) is { } error) return OperationResult.Fail(error);
        if (await users.GetByUsernameAsync(username, ct) is not null)
            return OperationResult.Fail("Username sudah dipakai.");

        var salt = PasswordHasher.NewSalt();
        await users.AddAsync(new User
        {
            Username = username,
            NamaLengkap = namaLengkap,
            Role = role,
            PasswordSalt = salt,
            PasswordHash = PasswordHasher.Hash(initialPassword, salt),
            MustChangePassword = true,
        }, ct);
        return OperationResult.Ok($"Akun {username} berhasil ditambahkan.");
    }

    public async Task<OperationResult> SetActiveAsync(int actingUserId, int targetUserId, bool isActive, CancellationToken ct = default)
    {
        if (!currentUser.IsInRole(UserRole.Admin) || currentUser.User!.UserId != actingUserId)
            return OperationResult.Fail(HanyaAdmin);
        if (!isActive && actingUserId == targetUserId)
            return OperationResult.Fail("Anda tidak dapat menonaktifkan akun sendiri.");

        var target = await users.GetByIdAsync(targetUserId, ct);
        if (target is null) return OperationResult.Fail("Akun tidak ditemukan.");

        target.IsActive = isActive;
        await users.UpdateAsync(target, ct);
        return OperationResult.Ok(isActive ? $"Akun {target.Username} diaktifkan." : $"Akun {target.Username} dinonaktifkan.");
    }

    private static string? PesanPasswordTidakValid(string password) =>
        password.Length < MinPanjangPassword ? $"Password minimal {MinPanjangPassword} karakter." : null;
}
