# Sends one HomeCheat command to the running game and prints the answer.
#   powershell -NoProfile -ExecutionPolicy Bypass -File send-command.ps1 "op=give&prefab=Wood&amount=50" [-GamePath <dir>] [-TimeoutSec 10]
# Writes BepInEx\config\homecheat\inbox\<name>.cmd; the plugin answers in outbox\<name>.out (polled every 0.5 s).
param(
    [Parameter(Mandatory = $true, Position = 0)][string]$Command,
    [string]$GamePath,
    [int]$TimeoutSec = 10
)
. (Join-Path $PSScriptRoot 'common.ps1')

$cfg = Join-Path (Find-ValheimDir $GamePath) 'BepInEx\config\homecheat'
if (-not (Test-Path (Join-Path $cfg 'inbox'))) { throw "no $cfg\inbox: run install.ps1 first" }
if (-not (Get-Process -Name valheim -ErrorAction SilentlyContinue)) { throw 'valheim.exe is not running: start the game and load a world first' }
$name = 'pkg-' + (Get-Date -Format 'HHmmss') + '-' + (Get-Random -Maximum 9999)
$out = Join-Path $cfg "outbox\$name.out"
$tmp = Join-Path $cfg "inbox\$name.tmp"
Set-Content -Path $tmp -Value $Command -Encoding ASCII -NoNewline
Move-Item $tmp (Join-Path $cfg "inbox\$name.cmd")   # rename, so the plugin never reads a half-written file
$until = (Get-Date).AddSeconds($TimeoutSec)
while (-not (Test-Path $out)) {
    if ((Get-Date) -gt $until) {
        # take the command back, so it does not run on the next game start
        Remove-Item (Join-Path $cfg "inbox\$name.cmd") -ErrorAction SilentlyContinue
        throw "no answer within $TimeoutSec s: is a world loaded? (command withdrawn)"
    }
    Start-Sleep -Milliseconds 250
}
Start-Sleep -Milliseconds 100
Get-Content $out
