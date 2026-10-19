using System.Windows;
using GymSys.App.Fakes;
using GymSys.App.Services;
using GymSys.App.ViewModels;
using GymSys.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GymSys.App;

public partial class App : Application
{
    private ServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // NFR-06: error tak terduga tidak boleh menutup aplikasi mendadak.
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(
                "Terjadi kesalahan tak terduga. Silakan ulangi aksi terakhir Anda.\n\n" + args.Exception.Message,
                "GYM-SYS", MessageBoxButton.OK, MessageBoxImage.Warning);
            args.Handled = true;
        };

        var services = new ServiceCollection();
        ConfigureServices(services);
        _services = services.BuildServiceProvider();

        _services.GetRequiredService<IThemeService>().LoadSaved();

        var window = new MainWindow
        {
            DataContext = _services.GetRequiredService<MainWindowViewModel>()
        };
        window.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // --- Layanan UI (milik Anggota 2) ---
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IDialogService, DialogService>();

        // --- Sementara memakai FAKE. Kalau implementasi asli Anggota 1/3 sudah ada di GymSys.Data,
        //     cukup tukar baris Fake* di bawah ini (ViewModel tidak perlu diubah). ---
        services.AddSingleton<FakeUserStore>();
        services.AddSingleton<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IAuthService, FakeAuthService>();
        services.AddSingleton<IAccountService, FakeAccountService>();
        services.AddSingleton<IDashboardQueries, FakeDashboardQueries>();
        services.AddSingleton<IReportQueries, FakeReportQueries>();

        // --- ViewModel ---
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<ShellViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ReportViewModel>();
        services.AddTransient<SettingsViewModel>();
    }
}
