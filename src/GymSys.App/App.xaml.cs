using System.Windows;
using GymSys.App.Fakes;
using GymSys.App.Services;
using GymSys.App.ViewModels;
using GymSys.Core.Interfaces;
using GymSys.Core.Services;
using GymSys.Data;
using Microsoft.Data.SqlClient;
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
            AppLog.Error(args.Exception, "DispatcherUnhandledException");
            // Pesan teknis hanya ke log; pengguna melihat pesan ramah.
            var pesan = args.Exception is SqlException
                ? "Tidak dapat terhubung ke database. Periksa koneksi lalu coba lagi."
                : "Terjadi kesalahan tak terduga. Silakan ulangi aksi terakhir Anda.";
            MessageBox.Show(pesan + "\n\nDetail tersimpan di " + AppLog.LogFolder,
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

        services.AddSingleton<ICurrentUserService, CurrentUserService>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ITransactionService, TransactionService>();

        // Ada connection string di appsettings(.Development).json -> database ASLI.
        // Tidak ada -> data PALSU (Fakes) supaya aplikasi tetap bisa didemokan tanpa database.
        var connectionString = ConnectionStringProvider.Get();
        if (connectionString is not null)
        {
            // Repository & query (EF Core + ADO.NET) didaftarkan di GymSys.Data.
            services.AddGymSysData(connectionString);
            services.AddSingleton<IAuthService, AuthService>();
            services.AddSingleton<IAccountService, AccountService>();
        }
        else
        {
            services.AddSingleton<IDashboardQueries, FakeDashboardQueries>();
            services.AddSingleton<IReportQueries, FakeReportQueries>();
            services.AddSingleton<FakeUserStore>();
            services.AddSingleton<IAuthService, FakeAuthService>();
            services.AddSingleton<IAccountService, FakeAccountService>();
            services.AddSingleton<ITransactionRepository, FakeTransactionRepository>();
        }

        // --- ViewModel ---
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<ShellViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ReportViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<NewTransactionViewModel>();
    }
}
