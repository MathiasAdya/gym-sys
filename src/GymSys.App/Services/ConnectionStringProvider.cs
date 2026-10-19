using System.IO;
using System.Text.Json;

namespace GymSys.App.Services;

/// <summary>
/// Membaca ConnectionStrings:GymSysDb dari appsettings.json lalu appsettings.Development.json
/// (file kedua menimpa yang pertama; file Development tidak di-commit, NFR-04).
/// Mengembalikan null kalau tidak ada, sehingga aplikasi memakai data palsu.
/// </summary>
public static class ConnectionStringProvider
{
    public const string Name = "GymSysDb";

    public static string? Get()
    {
        string? result = null;
        foreach (var file in new[] { "appsettings.json", "appsettings.Development.json" })
        {
            var value = TryRead(Path.Combine(AppContext.BaseDirectory, file));
            if (!string.IsNullOrWhiteSpace(value))
                result = value;
        }

        return result;
    }

    private static string? TryRead(string path)
    {
        try
        {
            if (!File.Exists(path))
                return null;

            using var doc = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

            return doc.RootElement.TryGetProperty("ConnectionStrings", out var section)
                   && section.TryGetProperty(Name, out var value)
                ? value.GetString()
                : null;
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Gagal membaca " + path);
            return null;
        }
    }
}
