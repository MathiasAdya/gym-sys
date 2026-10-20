using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GymSys.App.Views;

public partial class MembersView : UserControl
{
    public MembersView()
    {
        InitializeComponent();
    }

    // Shortcut Ctrl+F (PRD Bagian 9). Murni urusan tampilan, jadi boleh di code-behind.
    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            Search.FocusInput();
            e.Handled = true;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => Search.FocusInput();
}
