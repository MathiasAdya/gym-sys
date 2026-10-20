using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymSys.App.Services;
using GymSys.Core.Entities;
using GymSys.Core.Interfaces;
using GymSys.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GymSys.App.ViewModels;

/// <summary>Pilihan instruktur di Detail Member.</summary>
public sealed record TrainerOption(int TrainerId, string Nama, string? Keahlian)
{
    // Kotak ComboBox bertema menampilkan ToString(), bukan DisplayMemberPath.
    public override string ToString() => Keahlian is null ? Nama : $"{Nama} · {Keahlian}";
}

/// <summary>
/// Detail Member (PRD Bagian 9): profil, status + sisa hari (PRD 7.3), instruktur dan penugasan PT (FR-10),
/// riwayat transaksi, serta tombol Perpanjang yang membuka layar Transaksi Baru milik Anggota 1.
/// </summary>
public partial class MemberDetailViewModel : ObservableObject, IAsyncInitializable
{
    private static readonly CultureInfo Id = CultureInfo.GetCultureInfo("id-ID");

    private readonly IMemberRepository _members;
    private readonly ITrainerRepository _trainers;
    private readonly IMemberService _memberService;
    private readonly ITrainerService _trainerService;
    private readonly IDialogService _dialogs;
    private readonly INavigationService _navigation;
    private readonly IServiceProvider _services;
    private readonly TimeProvider _time;
    private readonly int _memberId;
    private readonly Action _back;

    public MemberDetailViewModel(
        IMemberRepository members,
        ITrainerRepository trainers,
        IMemberService memberService,
        ICurrentUserService currentUser,
        IDialogService dialogs,
        INavigationService navigation,
        IServiceProvider services,
        TimeProvider time,
        int memberId,
        Action back)
    {
        _members = members;
        _trainers = trainers;
        _memberService = memberService;
        _trainerService = services.GetRequiredService<ITrainerService>();
        _dialogs = dialogs;
        _navigation = navigation;
        _services = services;
        _time = time;
        _memberId = memberId;
        _back = back;
        CanManage = currentUser.IsInRole(UserRole.Admin, UserRole.Resepsionis);
    }

    /// <summary>Admin dan Resepsionis boleh mengubah data, menugaskan PT, dan memperpanjang.</summary>
    public bool CanManage { get; }

