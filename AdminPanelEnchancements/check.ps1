# AdminPanelEnchancements: check (read-only). Exit 0 = everything installed, 1 = something missing, 2 = unknown game code.
#   powershell -NoProfile -ExecutionPolicy Bypass -File check.ps1 [-GamePath <dir>]
param([string]$GamePath)
. (Join-Path $PSScriptRoot 'common.ps1')

$game = Find-ValheimDir $GamePath
$dll = Join-Path $game 'valheim_Data\Managed\assembly_valheim.dll'
$exit = 0
Say "game: $game"

Say ''
Say '== kick also bans =='
if (Get-Command node -ErrorAction SilentlyContinue) {
    & node (Join-Path $PSScriptRoot 'kickban\kick-ban-patch.js') check --dll $dll
    switch ($LASTEXITCODE) { 0 { } 1 { $exit = [Math]::Max($exit, 1); Say '-> not patched: close the game and run install.ps1' } default { $exit = 2; Say '-> unknown shape: do not force; see docs\kick-bans-lessons-learned.md' } }
} else { Warn 'Node.js not found, cannot check the DLL'; $exit = [Math]::Max($exit, 1) }

Say ''
Say '== world backups =='
$c = Get-PrefInt 'AutoBackups'; $s = Get-PrefInt 'AutoBackups_short'; $l = Get-PrefInt 'AutoBackups_long'
Say "backups kept:        $(if ($null -eq $c) { 'not set (game default 4)' } else { $c })"
Say "first backup after:  $(if ($null -eq $s) { 'not set (game default 7200 s)' } else { "$s s" })"
Say "later backups every: $(if ($null -eq $l) { 'not set (game default 43200 s)' } else { "$l s" })"
if ($null -eq $s -or $null -eq $l) { $exit = [Math]::Max($exit, 1); Say '-> backup timers not set: run install.ps1' }
$log = Join-Path $script:DataDir 'Player.log'
if (Test-Path $log) {
    $line = Select-String -Path $log -Pattern 'Considering autobackup' | Select-Object -Last 1
    if ($line) { Say "last log line: $($line.Line.Trim())" }
}
exit $exit
