# Panduan Instalasi GYM-SYS

Panduan ini untuk memasang GYM-SYS di komputer gym. Pengembang cukup membaca README.

## Isi paket

Paket berupa folder `GymSys` dari `GymSys-win-x64.zip`:

| File | Fungsi |
|---|---|
| `app\GymSys.App.exe` | Aplikasi (self-contained, tidak perlu memasang .NET) |
| `app\appsettings.json` | Pengaturan koneksi database |
| `GymSysDb.sql` | Skrip pembuatan/pembaruan database (aman dijalankan ulang) |
| `INSTALL.md` | Panduan ini |

## Kebutuhan

- Windows 10/11 64-bit.
- SQL Server 2019 atau lebih baru. **SQL Server Express** (gratis) sudah cukup.
- Hak administrator untuk memasang SQL Server.

## Langkah instalasi

### 1. Pasang SQL Server Express
1. Unduh dari https://www.microsoft.com/sql-server/sql-server-downloads, bagian *Express*.
2. Pilih instalasi **Basic**. Nama instance default-nya `SQLEXPRESS`.
3. Pasang juga **sqlcmd** bila belum ada: `winget install Microsoft.Sqlcmd`. Alternatifnya, pakai SSMS.

### 2. Buat database
Buka PowerShell di folder `GymSys`, lalu jalankan:

```powershell
sqlcmd -S localhost\SQLEXPRESS -E -C -Q "CREATE DATABASE GymSysDb"
sqlcmd -S localhost\SQLEXPRESS -E -C -I -b -d GymSysDb -i GymSysDb.sql
```

- `-I` wajib. Tanpa flag ini skrip gagal dengan Msg 1934.
- Untuk SQL Server *default instance*, ganti `localhost\SQLEXPRESS` dengan `localhost`.
- Kalau memakai SSMS: buat database `GymSysDb`, buka `GymSysDb.sql`, lalu Execute.

### 3. Atur koneksi
Buka `app\appsettings.json` dengan Notepad. Sesuaikan bagian `Server=` dengan SQL Server di komputer ini:

```json
"GymSysDb": "Server=localhost\\SQLEXPRESS;Database=GymSysDb;Trusted_Connection=True;TrustServerCertificate=True"
```

- Tanda `\` di JSON ditulis ganda (`\\`).
- Kalau database ada di komputer lain atau memakai login SQL, ganti `Trusted_Connection=True` dengan `User Id=...;Password=...`.
- Kalau memakai login SQL, batasi akses folder `app` supaya password di file ini tidak bisa dibaca sembarang orang.

### 4. Jalankan dan login pertama
1. Salin folder `GymSys` ke lokasi tetap, misalnya `C:\GymSys`.
2. Jalankan `app\GymSys.App.exe`. Agar mudah dibuka, buat shortcut ke Desktop.
3. Login dengan **`admin` / `Admin#123`**. Aplikasi langsung meminta Anda mengganti password.
4. Buat akun resepsionis dan instruktur di menu **Pengaturan**.

## Memperbarui ke versi baru
1. **Backup database** dulu, lalu tutup aplikasi di semua komputer:
   ```powershell
   sqlcmd -S localhost\SQLEXPRESS -E -C -Q "BACKUP DATABASE GymSysDb TO DISK='C:\Backup\GymSysDb.bak'"
   ```
   Folder `C:\Backup` harus sudah ada dan bisa ditulis oleh service SQL Server.
2. Jalankan `GymSysDb.sql` dari paket baru (perintah sama dengan langkah 2, tanpa `CREATE DATABASE`). Skrip hanya menerapkan perubahan yang belum ada.
3. Ganti isi folder `app` dengan versi baru, **kecuali** `appsettings.json` lama Anda.

## Backup rutin
Jalankan perintah `BACKUP DATABASE` di atas secara berkala, misalnya setiap hari lewat Task Scheduler. Simpan salinannya di luar komputer gym.

## Masalah umum

| Gejala | Solusi |
|---|---|
| "Connection string database belum diatur" | `app\appsettings.json` hilang atau `GymSysDb` kosong. Ulangi langkah 3. |
| "Tidak dapat terhubung ke database" | Service SQL Server belum jalan (`services.msc` → *SQL Server (SQLEXPRESS)* → Start), atau nama server di `appsettings.json` salah. |
| Skrip SQL gagal dengan Msg 1934 | Tambahkan `-I` pada perintah sqlcmd. |
| Lupa password admin | Aplikasi belum punya fitur reset password. Hubungi tim pengembang. Karena itu, buat minimal dua akun Admin. |

Log error tersimpan di `%AppData%\GymSys\logs`. Lampirkan file log saat melaporkan masalah.
