using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymSys.App.Services;
using GymSys.Core.Entities;
using GymSys.Core.Interfaces;

namespace GymSys.App.ViewModels;

/// <summary>Pilihan filter status di Daftar Member. Status null = semua kecuali Nonaktif (FR-04).</summary>
public sealed record MemberStatusOption(string Title, MemberStatus? Status)
{
    // Kotak ComboBox bertema menampilkan ToString(), bukan DisplayMemberPath.
    public override string ToString() => Title;
}

/// <summary>
/// Daftar Member (PRD Bagian 9): pencarian real-time (FR-05), filter status, paging 50 (NFR-02).
/// Instruktur hanya melihat member yang ditugaskan padanya (FR-10).
/// </summary>
public partial class MembersViewModel : ObservableObject, IAsyncInitializable
{
    public const int PageSize = 50;

    private readonly IMemberRepository _members;
    private readonly ITrainerRepository _trainers;
    private readonly IMemberService _memberService;
    private readonly ICurrentUserService _currentUser;
    private readonly IDialogService _dialogs;
    private readonly INavigationService _navigation;
    private readonly IServiceProvider _services;
    private readonly TimeProvider _time;

    private int _loadVersion;
    private int? _trainerId;
    private bool _trainerMissing;

    public MembersViewModel(
        IMemberRepository members,
        ITrainerRepository trainers,
        IMemberService memberService,
        ICurrentUserService currentUser,
        IDialogService dialogs,
        INavigationService navigation,
        IServiceProvider services,
        TimeProvider time)
    {
        _members = members;
        _trainers = trainers;
        _memberService = memberService;
        _currentUser = currentUser;
        _dialogs = dialogs;
        _navigation = navigation;
        _services = services;
        _time = time;
        IsInstruktur = currentUser.IsInRole(UserRole.Instruktur);
        CanManage = currentUser.IsInRole(UserRole.Admin, UserRole.Resepsionis);
        _selectedStatus = StatusOptions[0];
    }

    public bool IsInstruktur { get; }

    /// <summary>Admin dan Resepsionis boleh menambah, mengubah, dan menonaktifkan member.</summary>
    public bool CanManage { get; }

    public IReadOnlyList<MemberStatusOption> StatusOptions { get; } = new MemberStatusOption[]
    {
        new("Semua status", null),
        new("Aktif", MemberStatus.Aktif),
        new("Akan Habis", MemberStatus.AkanHabis),
        new("Kedaluwarsa", MemberStatus.Kedaluwarsa),
        new("Belum Berlangganan", MemberStatus.BelumBerlangganan),
        new("Nonaktif", MemberStatus.Nonaktif),
    };

    public ObservableCollection<MemberListRowDto> Rows { get; } = new();

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private MemberStatusOption _selectedStatus;
    [ObservableProperty] private int _pageIndex = 1;
    [ObservableProperty] private int _totalItems;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isEmpty;
    [ObservableProperty] private string _errorMessage = "";
    [ObservableProperty] private string _emptyText = "";

    public async Task InitializeAsync()
    {
        ErrorMessage = "";
        if (IsInstruktur)
        {
            try
            {
                var user = _currentUser.User;
                var trainer = user is null ? null : await _trainers.GetByUserIdAsync(user.UserId);
                _trainerId = trainer?.TrainerId;
                _trainerMissing = trainer is null;
            }
            catch (Exception ex)
            {
                AppLog.Error(ex, "MembersViewModel.Trainer");
                ErrorMessage = "Gagal memuat data instruktur. Periksa koneksi database, lalu coba lagi.";
                return;
            }
        }

        await LoadAsync();
    }

    /// <summary>Dipanggil SearchBox setelah debounce 300 ms (SearchText sudah diperbarui lewat binding).</summary>
    [RelayCommand]
    private Task SearchAsync()
    {
        PageIndex = 1;
        return LoadAsync();
    }

    partial void OnSelectedStatusChanged(MemberStatusOption value)
    {
        PageIndex = 1;
        _ = LoadAsync();
    }

    /// <summary>Dipanggil PagingBar dengan nomor halaman (PageIndex sudah diperbarui lewat binding).</summary>
    [RelayCommand]
    private Task GoToPageAsync(int page)
    {
        PageIndex = page;
        return LoadAsync();
    }

    [RelayCommand]
    private Task RetryAsync() => InitializeAsync();

    /// <summary>Tombol Tambah Member dan shortcut Ctrl+N.</summary>
    [RelayCommand(CanExecute = nameof(CanManage))]
    private void Add() => OpenForm(null);

    [RelayCommand]
    private void Edit(MemberListRowDto? row)
    {
        if (row is not null && CanManage)
            OpenForm(row.MemberId);
    }

    /// <summary>Tombol Detail dan klik dua kali baris. Semua peran boleh melihat detail member di daftarnya.</summary>
    [RelayCommand]
    private void Detail(MemberListRowDto? row)
    {
        if (row is null)
            return;
        _navigation.NavigateTo(new MemberDetailViewModel(
            _members, _trainers, _memberService, _currentUser, _dialogs, _navigation, _services, _time,
            row.MemberId, () => _navigation.NavigateTo(this)));
    }

    /// <summary>Kembali dari form langsung ke daftar ini (filter dan halaman tetap), lalu daftar dimuat ulang.</summary>
    private void OpenForm(int? memberId) =>
        _navigation.NavigateTo(new MemberFormViewModel(
            _memberService, _members, _dialogs, _time, memberId, () => _navigation.NavigateTo(this)));

    private async Task LoadAsync()
    {
        if (_trainerMissing)
        {
            Rows.Clear();
            TotalItems = 0;
            IsEmpty = true;
            EmptyText = "Akun Anda belum terhubung ke data instruktur. Hubungi Admin.";
            return;
        }

        var version = ++_loadVersion;
        IsBusy = true;
        ErrorMessage = "";
        try
        {
            var keyword = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();
            var hariIni = DateOnly.FromDateTime(_time.GetLocalNow().DateTime);
            var result = await _members.SearchAsync(new MemberListQuery(
                keyword, SelectedStatus.Status, hariIni, PageIndex, PageSize, _trainerId));
            if (version != _loadVersion) return;

            // Halaman terakhir bisa kosong bila data berkurang (mis. setelah member dinonaktifkan).
            var lastPage = Math.Max(1, (int)Math.Ceiling(result.TotalCount / (double)PageSize));
            if (result.Items.Count == 0 && PageIndex > lastPage)
            {
                PageIndex = lastPage;
                await LoadAsync();
                return;
            }

            Rows.Clear();
            foreach (var row in result.Items)
                Rows.Add(row);
            TotalItems = result.TotalCount;
            IsEmpty = result.TotalCount == 0;
            EmptyText = keyword is null
                ? "Tidak ada member untuk filter yang dipilih."
                : $"Tidak ada member yang cocok dengan \"{keyword}\".";
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "MembersViewModel");
            if (version == _loadVersion)
                ErrorMessage = "Gagal memuat daftar member. Periksa koneksi database, lalu coba lagi.";
        }
        finally
        {
            if (version == _loadVersion)
                IsBusy = false;
        }
    }
}
