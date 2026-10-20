using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymSys.App.Services;
using GymSys.Core.Entities;
using GymSys.Core.Interfaces;
using GymSys.Core.Services;

namespace GymSys.App.ViewModels;

/// <summary>Pilihan akun login untuk instruktur. UserId null = tidak dihubungkan.</summary>
public sealed record AccountOption(string Title, int? UserId)
{
    // Kotak ComboBox bertema menampilkan ToString(), bukan DisplayMemberPath.
    public override string ToString() => Title;
}

/// <summary>
/// Instruktur (FR-10): Admin mengelola data instruktur, Resepsionis melihat, Instruktur melihat profil sendiri.
/// Panel bawah menampilkan member yang sedang ditangani instruktur yang dipilih.
/// Penugasan PT ke member dilakukan dari Detail Member.
/// </summary>
public partial class TrainersViewModel : ObservableValidator, IAsyncInitializable
{
    private const int MaksMemberDitampilkan = 200;
    private static readonly AccountOption TanpaAkun = new("Tidak dihubungkan", null);

    private readonly ITrainerRepository _trainers;
    private readonly ITrainerService _service;
    private readonly IMemberRepository _members;
    private readonly IDialogService _dialogs;
    private readonly TimeProvider _time;
    private readonly int? _userId;

    private int _loadVersion;
    private int _membersVersion;

    public TrainersViewModel(
        ITrainerRepository trainers,
        ITrainerService service,
        IMemberRepository members,
        ICurrentUserService currentUser,
        IDialogService dialogs,
        TimeProvider time)
    {
        _trainers = trainers;
        _service = service;
        _members = members;
        _dialogs = dialogs;
        _time = time;
        _userId = currentUser.User?.UserId;
        IsAdmin = currentUser.IsInRole(UserRole.Admin);
        IsInstruktur = currentUser.IsInRole(UserRole.Instruktur);
        _selectedAccount = TanpaAkun;
    }

    public bool IsAdmin { get; }

    public bool IsInstruktur { get; }

    public bool IsViewOnly => !IsAdmin;

    public string ViewOnlyText => IsInstruktur
        ? "Profil instruktur Anda dan member yang Anda tangani."
        : "Hanya Admin yang dapat menambah atau mengubah instruktur. Penugasan PT dilakukan dari Detail Member.";

    public ObservableCollection<TrainerRowDto> Rows { get; } = new();
    public ObservableCollection<MemberListRowDto> AssignedMembers { get; } = new();
    public ObservableCollection<AccountOption> AccountOptions { get; } = new();

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isEmpty;
    [ObservableProperty] private string _emptyText = "";
    [ObservableProperty] private string _errorMessage = "";
    [ObservableProperty] private string _formError = "";

    [ObservableProperty] private TrainerRowDto? _selectedTrainer;
    [ObservableProperty] private string _assignedTitle = "Pilih instruktur untuk melihat member yang ditangani.";
    [ObservableProperty] private bool _hasNoAssignedMembers;

