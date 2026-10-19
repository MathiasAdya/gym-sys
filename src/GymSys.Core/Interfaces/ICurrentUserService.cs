using GymSys.Core.Entities;

namespace GymSys.Core.Interfaces;

/// <summary>Menyimpan pengguna yang sedang login selama aplikasi berjalan.</summary>
public interface ICurrentUserService
{
    UserInfo? User { get; }
    bool IsLoggedIn { get; }
    bool IsInRole(params UserRole[] roles);
    void SignIn(UserInfo user);
    void SignOut();
}
