using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymSys.App.Services;
using GymSys.Core.Entities;
using GymSys.Core.Interfaces;
using GymSys.Core.Services;

namespace GymSys.App.ViewModels;

/// <summary>
/// Paket membership (FR-09): daftar untuk semua peran, form tambah/ubah/nonaktif/hapus khusus Admin.
/// Paket yang sudah dipakai transaksi tidak bisa dihapus, hanya dinonaktifkan.
/// </summary>
public partial class PlansViewModel : ObservableValidator, IAsyncInitializable
{
    private static readonly CultureInfo Id = CultureInfo.GetCultureInfo("id-ID");

    private readonly IPlanRepository _plans;
    private readonly IPlanService _service;
    private readonly IDialogService _dialogs;
    private int _loadVersion;

    public PlansViewModel(
        IPlanRepository plans,
        IPlanService service,
        ICurrentUserService currentUser,
        IDialogService dialogs)
    {
        _plans = plans;
        _service = service;
        _dialogs = dialogs;
        IsAdmin = currentUser.IsInRole(UserRole.Admin);
    }

    public bool IsAdmin { get; }

    public bool IsViewOnly => !IsAdmin;

    public ObservableCollection<PlanRowDto> Rows { get; } = new();

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isEmpty;
    [ObservableProperty] private string _errorMessage = "";
    [ObservableProperty] private string _formError = "";

