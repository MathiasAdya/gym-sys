using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymSys.App.Services;
using GymSys.Core.Entities;
using GymSys.Core.Interfaces;
using GymSys.Core.Services;

namespace GymSys.App.ViewModels;

/// <summary>Satu baris tabel staf. Properti turunan dihitung supaya XAML tetap sederhana.</summary>
public sealed record StaffRow(int UserId, string Username, string NamaLengkap, string RoleText, bool IsActive)
{
    public string StatusText => IsActive ? "Aktif" : "Nonaktif";
    public string ToggleText => IsActive ? "Nonaktifkan" : "Aktifkan";
}

public partial class SettingsViewModel : ObservableObject, IAsyncInitializable, IDisposable
{
    private const int MinPasswordLength = AccountService.MinPanjangPassword;
    private const string DarkName = "Gelap";
    private const string LightName = "Terang";

    private readonly IThemeService _theme;
    private readonly IAccountService _accounts;
    private readonly IDialogService _dialogs;
    private readonly int _userId;

    public SettingsViewModel(
        IThemeService theme,
        IAccountService accounts,
        ICurrentUserService currentUser,
        IDialogService dialogs)
    {
        _theme = theme;
        _accounts = accounts;
        _dialogs = dialogs;

        var user = currentUser.User ?? throw new InvalidOperationException("Pengaturan dibuka tanpa pengguna yang login.");
        _userId = user.UserId;
        ShowStaffSection = user.Role == UserRole.Admin;
        MustChangePassword = user.MustChangePassword;

        _selectedTheme = ThemeName(_theme.Current);
        _theme.Changed += OnThemeChanged;
    }

    // ===== Tampilan =====
    public string[] ThemeOptions { get; } = { DarkName, LightName };

    [ObservableProperty]
    private string _selectedTheme = DarkName;

    partial void OnSelectedThemeChanged(string value)
    {
        var target = value == LightName ? AppTheme.Light : AppTheme.Dark;
        if (_theme.Current != target)
            _theme.Apply(target);
    }

    private void OnThemeChanged(AppTheme theme) => SelectedTheme = ThemeName(theme);

    private static string ThemeName(AppTheme theme) => theme == AppTheme.Light ? LightName : DarkName;

    // ===== Ganti password =====
    public string PasswordHint { get; } = $"Minimal {MinPasswordLength} karakter.";

    /// <summary>Akun baru/seed: tampilkan peringatan; Shell membuka menu lain setelah berhasil.</summary>
    public bool MustChangePassword { get; }

