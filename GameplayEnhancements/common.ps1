# Shared helpers for the Valheim package scripts (dot-sourced by install.ps1, check.ps1, uninstall.ps1).
# Copied into every package by scripts/valheim/packages/build.js, so each package stays standalone.
# PowerShell 5.1, ASCII only.

$ErrorActionPreference = 'Stop'
$script:PrefsKey = 'HKCU:\Software\IronGate\Valheim'
$script:DataDir = Join-Path $env:USERPROFILE 'AppData\LocalLow\IronGate\Valheim'

function Say([string]$msg) { Write-Host $msg }
function Warn([string]$msg) { Write-Host "WARNING: $msg" -ForegroundColor Yellow }

# Runs $action unless -DryRun was given; always prints what it does.
function Step([string]$desc, [scriptblock]$action) {
    if ($script:DryRun) { Say "[dry-run] $desc"; return }
    Say "- $desc"
    & $action
}

# Valheim folder: -GamePath, else every Steam library from libraryfolders.vdf, else the usual places.
function Find-ValheimDir([string]$GamePath) {
    $cands = @()
    if ($GamePath) { $cands += $GamePath }
    else {
        $steam = $null
        try { $steam = (Get-ItemProperty 'HKCU:\Software\Valve\Steam' -Name SteamPath).SteamPath } catch {}
        if ($steam) {
            $steam = $steam -replace '/', '\'
            $cands += Join-Path $steam 'steamapps\common\Valheim'
            $vdf = Join-Path $steam 'steamapps\libraryfolders.vdf'
            if (Test-Path $vdf) {
                foreach ($m in [regex]::Matches((Get-Content $vdf -Raw), '"path"\s+"([^"]+)"')) {
                    $cands += Join-Path ($m.Groups[1].Value -replace '\\\\', '\') 'steamapps\common\Valheim'
                }
            }
        }
        $cands += 'D:\Steam\steamapps\common\Valheim', 'C:\Program Files (x86)\Steam\steamapps\common\Valheim'
    }
    foreach ($c in $cands) { if (Test-Path (Join-Path $c 'valheim.exe')) { return (Resolve-Path $c).Path } }
    throw "Valheim not found (looked in: $($cands -join '; ')). Pass -GamePath <folder with valheim.exe>."
}

# Unity writes its settings and files when the game quits, so every write needs the game closed.
# VALHEIM_ALLOW_RUNNING=1 skips this, only for rehearsing on a copy of the game folder.
function Assert-GameClosed {
    if ($env:VALHEIM_ALLOW_RUNNING) { Warn 'VALHEIM_ALLOW_RUNNING is set: not checking for a running game'; return }
    if (Get-Process -Name valheim -ErrorAction SilentlyContinue) { throw 'valheim.exe is running. Close the game first.' }
}

function Get-BackupRoot([string]$BackupRoot) {
    if ($BackupRoot) { return $BackupRoot }
    if (Test-Path 'D:\ValheimBackups') { return 'D:\ValheimBackups' }
    return (Join-Path $env:USERPROFILE 'ValheimBackups')
}

function New-BackupDir([string]$root, [string]$name) {
    $d = Join-Path $root ((Get-Date -Format 'yyyy-MM-dd-HHmmss') + '-' + $name)
    if (-not $script:DryRun) { New-Item -ItemType Directory -Force -Path $d | Out-Null }
    return $d
}

# Newest backup folder of this package, or $null
function Get-LastBackupDir([string]$root, [string]$name) {
    if (-not (Test-Path $root)) { return $null }
    $d = Get-ChildItem $root -Directory | Where-Object { $_.Name -like "*-$name" } | Sort-Object Name -Descending | Select-Object -First 1
    if ($d) { return $d.FullName } else { return $null }
}

function Export-Prefs([string]$dir) {
    if (-not (Test-Path $script:PrefsKey)) { Warn 'no Valheim settings in the registry yet (start the game once)'; return }
    Step "export registry HKCU\Software\IronGate\Valheim to $dir\valheim-registry-before.reg" {
        & reg.exe export 'HKCU\Software\IronGate\Valheim' (Join-Path $dir 'valheim-registry-before.reg') /y | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'reg export failed' }
    }
}

# Unity PlayerPrefs stores key K as value K_h<djb2-xor hash of K>.
function Get-PrefName([string]$key) {
    [int64]$h = 5381
    foreach ($ch in $key.ToCharArray()) { $h = (($h * 33) -band 0xFFFFFFFFL) -bxor [int64][int][char]$ch }
    return "${key}_h$h"
}

function Get-PrefInt([string]$key) {
    try { return [int](Get-ItemProperty $script:PrefsKey -Name (Get-PrefName $key) -ErrorAction Stop).(Get-PrefName $key) } catch { return $null }
}

function Set-PrefInt([string]$key, [int]$value) {
    Step "registry $(Get-PrefName $key) = $value (REG_DWORD)" {
        if (-not (Test-Path $script:PrefsKey)) { New-Item -Path $script:PrefsKey -Force | Out-Null }
        New-ItemProperty -Path $script:PrefsKey -Name (Get-PrefName $key) -PropertyType DWord -Value $value -Force | Out-Null
    }
}

# Strings are REG_BINARY: UTF-8 bytes plus a trailing 0.
function Get-PrefString([string]$key) {
    try { $b = [byte[]](Get-ItemProperty $script:PrefsKey -Name (Get-PrefName $key) -ErrorAction Stop).(Get-PrefName $key) } catch { return $null }
    if ($b.Length -gt 0 -and $b[$b.Length - 1] -eq 0) { $b = $b[0..($b.Length - 2)] }
    if ($b.Length -eq 0) { return '' }
    return [Text.Encoding]::UTF8.GetString([byte[]]$b)
}

function Set-PrefString([string]$key, [string]$value) {
    $shown = $value -replace "`n", '\n'
    Step "registry $(Get-PrefName $key) = '$shown' (REG_BINARY)" {
        if (-not (Test-Path $script:PrefsKey)) { New-Item -Path $script:PrefsKey -Force | Out-Null }
        $bytes = [Text.Encoding]::UTF8.GetBytes($value) + [byte]0
        New-ItemProperty -Path $script:PrefsKey -Name (Get-PrefName $key) -PropertyType Binary -Value ([byte[]]$bytes) -Force | Out-Null
    }
}

function Remove-Pref([string]$key) {
    $n = Get-PrefName $key
    if ($null -eq (Get-ItemProperty $script:PrefsKey -Name $n -ErrorAction SilentlyContinue)) { return }
    Step "registry remove $n" { Remove-ItemProperty -Path $script:PrefsKey -Name $n }
}

# Moves a file or folder into the backup folder (nothing is deleted).
function Move-ToBackup([string]$path, [string]$dir) {
    if (-not (Test-Path $path)) { return }
    Step "move $path -> $dir" {
        New-Item -ItemType Directory -Force -Path $dir | Out-Null
        Move-Item -Path $path -Destination $dir -Force
    }
}

function Copy-ToBackup([string]$path, [string]$dir) {
    if (-not (Test-Path $path)) { return }
    Step "copy $path -> $dir" {
        New-Item -ItemType Directory -Force -Path $dir | Out-Null
        Copy-Item -Path $path -Destination $dir -Recurse -Force
    }
}
