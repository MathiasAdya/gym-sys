using System.ComponentModel.DataAnnotations;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymSys.App.Services;
using GymSys.Core.Interfaces;
using GymSys.Core.Services;

namespace GymSys.App.ViewModels;

/// <summary>Pilihan jenis kelamin. Kode null = tidak diisi.</summary>
public sealed record JenisKelaminOption(string Title, string? Kode)
{
    // Kotak ComboBox bertema menampilkan ToString(), bukan DisplayMemberPath.
    public override string ToString() => Title;
}

/// <summary>
/// Form tambah/ubah member (FR-03) serta nonaktifkan/aktifkan kembali (FR-04).
/// Validasi per field memakai MasterDataRules yang sama dengan MemberService.
/// </summary>
public partial class MemberFormViewModel : ObservableValidator, IAsyncInitializable
{
    private static readonly CultureInfo Id = CultureInfo.GetCultureInfo("id-ID");

    private readonly IMemberService _service;
    private readonly IMemberRepository _members;
    private readonly IDialogService _dialogs;
    private readonly TimeProvider _time;
    private readonly int? _memberId;
    private readonly Action _close;

    /// <summary>No. HP yang ditolak karena sudah dipakai member lain (dicek saat Simpan).</summary>
    private string? _noTeleponDipakai;

    public MemberFormViewModel(
        IMemberService service,
        IMemberRepository members,
        IDialogService dialogs,
        TimeProvider time,
        int? memberId,
        Action close)
    {
        _service = service;
        _members = members;
        _dialogs = dialogs;
        _time = time;
        _memberId = memberId;
        _close = close;
        _selectedJenisKelamin = JenisKelaminOptions[0];
    }

    public bool IsEdit => _memberId is not null;

    public string Title => IsEdit ? "Ubah Member" : "Tambah Member";

