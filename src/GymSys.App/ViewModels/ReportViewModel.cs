using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymSys.App.Services;
using GymSys.Core.Entities;
using GymSys.Core.Interfaces;
using GymSys.Core.Services;

namespace GymSys.App.ViewModels;

public enum ReportKind
{
    Transaksi,
    StatusMember,
    PendapatanPaket
}

public sealed record ReportTypeOption(ReportKind Kind, string Title);

public sealed record PeriodOption(string Title, Func<(DateTime From, DateTime To)> GetRange);

public partial class ReportViewModel : ObservableObject, IAsyncInitializable
{
    private static readonly CultureInfo Id = CultureInfo.GetCultureInfo("id-ID");

    private readonly IReportQueries _queries;
    private readonly IDialogService _dialogs;

    private string[] _exportHeaders = Array.Empty<string>();
    private List<string[]> _exportRows = new();
    private int _loadVersion;

    public ReportViewModel(IReportQueries queries, IDialogService dialogs, ICurrentUserService currentUser)
    {
        _queries = queries;
        _dialogs = dialogs;

        // PRD Bagian 4: Admin melihat semua laporan (termasuk pendapatan); Resepsionis hanya
        // riwayat transaksi dan status member. Instruktur tidak punya menu Laporan.
        var types = new List<ReportTypeOption>
        {
            new(ReportKind.Transaksi, "Riwayat Transaksi"),
            new(ReportKind.StatusMember, "Status Member"),
        };
        if (currentUser.User?.Role == UserRole.Admin)
            types.Add(new(ReportKind.PendapatanPaket, "Pendapatan per Paket"));
        ReportTypes = types;

        Periods = new List<PeriodOption>
        {
            new("Bulan ini", () =>
            {
                var t = DateTime.Today;
                return (new DateTime(t.Year, t.Month, 1), t);
            }),
            new("Bulan lalu", () =>
            {
                var first = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
                return (first, first.AddMonths(1).AddDays(-1));
            }),
            new("30 hari terakhir", () => (DateTime.Today.AddDays(-29), DateTime.Today)),
            new("Tahun ini", () => (new DateTime(DateTime.Today.Year, 1, 1), DateTime.Today)),
        };

        // Isi backing field langsung supaya tidak memicu pemuatan ganda di konstruktor.
        _selectedReportType = ReportTypes[0];
        _selectedPeriod = Periods[0];
        _showTransactions = true;
    }

    public IReadOnlyList<ReportTypeOption> ReportTypes { get; }
    public IReadOnlyList<PeriodOption> Periods { get; }

    public ObservableCollection<TransactionRowDto> Transactions { get; } = new();
    public ObservableCollection<MemberStatusRowDto> StatusRows { get; } = new();
    public ObservableCollection<RevenueByPlanDto> RevenueRows { get; } = new();

