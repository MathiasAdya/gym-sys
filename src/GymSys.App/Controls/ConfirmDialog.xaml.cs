using System.Windows;

namespace GymSys.App.Controls;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog(string title, string message, string confirmText, bool isDanger)
    {
        InitializeComponent();
        Title = title;
        TitleText.Text = title;
        MessageText.Text = message;
        ConfirmButton.Content = confirmText;

        if (isDanger)
            ConfirmButton.Style = (Style)FindResource("DangerButton");
    }

    private void OnConfirm(object sender, RoutedEventArgs e) => DialogResult = true;
}
