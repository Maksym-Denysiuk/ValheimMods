---
description: how the two Valheim packages are built, what each holds, how the scripts behave, how they were tested (2026-10-24) and their limits
---
# Packaging

Done 2026-10-24, Valheim 1.0.20 (network version 41), BepInEx 5.4.2351. The packages live unpacked in this repository; HomeOS mounts it as the submodule `references/ValheimCustomMods/` and builds the generated parts into it.

## The split

| Package | Holds | Needs |
|---|---|---|
| **AdminPanelEnchancements** | kick also bans ([docs](../AdminPanelEnchancements/docs/kick-bans-lessons-learned.md)); world backups 5 x 3 min ([docs](../AdminPanelEnchancements/docs/backup-settings-lessons-learned.md)) | PowerShell 5.1, Node.js for the kick patch; hosting PC only |
| **GameplayEnhancements** | BepInEx 5 loader, HomeCheat plugin with its core commands, every command DLL (Spells, Transform, FX `visualeffect` and `fxdump`, Keybinds), console key `~` and `bind F9 save` | PowerShell 5.1; BepInEx may be blocked where Smart App Control is enforced |

## What is source and what is generated

| Part | Source | Edited where |
|---|---|---|
| `<Package>/docs/` (with the `Cmd_<name>.cs.txt` sources) | this repository | here |
| `README.md` and `*.ps1` of each package | HomeOS `scripts/valheim/packages/<Package>/`, shared `common.ps1` | HomeOS |
| `AdminPanelEnchancements/kickban/` | HomeOS `scripts/valheim/kick-ban-patch.js`, `scripts/valheim/lib/il-reader.js` | HomeOS |
| `GameplayEnhancements/payload/` | the game folder of the PC that builds: `BepInEx/plugins/HomeCheat.dll`, the BepInEx core, and for each command the highest `HomeCheat.Cmd.<name>.v<time>.dll` (the plugin's own load order: the last one wins) | rebuilt, never by hand |
| `GameplayEnhancements/src/` | the docs' `.cs.txt` files, as `.cs`, plus `build-cmd.sh` | rebuilt |
| `GameplayEnhancements/skills/valheim-spell-fx/` | HomeOS `.claude/skills/valheim-spell-fx/` (spell effect composer: finds the FX docs from either place) | HomeOS |
| `MANIFEST.md` | size and sha256 of every file | rebuilt |

## Build

From HomeOS:

```bash
node scripts/valheim/packages/build.js [--game <Valheim folder>] [--zips <dir>]
```

It rewrites the generated parts in place, stops when a command DLL has no source in the docs, and writes the two zips to `LocalOutputs/<yyyy-mm-dd--hh-mm>/packages/` (or `--zips`). Then commit and push this repository, then the submodule pointer in HomeOS.

## Package scripts

| Script | AdminPanelEnchancements | GameplayEnhancements |
|---|---|---|
| `install.ps1` | registry export, copies of the existing world backups and list files, the three backup values (read back), `kick-ban-patch.js apply` | registry export; BepInEx only if missing (`-ForceBepInEx`); plugin; older command builds moved out so one build per command loads; spell files only if missing; console key and F9 bind (`-NoKeySettings`) |
| `check.ps1` | patch state, backup values, last `Considering autobackup` log line | BepInEx version, every file against the package hash, key settings, last plugin log lines |
| `uninstall.ps1` | original DLL from its backup (same game version only), previous or default backup values | plugin, commands and config moved out, console F5, F9 bind removed; `-RemoveBepInEx` |
| `send-command.ps1` | - | writes `inbox/<name>.cmd`, prints the answer; refuses when the game is not running; withdraws the command when no answer comes |

All of them: `-GamePath` (else found through Steam's `libraryfolders.vdf`), `-BackupRoot` (default `D:\ValheimBackups`, else `%USERPROFILE%\ValheimBackups`), `-DryRun`, refuse to write while `valheim.exe` runs. Nothing is deleted: replaced files go to `<BackupRoot>\<time>-<package>\`.

## Verification (2026-10-24)

- Both `check.ps1` against the real install: exit 0 (kick patched, 5 / 180 / 180, the 11 commands and both key settings found). This also proves the PlayerPrefs name hash and the REG_BINARY string reading in PowerShell.
- AdminPanelEnchancements on a copy of the original DLL: install patched both sites and gave the same sha256 as the real patched DLL (`f74553ce…`); a second install said "already patched"; uninstall restored it byte for byte. Registry steps were run as `-DryRun` only.
- GameplayEnhancements on an empty game folder holding one stale command DLL: install put in BepInEx, the plugin, 11 commands and the spell files and moved the stale DLL out; check exit 0; uninstall `-RemoveBepInEx` left only `valheim.exe`. Key steps as `-DryRun` against the real registry: console value name matches, `F9 save` found as already bound.
- **Not tested:** a real install on a second PC, BepInEx loading there, and `send-command.ps1` getting an answer (the game was closed by then; its refusal was tested).

## Limits

- The kick patch covers only the PC it runs on and is undone by every Steam update: run `check.ps1` after updates.
- The packaged DLLs are built against Valheim 1.0.20; a game update can break a command. Rebuild from `src/` (`build-cmd.sh`, `VALHEIM_DIR` for another install path).
- The packages hold no world data and no per-player state (spell pins, `spells.state`).
- `payload/` redistributes BepInEx 5 and the libraries it ships (HarmonyX, MonoMod, Mono.Cecil) unchanged; their own licences apply to those files.
