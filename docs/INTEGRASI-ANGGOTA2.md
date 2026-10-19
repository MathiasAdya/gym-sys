# Integrasi Anggota 2 (UI Shell & Analitik) ke repo gym-sys

Sudah disesuaikan dengan repo Anggota 1: .NET 8 (`net8.0` / `net8.0-windows`), folder `src/`, `global.json` 8.0.100.

## Yang ditambahkan (semua milik Anggota 2)
- `src/GymSys.App/{Themes,Controls,Services,Converters,Fakes,ViewModels}`
- `src/GymSys.App/Views/{Shell,Login,Dashboard,Reports,Settings}` + `Views/ViewMap.xaml`, `Views/PlaceholderView`
- `src/GymSys.Core/{Entities/UserRole.cs, Interfaces/*}`  <- USULAN KONTRAK, perlu review bersama (lihat CODEOWNERS)

## Yang MENYENTUH file milik Anggota 1 (harus dibahas di PR)
1. `src/GymSys.App/App.xaml`: StartupUri dihapus; ditambah merged dictionary tema + 2 converter.
2. `src/GymSys.App/App.xaml.cs`: DI container + `OnStartup` (tampilkan MainWindow dengan MainWindowViewModel).
3. `src/GymSys.App/GymSys.App.csproj`: tambah paket CommunityToolkit.Mvvm 8.4.0 dan Microsoft.Extensions.DependencyInjection 9.0.0.
4. `src/GymSys.App/MainWindow.xaml(.cs)`: diganti jadi ContentControl.

## Untuk Anggota 1
- Interface di `Core/Interfaces`: `IAuthService`, `ICurrentUserService`, `IDashboardQueries`, `IReportQueries`, `IAccountService`.
  Kalau kamu sudah punya versi sendiri, sesuaikan salah satu sisi. ViewModel Anggota 2 hanya bergantung pada interface.
- `Core/Entities/UserRole.cs` (Admin, Resepsionis, Instruktur). Kalau entity User kalian punya enum peran sendiri,
  satukan jadi satu (hapus salah satu) supaya tidak ada dua `UserRole`.
- Tukar baris `Fake*` di `App.xaml.cs` dengan implementasi asli dari `GymSys.Data`.
  Password asli wajib PBKDF2; `Fakes/` menyimpan password sebagai teks biasa hanya untuk demo.
- Saat database siap, `Fakes/` boleh dihapus.

## Untuk Anggota 3
Lihat `docs/README-Anggota2.md` bagian "Pemakaian kontrol bersama" dan "menambah layar".