    [ObservableProperty] private string _oldPassword = "";
    [ObservableProperty] private string _newPassword = "";
    [ObservableProperty] private string _confirmPassword = "";
    [ObservableProperty] private string _passwordError = "";
    [ObservableProperty] private string _passwordSuccess = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ChangePasswordCommand))]
    private bool _isPasswordBusy;

    /// <summary>View mendengarkan ini untuk mengosongkan kotak password setelah berhasil.</summary>
    public event Action? PasswordChangeCompleted;

    private bool CanChangePassword() => !IsPasswordBusy;

    [RelayCommand(CanExecute = nameof(CanChangePassword))]
    private async Task ChangePasswordAsync()
    {
        PasswordError = "";
        PasswordSuccess = "";

        if (string.IsNullOrEmpty(OldPassword) || string.IsNullOrEmpty(NewPassword) || string.IsNullOrEmpty(ConfirmPassword))
        {
            PasswordError = "Semua kolom password wajib diisi.";
            return;
        }

        if (NewPassword.Length < MinPasswordLength)
        {
            PasswordError = $"Password baru minimal {MinPasswordLength} karakter.";
            return;
        }

        if (NewPassword != ConfirmPassword)
        {
            PasswordError = "Konfirmasi password tidak sama dengan password baru.";
            return;
        }

        if (NewPassword == OldPassword)
        {
            PasswordError = "Password baru harus berbeda dari password lama.";
            return;
        }

        IsPasswordBusy = true;
        try
        {
            var result = await _accounts.ChangePasswordAsync(_userId, OldPassword, NewPassword);
            if (!result.Success)
            {
                PasswordError = result.Message;
                return;
            }

            PasswordSuccess = result.Message;
            OldPassword = "";
            NewPassword = "";
            ConfirmPassword = "";
            PasswordChangeCompleted?.Invoke();
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "SettingsViewModel");
            PasswordError = "Tidak dapat menyimpan password. Periksa koneksi database lalu coba lagi.";
        }
        finally
        {
            IsPasswordBusy = false;
        }
    }

    // ===== Manajemen staf (hanya Admin) =====
    public bool ShowStaffSection { get; }

    public UserRole[] RoleOptions { get; } = Enum.GetValues<UserRole>();

    public ObservableCollection<StaffRow> StaffMembers { get; } = new();

    [ObservableProperty] private string _newStaffUsername = "";
    [ObservableProperty] private string _newStaffName = "";
    [ObservableProperty] private UserRole _newStaffRole = UserRole.Resepsionis;
    [ObservableProperty] private string _newStaffPassword = "";
    [ObservableProperty] private string _staffError = "";
    [ObservableProperty] private string _staffSuccess = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddStaffCommand))]
    private bool _isStaffBusy;

    /// <summary>View mendengarkan ini untuk mengosongkan kotak password awal setelah staf ditambahkan.</summary>
    public event Action? StaffAdded;

    public async Task InitializeAsync()
    {
        if (ShowStaffSection)
            await LoadStaffAsync();
    }

    private async Task LoadStaffAsync()
    {
        try
        {
            var staff = await _accounts.GetStaffAsync();
            StaffMembers.Clear();
            foreach (var s in staff)
                StaffMembers.Add(new StaffRow(s.UserId, s.Username, s.NamaLengkap, RoleText(s.Role), s.IsActive));
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "SettingsViewModel");
            StaffError = "Gagal memuat daftar staf. Periksa koneksi database lalu buka ulang halaman ini.";
        }
    }

    private static string RoleText(UserRole role) => role switch
    {
        UserRole.Instruktur => "Instruktur (PT)",
        _ => role.ToString()
    };

    private bool CanAddStaff() => !IsStaffBusy;

    [RelayCommand(CanExecute = nameof(CanAddStaff))]
    private async Task AddStaffAsync()
    {
        StaffError = "";
        StaffSuccess = "";

        if (string.IsNullOrWhiteSpace(NewStaffUsername) || string.IsNullOrWhiteSpace(NewStaffName) || string.IsNullOrEmpty(NewStaffPassword))
        {
            StaffError = "Username, nama lengkap, dan password awal wajib diisi.";
            return;
        }

        if (NewStaffPassword.Length < MinPasswordLength)
        {
            StaffError = $"Password awal minimal {MinPasswordLength} karakter.";
            return;
        }

        IsStaffBusy = true;
        try
        {
            var result = await _accounts.AddStaffAsync(NewStaffUsername, NewStaffName, NewStaffRole, NewStaffPassword);
            if (!result.Success)
            {
                StaffError = result.Message;
                return;
            }

            StaffSuccess = result.Message;
            NewStaffUsername = "";
            NewStaffName = "";
            NewStaffPassword = "";
            StaffAdded?.Invoke();
            await LoadStaffAsync();
        }
        catch (Exception)
        {
            StaffError = "Gagal menambahkan staf. Periksa koneksi database lalu coba lagi.";
        }
        finally
        {
            IsStaffBusy = false;
        }
    }

    [RelayCommand]
    private async Task ToggleActiveAsync(StaffRow? row)
    {
        if (row is null)
            return;

        StaffError = "";
        StaffSuccess = "";

        // Pesan ramah di UI; aturan yang sama TETAP dijaga di service (FR12).
        if (row.IsActive && row.UserId == _userId)
        {
            StaffError = "Anda tidak dapat menonaktifkan akun sendiri.";
            return;
        }

        if (row.IsActive && !_dialogs.Confirm(
                "Nonaktifkan akun",
                $"Nonaktifkan akun \"{row.Username}\"? Pengguna ini tidak akan bisa login sampai diaktifkan kembali.",
                "Nonaktifkan",
                isDanger: true))
        {
            return;
        }

        try
        {
            var result = await _accounts.SetActiveAsync(_userId, row.UserId, !row.IsActive);
            if (!result.Success)
            {
                StaffError = result.Message;
                return;
            }

            StaffSuccess = result.Message;
            await LoadStaffAsync();
        }
        catch (Exception)
        {
            StaffError = "Gagal mengubah status akun. Periksa koneksi database lalu coba lagi.";
        }
    }

    public void Dispose() => _theme.Changed -= OnThemeChanged;
}
