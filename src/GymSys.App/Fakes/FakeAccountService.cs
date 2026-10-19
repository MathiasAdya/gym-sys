using GymSys.Core.Entities;
using GymSys.Core.Interfaces;

namespace GymSys.App.Fakes;

/// <summary>Akun & staf PALSU (data di memori). Ganti dengan implementasi asli Anggota 1 nanti.</summary>
public sealed class FakeAccountService : IAccountService
{
    private const int MinPasswordLength = 6;
    private readonly FakeUserStore _store;

    public FakeAccountService(FakeUserStore store) => _store = store;

    public async Task<OperationResult> ChangePasswordAsync(int userId, string oldPassword, string newPassword, CancellationToken ct = default)
    {
        await Task.Delay(400, ct);

        var user = _store.FindById(userId);
        if (user is null)
            return OperationResult.Fail("Akun tidak ditemukan.");
        if (user.Password != oldPassword)
            return OperationResult.Fail("Password lama salah.");
        if (newPassword.Length < MinPasswordLength)
            return OperationResult.Fail($"Password baru minimal {MinPasswordLength} karakter.");

        user.Password = newPassword;
        return OperationResult.Ok("Password berhasil diganti.");
    }

    public async Task<IReadOnlyList<StaffAccountDto>> GetStaffAsync(CancellationToken ct = default)
    {
        await Task.Delay(250, ct);
        return _store.All
            .Select(u => new StaffAccountDto(u.UserId, u.Username, u.NamaLengkap, u.Role, u.IsActive))
            .ToList();
    }

    public async Task<OperationResult> AddStaffAsync(string username, string namaLengkap, UserRole role, string initialPassword, CancellationToken ct = default)
    {
        await Task.Delay(400, ct);

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(namaLengkap))
            return OperationResult.Fail("Username dan nama lengkap wajib diisi.");
        if (initialPassword.Length < MinPasswordLength)
            return OperationResult.Fail($"Password awal minimal {MinPasswordLength} karakter.");
        if (_store.FindByUsername(username) is not null)
            return OperationResult.Fail("Username sudah dipakai.");

        _store.Add(username, namaLengkap, role, initialPassword);
        return OperationResult.Ok($"Akun {username.Trim()} berhasil ditambahkan.");
    }

    public async Task<OperationResult> SetActiveAsync(int actingUserId, int targetUserId, bool isActive, CancellationToken ct = default)
    {
        await Task.Delay(300, ct);

        var target = _store.FindById(targetUserId);
        if (target is null)
            return OperationResult.Fail("Akun tidak ditemukan.");
        if (!isActive && actingUserId == targetUserId)
            return OperationResult.Fail("Anda tidak dapat menonaktifkan akun sendiri.");

        target.IsActive = isActive;
        return OperationResult.Ok(isActive ? $"Akun {target.Username} diaktifkan." : $"Akun {target.Username} dinonaktifkan.");
    }
}
