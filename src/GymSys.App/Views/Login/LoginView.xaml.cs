using System.Windows;
using System.Windows.Controls;
using GymSys.App.ViewModels;

namespace GymSys.App.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    // PasswordBox.Password tidak bisa di-binding (sengaja, demi keamanan), jadi diteruskan manual.
    // Ini murni urusan tampilan; semua logika login ada di LoginViewModel.
    private void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
            vm.Password = ((PasswordBox)sender).Password;
    }
}
