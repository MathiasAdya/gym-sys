# Integrasi Anggota 2 (UI Shell & Analitik) ke repo gym-sys

Sudah disesuaikan dengan repo Anggota 1: .NET 8 (`net8.0` / `net8.0-windows`), folder `src/`, `global.json` 8.0.100.

## Yang ditambahkan (semua milik Anggota 2)
- `src/GymSys.App/{Themes,Controls,Services,Converters,Fakes,ViewModels}`
- `src/GymSys.App/Views/{Shell,Login,Dashboard,Reports,Settings}` + `Views/ViewMap.xaml`, `Views/PlaceholderView`
- `src/GymSys.Core/{Entities/UserRole.cs, Interfaces/*}`  <- USULAN KONTRAK, perlu review bersama (lihat CODEOWNERS)
- `src/GymSys.Core/Services/CsvExporter.cs` (ekspor CSV, dites di `tests/GymSys.Tests/CsvExporterTests.cs`)
- `src/GymSys.Data/Queries/{SqlConnectionFactory,DashboardQueries,ReportQueries}.cs`: query ADO.NET asli untuk Dashboard dan Laporan
  (sesuai PRD Bagian 11 dan 12.2, pembuatnya Anggota 2). Memakai tabel dan view `vw_MemberStatus` dari skema PRD Bagian 10.
- `src/GymSys.App/Services/{AppLog,ConnectionStringProvider}.cs`: log error harian di `%AppData%\GymSys\logs` (NFR-06) dan pembaca connection string (NFR-04).

## Yang MENYENTUH file milik Anggota 1 (harus dibahas di PR)
1. `src/GymSys.App/App.xaml`: StartupUri dihapus; ditambah merged dictionary tema + 2 converter.
2. `src/GymSys.App/App.xaml.cs`: DI container + `OnStartup` (tampilkan MainWindow dengan MainWindowViewModel).
3. `src/GymSys.App/GymSys.App.csproj`: tambah paket CommunityToolkit.Mvvm 8.4.0 dan Microsoft.Extensions.DependencyInjection 9.0.0.
4. `src/GymSys.App/MainWindow.xaml(.cs)`: diganti jadi ContentControl.
5. `src/GymSys.Data/GymSys.Data.csproj`: tambah paket Microsoft.Data.SqlClient 5.2.2 (untuk query ADO.NET).

## Untuk Anggota 1
- Interface di `Core/Interfaces`: `IAuthService`, `ICurrentUserService`, `IDashboardQueries`, `IReportQueries`, `IAccountService`.
  Kalau kamu sudah punya versi sendiri, sesuaikan salah satu sisi. ViewModel Anggota 2 hanya bergantung pada interface.
- `Core/Entities/UserRole.cs` (Admin, Resepsionis, Instruktur). Kalau entity User kalian punya enum peran sendiri,
  satukan jadi satu (hapus salah satu) supaya tidak ada dua `UserRole`.
- **Query Dashboard dan Laporan sudah ditulis (ADO.NET) oleh Anggota 2**, bukan tugasmu. Kamu cukup menyediakan skema sesuai PRD Bagian 10
  (tabel `Members`, `MembershipPlans`, `Transactions`, dan view `vw_MemberStatus`). Kalau nama tabel/kolom berbeda, beri tahu Anggota 2.
  Aplikasi otomatis memakai database asli bila ada `ConnectionStrings:GymSysDb` di `appsettings.Development.json`
  (lihat README bagian 3); kalau tidak ada, memakai data palsu.
- Yang masih Fake dan perlu diganti implementasi asli milikmu: `IAuthService`, `IAccountService`, `ICurrentUserService` di `App.xaml.cs`
  (termasuk lockout 5x salah password selama 5 menit, FR-01).
  Password asli wajib PBKDF2; `Fakes/` menyimpan password sebagai teks biasa hanya untuk demo.
- Saat database siap, `Fakes/` boleh dihapus.

## Untuk Anggota 3
Lihat `docs/README-Anggota2.md` bagian "Pemakaian kontrol bersama" dan "menambah layar".
