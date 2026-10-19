namespace GymSys.App.ViewModels;

/// <summary>
/// Implementasikan di ViewModel yang perlu memuat data saat layarnya dibuka.
/// NavigationService memanggilnya otomatis. Implementasi WAJIB menangkap exception sendiri
/// (set ErrorMessage), karena dipanggil tanpa await.
/// </summary>
public interface IAsyncInitializable
{
    Task InitializeAsync();
}
