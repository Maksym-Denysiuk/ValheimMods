# GameplayEnhancements: check (read-only). Exit 0 = installed as in this package, 1 = something missing or different.
#   powershell -NoProfile -ExecutionPolicy Bypass -File check.ps1 [-GamePath <dir>]
param([string]$GamePath)
. (Join-Path $PSScriptRoot 'common.ps1')

$pay = Join-Path $PSScriptRoot 'payload'
$game = Find-ValheimDir $GamePath
$bep = Join-Path $game 'BepInEx'
$exit = 0
function Bad([string]$m) { Say "  MISSING/DIFFERENT: $m"; $script:exit = 1 }
function Same([string]$a, [string]$b) { (Test-Path $a) -and (Test-Path $b) -and ((Get-FileHash $a).Hash -eq (Get-FileHash $b).Hash) }
Say "game: $game"

Say '== BepInEx =='
$core = Join-Path $bep 'core\BepInEx.dll'
if (Test-Path $core) { Say "  BepInEx $((Get-Item $core).VersionInfo.FileVersion)" } else { Bad 'BepInEx\core\BepInEx.dll' }
if (-not (Test-Path (Join-Path $game 'winhttp.dll'))) { Bad 'winhttp.dll (BepInEx loader)' }

Say '== HomeCheat =='
if (Same (Join-Path $bep 'plugins\HomeCheat.dll') (Join-Path $pay 'BepInEx\plugins\HomeCheat.dll')) { Say '  plugins\HomeCheat.dll = package build' } else { Bad 'plugins\HomeCheat.dll' }
$cmds = Join-Path $bep 'homecheat-commands'
foreach ($f in Get-ChildItem (Join-Path $pay 'BepInEx\homecheat-commands') -Filter '*.dll') {
    if (Same (Join-Path $cmds $f.Name) $f.FullName) { Say "  $($f.Name)" } else { Bad $f.Name }
}
if (Test-Path $cmds) {
    $extra = @(Get-ChildItem $cmds -Filter '*.dll' | Where-Object { -not (Test-Path (Join-Path $pay "BepInEx\homecheat-commands\$($_.Name)")) })
    if ($extra.Count) { Say "  note: $($extra.Count) other DLL(s) in homecheat-commands (older builds or new commands); the newest name per command wins" }
}

Say '== keys =='
$con = Get-PrefString 'kbmBinding_Console'
$binds = Get-PrefString 'ConsoleBindings'
Say "  console key: $(if ($con) { $con } else { 'default (F5)' })"
if ($con -ne '<Keyboard>/backquote') { Bad 'console key is not backquote' }
if (@(($binds -split "`n")) -contains 'F9 save') { Say '  F9 save bound' } else { Bad 'console bind F9 save' }

$log = Join-Path $bep 'LogOutput.log'
if (Test-Path $log) {
    Say '== last HomeCheat log lines =='
    Select-String -Path $log -Pattern 'HomeCheat|keybinds' | Select-Object -Last 5 | ForEach-Object { Say "  $($_.Line.Trim())" }
}
exit $exit