    [ObservableProperty] private ReportTypeOption? _selectedReportType;
    [ObservableProperty] private PeriodOption? _selectedPeriod;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _errorMessage = "";
    [ObservableProperty] private string _summaryText = "";
    [ObservableProperty] private bool _isEmpty;
    [ObservableProperty] private bool _isPeriodEnabled = true;
    [ObservableProperty] private bool _showTransactions;
    [ObservableProperty] private bool _showStatus;
    [ObservableProperty] private bool _showRevenue;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportCommand))]
    private bool _hasData;

    partial void OnSelectedReportTypeChanged(ReportTypeOption? value) => _ = LoadAsync();

    partial void OnSelectedPeriodChanged(PeriodOption? value) => _ = LoadAsync();

    public Task InitializeAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        var type = SelectedReportType;
        var period = SelectedPeriod;
        if (type is null || period is null)
            return;

        var version = ++_loadVersion;
        IsBusy = true;
        ErrorMessage = "";
        ShowTransactions = type.Kind == ReportKind.Transaksi;
        ShowStatus = type.Kind == ReportKind.StatusMember;
        ShowRevenue = type.Kind == ReportKind.PendapatanPaket;
        IsPeriodEnabled = type.Kind != ReportKind.StatusMember;

        try
        {
            var (rangeFrom, rangeTo) = period.GetRange();

            switch (type.Kind)
            {
                case ReportKind.Transaksi:
                {
                    var rows = await _queries.GetTransactionsAsync(rangeFrom, rangeTo);
                    if (version != _loadVersion) return;

                    Replace(Transactions, rows);
                    var total = rows.Where(r => r.Status == "Lunas").Sum(r => r.TotalBayar);
                    SummaryText = $"{rows.Count} transaksi  ·  Total pendapatan (Lunas): {total.ToString("C0", Id)}";
                    SetExport(
                        new[] { "No. Kuitansi", "Tanggal", "Member", "Paket", "Total Bayar", "Metode", "Status" },
                        rows.Select(r => new[]
                        {
                            r.NoKuitansi, r.TglTransaksi.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                            r.NamaMember, r.NamaPaket, r.TotalBayar.ToString("0.##", Id), r.MetodeBayar, r.Status
                        }),
                        rows.Count);
                    break;
                }

                case ReportKind.StatusMember:
                {
                    var rows = await _queries.GetMemberStatusAsync();
                    if (version != _loadVersion) return;

                    Replace(StatusRows, rows);
                    SummaryText = $"{rows.Count} member  ·  Aktif: {rows.Count(r => r.Status == "Aktif")}, " +
                                  $"Akan Habis: {rows.Count(r => r.Status == "Akan Habis")}, " +
                                  $"Kedaluwarsa: {rows.Count(r => r.Status == "Kedaluwarsa")}";
                    SetExport(
                        new[] { "Kode Member", "Nama", "No. HP", "Berakhir", "Status" },
                        rows.Select(r => new[]
                        {
                            r.KodeMember, r.NamaLengkap, r.NoTelepon,
                            r.TglBerakhir?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "", r.Status
                        }),
                        rows.Count);
                    break;
                }

                default:
                {
                    var rows = await _queries.GetRevenueByPlanAsync(rangeFrom, rangeTo);
                    if (version != _loadVersion) return;

                    Replace(RevenueRows, rows);
                    SummaryText = $"Total pendapatan: {rows.Sum(r => r.TotalPendapatan).ToString("C0", Id)}";
                    SetExport(
                        new[] { "Paket", "Jumlah Transaksi", "Total Pendapatan" },
                        rows.Select(r => new[] { r.NamaPaket, r.JumlahTransaksi.ToString(Id), r.TotalPendapatan.ToString("0.##", Id) }),
                        rows.Count);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "ReportViewModel");
            if (version == _loadVersion)
            {
                ErrorMessage = "Gagal memuat laporan. Periksa koneksi database, lalu coba lagi.";
                SummaryText = "";
                IsEmpty = false;
                HasData = false;
            }
        }
        finally
        {
            if (version == _loadVersion)
                IsBusy = false;
        }
    }

    private void SetExport(string[] headers, IEnumerable<string[]> rows, int count)
    {
        _exportHeaders = headers;
        _exportRows = rows.ToList();
        HasData = count > 0;
        IsEmpty = count == 0;
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items)
            target.Add(item);
    }

    [RelayCommand]
    private Task RetryAsync() => LoadAsync();

    private bool CanExport() => HasData;

    [RelayCommand(CanExecute = nameof(CanExport))]
    private void Export()
    {
        var kind = SelectedReportType?.Kind.ToString() ?? "Laporan";
        var path = _dialogs.PickSaveFile($"laporan-{kind}-{DateTime.Today:yyyyMMdd}.csv", "File CSV (*.csv)|*.csv");
        if (path is null)
            return;

        try
        {
            CsvExporter.Write(path, _exportHeaders, _exportRows);
            _dialogs.Info("Ekspor selesai", $"Laporan disimpan di:\n{path}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _dialogs.Error(
                "Ekspor gagal",
                "File tidak dapat ditulis. Pastikan file tidak sedang terbuka di aplikasi lain, atau pilih lokasi lain.");
        }
    }
}
