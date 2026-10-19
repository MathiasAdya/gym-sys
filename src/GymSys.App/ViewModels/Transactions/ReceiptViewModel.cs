using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymSys.App.Services;
using GymSys.Core.Entities;
using GymSys.Core.Interfaces;

namespace GymSys.App.ViewModels;

/// <summary>
/// Layar Kuitansi (PRD Bagian 9). Dibuka dari Transaksi Baru (setelah simpan) atau Riwayat Transaksi;
/// "Kembali" memanggil goBack supaya kembali ke layar asal. Cetak ada di code-behind view (PrintDialog).
/// </summary>
public partial class ReceiptViewModel : ObservableObject, IAsyncInitializable
{
    private readonly ITransactionRepository _repository;
    private readonly int _transactionId;
    private readonly Action _goBack;

    public ReceiptViewModel(ITransactionRepository repository, int transactionId, Action goBack)
    {
        _repository = repository;
        _transactionId = transactionId;
        _goBack = goBack;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasReceipt), nameof(IsBatal))]
    private ReceiptDto? _receipt;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _errorMessage = "";

    public bool HasReceipt => Receipt is not null;
    public bool IsBatal => Receipt?.Status == StatusTransaksi.Batal;

    public async Task InitializeAsync()
    {
        IsBusy = true;
        ErrorMessage = "";
        try
        {
            Receipt = await _repository.GetReceiptAsync(_transactionId);
            if (Receipt is null)
                ErrorMessage = "Transaksi tidak ditemukan.";
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "ReceiptViewModel");
            ErrorMessage = "Gagal memuat kuitansi. Periksa koneksi database, lalu coba lagi.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task RetryAsync() => InitializeAsync();

    [RelayCommand]
    private void Back() => _goBack();
}
