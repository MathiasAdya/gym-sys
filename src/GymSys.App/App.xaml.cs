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

        var connectionString = ConnectionStringProvider.Get();
#if !DEBUG
        // Build rilis tanpa connection string tidak boleh diam-diam memakai data palsu:
        // transaksi yang "tersimpan" akan hilang saat aplikasi ditutup.
        if (connectionString is null)
        {
            MessageBox.Show("Connection string database belum diatur.\n\nIsi ConnectionStrings:GymSysDb di "
                + System.IO.Path.Combine(AppContext.BaseDirectory, "appsettings.json")
                + " (lihat INSTALL.md), lalu buka aplikasi lagi.",
                "GYM-SYS", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }
#endif

        var services = new ServiceCollection();
        ConfigureServices(services, connectionString);
        _services = services.BuildServiceProvider();

        _services.GetRequiredService<IThemeService>().LoadSaved();

        var window = new MainWindow
        {
            DataContext = _services.GetRequiredService<MainWindowViewModel>()
        };
        window.Show();
    }

    private static void ConfigureServices(IServiceCollection services, string? connectionString)
    {
        // --- Layanan UI (milik Anggota 2) ---
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IDialogService, DialogService>();

        services.AddSingleton<ICurrentUserService, CurrentUserService>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ITransactionService, TransactionService>();

        // Ada connection string di appsettings(.Development).json -> database ASLI.
        // Tidak ada -> data PALSU (Fakes) supaya aplikasi tetap bisa didemokan tanpa database (hanya build Debug).
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

        // --- Anggota 3: Member, Paket, Instruktur ---
        // SEMENTARA selalu memakai data palsu sampai repository EF Core asli selesai;
        // setelah itu ketiga repository pindah ke AddGymSysData dan baris Fake ini masuk ke blok else di atas.
        services.AddSingleton<FakeMasterDataStore>();
        services.AddSingleton<IMemberRepository, FakeMemberRepository>();
        services.AddSingleton<IPlanRepository, FakePlanRepository>();
        services.AddSingleton<ITrainerRepository, FakeTrainerRepository>();
        services.AddSingleton<IMemberService, MemberService>();
        services.AddSingleton<IPlanService, PlanService>();
        services.AddSingleton<ITrainerService, TrainerService>();

        // --- ViewModel ---
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<ShellViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ReportViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<NewTransactionViewModel>();
        services.AddTransient<TransactionHistoryViewModel>();
    }
}
