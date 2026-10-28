# GameplayEnhancements

The HomeCheat plugin and every gameplay change built on it, as a standalone package for our own Valheim game and our own worlds. Built and tested on Valheim 1.0.20 (network version 41) with BepInEx 5.4.2351. Client-side: no network port, no console cheat path, no game file changed.

## What is in it

| Group | Commands | Docs |
|---|---|---|
| Core plugin: file command channel, command registry, shared helpers | `ping`, `status`, `reload`, `commands`, `help`, `harmeffect` | `docs\HomeCheat-plugin\` |
| World and Diag (feature DLLs): building and world helpers, discovery | `repair`, `refill`, `plant`, `field`, `cultivate`, `place`, `scale` (tree size, kept on reload), `give`, `spawn`, `list`, `groundlevel`, `path`, `inspect`; `cheatstate`, `cheatinfo` | `docs\HomeCheat-plugin\` |
| Spells | `earthquake`, `slime`, `spellshot`, `heal` / `cleanse` / `bless`, `spells` (spell bar: G opens the radial, 10-cell bar) | `docs\Spells\` |
| Transform | `transform` (raven, serpent, fish, human), `hidebody`, `killmobs` (test helper) | `docs\Transform\` |
| Effects | `visualeffect` (fire any fx_/vfx_ prefab, weather), `fxdump` (prefab inventory) | `docs\FX\` |
| Keys | `keybinds`: stats window F2 -> F11 (vanilla F11 screenshot off), F9 no longer cycles the gamepad layout; plus the game settings console key `~` and `bind F9 save` | `docs\Keybinds\` |

`MANIFEST.md` lists the exact feature DLL builds. Status per command (run in game or only built) is in `docs\HomeCheat-plugin\README.md`, column Status.

## Requirements

- Windows 10 or 11, Windows PowerShell 5.1 (built in), Valheim from Steam.
- The game closed while installing or uninstalling.
- BepInEx 5 is included and installed when the game has none. On a PC with Smart App Control enforced, the unsigned loader may be blocked; switching Smart App Control off is the PC owner's decision, not part of this package.
- Steam launch option `-console` (Steam > Valheim > Properties > Launch options) for the console key.

## Install

```
powershell -NoProfile -ExecutionPolicy Bypass -File install.ps1
```

Options: `-GamePath <folder with valheim.exe>` (found through Steam otherwise), `-BackupRoot <dir>` (default `D:\ValheimBackups` if it exists, else `%USERPROFILE%\ValheimBackups`), `-SkipBepInEx`, `-ForceBepInEx` (replace an existing BepInEx core), `-NoKeySettings` (leave the console key and binds alone), `-DryRun` (prints every step, writes nothing).

Everything it replaces goes to `<BackupRoot>\<time>-gameplay\` first: the registry export, the old `HomeCheat.dll`, every older command build (moved out, so exactly one build per command loads), and the BepInEx files when it installs the loader. Spell files that already exist are kept.

## Check

```
powershell -NoProfile -ExecutionPolicy Bypass -File check.ps1
```

Exit 0: BepInEx, the plugin, every command DLL of this package and the key settings are in place. After a Valheim update also send `op=keybinds&status=1`: the log warns when the game code no longer matches the two key patches.

## Use

Start the game and load a world, then send commands from this folder:

```
powershell -NoProfile -ExecutionPolicy Bypass -File send-command.ps1 "op=commands"
powershell -NoProfile -ExecutionPolicy Bypass -File send-command.ps1 "op=give&prefab=Wood&amount=50"
powershell -NoProfile -ExecutionPolicy Bypass -File send-command.ps1 "op=spells&enabled=1"
```

`send-command.ps1` writes `BepInEx\config\homecheat\inbox\<name>.cmd` and prints `outbox\<name>.out`. Any tool that writes such a file works the same way. Syntax and defaults of every command: `docs\HomeCheat-plugin\README.md`, section Command index.

The spell bar starts in placeholder mode (casts only show a toast); `op=spells&placeholder=0` switches to the real spells in `spells.txt`.

## Build from source

`src\` holds the core (`Core\`), one folder per feature DLL (`Features\<Feature>\`, extra game references in its `refs.txt`), the logic tests (`SelfTest\`) and `build.sh`. With Git Bash, from this folder: `bash src/build.sh <feature>` (then `op=reload` in game), `bash src/build.sh core` (game closed), `bash src/build.sh all`, `bash src/build.sh selftest`. Set `VALHEIM_DIR=/c/...` when the game is not in `D:\Steam`. It needs only the .NET Framework 4 compiler that ships with Windows (C# 5). Classes, lifecycle, extension points and how to add a command: `docs\architecture\homecheat-architecture.md`; every command with what it changes: `docs\architecture\homecheat-commands.md` (copies of the repository's `docs/`, written by the HomeOS build).

## Claude skills

Two Claude Code skills ship in `skills\`, as independent copies that work without the maintainer's own setup:

| Skill | What it does |
|---|---|
| `skills\valheim-spell-fx\` | describe a spell ("a frost nova that roots enemies") and it proposes five effect combinations from the real prefabs in `docs\FX\`, with timing, preview commands and what is still needed to make the spell work; it fires nothing without a yes |
| `skills\valheim-spells-craft\` | plans, builds, tests and documents new HomeCheat commands or extends existing ones, always from the docs in `docs\architecture\` and the code in `src\`; plan mode writes spec cards and a session plan, implement mode builds one card |

To use one, copy its folder to `.claude\skills\` inside this folder (or inside a ValheimMods clone) and start Claude Code there; the skills find this package by themselves. Each folder's `README.md` has the requirements, the install check, what the copy skips and its evals (`evals.md`). The copies are generated by every package build: send fixes to the ValheimMods repository instead of editing them.

## Uninstall

```
powershell -NoProfile -ExecutionPolicy Bypass -File uninstall.ps1
```

Moves `HomeCheat.dll`, the command DLLs and `config\homecheat\` to `<BackupRoot>\<time>-gameplay-removed\`, sets the console key back to F5 and removes the `F9 save` bind. `-RemoveBepInEx` also moves BepInEx out of the game folder; `-KeepConfig`, `-KeepKeySettings`, `-DryRun`. Objects planted, placed or resized with the commands are world data and stay: restore a world backup for those.

## Contents

| Path | What |
|---|---|
| `install.ps1`, `check.ps1`, `uninstall.ps1`, `send-command.ps1`, `common.ps1` | the package scripts |
| `payload\winhttp.dll`, `payload\doorstop_config.ini`, `payload\BepInEx\core\`, `payload\BepInEx\config\BepInEx.cfg` | BepInEx 5.4.2351 loader as installed on the PC it was tested on |
| `payload\BepInEx\plugins\HomeCheat.dll` | the core plugin |
| `payload\BepInEx\homecheat-commands\HomeCheat.Cmd.<feature>.v<yyyyMMddHHmmss>.dll` | the newest build of each feature |
| `payload\BepInEx\config\homecheat\spells.txt`, `spells-ui-test.txt` | default spell files |
| `src\` | the sources: `Core\`, `Features\<Feature>\`, `SelfTest\`, `build.sh` (edited in the ValheimMods repository) |
| `skills\valheim-spell-fx\`, `skills\valheim-spells-craft\` | the two Claude skills (`SKILL.md`, `README.md` setup, `evals.md`, `scripts\`; spells-craft also `instructions\`, `baseline.md`) |
| `docs\` | the documentation folders |
| `MANIFEST.md` | every file with size and sha256 |
