using System.IO;
using System.Windows;
using GymSys.App.Controls;
using Microsoft.Win32;

namespace GymSys.App.Services;

/// <summary>Dialog konfirmasi/info/error/simpan-file. ViewModel memakai ini, bukan MessageBox/dialog langsung.</summary>
public interface IDialogService
{
    /// <summary>Dialog konfirmasi bertema. isDanger=true untuk aksi berbahaya (tombol merah).</summary>
    bool Confirm(string title, string message, string confirmText = "Ya", bool isDanger = false);

    void Info(string title, string message);
    void Error(string title, string message);

    /// <summary>Dialog "Simpan sebagai". Mengembalikan null bila dibatalkan.</summary>
    string? PickSaveFile(string suggestedFileName, string filter);
}

public sealed class DialogService : IDialogService
{
    public bool Confirm(string title, string message, string confirmText = "Ya", bool isDanger = false)
    {
        var dialog = new ConfirmDialog(title, message, confirmText, isDanger)
        {
            Owner = Application.Current.MainWindow
        };
        return dialog.ShowDialog() == true;
    }

    public void Info(string title, string message)
        => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public void Error(string title, string message)
        => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    public string? PickSaveFile(string suggestedFileName, string filter)
    {
        var dialog = new SaveFileDialog
        {
            FileName = suggestedFileName,
            Filter = filter,
            AddExtension = true,
            DefaultExt = Path.GetExtension(suggestedFileName),
            OverwritePrompt = true
        };

        return dialog.ShowDialog(Application.Current.MainWindow) == true ? dialog.FileName : null;
    }
}
