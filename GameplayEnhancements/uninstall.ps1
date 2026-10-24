# GameplayEnhancements: uninstall. Run with the game closed. Files are moved to <BackupRoot>\<time>-gameplay-removed, not deleted.
#   powershell -NoProfile -ExecutionPolicy Bypass -File uninstall.ps1 [-GamePath <dir>] [-BackupRoot <dir>]
#     [-RemoveBepInEx] [-KeepConfig] [-KeepKeySettings] [-DryRun]
# World changes made with the commands (planted, placed, resized objects) are world data and stay: restore a world backup for those.
param(
    [string]$GamePath,
    [string]$BackupRoot,
    [switch]$RemoveBepInEx,
    [switch]$KeepConfig,
    [switch]$KeepKeySettings,
    [switch]$DryRun
)
. (Join-Path $PSScriptRoot 'common.ps1')

$game = Find-ValheimDir $GamePath
$bep = Join-Path $game 'BepInEx'
$root = Get-BackupRoot $BackupRoot
Say "game:    $game"
Assert-GameClosed

$bk = New-BackupDir $root 'gameplay-removed'
Say "moved files go to: $bk"
Export-Prefs $bk

Move-ToBackup (Join-Path $bep 'plugins\HomeCheat.dll') (Join-Path $bk 'plugins')
$cmds = Join-Path $bep 'homecheat-commands'
if (Test-Path $cmds) {
    foreach ($f in Get-ChildItem $cmds -Filter 'HomeCheat.Cmd.*.dll') { Move-ToBackup $f.FullName (Join-Path $bk 'homecheat-commands') }
}
if (-not $KeepConfig) { Move-ToBackup (Join-Path $bep 'config\homecheat') (Join-Path $bk 'config') }

if (-not $KeepKeySettings -and (Test-Path $script:PrefsKey)) {
    if ((Get-PrefString 'kbmBinding_Console') -eq '<Keyboard>/backquote') { Set-PrefString 'kbmBinding_Console' '<Keyboard>/F5' }
    $binds = Get-PrefString 'ConsoleBindings'
    if ($binds -and (@($binds -split "`n") -contains 'F9 save')) {
        $rest = @($binds -split "`n" | Where-Object { $_ -ne '' -and $_ -ne 'F9 save' })
        if ($rest.Count) { Set-PrefString 'ConsoleBindings' ("`n" + ($rest -join "`n")) } else { Set-PrefString 'ConsoleBindings' '' }
    }
}

if ($RemoveBepInEx) {
    Move-ToBackup $bep $bk
    foreach ($f in 'winhttp.dll', 'doorstop_config.ini', '.doorstop_version', 'changelog.txt') { Move-ToBackup (Join-Path $game $f) (Join-Path $bk 'game-root') }
}
Say 'Done. Vanilla F2 (stats window) and F9 (gamepad layout) come back with the keybinds DLL gone.'
