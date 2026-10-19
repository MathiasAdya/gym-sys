using System.IO;
using System.Windows;

namespace GymSys.App.Services;

public enum AppTheme
{
    Dark,
    Light
}

public interface IThemeService
{
    AppTheme Current { get; }
    event Action<AppTheme>? Changed;

    /// <summary>Muat tema tersimpan untuk pengguna aktif (atau tema layar login bila belum ada pengguna).</summary>
    void LoadSaved();

    /// <summary>Set pengguna aktif: tema dimuat dari preferensi pengguna itu. null = kembali ke tema layar login.</summary>
    void SetUser(string? username);

    void Apply(AppTheme theme);
    void Toggle();
}

/// <summary>
/// Ganti tema = tukar ResourceDictionary di index 0 (Dark.xaml / Light.xaml).
/// Preferensi tersimpan PER PENGGUNA di %AppData%\GymSys\theme-{username}.txt
/// (layar login memakai theme.txt). Kalau nanti database punya kolom Users.Theme, cukup ganti isi
/// LoadSaved/Save tanpa mengubah ViewModel mana pun.
/// </summary>
public sealed class ThemeService : IThemeService
{
    private static readonly string Folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GymSys");

    private string? _username;

    public AppTheme Current { get; private set; } = AppTheme.Dark;

    public event Action<AppTheme>? Changed;

    public void SetUser(string? username)
    {
        _username = username;
        LoadSaved();
    }

    public void LoadSaved()
    {
        // Pengguna baru yang belum punya preferensi mewarisi tema yang sedang tampil;
        // layar login tanpa preferensi memakai Dark.
        var theme = _username is null ? AppTheme.Dark : Current;
        try
        {
            var path = GetPath();
            if (File.Exists(path) && Enum.TryParse(File.ReadAllText(path).Trim(), out AppTheme saved))
                theme = saved;
        }
        catch
        {
            // File tema rusak/tidak terbaca: abaikan.
        }

        Apply(theme);
    }

    public void Apply(AppTheme theme)
    {
        var uri = new Uri($"pack://application:,,,/Themes/{theme}.xaml", UriKind.Absolute);
        Application.Current.Resources.MergedDictionaries[0] = new ResourceDictionary { Source = uri };
        Current = theme;
        Save();
        Changed?.Invoke(theme);
    }

    public void Toggle() => Apply(Current == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);

    private string GetPath()
    {
        if (_username is null)
            return Path.Combine(Folder, "theme.txt");

        var invalid = Path.GetInvalidFileNameChars();
        var safe = string.Concat(_username.Select(c => invalid.Contains(c) ? '_' : c));
        return Path.Combine(Folder, $"theme-{safe}.txt");
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(GetPath(), Current.ToString());
        }
        catch
        {
            // Gagal menyimpan preferensi tidak boleh mengganggu pengguna.
        }
    }
}
