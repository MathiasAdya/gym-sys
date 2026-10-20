using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymSys.App.Services;
using GymSys.Core.Entities;
using GymSys.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GymSys.App.ViewModels;

/// <summary>Satu entri menu sidebar. CreateViewModel dipanggil setiap menu dipilih.</summary>
public sealed record MenuItemModel(string Title, string Icon, Func<object> CreateViewModel);

public partial class ShellViewModel : ObservableObject
{
    private readonly INavigationService _navigation;
    private readonly IThemeService _theme;
    private readonly ICurrentUserService _currentUser;
    private readonly IDialogService _dialogs;
    private readonly IServiceProvider _services;

    public ObservableCollection<MenuItemModel> MenuItems { get; } = new();

    public string UserName { get; }
    public string RoleText { get; }

    public event Action? LogoutRequested;

    [ObservableProperty]
    private MenuItemModel? _selectedMenu;

    [ObservableProperty]
    private object? _content;

    [ObservableProperty]
    private string _pageTitle = "";

    [ObservableProperty]
    private string _themeToggleText = "";

    public ShellViewModel(
        INavigationService navigation,
        IThemeService theme,
        ICurrentUserService currentUser,
        IDialogService dialogs,
        IServiceProvider services)
    {
        _navigation = navigation;
        _theme = theme;
        _currentUser = currentUser;
        _dialogs = dialogs;
        _services = services;

        var user = currentUser.User
                   ?? throw new InvalidOperationException("Shell dibuka tanpa pengguna yang login.");

        UserName = user.NamaLengkap;
        _theme.SetUser(user.Username); // muat tema tersimpan milik pengguna ini
        RoleText = user.Role switch
        {
            UserRole.Admin => "Admin",
            UserRole.Resepsionis => "Resepsionis",
            UserRole.Instruktur => "Instruktur (PT)",
            _ => user.Role.ToString()
        };

        _navigation.Navigated += OnNavigated;
        _theme.Changed += OnThemeChanged;
        UpdateThemeText();

        if (user.MustChangePassword)
            MenuItems.Add(new("Pengaturan", "⚙", CreateSettingsUntilPasswordChanged));
        else
            BuildMenu(user.Role);
        SelectedMenu = MenuItems[0];
    }

    /// <summary>Akun yang wajib ganti password hanya bisa membuka Pengaturan sampai password diganti.</summary>
    private object CreateSettingsUntilPasswordChanged()
    {
        var settings = _services.GetRequiredService<SettingsViewModel>();
        settings.PasswordChangeCompleted += () =>
        {
            var user = _currentUser.User;
            if (user is null || user.MustChangePassword)
                return;

            _dialogs.Info("Password diganti", "Password berhasil diganti. Semua menu sekarang bisa dipakai.");
            MenuItems.Clear();
            BuildMenu(user.Role);
            SelectedMenu = MenuItems[0];
        };
        return settings;
    }

    /// <summary>
    /// Menu sesuai matriks hak akses PRD Bagian 4 (Instruktur tidak punya Transaksi & Laporan).
    /// Layar yang belum dibuat memakai PlaceholderViewModel; Anggota 1/3 mengganti isi lambda-nya
    /// dengan ViewModel asli milik mereka, mis. () => _services.GetRequiredService{MembersViewModel}().
    /// </summary>
    private void BuildMenu(UserRole role)
    {
        MenuItems.Add(new("Dashboard", "🏠", () => _services.GetRequiredService<DashboardViewModel>()));
        MenuItems.Add(new("Member", "👥", () => _services.GetRequiredService<MembersViewModel>()));

        if (role != UserRole.Instruktur)
        {
            MenuItems.Add(new("Transaksi", "💳", () => _services.GetRequiredService<NewTransactionViewModel>()));
            MenuItems.Add(new("Riwayat Transaksi", "🧾", () => _services.GetRequiredService<TransactionHistoryViewModel>()));
        }

        MenuItems.Add(new("Paket", "🏷", () => _services.GetRequiredService<PlansViewModel>()));
        MenuItems.Add(new("Instruktur", "🏋", () => new PlaceholderViewModel("Instruktur", "Anggota 3")));

        if (role != UserRole.Instruktur)
            MenuItems.Add(new("Laporan", "📊", () => _services.GetRequiredService<ReportViewModel>()));

        MenuItems.Add(new("Pengaturan", "⚙", () => _services.GetRequiredService<SettingsViewModel>()));
    }

    partial void OnSelectedMenuChanged(MenuItemModel? value)
    {
        if (value is null)
            return;

        PageTitle = value.Title;
        _navigation.NavigateTo(value.CreateViewModel());
    }

    private void OnNavigated(object? viewModel) => Content = viewModel;

    private void OnThemeChanged(AppTheme _) => UpdateThemeText();

    private void UpdateThemeText()
        => ThemeToggleText = _theme.Current == AppTheme.Dark ? "☀ Mode Terang" : "🌙 Mode Gelap";

    [RelayCommand]
    private void ToggleTheme() => _theme.Toggle();

    [RelayCommand]
    private void Logout()
    {
        if (!_dialogs.Confirm("Keluar", "Keluar dari akun ini?"))
            return;

        _navigation.Navigated -= OnNavigated;
        _theme.Changed -= OnThemeChanged;
        _currentUser.SignOut();
        _theme.SetUser(null); // layar login kembali ke tema default
        LogoutRequested?.Invoke();
    }
}
