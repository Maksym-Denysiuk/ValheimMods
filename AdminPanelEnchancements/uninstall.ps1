# AdminPanelEnchancements: uninstall. Run with the game closed.
#   powershell -NoProfile -ExecutionPolicy Bypass -File uninstall.ps1 [-GamePath <dir>] [-BackupRoot <dir>]
#     [-SkipKickBan] [-SkipBackupSettings] [-DryRun]
# Kick-ban: restores the original DLL from the backup made by install (same game version only; else use
# Steam > Valheim > Properties > Installed Files > Verify integrity of game files).
# Backup settings: puts back the values recorded by install, or the game defaults (4 backups, no timer values).
param(
    [string]$GamePath,
    [string]$BackupRoot,
    [switch]$SkipKickBan,
    [switch]$SkipBackupSettings,
    [switch]$DryRun
)
. (Join-Path $PSScriptRoot 'common.ps1')

$game = Find-ValheimDir $GamePath
$dll = Join-Path $game 'valheim_Data\Managed\assembly_valheim.dll'
$root = Get-BackupRoot $BackupRoot
Say "game:    $game"
Say "backups: $root"
Assert-GameClosed

if (-not $SkipKickBan) {
    if (-not (Get-Command node -ErrorAction SilentlyContinue)) { throw 'Node.js is needed to undo the kick-ban patch. Or use Steam: Verify integrity of game files.' }
    $env:VALHEIM_BACKUP_ROOT = $root
    $js = Join-Path $PSScriptRoot 'kickban\kick-ban-patch.js'
    if ($DryRun) { Say "[dry-run] node kick-ban-patch.js undo --dll $dll"; & node $js check --dll $dll }
    else {
        & node $js undo --dll $dll
        if ($LASTEXITCODE -ne 0) { Warn "kick-ban undo refused (exit $LASTEXITCODE): use Steam > Verify integrity of game files" }
    }
}

if (-not $SkipBackupSettings) {
    $last = Get-LastBackupDir $root 'admin-panel'
    $vals = @{}
    if ($last -and (Test-Path (Join-Path $last 'previous-values.txt'))) {
        foreach ($ln in Get-Content (Join-Path $last 'previous-values.txt')) { $k, $v = $ln -split '=', 2; if ($k) { $vals[$k] = $v } }
        Say "previous values from $last"
    } else { Say 'no recorded previous values: using the game defaults' }
    if ($vals['AutoBackups']) { Set-PrefInt 'AutoBackups' ([int]$vals['AutoBackups']) } else { Set-PrefInt 'AutoBackups' 4 }
    foreach ($k in 'AutoBackups_short', 'AutoBackups_long') {
        if ($vals[$k]) { Set-PrefInt $k ([int]$vals[$k]) } else { Remove-Pref $k }
    }
}
Say 'Done. World backups already made stay in worlds_local; the ban list is not touched.'