    // --- Form ---

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditing), nameof(FormTitle))]
    private int? _editingTrainerId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToggleActiveText), nameof(EditingStatusText))]
    private bool _editingIsActive;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(TrainersViewModel), nameof(ValidateNama))]
    private string _nama = "";

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(TrainersViewModel), nameof(ValidateNoTelepon))]
    private string _noTelepon = "";

    [ObservableProperty] private string _keahlian = "";
    [ObservableProperty] private AccountOption _selectedAccount;

    public bool IsEditing => EditingTrainerId is not null;

    public string FormTitle => IsEditing ? "Ubah Instruktur" : "Tambah Instruktur";

    public string ToggleActiveText => EditingIsActive ? "Nonaktifkan Instruktur" : "Aktifkan Kembali";

    public string EditingStatusText => EditingIsActive
        ? "Status: Aktif"
        : "Status: Nonaktif (tidak bisa ditugaskan ke member baru)";

    public async Task InitializeAsync()
    {
        if (IsAdmin)
        {
            try
            {
                await LoadAccountsAsync(null, null);
            }
            catch (Exception ex)
            {
                AppLog.Error(ex, "TrainersViewModel.Accounts");
                FormError = "Gagal memuat daftar akun login.";
            }
        }

        await LoadAsync();
    }

    [RelayCommand]
    private Task RetryAsync() => InitializeAsync();

    private async Task LoadAsync()
    {
        var version = ++_loadVersion;
        IsBusy = true;
        ErrorMessage = "";
        try
        {
            var rows = await _trainers.GetAllAsync(includeInactive: IsAdmin);
            if (version != _loadVersion) return;

            // Instruktur hanya melihat profil sendiri (PRD Bagian 4).
            if (IsInstruktur)
                rows = rows.Where(r => r.UserId == _userId).ToList();

            var selectedId = SelectedTrainer?.TrainerId;
            Rows.Clear();
            foreach (var row in rows)
                Rows.Add(row);

            IsEmpty = Rows.Count == 0;
            EmptyText = IsInstruktur
                ? "Akun Anda belum terhubung ke data instruktur. Hubungi Admin."
                : "Belum ada instruktur.";
            SelectedTrainer = Rows.FirstOrDefault(r => r.TrainerId == selectedId)
                              ?? (IsInstruktur ? Rows.FirstOrDefault() : null);
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "TrainersViewModel");
            if (version == _loadVersion)
                ErrorMessage = "Gagal memuat daftar instruktur. Periksa koneksi database, lalu coba lagi.";
        }
        finally
        {
            if (version == _loadVersion)
                IsBusy = false;
        }
    }

    partial void OnSelectedTrainerChanged(TrainerRowDto? value) => _ = LoadAssignedMembersAsync(value);

    private async Task LoadAssignedMembersAsync(TrainerRowDto? trainer)
    {
        var version = ++_membersVersion;
        AssignedMembers.Clear();
        HasNoAssignedMembers = false;
        if (trainer is null)
        {
            AssignedTitle = "Pilih instruktur untuk melihat member yang ditangani.";
            return;
        }

        AssignedTitle = $"Member yang ditangani {trainer.Nama}";
        try
        {
            var hariIni = DateOnly.FromDateTime(_time.GetLocalNow().DateTime);
            var result = await _members.SearchAsync(new MemberListQuery(
                null, null, hariIni, 1, MaksMemberDitampilkan, trainer.TrainerId));
            if (version != _membersVersion) return;

            foreach (var row in result.Items)
                AssignedMembers.Add(row);
            HasNoAssignedMembers = result.TotalCount == 0;
            if (result.TotalCount > result.Items.Count)
                AssignedTitle += $" (menampilkan {result.Items.Count} dari {result.TotalCount})";
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "TrainersViewModel.Members");
            if (version == _membersVersion)
                AssignedTitle = "Gagal memuat member yang ditangani. Pilih ulang instruktur.";
        }
    }

    private async Task LoadAccountsAsync(int? trainerId, int? selectedUserId)
    {
        var accounts = await _trainers.GetAvailableAccountsAsync(trainerId);
        AccountOptions.Clear();
        AccountOptions.Add(TanpaAkun);
        foreach (var account in accounts)
            AccountOptions.Add(new AccountOption(account.Display, account.UserId));
        SelectedAccount = AccountOptions.FirstOrDefault(o => o.UserId == selectedUserId) ?? TanpaAkun;
    }

    // --- Validasi field (aturan yang sama dengan TrainerService) ---

    public static ValidationResult? ValidateNama(string? value, ValidationContext context) =>
        ToResult(MasterDataRules.ValidasiNama(value));

    /// <summary>No. HP instruktur boleh kosong, tetapi harus valid bila diisi.</summary>
    public static ValidationResult? ValidateNoTelepon(string? value, ValidationContext context) =>
        string.IsNullOrWhiteSpace(value) ? ValidationResult.Success : ToResult(MasterDataRules.ValidasiNoTelepon(value));

    private static ValidationResult? ToResult(string? error) =>
        error is null ? ValidationResult.Success : new ValidationResult(error);

    // --- Aksi (Admin) ---

    [RelayCommand]
    private async Task EditAsync(TrainerRowDto? row)
    {
        if (row is null || !IsAdmin)
            return;

        SelectedTrainer = row;
        EditingTrainerId = row.TrainerId;
        EditingIsActive = row.IsActive;
        Nama = row.Nama;
        NoTelepon = row.NoTelepon ?? "";
        Keahlian = row.Keahlian ?? "";
        ClearErrors();
        FormError = "";
        try
        {
            await LoadAccountsAsync(row.TrainerId, row.UserId);
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "TrainersViewModel.Accounts");
            FormError = "Gagal memuat daftar akun login.";
        }
    }

    [RelayCommand]
    private Task CancelEditAsync() => ResetFormAsync();

    private async Task ResetFormAsync()
    {
        EditingTrainerId = null;
        EditingIsActive = false;
        Nama = "";
        NoTelepon = "";
        Keahlian = "";
        ClearErrors();
        FormError = "";
        try
        {
            await LoadAccountsAsync(null, null);
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "TrainersViewModel.Accounts");
            FormError = "Gagal memuat daftar akun login.";
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!IsAdmin)
            return;

        ValidateAllProperties();
        if (HasErrors)
        {
            FormError = "Periksa kembali isian yang ditandai merah.";
            return;
        }

        IsBusy = true;
        FormError = "";
        try
        {
            var input = new TrainerInput(Nama, NoTelepon, Keahlian, SelectedAccount.UserId);
            var result = EditingTrainerId is { } id
                ? await _service.UpdateAsync(id, input)
                : await _service.CreateAsync(input);
            if (!result.Success)
            {
                FormError = result.Message;
                return;
            }

            _dialogs.Info("Instruktur", result.Message);
            await ResetFormAsync();
            await LoadAsync();
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "TrainersViewModel.Save");
            FormError = "Data instruktur gagal disimpan dan tidak ada data yang berubah. Periksa koneksi database, lalu coba lagi.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ToggleActiveAsync()
    {
        if (EditingTrainerId is not { } id)
            return;

        var nonaktifkan = EditingIsActive;
        var setuju = nonaktifkan
            ? _dialogs.Confirm(
                "Nonaktifkan instruktur",
                $"Nonaktifkan {Nama}?\n\nInstruktur nonaktif tidak bisa ditugaskan ke member baru. " +
                "Member yang sedang ditanganinya bisa dipindahkan lewat Detail Member.",
                "Nonaktifkan",
                isDanger: true)
            : _dialogs.Confirm("Aktifkan instruktur", $"Aktifkan kembali {Nama}?", "Aktifkan");
        if (!setuju)
            return;

        IsBusy = true;
        FormError = "";
        try
        {
            var result = await _service.SetActiveAsync(id, !nonaktifkan);
            if (!result.Success)
            {
                FormError = result.Message;
                return;
            }

            EditingIsActive = !nonaktifkan;
            _dialogs.Info("Instruktur", result.Message);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "TrainersViewModel.ToggleActive");
            FormError = "Status instruktur gagal diubah. Periksa koneksi database, lalu coba lagi.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
