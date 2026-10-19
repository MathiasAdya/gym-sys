using System.IO;
using System.Text;

namespace GymSys.App.Services;

/// <summary>
/// Log error harian ke %AppData%\GymSys\logs\yyyy-MM-dd.log (NFR-06).
/// Pengguna hanya melihat pesan ramah; detail teknis ditulis ke sini. Tidak pernah melempar exception.
/// </summary>
public static class AppLog
{
    private static readonly object Gate = new();

    public static string LogFolder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GymSys", "logs");

    public static void Error(Exception exception, string? context = null)
    {
        try
        {
            Directory.CreateDirectory(LogFolder);
            var path = Path.Combine(LogFolder, $"{DateTime.Now:yyyy-MM-dd}.log");

            var sb = new StringBuilder();
            sb.Append('[').Append(DateTime.Now.ToString("HH:mm:ss")).Append("] ERROR");
            if (!string.IsNullOrEmpty(context))
                sb.Append(' ').Append(context);
            sb.AppendLine();
            sb.AppendLine(exception.ToString());
            sb.AppendLine();

            lock (Gate)
                File.AppendAllText(path, sb.ToString(), Encoding.UTF8);
        }
        catch
        {
            // Gagal menulis log tidak boleh menjatuhkan aplikasi.
        }
    }
}
