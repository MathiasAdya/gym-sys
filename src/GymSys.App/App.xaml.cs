using System.IO;
using System.Windows;
using System.Windows.Threading;
using GymSys.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GymSys.App;

public partial class App : Application
{
    private static readonly string LogFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GymSys", "logs");

    private readonly IHost _host;

    public App()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ContentRootPath = AppContext.BaseDirectory,
#if DEBUG
            // Build Debug membaca appsettings.Development.json milik masing-masing anggota (NFR-04).
            EnvironmentName = Environments.Development,
#endif
        });

        var connectionString = builder.Configuration.GetConnectionString("GymSysDb")
            ?? throw new InvalidOperationException("ConnectionStrings:GymSysDb tidak ada di appsettings.json.");

        builder.Services.AddGymSysData(connectionString);
        builder.Services.AddSingleton<MainWindow>();

        _host = builder.Build();
        DispatcherUnhandledException += OnUnhandledException;
    }

    public IServiceProvider Services => _host.Services;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        await _host.StartAsync();
        _host.Services.GetRequiredService<MainWindow>().Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }

    // NFR-06: pesan ramah untuk pengguna, detail ke log harian di %AppData%\GymSys\logs.
    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        WriteLog(e.Exception);
        var message = e.Exception is SqlException || e.Exception.InnerException is SqlException
            ? "Tidak dapat mengakses database. Pastikan SQL Server berjalan, lalu coba lagi."
            : "Terjadi kesalahan tak terduga. Detail sudah dicatat di log.";
        MessageBox.Show(message, "GYM-SYS", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    public static void WriteLog(Exception ex)
    {
        try
        {
            Directory.CreateDirectory(LogFolder);
            File.AppendAllText(Path.Combine(LogFolder, $"gymsys-{DateTime.Now:yyyyMMdd}.log"),
                $"[{DateTime.Now:HH:mm:ss}] {ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch (Exception logError) when (logError is IOException or UnauthorizedAccessException)
        {
            // Gagal menulis log tidak boleh membuat aplikasi crash.
        }
    }
}
