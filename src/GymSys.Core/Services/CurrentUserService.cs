using GymSys.Core.Entities;
using GymSys.Core.Interfaces;

namespace GymSys.Core.Services;

/// <summary>Pengguna yang sedang login, disimpan di memori selama aplikasi berjalan.</summary>
public sealed class CurrentUserService : ICurrentUserService
{
    public UserInfo? User { get; private set; }

    public bool IsLoggedIn => User is not null;

    public bool IsInRole(params UserRole[] roles) => User is not null && roles.Contains(User.Role);

    public void SignIn(UserInfo user) => User = user;

    public void SignOut() => User = null;
}
