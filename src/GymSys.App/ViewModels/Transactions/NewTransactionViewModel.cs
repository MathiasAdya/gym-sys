using System.Collections.ObjectModel;
using System.ComponentModel;
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
/// FR-06 Transaksi Baru: pilih member dan paket, pratinjau tanggal, diskon, metode bayar.
/// Pratinjau memakai MembershipCalculator yang sama dengan TransactionService, jadi angka di layar = yang disimpan.
/// </summary>
public partial class NewTransactionViewModel : ObservableValidator, IAsyncInitializable
{
    private const int MaksHasilCari = 20;
    private static readonly CultureInfo Id = CultureInfo.GetCultureInfo("id-ID");

    private readonly ITransactionRepository _repository;
    private readonly ITransactionService _service;
    private readonly IDialogService _dialogs;
    private readonly INavigationService _navigation;
    private readonly TimeProvider _time;
    private readonly bool _isAdmin;

    private DateOnly? _berakhirTerakhir;
    private bool _memberLoaded;
    private int _searchVersion;
    private int _memberVersion;

    public NewTransactionViewModel(
        ITransactionRepository repository,
        ITransactionService service,
        ICurrentUserService currentUser,
        IDialogService dialogs,
        INavigationService navigation,
        TimeProvider time)
    {
        _repository = repository;
        _service = service;
        _dialogs = dialogs;
        _navigation = navigation;
        _time = time;
        _isAdmin = currentUser.IsInRole(UserRole.Admin);
        ErrorsChanged += OnErrorsChanged;
    }

