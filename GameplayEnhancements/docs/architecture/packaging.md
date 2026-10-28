---
description: how the two Valheim packages are built, what each holds, how the scripts behave, the install runbook with known failures and fixes, how they were tested (2026-10-24) and their limits
---
> Copy of `docs/packaging.md` from the ValheimMods repository, written by the HomeOS package build; edit the original.

# Packaging

Done 2026-10-24, Valheim 1.0.20 (network version 41), BepInEx 5.4.2351. The packages live unpacked in this repository; HomeOS mounts it as the submodule `references/ValheimCustomMods/` and builds the generated parts into it.

## The split

| Package | Holds | Needs |
|---|---|---|
| **AdminPanelEnchancements** | kick also bans ([docs](https://github.com/Maksym-Denysiuk/ValheimMods/blob/main/AdminPanelEnchancements/docs/kick-bans-lessons-learned.md)); world backups 5 x 3 min ([docs](https://github.com/Maksym-Denysiuk/ValheimMods/blob/main/AdminPanelEnchancements/docs/backup-settings-lessons-learned.md)) | PowerShell 5.1, Node.js for the kick patch; hosting PC only |
| **GameplayEnhancements** | BepInEx 5 loader, HomeCheat plugin with its core commands, every command DLL (Spells, Transform, FX `visualeffect` and `fxdump`, Keybinds), console key `~` and `bind F9 save` | PowerShell 5.1; BepInEx may be blocked where Smart App Control is enforced |

## What is source and what is generated

| Part | Source | Edited where |
|---|---|---|
| `<Package>/docs/` | this repository | here |
| `GameplayEnhancements/src/` (`Core/`, `Features/<Feature>/`, `SelfTest/`, `build.sh`; design in [homecheat-architecture.md](homecheat-architecture.md)) | this repository | here; checked, not rewritten, by the build |
| `README.md` and `*.ps1` of each package | HomeOS `scripts/valheim/packages/<Package>/`, shared `common.ps1` | HomeOS |
| `AdminPanelEnchancements/kickban/` | HomeOS `scripts/valheim/kick-ban-patch.js`, `scripts/valheim/lib/il-reader.js` | HomeOS |
| `GameplayEnhancements/payload/` | the game folder of the PC that builds: `BepInEx/plugins/HomeCheat.dll`, the BepInEx core, and for each feature the `HomeCheat.Cmd.<feature>.v<stamp>.dll` with the highest stamp as a number (the plugin's own rule) | rebuilt, never by hand |
| `GameplayEnhancements/skills/valheim-spell-fx/`, `skills/valheim-spells-craft/` | HomeOS `.claude/skills/` (spell effect composer; command builder), rendered as independent copies: see [Skill copies](#skill-copies) | HomeOS |
| `GameplayEnhancements/docs/architecture/` | this repository's `docs/*.md` and `docs/diagrams/*.svg`, links rewritten (inside the package: relative; elsewhere: GitHub) | here, in `docs/` |
| `MANIFEST.md` | size and sha256 of every file | rebuilt |

## Build

From HomeOS:

```bash
node scripts/valheim/packages/build.js [--game <Valheim folder>] [--zips <dir>]
```

It rewrites the generated parts in place, stops when a feature DLL has no `src/Features/<Feature>/` folder or a feature folder has no build, and writes the two zips to `LocalOutputs/<yyyy-mm-dd--hh-mm>/packages/` (or `--zips`). Then commit and push this repository, then the submodule pointer in HomeOS.

`--no-game` rebuilds GameplayEnhancements without the game folder: the package scripts and README, the skill copies, `docs/architecture/` and `MANIFEST.md` (the feature list read from `payload/`). It leaves `payload/` as committed, skips AdminPanelEnchancements and writes no zips. Use it when only docs, scripts or the skills changed.

### Skill copies

The skills in `GameplayEnhancements/skills/` are independent copies for people without HomeOS, rendered from the HomeOS originals by `renderSkill` in `build.js`:

| Step | What |
|---|---|
| drop | lines marked `homeos-only`, blocks between `homeos-only:start` and `homeos-only:end`, files whose first line holds `homeos-only:file` (`instructions/refresh.md`), the HomeOS security record `skill-meta-data.md` |
| overlay | the original's `package/` folder: `README.md` (requirements, install, what the copy skips) and `evals.md` (the copy's evals, in place of the HomeOS ones) |
| swap | the owner's name, the second test account and the test world get generic names |
| leak check | any HomeOS path, rule, step or personal path left in a rendered file stops the build with file:line; nothing is written |

Installed in a project's `.claude/skills/` (the unpacked package, or a clone of this repository), `commands-check.js` and `find-fx.js` find the package by themselves. Edit the originals in HomeOS; HomeOS `HomeOS-Documentation/skill-versions.md` explains both versions.

## Package scripts

| Script | AdminPanelEnchancements | GameplayEnhancements |
|---|---|---|
| `install.ps1` | registry export, copies of the existing world backups and list files, the three backup values (read back), `kick-ban-patch.js apply` | registry export; BepInEx only if missing (`-ForceBepInEx`); plugin; older command builds moved out so one build per command loads; spell files only if missing; console key and F9 bind (`-NoKeySettings`) |
| `check.ps1` | patch state, backup values, last `Considering autobackup` log line | BepInEx version, every file against the package hash, key settings, last plugin log lines |
| `uninstall.ps1` | original DLL from its backup (same game version only), previous or default backup values | plugin, commands and config moved out, console F5, F9 bind removed; `-RemoveBepInEx` |
| `send-command.ps1` | - | writes `inbox/<name>.cmd`, prints the answer; refuses when the game is not running; withdraws the command when no answer comes |

All of them: `-GamePath` (else found through Steam's `libraryfolders.vdf`), `-BackupRoot` (default `D:\ValheimBackups`, else `%USERPROFILE%\ValheimBackups`), `-DryRun`, refuse to write while `valheim.exe` runs. Nothing is deleted: replaced files go to `<BackupRoot>\<time>-<package>\`.
## Install runbook

For an install on request ("install GameplayEnhancements", "reinstall after the update"). Run from the unpacked package folder in Windows PowerShell 5.1; 5.1 has no `&&`, so run one command per line.

1. **Preconditions.** `valheim.exe` is not running (`Get-Process valheim -ErrorAction SilentlyContinue` returns nothing; every script refuses otherwise). Note a non-default game folder for `-GamePath`.
2. **Dry run.** `powershell -NoProfile -ExecutionPolicy Bypass -File install.ps1 -DryRun`. Read the steps it prints: BepInEx (only if missing), plugin, command builds, older builds moved out, spell files (only if missing), key settings.
3. **Install.** The same command without `-DryRun`. Everything replaced goes to `<BackupRoot>\<time>-gameplay\`.
4. **Check.** `powershell -NoProfile -ExecutionPolicy Bypass -File check.ps1` until exit 0.
5. **In game.** Start Valheim (launch option `-console` for the console key), load a world, then `send-command.ps1 "op=ping"` (answer `pong`), `"op=status"`, `"op=commands&builds=1"` (one build per feature, the package's stamps).

| Failure | Cause | Fix |
|---|---|---|
| a script says the game is running | `valheim.exe` still open (also after a crash) | close it, check the process list, run again |
| `The token '&&' is not a valid statement separator` | PowerShell 5.1 | one command per line, or `;` |
| no BepInEx log after starting the game, commands never answered | BepInEx blocked (Smart App Control enforced) or `winhttp.dll` missing | `check.ps1` names the missing file; Smart App Control is the PC owner's decision |
| `send-command.ps1` withdraws the command with no answer | no world loaded, or the plugin did not start | load a world; read `BepInEx\LogOutput.log` for `HomeCheat` lines |
| an older or a dev command build answers | a build with a higher stamp in `homecheat-commands\` (the newest stamp loads) | move the extra build out, restart the game |
| the game hangs on the main scene after an update | a Harmony patch applied at start on a changed game method (as the spell bar did on 2026-10-25) | move that feature's DLL out, start, rebuild from `src\` |
| a command fails after a Valheim update | game API changed | rebuild from `src\` (`bash src/build.sh all`, `VALHEIM_DIR` for another path), `op=reload`, or restart for the core |
| the spell bar shows the old spells | `install.ps1` keeps an existing `spells.txt` | copy `payload\BepInEx\config\homecheat\spells.txt` over it, then `op=spells&reload=1` |

A failure not in this table is fixed, then added here in the same task.

## Verification (2026-10-24)

- Both `check.ps1` against the real install: exit 0 (kick patched, 5 / 180 / 180, the 11 commands and both key settings found). This also proves the PlayerPrefs name hash and the REG_BINARY string reading in PowerShell.
- AdminPanelEnchancements on a copy of the original DLL: install patched both sites and gave the same sha256 as the real patched DLL (`f74553ce…`); a second install said "already patched"; uninstall restored it byte for byte. Registry steps were run as `-DryRun` only.
- GameplayEnhancements on an empty game folder holding one stale command DLL: install put in BepInEx, the plugin, 11 commands and the spell files and moved the stale DLL out; check exit 0; uninstall `-RemoveBepInEx` left only `valheim.exe`. Key steps as `-DryRun` against the real registry: console value name matches, `F9 save` found as already bound.
- **Not tested:** a real install on a second PC, BepInEx loading there, and `send-command.ps1` getting an answer (the game was closed by then; its refusal was tested).

## Limits

- The kick patch covers only the PC it runs on and is undone by every Steam update: run `check.ps1` after updates.
- The packaged DLLs are built against Valheim 1.0.20; a game update can break a command. Rebuild from `src/` (`bash src/build.sh all`, `VALHEIM_DIR` for another install path).
- The packages hold no world data and no per-player state (spell pins, `spells.state`).
- `payload/` redistributes BepInEx 5 and the libraries it ships (HarmonyX, MonoMod, Mono.Cecil) unchanged; their own licences apply to those files.
