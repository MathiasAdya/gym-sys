using GymSys.Core.Entities;

namespace GymSys.Core.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);

    Task<User?> GetByIdAsync(int userId, CancellationToken ct = default);

    /// <summary>Semua akun, urut username (halaman Pengaturan, FR12).</summary>
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default);

    Task AddAsync(User user, CancellationToken ct = default);

    /// <summary>Simpan perubahan kolom user (counter login, password, tema, dll.).</summary>
    Task UpdateAsync(User user, CancellationToken ct = default);
}
