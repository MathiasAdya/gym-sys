using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GymSys.App.ViewModels;

namespace GymSys.App.Views;

public partial class MembersView : UserControl
{
    public MembersView()
    {
        InitializeComponent();
    }

    // Shortcut Ctrl+F dan Ctrl+N (PRD Bagian 9). Hanya meneruskan ke kontrol/command, tanpa logika bisnis.
    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control)
            return;

        if (e.Key == Key.F)
        {
            Search.FocusInput();
            e.Handled = true;
        }
        else if (e.Key == Key.N && DataContext is MembersViewModel vm && vm.AddCommand.CanExecute(null))
        {
            vm.AddCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => Search.FocusInput();
}