    // --- Form ---

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditing), nameof(FormTitle), nameof(CanDelete), nameof(EditHint))]
    private int? _editingPlanId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToggleActiveText), nameof(EditingStatusText))]
    private bool _editingIsActive;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDelete), nameof(EditHint))]
    private bool _editingIsUsed;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(PlansViewModel), nameof(ValidateNamaPaket))]
    private string _namaPaket = "";

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(PlansViewModel), nameof(ValidateDurasi))]
    private string _durasiHariText = "";

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(PlansViewModel), nameof(ValidateHarga))]
    private string _hargaText = "";

    [ObservableProperty] private string _deskripsi = "";

    public bool IsEditing => EditingPlanId is not null;

    public string FormTitle => IsEditing ? "Ubah Paket" : "Tambah Paket";

    public bool CanDelete => IsEditing && !EditingIsUsed;

    public string ToggleActiveText => EditingIsActive ? "Nonaktifkan Paket" : "Aktifkan Kembali";

    public string EditingStatusText => EditingIsActive
        ? "Status: Aktif"
        : "Status: Nonaktif (tidak muncul di transaksi baru)";

    public string EditHint => IsEditing && EditingIsUsed
        ? "Paket ini sudah dipakai transaksi, jadi tidak bisa dihapus. Perubahan harga dan durasi hanya berlaku untuk transaksi baru."
        : "";

    public Task InitializeAsync() => LoadAsync();

    [RelayCommand]
    private Task RetryAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        var version = ++_loadVersion;
        IsBusy = true;
        ErrorMessage = "";
        try
        {
            var rows = await _plans.GetAllAsync();
            if (version != _loadVersion) return;

            Rows.Clear();
            foreach (var row in rows)
                Rows.Add(row);
            IsEmpty = rows.Count == 0;
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "PlansViewModel");
            if (version == _loadVersion)
                ErrorMessage = "Gagal memuat daftar paket. Periksa koneksi database, lalu coba lagi.";
        }
        finally
        {
            if (version == _loadVersion)
                IsBusy = false;
        }
    }

    // --- Validasi field (aturan yang sama dengan PlanService) ---

    public static ValidationResult? ValidateNamaPaket(string? value, ValidationContext context) =>
        ToResult(MasterDataRules.ValidasiNamaPaket(value));

    public static ValidationResult? ValidateDurasi(string? value, ValidationContext context) =>
        ParseDurasi(value) is { } durasi
            ? ToResult(MasterDataRules.ValidasiDurasiHari(durasi))
            : new ValidationResult("Durasi wajib diisi dengan angka.");

    public static ValidationResult? ValidateHarga(string? value, ValidationContext context) =>
        ParseHarga(value) is { } harga
            ? ToResult(MasterDataRules.ValidasiHarga(harga))
            : new ValidationResult("Harga wajib diisi dengan angka, mis. 250000 atau 250.000.");

    private static int? ParseDurasi(string? text) =>
        int.TryParse(text?.Trim(), NumberStyles.Integer, Id, out var durasi) ? durasi : null;

    /// <summary>Format Indonesia: titik pemisah ribuan, koma pemisah desimal.</summary>
    private static decimal? ParseHarga(string? text) =>
        decimal.TryParse(text?.Trim(), NumberStyles.Number, Id, out var harga) ? harga : null;

    private static ValidationResult? ToResult(string? error) =>
        error is null ? ValidationResult.Success : new ValidationResult(error);

    // --- Aksi ---

    [RelayCommand]
    private void Edit(PlanRowDto? row)
    {
        if (row is null || !IsAdmin)
            return;

        EditingPlanId = row.PlanId;
        EditingIsActive = row.IsActive;
        EditingIsUsed = row.IsUsed;
        NamaPaket = row.NamaPaket;
        DurasiHariText = row.DurasiHari.ToString(Id);
        HargaText = row.Harga.ToString("0.##", Id);
        Deskripsi = row.Deskripsi ?? "";
        ClearErrors();
        FormError = "";
    }

    [RelayCommand]
    private void CancelEdit() => ResetForm();

    private void ResetForm()
    {
        EditingPlanId = null;
        EditingIsActive = false;
        EditingIsUsed = false;
        NamaPaket = "";
        DurasiHariText = "";
        HargaText = "";
        Deskripsi = "";
        ClearErrors();
        FormError = "";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!IsAdmin)
            return;

        ValidateAllProperties();
        if (HasErrors || ParseDurasi(DurasiHariText) is not { } durasi || ParseHarga(HargaText) is not { } harga)
        {
            FormError = "Periksa kembali isian yang ditandai merah.";
            return;
        }

        IsBusy = true;
        FormError = "";
        try
        {
            var input = new PlanInput(NamaPaket, durasi, harga, Deskripsi);
            var result = EditingPlanId is { } id
                ? await _service.UpdateAsync(id, input)
                : await _service.CreateAsync(input);
            if (!result.Success)
            {
                FormError = result.Message;
                return;
            }

            _dialogs.Info("Paket", result.Message);
            ResetForm();
            await LoadAsync();
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "PlansViewModel.Save");
            FormError = "Paket gagal disimpan dan tidak ada data yang berubah. Periksa koneksi database, lalu coba lagi.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ToggleActiveAsync()
    {
        if (EditingPlanId is not { } id)
            return;

        var nonaktifkan = EditingIsActive;
        var setuju = nonaktifkan
            ? _dialogs.Confirm(
                "Nonaktifkan paket",
                $"Nonaktifkan paket {NamaPaket}?\n\nPaket tidak muncul lagi di transaksi baru, tetapi riwayat transaksi tetap utuh.",
                "Nonaktifkan",
                isDanger: true)
            : _dialogs.Confirm("Aktifkan paket", $"Aktifkan kembali paket {NamaPaket}?", "Aktifkan");
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
            _dialogs.Info("Paket", result.Message);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "PlansViewModel.ToggleActive");
            FormError = "Status paket gagal diubah. Periksa koneksi database, lalu coba lagi.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (EditingPlanId is not { } id || EditingIsUsed)
            return;

        if (!_dialogs.Confirm(
                "Hapus paket",
                $"Hapus paket {NamaPaket} secara permanen?\n\nPaket ini belum pernah dipakai transaksi.",
                "Hapus",
                isDanger: true))
            return;

        IsBusy = true;
        FormError = "";
        try
        {
            var result = await _service.DeleteAsync(id);
            if (!result.Success)
            {
                FormError = result.Message;
                return;
            }

            _dialogs.Info("Paket", result.Message);
            ResetForm();
            await LoadAsync();
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "PlansViewModel.Delete");
            FormError = "Paket gagal dihapus. Periksa koneksi database, lalu coba lagi.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