    public ObservableCollection<TransactionHistoryRowDto> Transactions { get; } = new();
    public ObservableCollection<TrainerOption> TrainerOptions { get; } = new();

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _errorMessage = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRenew), nameof(CanEdit), nameof(CanAssign), nameof(CanEndAssignment))]
    private bool _isLoaded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRenew), nameof(CanAssign), nameof(CanEndAssignment))]
    private bool _isActive;

    [ObservableProperty] private string _namaLengkap = "";
    [ObservableProperty] private string _kodeMember = "";
    [ObservableProperty] private string _tglBergabung = "";
    [ObservableProperty] private MemberStatus _status;
    [ObservableProperty] private string _statusLabel = "";
    [ObservableProperty] private string _sisaText = "";
    [ObservableProperty] private string _noTelepon = "";
    [ObservableProperty] private string _email = "-";
    [ObservableProperty] private string _jenisKelamin = "-";
    [ObservableProperty] private string _tglLahir = "-";
    [ObservableProperty] private string _alamat = "-";
    [ObservableProperty] private string _kontakDarurat = "-";
    [ObservableProperty] private string _catatanKesehatan = "-";
    [ObservableProperty] private string _instruktur = "-";
    [ObservableProperty] private bool _hasNoTransactions;

    [ObservableProperty] private TrainerOption? _selectedTrainerOption;
    [ObservableProperty] private string _assignError = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEndAssignment))]
    private bool _hasTrainer;

    public bool CanEdit => CanManage && IsLoaded;

    /// <summary>Member nonaktif tidak bisa diperpanjang (TransactionService menolaknya).</summary>
    public bool CanRenew => CanManage && IsLoaded && IsActive;

    /// <summary>Member nonaktif tidak bisa ditugaskan PT (TrainerService menolaknya).</summary>
    public bool CanAssign => CanManage && IsLoaded && IsActive;

    public bool CanEndAssignment => CanAssign && HasTrainer;

    public async Task InitializeAsync()
    {
        IsBusy = true;
        ErrorMessage = "";
        try
        {
            var member = await _members.GetByIdAsync(_memberId);
            if (member is null)
            {
                ErrorMessage = "Member tidak ditemukan.";
                return;
            }

            var transaksi = await _members.GetTransactionsAsync(_memberId);
            var pt = await _trainers.GetActiveAssignmentAsync(_memberId);

            // Status dihitung dari transaksi Lunas saja, sama seperti vw_MemberStatus (PRD 7.3).
            var hariIni = DateOnly.FromDateTime(_time.GetLocalNow().DateTime);
            var berakhir = transaksi
                .Where(t => t.Status == StatusTransaksi.Lunas)
                .Select(t => (DateOnly?)t.TglBerakhir)
                .Max();
            var status = MembershipCalculator.HitungStatus(member.IsActive, berakhir, hariIni);

            NamaLengkap = member.NamaLengkap;
            KodeMember = member.KodeMember;
            TglBergabung = member.TglBergabung.ToString("dd MMMM yyyy", Id);
            IsActive = member.IsActive;
            Status = status;
            StatusLabel = status.ToLabel();
            SisaText = BuatSisaText(status, berakhir, hariIni);
            NoTelepon = member.NoTelepon;
            Email = Isi(member.Email);
            JenisKelamin = member.JenisKelamin switch { "L" => "Laki-laki", "P" => "Perempuan", _ => "-" };
            TglLahir = member.TglLahir?.ToString("dd MMMM yyyy", Id) ?? "-";
            Alamat = Isi(member.Alamat);
            KontakDarurat = Isi(member.KontakDarurat);
            CatatanKesehatan = Isi(member.CatatanKesehatan);
            Instruktur = pt is null
                ? "Belum ada instruktur"
                : $"{pt.NamaTrainer} (sejak {pt.TglMulai.ToString("dd MMM yyyy", Id)})";
            HasTrainer = pt is not null;

            if (CanManage)
            {
                var trainers = await _trainers.GetAllAsync(includeInactive: false);
                TrainerOptions.Clear();
                foreach (var t in trainers)
                    TrainerOptions.Add(new TrainerOption(t.TrainerId, t.Nama, t.Keahlian));
                SelectedTrainerOption = TrainerOptions.FirstOrDefault(o => o.TrainerId == pt?.TrainerId);
            }

            Transactions.Clear();
            foreach (var row in transaksi)
                Transactions.Add(row);
            HasNoTransactions = transaksi.Count == 0;
            AssignError = "";
            IsLoaded = true;
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "MemberDetailViewModel");
            ErrorMessage = "Gagal memuat detail member. Periksa koneksi database, lalu coba lagi.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string BuatSisaText(MemberStatus status, DateOnly? berakhir, DateOnly hariIni)
    {
        var tanggal = berakhir?.ToString("dd MMMM yyyy", Id) ?? "";
        return status switch
        {
            MemberStatus.Nonaktif => "Member dinonaktifkan.",
            MemberStatus.BelumBerlangganan => "Belum pernah berlangganan.",
            MemberStatus.Kedaluwarsa => $"Masa aktif berakhir {tanggal}.",
            _ => MembershipCalculator.HitungSisaHari(berakhir, hariIni) is var sisa && sisa > 0
                ? $"Sisa {sisa} hari, berakhir {tanggal}."
                : "Hari ini hari terakhir masa aktif.",
        };
    }

    private static string Isi(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value;

    [RelayCommand]
    private Task RetryAsync() => InitializeAsync();

    [RelayCommand]
    private void Back() => _back();

    /// <summary>Kembali dari form langsung ke detail ini, lalu detail dimuat ulang.</summary>
    [RelayCommand]
    private void Edit()
    {
        if (!CanEdit)
            return;
        _navigation.NavigateTo(new MemberFormViewModel(
            _memberService, _members, _dialogs, _time, _memberId, () => _navigation.NavigateTo(this)));
    }

    /// <summary>
    /// Buka layar Transaksi Baru (Anggota 1) lalu cari member ini lewat kodenya.
    /// NewTransactionViewModel otomatis memilih member bila hasil pencarian tepat satu.
    /// </summary>
    [RelayCommand]
    private void Renew()
    {
        if (!CanRenew)
            return;
        var transaksi = _services.GetRequiredService<NewTransactionViewModel>();
        _navigation.NavigateTo(transaksi);
        transaksi.SearchText = KodeMember;
    }

    /// <summary>FR-10: penugasan baru otomatis mengakhiri penugasan aktif sebelumnya.</summary>
    [RelayCommand]
    private async Task AssignAsync()
    {
        if (!CanAssign)
            return;
        if (SelectedTrainerOption is not { } option)
        {
            AssignError = "Pilih instruktur terlebih dahulu.";
            return;
        }

        IsBusy = true;
        AssignError = "";
        try
        {
            var result = await _trainerService.AssignAsync(_memberId, option.TrainerId);
            if (!result.Success)
            {
                AssignError = result.Message;
                return;
            }

            _dialogs.Info("Instruktur", result.Message);
            await InitializeAsync();
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "MemberDetailViewModel.Assign");
            AssignError = "Penugasan gagal disimpan. Periksa koneksi database, lalu coba lagi.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task EndAssignmentAsync()
    {
        if (!CanEndAssignment)
            return;
        if (!_dialogs.Confirm("Akhiri penugasan", $"Akhiri penugasan instruktur untuk {NamaLengkap}?", "Akhiri", isDanger: true))
            return;

        IsBusy = true;
        AssignError = "";
        try
        {
            var result = await _trainerService.EndAssignmentAsync(_memberId);
            if (!result.Success)
            {
                AssignError = result.Message;
                return;
            }

            _dialogs.Info("Instruktur", result.Message);
            await InitializeAsync();
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "MemberDetailViewModel.EndAssignment");
            AssignError = "Penugasan gagal diakhiri. Periksa koneksi database, lalu coba lagi.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
