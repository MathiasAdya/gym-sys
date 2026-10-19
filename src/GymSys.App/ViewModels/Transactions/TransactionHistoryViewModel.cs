using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymSys.App.Services;
using GymSys.Core.Entities;
using GymSys.Core.Interfaces;

namespace GymSys.App.ViewModels;

public sealed record StatusFilterOption(string Title, StatusTransaksi? Status);

/// <summary>
/// Riwayat Transaksi (PRD Bagian 9): filter tanggal + status, paging 50 (NFR-02),
/// buka kuitansi, dan pembatalan dengan alasan khusus Admin (FR-07).
/// </summary>
public partial class TransactionHistoryViewModel : ObservableObject, IAsyncInitializable
{
    public const int PageSize = 50;

    private readonly ITransactionRepository _repository;
    private readonly ITransactionService _service;
    private readonly INavigationService _navigation;
    private int _loadVersion;

    public TransactionHistoryViewModel(
        ITransactionRepository repository,
        ITransactionService service,
        ICurrentUserService currentUser,
        INavigationService navigation)
    {
        _repository = repository;
        _service = service;
        _navigation = navigation;
        IsAdmin = currentUser.IsInRole(UserRole.Admin);
        _selectedStatus = StatusOptions[0];
    }

    public bool IsAdmin { get; }

    public IReadOnlyList<StatusFilterOption> StatusOptions { get; } = new StatusFilterOption[]
    {
        new("Semua status", null),
        new("Lunas", StatusTransaksi.Lunas),
        new("Batal", StatusTransaksi.Batal),
    };

    public ObservableCollection<TransactionHistoryRowDto> Rows { get; } = new();

    [ObservableProperty] private DateTime? _dateFrom = DateTime.Today.AddDays(-29);
    [ObservableProperty] private DateTime? _dateTo = DateTime.Today;
    [ObservableProperty] private StatusFilterOption _selectedStatus;
    [ObservableProperty] private int _pageIndex = 1;
    [ObservableProperty] private int _totalItems;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isEmpty;
    [ObservableProperty] private string _errorMessage = "";

    // Panel pembatalan
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCancelOpen))]
    private TransactionHistoryRowDto? _cancelTarget;

    [ObservableProperty] private string _cancelReason = "";
    [ObservableProperty] private string _cancelError = "";

    public bool IsCancelOpen => CancelTarget is not null;

    public Task InitializeAsync() => LoadAsync();

    [RelayCommand]
    private Task ApplyFilterAsync()
    {
        PageIndex = 1;
        return LoadAsync();
    }

    /// <summary>Dipanggil PagingBar dengan nomor halaman (PageIndex sudah diperbarui lewat binding).</summary>
    [RelayCommand]
    private Task GoToPageAsync(int page)
    {
        PageIndex = page;
        return LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (DateFrom is not { } from || DateTo is not { } to)
        {
            ErrorMessage = "Isi tanggal awal dan akhir.";
            return;
        }
        if (from.Date > to.Date)
        {
            ErrorMessage = "Tanggal awal tidak boleh setelah tanggal akhir.";
            return;
        }

        var version = ++_loadVersion;
        IsBusy = true;
        ErrorMessage = "";
        try
        {
            var result = await _repository.GetHistoryAsync(new TransactionHistoryQuery(
                DateOnly.FromDateTime(from), DateOnly.FromDateTime(to), SelectedStatus.Status, PageIndex, PageSize));
            if (version != _loadVersion) return;

            // Halaman terakhir bisa kosong bila data berkurang (mis. filter Lunas setelah pembatalan).
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
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "TransactionHistoryViewModel");
            if (version == _loadVersion)
                ErrorMessage = "Gagal memuat riwayat transaksi. Periksa koneksi database, lalu coba lagi.";
        }
        finally
        {
            if (version == _loadVersion)
                IsBusy = false;
        }
    }

    [RelayCommand]
    private void ViewReceipt(TransactionHistoryRowDto? row)
    {
        if (row is not null)
            _navigation.NavigateTo(new ReceiptViewModel(_repository, row.TransactionId, () => _navigation.NavigateTo(this)));
    }

    [RelayCommand]
    private void BeginCancel(TransactionHistoryRowDto? row)
    {
        if (!IsAdmin || row is not { IsLunas: true })
            return;
        CancelTarget = row;
        CancelReason = "";
        CancelError = "";
    }

    [RelayCommand]
    private void AbortCancel() => CancelTarget = null;

    [RelayCommand]
    private async Task ConfirmCancelAsync()
    {
        if (CancelTarget is not { } target)
            return;

        IsBusy = true;
        CancelError = "";
        try
        {
            var result = await _service.CancelAsync(target.TransactionId, CancelReason);
            if (!result.Success)
            {
                CancelError = result.Message;
                return;
            }

            CancelTarget = null;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "TransactionHistoryViewModel.Cancel");
            CancelError = "Pembatalan gagal dan tidak ada data yang berubah. Periksa koneksi database, lalu coba lagi.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