    public IReadOnlyList<JenisKelaminOption> JenisKelaminOptions { get; } = new JenisKelaminOption[]
    {
        new("Tidak diisi", null),
        new("Laki-laki", "L"),
        new("Perempuan", "P"),
    };

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(MemberFormViewModel), nameof(ValidateNama))]
    private string _namaLengkap = "";

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(MemberFormViewModel), nameof(ValidateNoTelepon))]
    private string _noTelepon = "";

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(MemberFormViewModel), nameof(ValidateEmail))]
    private string _email = "";

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(MemberFormViewModel), nameof(ValidateTglLahir))]
    private DateTime? _tglLahir;

    [ObservableProperty] private JenisKelaminOption _selectedJenisKelamin;
    [ObservableProperty] private string _alamat = "";
    [ObservableProperty] private string _kontakDarurat = "";
    [ObservableProperty] private string _catatanKesehatan = "";

    [ObservableProperty] private string _kodeMember = "Dibuat otomatis saat disimpan";
    [ObservableProperty] private string _tglBergabungText = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToggleActiveText), nameof(StatusText))]
    private bool _isActive = true;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isLoaded;
    [ObservableProperty] private string _errorMessage = "";

    public string ToggleActiveText => IsActive ? "Nonaktifkan Member" : "Aktifkan Kembali";

    public string StatusText => IsActive ? "Aktif" : "Nonaktif (tidak muncul di daftar default)";

    private DateOnly HariIni => DateOnly.FromDateTime(_time.GetLocalNow().DateTime);

    private DateOnly? TglLahirDate => TglLahir is { } t ? DateOnly.FromDateTime(t) : (DateOnly?)null;

    public async Task InitializeAsync()
    {
        if (_memberId is not { } id)
        {
            IsLoaded = true;
            return;
        }

        IsBusy = true;
        ErrorMessage = "";
        try
        {
            var member = await _members.GetByIdAsync(id);
            if (member is null)
            {
                ErrorMessage = "Member tidak ditemukan.";
                return;
            }

            KodeMember = member.KodeMember;
            TglBergabungText = member.TglBergabung.ToString("dd MMMM yyyy", Id);
            NamaLengkap = member.NamaLengkap;
            NoTelepon = member.NoTelepon;
            Email = member.Email ?? "";
            Alamat = member.Alamat ?? "";
            TglLahir = member.TglLahir?.ToDateTime(TimeOnly.MinValue);
            SelectedJenisKelamin = JenisKelaminOptions.FirstOrDefault(o => o.Kode == member.JenisKelamin) ?? JenisKelaminOptions[0];
            KontakDarurat = member.KontakDarurat ?? "";
            CatatanKesehatan = member.CatatanKesehatan ?? "";
            IsActive = member.IsActive;
            ClearErrors();
            IsLoaded = true;
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "MemberFormViewModel.Load");
            ErrorMessage = "Gagal memuat data member. Periksa koneksi database, lalu kembali dan coba lagi.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public static ValidationResult? ValidateNama(string? value, ValidationContext context) =>
        ToResult(MasterDataRules.ValidasiNama(value));

    public static ValidationResult? ValidateNoTelepon(string? value, ValidationContext context)
    {
        var vm = (MemberFormViewModel)context.ObjectInstance;
        if (MasterDataRules.ValidasiNoTelepon(value) is { } error)
            return new ValidationResult(error);
        return vm._noTeleponDipakai is not null && vm._noTeleponDipakai == value?.Trim()
            ? new ValidationResult("No. HP sudah dipakai member lain.")
            : ValidationResult.Success;
    }

    public static ValidationResult? ValidateEmail(string? value, ValidationContext context) =>
        ToResult(MasterDataRules.ValidasiEmail(value));

    public static ValidationResult? ValidateTglLahir(DateTime? value, ValidationContext context)
    {
        var vm = (MemberFormViewModel)context.ObjectInstance;
        var tanggal = value is { } d ? DateOnly.FromDateTime(d) : (DateOnly?)null;
        return ToResult(MasterDataRules.ValidasiTglLahir(tanggal, vm.HariIni));
    }

    private static ValidationResult? ToResult(string? error) =>
        error is null ? ValidationResult.Success : new ValidationResult(error);

    [RelayCommand]
    private async Task SaveAsync()
    {
        _noTeleponDipakai = null;
        ValidateAllProperties();
        if (HasErrors)
        {
            ErrorMessage = "Periksa kembali isian yang ditandai merah.";
            return;
        }

        IsBusy = true;
        ErrorMessage = "";
        try
        {
            // Dicek di sini juga supaya pesannya muncul di bawah kolom No. HP; MemberService tetap mengecek ulang.
            var noTelepon = NoTelepon.Trim();
            if (await _members.IsNoTeleponTakenAsync(noTelepon, _memberId))
            {
                _noTeleponDipakai = noTelepon;
                ValidateProperty(NoTelepon, nameof(NoTelepon));
                ErrorMessage = "Periksa kembali isian yang ditandai merah.";
                return;
            }

            var input = new MemberInput(
                NamaLengkap, NoTelepon, Email, Alamat, TglLahirDate,
                SelectedJenisKelamin.Kode, KontakDarurat, CatatanKesehatan);

            if (_memberId is { } id)
            {
                var result = await _service.UpdateAsync(id, input);
                if (!result.Success)
                {
                    ErrorMessage = result.Message;
                    return;
                }
                _dialogs.Info("Member disimpan", result.Message);
            }
            else
            {
                var result = await _service.CreateAsync(input);
                if (!result.Success)
                {
                    ErrorMessage = result.Message;
                    return;
                }
                _dialogs.Info("Member ditambahkan", result.Message);
            }

            _close();
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "MemberFormViewModel.Save");
            ErrorMessage = "Data member gagal disimpan dan tidak ada data yang berubah. Periksa koneksi database, lalu coba lagi.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ToggleActiveAsync()
    {
        if (_memberId is not { } id)
            return;

        var nonaktifkan = IsActive;
        var setuju = nonaktifkan
            ? _dialogs.Confirm(
                "Nonaktifkan member",
                $"Nonaktifkan {NamaLengkap}?\n\nData tidak dihapus: tetap ada di laporan dan riwayat, dan bisa diaktifkan kembali.",
                "Nonaktifkan",
                isDanger: true)
            : _dialogs.Confirm("Aktifkan member", $"Aktifkan kembali {NamaLengkap}?", "Aktifkan");
        if (!setuju)
            return;

        IsBusy = true;
        ErrorMessage = "";
        try
        {
            var result = await _service.SetActiveAsync(id, !nonaktifkan);
            if (!result.Success)
            {
                ErrorMessage = result.Message;
                return;
            }
            IsActive = !nonaktifkan;
            _dialogs.Info("Status member", result.Message);
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "MemberFormViewModel.ToggleActive");
            ErrorMessage = "Status member gagal diubah. Periksa koneksi database, lalu coba lagi.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel() => _close();
}
