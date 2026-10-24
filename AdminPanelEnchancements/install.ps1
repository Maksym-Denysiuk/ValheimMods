# AdminPanelEnchancements: install
#   1. kick also bans: console `kick` and Escape > players list > Kick put the player on the ban list
#      (two IL call tokens in assembly_valheim.dll, ZNet.Kick -> ZNet.Ban; needs Node.js)
#   2. world backups: 5 backups, 3-minute timers (registry, no game file changed)
# Run with the game closed:
#   powershell -NoProfile -ExecutionPolicy Bypass -File install.ps1 [-GamePath <dir>] [-BackupRoot <dir>]
#     [-SkipKickBan] [-SkipBackupSettings] [-BackupCount 5] [-BackupShort 180] [-BackupLong 180] [-DryRun]
param(
    [string]$GamePath,
    [string]$BackupRoot,
    [switch]$SkipKickBan,
    [switch]$SkipBackupSettings,
    [int]$BackupCount = 5,
    [int]$BackupShort = 180,
    [int]$BackupLong = 180,
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
    $node = Get-Command node -ErrorAction SilentlyContinue
    if (-not $node) { throw 'Node.js is needed for the kick-ban patch (https://nodejs.org, LTS). Or run with -SkipKickBan.' }
}

$bk = New-BackupDir $root 'admin-panel'
Say "safety copies: $bk"
Export-Prefs $bk

if (-not $SkipBackupSettings) {
    # a new rotation deletes old backups at once: copy the existing ones first
    $worlds = Join-Path $script:DataDir 'worlds_local'
    if (Test-Path $worlds) {
        foreach ($d in Get-ChildItem $worlds -Directory | Where-Object { $_.Name -like '*_backup_*' }) {
            Copy-ToBackup $d.FullName (Join-Path $bk 'worlds_local')
        }
    }
    $prev = "AutoBackups=$(Get-PrefInt 'AutoBackups')`r`nAutoBackups_short=$(Get-PrefInt 'AutoBackups_short')`r`nAutoBackups_long=$(Get-PrefInt 'AutoBackups_long')`r`n"
    Step "record previous backup settings in $bk\previous-values.txt" { Set-Content -Path (Join-Path $bk 'previous-values.txt') -Value $prev -Encoding ASCII }
    Set-PrefInt 'AutoBackups' $BackupCount
    Set-PrefInt 'AutoBackups_short' $BackupShort
    Set-PrefInt 'AutoBackups_long' $BackupLong
    if (-not $DryRun) {
        $got = @((Get-PrefInt 'AutoBackups'), (Get-PrefInt 'AutoBackups_short'), (Get-PrefInt 'AutoBackups_long'))
        if ($got[0] -ne $BackupCount -or $got[1] -ne $BackupShort -or $got[2] -ne $BackupLong) { throw "registry read-back differs: $($got -join ', ')" }
        Say "backup settings: count $($got[0]), first after $($got[1]) s, then every $($got[2]) s (read back)"
    }
}

if (-not $SkipKickBan) {
    foreach ($f in 'bannedlist.txt', 'adminlist.txt', 'permittedlist.txt') { Copy-ToBackup (Join-Path $script:DataDir $f) $bk }
    $env:VALHEIM_BACKUP_ROOT = $root
    $js = Join-Path $PSScriptRoot 'kickban\kick-ban-patch.js'
    if ($DryRun) {
        Say "[dry-run] node kick-ban-patch.js apply --dll $dll   (current state below)"
        & node $js check --dll $dll
    } else {
        & node $js apply --dll $dll
        if ($LASTEXITCODE -ne 0) { throw "kick-ban apply failed (exit $LASTEXITCODE). Nothing else to undo for it; see docs\kick-bans-lessons-learned.md" }
    }
}

Say ''
Say 'Done. Notes:'
Say '- Backups are made only at a save, and never in the first 20 minutes of a session (hard-coded in the game).'
Say '- A Steam update or "Verify integrity" restores the original DLL: run check.ps1 after every Valheim update.'
Say '- Unban: F5 console  unban <id>  (the id is in bannedlist.txt and in the "Banning user" log line).'
