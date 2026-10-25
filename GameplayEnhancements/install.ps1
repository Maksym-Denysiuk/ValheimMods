# GameplayEnhancements: install the HomeCheat plugin, its command DLLs and the key settings.
# Run with the game closed:
#   powershell -NoProfile -ExecutionPolicy Bypass -File install.ps1 [-GamePath <dir>] [-BackupRoot <dir>]
#     [-SkipBepInEx] [-ForceBepInEx] [-NoKeySettings] [-DryRun]
# What it writes (everything it replaces is moved to <BackupRoot>\<time>-gameplay first):
#   BepInEx 5 loader (only when the game has none, or with -ForceBepInEx)
#   BepInEx\plugins\HomeCheat.dll, BepInEx\homecheat-commands\HomeCheat.Cmd.*.dll (one per command)
#   BepInEx\config\homecheat\ with inbox\, outbox\ and the spell files (only those not there yet)
#   registry: console key -> backquote, console bind "F9 save" (skip with -NoKeySettings)
param(
    [string]$GamePath,
    [string]$BackupRoot,
    [switch]$SkipBepInEx,
    [switch]$ForceBepInEx,
    [switch]$NoKeySettings,
    [switch]$DryRun
)
. (Join-Path $PSScriptRoot 'common.ps1')

$pay = Join-Path $PSScriptRoot 'payload'
$game = Find-ValheimDir $GamePath
$bep = Join-Path $game 'BepInEx'
$root = Get-BackupRoot $BackupRoot
Say "game:    $game"
Say "backups: $root"
Assert-GameClosed

$bk = New-BackupDir $root 'gameplay'
Say "safety copies: $bk"
Export-Prefs $bk

# 1. BepInEx loader
$hasBep = Test-Path (Join-Path $bep 'core\BepInEx.dll')
if ($SkipBepInEx) { if (-not $hasBep) { Warn 'no BepInEx in the game folder: the plugin will not load until BepInEx 5 is installed' } }
elseif ($hasBep -and -not $ForceBepInEx) {
    $v = (Get-Item (Join-Path $bep 'core\BepInEx.dll')).VersionInfo.FileVersion
    Say "BepInEx already installed ($v): kept"
} else {
    foreach ($f in 'winhttp.dll', 'doorstop_config.ini') {
        Copy-ToBackup (Join-Path $game $f) (Join-Path $bk 'game-root')
        Step "install $f" { Copy-Item (Join-Path $pay $f) $game -Force }
    }
    Copy-ToBackup (Join-Path $bep 'core') (Join-Path $bk 'BepInEx')
    Step 'install BepInEx\core' {
        New-Item -ItemType Directory -Force -Path (Join-Path $bep 'core') | Out-Null
        Copy-Item (Join-Path $pay 'BepInEx\core\*') (Join-Path $bep 'core') -Force
    }
    if (-not (Test-Path (Join-Path $bep 'config\BepInEx.cfg'))) {
        Step 'install BepInEx\config\BepInEx.cfg' {
            New-Item -ItemType Directory -Force -Path (Join-Path $bep 'config') | Out-Null
            Copy-Item (Join-Path $pay 'BepInEx\config\BepInEx.cfg') (Join-Path $bep 'config') -Force
        }
    }
}

# 2. core plugin
$plugins = Join-Path $bep 'plugins'
Copy-ToBackup (Join-Path $plugins 'HomeCheat.dll') (Join-Path $bk 'plugins')
Step 'install BepInEx\plugins\HomeCheat.dll' {
    New-Item -ItemType Directory -Force -Path $plugins | Out-Null
    Copy-Item (Join-Path $pay 'BepInEx\plugins\HomeCheat.dll') $plugins -Force
}

# 3. command DLLs: older builds are moved out, so exactly one build per command is loaded
$cmds = Join-Path $bep 'homecheat-commands'
if (Test-Path $cmds) {
    $old = @(Get-ChildItem $cmds -Filter 'HomeCheat.Cmd.*.dll')
    if ($old.Count) {
        Step "move $($old.Count) existing command DLL(s) to $bk\homecheat-commands" {
            New-Item -ItemType Directory -Force -Path (Join-Path $bk 'homecheat-commands') | Out-Null
            $old | Move-Item -Destination (Join-Path $bk 'homecheat-commands') -Force
        }
    }
}
$new = @(Get-ChildItem (Join-Path $pay 'BepInEx\homecheat-commands') -Filter '*.dll')
Step "install $($new.Count) command DLL(s): $(($new | ForEach-Object { ($_.Name -split '\.')[2] }) -join ', ')" {
    New-Item -ItemType Directory -Force -Path $cmds | Out-Null
    $new | Copy-Item -Destination $cmds -Force
}

# 4. command channel and spell files (existing files are the player's: kept)
$cfg = Join-Path $bep 'config\homecheat'
Step 'create BepInEx\config\homecheat\inbox and outbox' {
    New-Item -ItemType Directory -Force -Path (Join-Path $cfg 'inbox'), (Join-Path $cfg 'outbox') | Out-Null
}
foreach ($f in Get-ChildItem (Join-Path $pay 'BepInEx\config\homecheat') -File) {
    if (Test-Path (Join-Path $cfg $f.Name)) { Say "kept existing $($f.Name)" }
    else { Step "install spell file $($f.Name)" { Copy-Item $f.FullName $cfg } }
}

# 5. key settings (the game's own settings; F2 -> F11, F11 screenshot off and F9 free are done by the keybinds DLL at load)
if (-not $NoKeySettings) {
    if (-not (Test-Path $script:PrefsKey)) { Warn 'no Valheim settings yet: start the game once, close it, run install.ps1 again for the keys' }
    else {
        Set-PrefString 'kbmBinding_Console' '<Keyboard>/backquote'
        $binds = Get-PrefString 'ConsoleBindings'
        if ($null -eq $binds) { $binds = '' }
        $lines = @($binds -split "`n" | Where-Object { $_ -ne '' })
        $f9 = @($lines | Where-Object { $_ -match '^F9\s' })
        if ($f9 -contains 'F9 save') { Say 'console bind F9 save: already there' }
        elseif ($f9.Count) { Warn "F9 is already bound to '$($f9[0])': not changed" }
        else { Set-PrefString 'ConsoleBindings' ("`n" + (($lines + 'F9 save') -join "`n")) }
    }
}

Say ''
Say 'Done. Next:'
Say '- Steam > Valheim > Properties > Launch options must contain  -console  for the console key to work.'
Say '- Start the game; BepInEx\LogOutput.log should show HomeCheat loading its commands.'
Say '- Try it in game:  .\send-command.ps1 "op=commands"   (lists every loaded command)'
Say '- Spell bar:  .\send-command.ps1 "op=spells&enabled=1"  then G in game.'
