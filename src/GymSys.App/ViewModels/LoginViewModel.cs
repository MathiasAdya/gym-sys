using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymSys.Core.Interfaces;

namespace GymSys.App.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _auth;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _username = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _password = "";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private bool _isBusy;

    public event Action? LoginSucceeded;

    public LoginViewModel(IAuthService auth, ICurrentUserService currentUser)
    {
        _auth = auth;
        _currentUser = currentUser;
    }

    private bool CanLogin()
        => !IsBusy && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrEmpty(Password);

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        ErrorMessage = "";
        IsBusy = true;
        try
        {
            var result = await _auth.LoginAsync(Username, Password);
            if (!result.Success || result.User is null)
            {
                ErrorMessage = result.Message ?? "Login gagal.";
                return;
            }

            _currentUser.SignIn(result.User);
            LoginSucceeded?.Invoke();
        }
        catch (Exception)
        {
            // TODO: tulis detail exception ke file log (NFR-06); pengguna cukup lihat pesan ramah.
            ErrorMessage = "Tidak dapat terhubung ke database. Periksa koneksi lalu coba lagi.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
