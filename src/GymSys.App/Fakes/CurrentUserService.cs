using GymSys.Core.Entities;
using GymSys.Core.Interfaces;

namespace GymSys.App.Fakes;

/// <summary>Implementasi sederhana (hanya menyimpan state di memori). Boleh dipakai terus atau diganti milik Anggota 1.</summary>
public sealed class CurrentUserService : ICurrentUserService
{
    public UserInfo? User { get; private set; }

    public bool IsLoggedIn => User is not null;

    public bool IsInRole(params UserRole[] roles) => User is not null && roles.Contains(User.Role);

    public void SignIn(UserInfo user) => User = user;

    public void SignOut() => User = null;
}