    public ObservableCollection<MemberLookupDto> Members { get; } = new();
    public ObservableCollection<PlanLookupDto> Plans { get; } = new();
    public MetodeBayar[] MetodeOptions { get; } = Enum.GetValues<MetodeBayar>();

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _errorMessage = "";
    [ObservableProperty] private string _searchInfo = "Ketik minimal 2 huruf nama, kode, atau no. HP member.";
    [ObservableProperty] private string _statusMemberText = "";
    [ObservableProperty] private string _hargaText = "-";
    [ObservableProperty] private string _diskonText = "-";
    [ObservableProperty] private string _totalText = "-";
    [ObservableProperty] private string _periodeText = "-";
    [ObservableProperty] private string _tglMulaiHint = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private MemberLookupDto? _selectedMember;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private PlanLookupDto? _selectedPlan;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(NewTransactionViewModel), nameof(ValidateTglMulai))]
    private DateTime? _tglMulai;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(NewTransactionViewModel), nameof(ValidateDiskon))]
    private decimal _diskon;

    [ObservableProperty] private MetodeBayar _metodeBayar = MetodeBayar.Tunai;

    private DateOnly HariIni => DateOnly.FromDateTime(_time.GetLocalNow().DateTime);

    public async Task InitializeAsync()
    {
        IsBusy = true;
        ErrorMessage = "";
        try
        {
            var plans = await _repository.GetActivePlansAsync();
            Plans.Clear();
            foreach (var plan in plans)
                Plans.Add(plan);
            SelectedPlan ??= Plans.FirstOrDefault();
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "NewTransactionViewModel");
            ErrorMessage = "Gagal memuat daftar paket. Periksa koneksi database, lalu coba lagi.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task RetryAsync() => InitializeAsync();

    partial void OnSearchTextChanged(string value) => _ = SearchAsync(value);

    private async Task SearchAsync(string keyword)
    {
        var version = ++_searchVersion;
        if (keyword.Trim().Length < 2)
        {
            Members.Clear();
            SearchInfo = "Ketik minimal 2 huruf nama, kode, atau no. HP member.";
            return;
        }

        try
        {
            var rows = await _repository.SearchMembersAsync(keyword, MaksHasilCari);
            if (version != _searchVersion) return;

            Members.Clear();
            foreach (var row in rows)
                Members.Add(row);
            SearchInfo = rows.Count switch
            {
                0 => "Member aktif tidak ditemukan.",
                MaksHasilCari => $"Menampilkan {MaksHasilCari} hasil pertama; perjelas kata kunci.",
                _ => $"{rows.Count} member ditemukan.",
            };
            if (rows.Count == 1)
                SelectedMember = rows[0];
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "NewTransactionViewModel");
            if (version == _searchVersion)
                SearchInfo = "Pencarian gagal. Periksa koneksi database.";
        }
    }

    partial void OnSelectedMemberChanged(MemberLookupDto? value) => _ = LoadMemberAsync(value);

    private async Task LoadMemberAsync(MemberLookupDto? member)
    {
        var version = ++_memberVersion;
        _memberLoaded = false;
        _berakhirTerakhir = null;
        StatusMemberText = "";
        if (member is null)
        {
            TglMulai = null;
            ClearErrors(nameof(TglMulai));
            UpdatePreview();
            return;
        }

        try
        {
            var berakhir = await _repository.GetTglBerakhirTerakhirAsync(member.MemberId);
            if (version != _memberVersion) return;

            var hariIni = HariIni;
            _berakhirTerakhir = berakhir;
            _memberLoaded = true;
            var status = MembershipCalculator.HitungStatus(true, berakhir, hariIni);
            StatusMemberText = berakhir is { } b
                ? $"Status: {status.ToLabel()} (berakhir {Format(b)})"
                : $"Status: {status.ToLabel()}";

            var tglDefault = MembershipCalculator.HitungTglMulaiDefault(berakhir, hariIni);
            TglMulaiHint = tglDefault == hariIni
                ? "Default hari ini; boleh dipilih sampai 30 hari ke depan."
                : "Perpanjangan: dimulai sehari setelah masa aktif berakhir.";
            TglMulai = tglDefault.ToDateTime(TimeOnly.MinValue);
            ValidateProperty(TglMulai, nameof(TglMulai));
            UpdatePreview();
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "NewTransactionViewModel");
            if (version == _memberVersion)
                StatusMemberText = "Gagal memuat status member. Pilih ulang member.";
        }
        finally
        {
            SaveCommand.NotifyCanExecuteChanged();
        }
    }

    partial void OnSelectedPlanChanged(PlanLookupDto? value)
    {
        ValidateProperty(Diskon, nameof(Diskon));
        UpdatePreview();
    }

    partial void OnTglMulaiChanged(DateTime? value) => UpdatePreview();

    partial void OnDiskonChanged(decimal value) => UpdatePreview();

    public static ValidationResult? ValidateTglMulai(DateTime? value, ValidationContext context)
    {
        var vm = (NewTransactionViewModel)context.ObjectInstance;
        if (!vm._memberLoaded)
            return ValidationResult.Success;
        if (value is null)
            return new ValidationResult("Tanggal mulai wajib diisi.");

        var error = MembershipCalculator.ValidasiTglMulai(
            DateOnly.FromDateTime(value.Value), vm._berakhirTerakhir, vm.HariIni, vm._isAdmin);
        return error is null ? ValidationResult.Success : new ValidationResult(error);
    }

    public static ValidationResult? ValidateDiskon(decimal value, ValidationContext context)
    {
        var vm = (NewTransactionViewModel)context.ObjectInstance;
        if (value < 0)
            return new ValidationResult("Diskon tidak boleh negatif.");
        if (vm.SelectedPlan is { } plan && value > plan.Harga)
            return new ValidationResult("Diskon tidak boleh melebihi harga paket.");
        return ValidationResult.Success;
    }

    private void UpdatePreview()
    {
        if (SelectedPlan is not { } plan)
        {
            HargaText = DiskonText = TotalText = PeriodeText = "-";
            return;
        }

        HargaText = plan.Harga.ToString("C0", Id);
        DiskonText = Diskon.ToString("C0", Id);
        TotalText = Diskon >= 0 && Diskon <= plan.Harga
            ? MembershipCalculator.HitungTotalBayar(plan.Harga, Diskon).ToString("C0", Id)
            : "-";
        PeriodeText = TglMulai is { } mulai
            ? $"{Format(DateOnly.FromDateTime(mulai))} s.d. {Format(MembershipCalculator.HitungTglBerakhir(DateOnly.FromDateTime(mulai), plan.DurasiHari))}"
            : "-";
    }

    private static string Format(DateOnly date) => date.ToString("dd MMM yyyy", Id);

    private void OnErrorsChanged(object? sender, DataErrorsChangedEventArgs e) => SaveCommand.NotifyCanExecuteChanged();

    private bool CanSave() => !IsBusy && _memberLoaded && SelectedMember is not null && SelectedPlan is not null && !HasErrors;

    partial void OnIsBusyChanged(bool value) => SaveCommand.NotifyCanExecuteChanged();

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        ValidateAllProperties();
        if (HasErrors || SelectedMember is not { } member || SelectedPlan is not { } plan || TglMulai is not { } mulai)
            return;

        if (!_dialogs.Confirm(
                "Simpan transaksi",
                $"{member.NamaLengkap}\n{plan.NamaPaket}, {PeriodeText}\nTotal bayar {TotalText} ({MetodeBayar}).\n\n" +
                "Transaksi yang sudah disimpan tidak dapat diubah.",
                "Simpan"))
            return;

        IsBusy = true;
        ErrorMessage = "";
        try
        {
            var result = await _service.CreateAsync(
                new NewTransactionRequest(member.MemberId, plan.PlanId, DateOnly.FromDateTime(mulai), Diskon, MetodeBayar));
            if (!result.Success)
            {
                ErrorMessage = result.Message;
                return;
            }

            // Form dikosongkan dulu, jadi "Kembali" dari kuitansi langsung siap untuk transaksi berikutnya.
            ResetForm();
            _navigation.NavigateTo(new ReceiptViewModel(_repository, result.Transaction!.TransactionId, () => _navigation.NavigateTo(this)));
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "NewTransactionViewModel");
            ErrorMessage = "Transaksi gagal disimpan dan tidak ada data yang berubah. Periksa koneksi database, lalu coba lagi.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ResetForm()
    {
        SelectedMember = null;
        SearchText = "";
        Diskon = 0;
        MetodeBayar = MetodeBayar.Tunai;
        ClearErrors();
    }
}
