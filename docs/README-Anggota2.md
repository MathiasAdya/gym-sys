# GYM-SYS - Bagian Anggota 2 (UI Shell & Analitik)

Isi paket ini: tema Dark/Light, shell + sidebar + navigasi, Login, Dashboard, Laporan, Pengaturan, dan kontrol bersama.
Semua jalan **tanpa database** karena memakai data palsu (fake) dulu.

## Cara menjalankan
1. Buka `GymSys.sln` di Visual Studio, workload ".NET desktop development".
2. Versi .NET: repo ini memakai **net8.0** (lihat global.json). Tidak perlu mengubah apa pun.
3. Restore NuGet lalu tekan F5.
4. Akun demo: `admin / admin123`, `resepsionis / resep123`, `pt / pt123`.

## Yang sudah ada
| Bagian | File |
|---|---|
| Tema Dark/Light: warna, tombol, input, DataGrid, sidebar | `Themes/Dark.xaml`, `Light.xaml`, `Styles.xaml` |
| Tema ComboBox dan ScrollBar | `Themes/Controls.xaml` |
| Ganti tema saat runtime, tersimpan **per pengguna** (`%AppData%\GymSys\theme-{username}.txt`) | `Services/ThemeService.cs` |
| Navigasi berbasis ViewModel | `Services/NavigationService.cs`, `Views/ViewMap.xaml` |
| Shell: sidebar menu sesuai peran, header, tombol tema, logout (dengan dialog konfirmasi) | `Views/ShellView.xaml` |
| Login | `Views/LoginView.xaml` |
| Dashboard: 4 kartu, grafik 12 bulan, tabel "Akan Habis 7 Hari" | `Views/DashboardView.xaml` |
| Laporan: Riwayat Transaksi, Status Member, Pendapatan per Paket (Admin saja), filter periode, **ekspor CSV** | `Views/ReportView.xaml` |
| Pengaturan: tema, ganti password, **manajemen staf** (Admin: tambah, aktif/nonaktif; tidak bisa menonaktifkan akun sendiri) | `Views/SettingsView.xaml` |
| Kontrol bersama | `Controls/StatCard`, `LoadingOverlay`, `SearchBox`, `PagingBar`, `ConfirmDialog` |
| Dialog: konfirmasi bertema, info, error, simpan-file | `Services/DialogService.cs` |
| Data palsu | `Fakes/` |

Semua halaman yang memuat data punya state: loading, error + tombol "Coba lagi", dan data kosong.

## Pemakaian kontrol bersama (untuk Anggota 3)
```xml
<!-- SearchBox: SearchText hanya berubah SETELAH debounce 300 ms dan minimal 2 karakter (FR05) -->
<controls:SearchBox Placeholder="Cari kode, nama, atau No. HP"
                    SearchText="{Binding SearchText, Mode=TwoWay}"
                    Command="{Binding SearchCommand}" Width="320"/>

<!-- PagingBar: Command dipanggil dengan nomor halaman baru (mulai 1) -->
<controls:PagingBar PageIndex="{Binding PageIndex, Mode=TwoWay}" PageSize="50"
                    TotalItems="{Binding TotalItems}" Command="{Binding GoToPageCommand}"/>

<!-- LoadingOverlay -->
<controls:LoadingOverlay Visibility="{Binding IsBusy, Converter={StaticResource BoolToVis}}"/>
```
```csharp
// ConfirmDialog dipanggil lewat IDialogService (inject di ViewModel):
if (_dialogs.Confirm("Nonaktifkan member", "Yakin?", "Nonaktifkan", isDanger: true)) { ... }
```
Style tombol: `PrimaryButton`, `GhostButton` (default), `DangerButton`. Kartu: `Style="{StaticResource Card}"`.

## Catatan teknis
- Grafik dashboard memakai batang native WPF (bukan LiveCharts) supaya tidak ada risiko paket NuGet. Hasilnya sama untuk kebutuhan PRD.
- Ekspor CSV memakai pemisah `;` dan UTF-8 BOM supaya terbuka benar di Excel dengan pengaturan regional Indonesia.
- Fake menyimpan password sebagai teks biasa HANYA karena data palsu. Versi asli (Anggota 1) wajib hash PBKDF2.

## Cara menyambungkan ke teman satu tim
Interface di `GymSys.Core/Interfaces` adalah **usulan kontrak**. Cocokkan dengan Anggota 1 sebelum merge:
`IAuthService`, `ICurrentUserService`, `IDashboardQueries`, `IReportQueries`, `IAccountService`.

- **Anggota 1** (database asli): di `App.xaml.cs` ganti baris `Fake*` (auth/akun) ke implementasi asli. ViewModel tidak perlu diubah.
  Query Dashboard dan Laporan sudah ada di `GymSys.Data/Queries` (ADO.NET, milik Anggota 2) dan aktif otomatis kalau connection string tersedia.
- **Anggota 1 / 3** (menambah layar): (1) buat View + ViewModel, (2) tambahkan satu `DataTemplate` di
  `Views/ViewMap.xaml`, (3) ganti `PlaceholderViewModel` di `ShellViewModel.BuildMenu` dengan ViewModel asli.
  Kalau layar perlu memuat data saat dibuka, implementasikan `IAsyncInitializable`.
- Kalau repo bersama sudah punya solution dan `App.xaml.cs` dari Anggota 1, jangan timpa. Salin hanya folder
  `Themes/`, `Controls/`, `Services/`, `Converters/`, `Views/{Shell,Login,Dashboard,Report,Settings,Placeholder}`, `ViewModels/`, `Fakes/`,
  lalu gabungkan isi `App.xaml` (merged dictionaries + 2 converter) dan baris DI di `App.xaml.cs`.

## Memakai database asli untuk Dashboard dan Laporan
Buat file `src/GymSys.App/appsettings.Development.json` (tidak di-commit):
```json
{ "ConnectionStrings": { "GymSysDb": "Server=localhost;Database=GymSysDb;Trusted_Connection=True;TrustServerCertificate=True" } }
```
Tanpa file ini, aplikasi memakai data palsu. Error database ditampilkan sebagai pesan ramah + tombol "Coba lagi";
detailnya ditulis ke `%AppData%\GymSys\logs\yyyy-MM-dd.log`.
