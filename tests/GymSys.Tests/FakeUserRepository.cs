using GymSys.Core.Entities;
using GymSys.Core.Interfaces;

namespace GymSys.Tests;

/// <summary>Repository di memori; objek yang dikembalikan sama dengan yang disimpan, jadi perubahan langsung terlihat.</summary>
internal sealed class FakeUserRepository : IUserRepository
{
    private readonly List<User> _users = [];

    public IReadOnlyList<User> Users => _users;

    public void Seed(User user) => _users.Add(user);

    public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        Task.FromResult(_users.SingleOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase)));

    public Task<User?> GetByIdAsync(int userId, CancellationToken ct = default) =>
        Task.FromResult(_users.SingleOrDefault(u => u.UserId == userId));

    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<User>>(_users.OrderBy(u => u.Username).ToList());

    public Task AddAsync(User user, CancellationToken ct = default)
    {
        user.UserId = _users.Max(u => u.UserId) + 1;
        _users.Add(user);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user, CancellationToken ct = default) => Task.CompletedTask;
}
