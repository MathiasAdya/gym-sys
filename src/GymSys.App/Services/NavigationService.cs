using GymSys.App.ViewModels;

namespace GymSys.App.Services;

/// <summary>
/// Navigasi berbasis ViewModel: area konten Shell menampilkan ViewModel aktif,
/// View-nya dipilih lewat DataTemplate di Views/ViewMap.xaml.
/// </summary>
public interface INavigationService
{
    object? Current { get; }
    event Action<object?>? Navigated;
    void NavigateTo(object viewModel);
}

public sealed class NavigationService : INavigationService
{
    public object? Current { get; private set; }

    public event Action<object?>? Navigated;

    public void NavigateTo(object viewModel)
    {
        var previous = Current;
        Current = viewModel;
        Navigated?.Invoke(viewModel);

        // ViewModel lama yang IDisposable dibersihkan (mis. melepas event ke service singleton).
        if (previous is IDisposable disposable)
            disposable.Dispose();

        // ViewModel yang butuh memuat data saat dibuka cukup mengimplementasikan IAsyncInitializable.
        if (viewModel is IAsyncInitializable initializable)
            _ = initializable.InitializeAsync();
    }
}
