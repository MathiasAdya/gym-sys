# GYM-SYS: Sistem Manajemen Keanggotaan Gym

Aplikasi desktop Windows (WPF) untuk resepsionis dan pemilik gym. Spesifikasi lengkap ada di [docs/PRD-GYM-SYS-v2.0.pdf](docs/PRD-GYM-SYS-v2.0.pdf).

| Komponen | Versi yang dipakai tim |
|---|---|
| Bahasa | C# + XAML |
| Runtime | **.NET 8** (`net8.0` / `net8.0-windows`), dikunci lewat `global.json` |
| IDE | Visual Studio 2022 Community + workload **.NET desktop development** |
| Database | SQL Server 2019/2022/2025 (Express, Developer, default instance, atau LocalDB) |
| Akses data | EF Core (CRUD + migrations), ADO.NET `Microsoft.Data.SqlClient` (dashboard/laporan) |
| Test | xUnit |

> PRD menyebut .NET 10 + VS 2026 sebagai target utama dan .NET 8 untuk tim yang memakai VS 2022 (PRD Bagian 3). Tim memakai .NET 8. Kalau nanti semua anggota sudah pindah ke VS 2026, upgrade cukup dengan mengubah `TargetFramework` dan `global.json`.

## Struktur

```
GymSys.sln
├─ src/GymSys.App     WPF (View, ViewModel, Themes, Controls)
├─ src/GymSys.Core    Entitas, interface, logika bisnis (MembershipCalculator, dll.)
├─ src/GymSys.Data    EF Core DbContext, migrations, repository, query ADO.NET
├─ tests/GymSys.Tests xUnit
├─ db/                Skrip SQL pembuatan database (hasil generate migration)
├─ docs/              PRD
└─ scripts/           check-env.ps1
```

## Setup untuk anggota baru (sekitar 15 menit)

### 1. Install (lewati yang sudah ada)
1. **Git for Windows**: https://git-scm.com/download/win
2. **Visual Studio 2022 Community**: https://visualstudio.microsoft.com/vs/community/
   Saat install, centang workload **.NET desktop development** (sudah termasuk .NET 8 SDK).
3. **SQL Server**, pilih salah satu:
   - **SQL Server Express** (https://www.microsoft.com/sql-server/sql-server-downloads), pilih *Basic*; atau
   - **LocalDB**: di Visual Studio Installer > Individual components > *SQL Server Express LocalDB*.
4. (Opsional) **SSMS** atau ekstensi *SQL Server (mssql)* di VS Code untuk melihat isi database.

### 2. Clone dan cek environment
Terima undangan repo dari email/GitHub dulu, lalu di PowerShell:

```powershell
git clone https://github.com/MathiasAdya/gym-sys.git
cd gym-sys
git checkout develop
powershell -ExecutionPolicy Bypass -File scripts\check-env.ps1
dotnet test GymSys.sln
```

Semua `[OK]` dan test hijau berarti siap. Buka `GymSys.sln` di Visual Studio.

### 3. Connection string lokal (NFR-04)
Setiap anggota membuat file sendiri `src/GymSys.App/appsettings.Development.json` (sudah di-`.gitignore`, **jangan di-commit**). Pilih sesuai SQL Server di laptop masing-masing:

```jsonc
{
  "ConnectionStrings": {
    // SQL Server default instance:
    "GymSysDb": "Server=localhost;Database=GymSysDb;Trusted_Connection=True;TrustServerCertificate=True"
    // SQL Server Express:  "Server=localhost\\SQLEXPRESS;Database=GymSysDb;Trusted_Connection=True;TrustServerCertificate=True"
    // LocalDB:             "Server=(localdb)\\MSSQLLocalDB;Database=GymSysDb;Trusted_Connection=True"
  }
}
```

### 4. Buat database `GymSysDb`
Pastikan service SQL Server jalan (`Start-Service MSSQLSERVER` di PowerShell Admin, atau `MSSQL$SQLEXPRESS` untuk Express). Pilih **salah satu** cara:

**A. Lewat EF Core (disarankan):**
```powershell
dotnet tool restore
dotnet ef database update --project src/GymSys.Data --connection "<connection string kamu>"
```

**B. Lewat skrip SQL** (tanpa `dotnet ef`): buka `db/GymSysDb.sql` di SSMS lalu Execute, atau:
```powershell
sqlcmd -S localhost -E -Q "CREATE DATABASE GymSysDb"
sqlcmd -S localhost -E -d GymSysDb -i db\GymSysDb.sql
```

Data awal: akun **`admin` / `Admin#123`** (wajib ganti password saat login pertama) dan paket Bronze/Silver/Gold.
Kalau `develop` membawa migration baru, ulangi langkah A, atau jalankan ulang skripnya (aman diulang).

### Aturan migration
- Hanya Anggota 1 yang menambah migration: `dotnet ef migrations add <Nama> --project src/GymSys.Data`, lalu generate ulang skripnya: `dotnet ef migrations script --idempotent --project src/GymSys.Data -o db/GymSysDb.sql`.
- Butuh kolom/tabel baru? Buat issue, jangan ubah entitas di `GymSys.Core/Entities` sendiri. CI akan gagal kalau entitas berubah tanpa migration.

## Alur kerja Git (PRD Bagian 14)

- `main` untuk rilis, `develop` untuk integrasi (branch default), `feature/<nama-anggota>/<fitur>` untuk kerja harian.
- Selalu buat branch dari `develop` terbaru, lalu PR ke `develop` dengan minimal 1 reviewer. CI (build + test) harus hijau.
- Merge ke `develop` minimal 2x seminggu per orang.

```powershell
git checkout develop
git pull
git checkout -b feature/anggota3/member-search
# ... kerja, commit ...
git push -u origin feature/anggota3/member-search
gh pr create --base develop      # atau buat PR lewat web GitHub
```

### Kepemilikan file (dicek otomatis lewat CODEOWNERS)

| Anggota | Peran | Folder milik |
|---|---|---|
| 1 (@MathiasAdya) | Data & Transaksi | `src/GymSys.Data/*`, `Core/Services/{MembershipCalculator,AuthService}`, `App/{Views,ViewModels}/Transactions`, `App.xaml(.cs)`, migrations |
| 2 (@prinkaachz) | UI Shell & Analitik | `App/Themes`, `App/Controls`, `App/Services`, `App/Views/{Shell,Login,Dashboard,Reports,Settings}` |
| 3 (@faalas) | Master Data & Member | `App/Views/{Members,Plans,Trainers}` + ViewModel-nya, repository Member/Plan/Trainer |

- Hanya Anggota 1 yang membuat migration. Kalau butuh kolom baru, buat issue.
- Interface di `GymSys.Core/Interfaces` hanya berubah lewat PR yang direview dua anggota lain.

### Konvensi kode
- Kelas/properti `PascalCase`, field privat `_camelCase`, `XxxViewModel`, `XxxView`.
- Tidak ada logika bisnis di code-behind. Semuanya lewat binding dan command.
- Semua method yang mengakses database `async` dan berakhiran `Async`.
