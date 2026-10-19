# Cek kesiapan environment GYM-SYS. Jalankan dari root repo:
#   powershell -ExecutionPolicy Bypass -File scripts\check-env.ps1
$ok = $true
function Check($name, $pass, $hint) {
    if ($pass) { Write-Host "[OK]   $name" -ForegroundColor Green }
    else { Write-Host "[FAIL] $name -> $hint" -ForegroundColor Red; $script:ok = $false }
}

Check "Git" ([bool](Get-Command git -EA SilentlyContinue)) "Install dari https://git-scm.com/download/win"

$sdks = if (Get-Command dotnet -EA SilentlyContinue) { dotnet --list-sdks } else { @() }
Check ".NET 8 SDK" ([bool]($sdks | Select-String '^8\.0\.')) "winget install Microsoft.DotNet.SDK.8"

$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$vs = if (Test-Path $vswhere) { & $vswhere -latest -requires Microsoft.VisualStudio.Workload.ManagedDesktop -property displayName } else { $null }
Check "Visual Studio + workload '.NET desktop development'" ([bool]$vs) "Buka Visual Studio Installer > Modify > centang '.NET desktop development'"

$svc = Get-Service MSSQLSERVER, 'MSSQL$SQLEXPRESS' -EA SilentlyContinue
$localdb = [bool](Get-Command sqllocaldb -EA SilentlyContinue)
Check "SQL Server (default instance / SQLEXPRESS / LocalDB)" ($svc -or $localdb) "Install SQL Server Express atau LocalDB (lihat README)"
foreach ($s in $svc) {
    if ($s.Status -ne 'Running') { Write-Host "       Service $($s.Name) belum jalan: Start-Service '$($s.Name)' (PowerShell Admin)" -ForegroundColor Yellow }
}

$appsettings = Join-Path $PSScriptRoot '..\src\GymSys.App\appsettings.Development.json'
if (-not (Test-Path $appsettings)) { Write-Host "[INFO] appsettings.Development.json belum ada (dibutuhkan setelah modul data siap, lihat README)" -ForegroundColor Yellow }

if ($ok) { Write-Host "`nEnvironment siap. Coba: dotnet test GymSys.sln" -ForegroundColor Green } else { exit 1 }
