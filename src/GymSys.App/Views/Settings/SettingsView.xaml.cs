using System.Windows;
using System.Windows.Controls;
using GymSys.App.ViewModels;

namespace GymSys.App.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is SettingsViewModel oldVm)
        {
            oldVm.PasswordChangeCompleted -= ClearPasswordBoxes;
            oldVm.StaffAdded -= ClearStaffPassword;
        }

        if (e.NewValue is SettingsViewModel newVm)
        {
            newVm.PasswordChangeCompleted += ClearPasswordBoxes;
            newVm.StaffAdded += ClearStaffPassword;
        }
    }

    // PasswordBox.Password tidak bisa di-binding (sengaja, demi keamanan), jadi diteruskan manual.
    // Ini murni urusan tampilan; semua validasi dan logika ada di SettingsViewModel.
    private void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm)
            return;

        var box = (PasswordBox)sender;
        switch (box.Tag as string)
        {
            case "Old": vm.OldPassword = box.Password; break;
            case "New": vm.NewPassword = box.Password; break;
            case "Confirm": vm.ConfirmPassword = box.Password; break;
            case "Staff": vm.NewStaffPassword = box.Password; break;
        }
    }

    private void ClearPasswordBoxes()
    {
        OldBox.Clear();
        NewBox.Clear();
        ConfirmBox.Clear();
    }

    private void ClearStaffPassword() => StaffPasswordBox.Clear();
}
