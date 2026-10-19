# Membuat paket rilis GYM-SYS (NFR-09). Jalankan dari root repo:
#   powershell -ExecutionPolicy Bypass -File scripts\publish.ps1
# Hasil: dist\GymSys\ (aplikasi self-contained + skrip SQL + panduan) dan dist\GymSys-win-x64.zip
$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$dist = Join-Path $root 'dist'
$out = Join-Path $dist 'GymSys'

if (Test-Path $dist) { Remove-Item $dist -Recurse -Force }

# Self-contained: komputer gym tidak perlu memasang .NET runtime.
dotnet publish (Join-Path $root 'src\GymSys.App\GymSys.App.csproj') -c Release -r win-x64 --self-contained true -o (Join-Path $out 'app')
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish gagal' }

Copy-Item (Join-Path $root 'deploy\appsettings.json') (Join-Path $out 'app\appsettings.json')
Copy-Item (Join-Path $root 'db\GymSysDb.sql') $out
Copy-Item (Join-Path $root 'docs\INSTALL.md') $out

Compress-Archive -Path $out -DestinationPath (Join-Path $dist 'GymSys-win-x64.zip')
Write-Host "Paket rilis siap: $dist" -ForegroundColor Green
