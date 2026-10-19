using System.Windows;
using System.Windows.Controls;
using GymSys.App.Services;
using GymSys.Core.Interfaces;

namespace GymSys.App.Views;

public partial class ReceiptView : UserControl
{
    public ReceiptView()
    {
        InitializeComponent();
    }

    /// <summary>Cetak lewat PrintDialog Windows (PRD Bagian 9). Murni urusan tampilan, jadi di code-behind.</summary>
    private void OnPrint(object sender, RoutedEventArgs e)
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
            return;

        try
        {
            dialog.PrintVisual(ReceiptPaper, $"Kuitansi {(ReceiptPaper.DataContext as ReceiptDto)?.NoKuitansi}");
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "ReceiptView.Print");
            MessageBox.Show(Window.GetWindow(this), "Gagal mencetak kuitansi. Periksa printer, lalu coba lagi.", "Cetak gagal",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
