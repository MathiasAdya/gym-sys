using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace GymSys.App.ViewModels;

/// <summary>Menentukan layar tingkat atas: Login atau Shell (setelah login).</summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly IServiceProvider _services;

    [ObservableProperty]
    private object? _root;

    public MainWindowViewModel(IServiceProvider services)
    {
        _services = services;
        ShowLogin();
    }

    private void ShowLogin()
    {
        var login = _services.GetRequiredService<LoginViewModel>();
        login.LoginSucceeded += ShowShell;
        Root = login;
    }

    private void ShowShell()
    {
        var shell = _services.GetRequiredService<ShellViewModel>();
        shell.LogoutRequested += ShowLogin;
        Root = shell;
    }
}
