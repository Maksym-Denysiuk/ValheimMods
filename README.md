---
description: Valheim mods and settings changes for our own game, as two standalone packages - AdminPanelEnchancements (kick also bans, world backups every 3 minutes) and GameplayEnhancements (BepInEx, HomeCheat plugin with spells, transform, effects and key changes); each folder holds install, check and uninstall scripts, its payload and its documentation
---
# ValheimMods

Changes to Valheim 1.0.20 (network version 41) for our own game and our own worlds, as two packages. Each package folder is complete: copy it (or its zip) to a Windows PC and run its scripts. Every change says how it was found, what it changes, how it was verified and how to undo it.

| Package | What it does | Start here |
|---|---|---|
| [AdminPanelEnchancements](AdminPanelEnchancements/README.md) | host admin: `kick` (console and players-list button) also bans; world backups: 5 kept, one at most every 3 minutes | [README](AdminPanelEnchancements/README.md), docs in [docs/](AdminPanelEnchancements/docs/) |
| [GameplayEnhancements](GameplayEnhancements/README.md) | BepInEx 5 + the HomeCheat plugin: building and world helpers, spells and the spell bar, transform, visual effects, key changes | [README](GameplayEnhancements/README.md), command index in [docs/HomeCheat-plugin/](GameplayEnhancements/docs/HomeCheat-plugin/README.md) |

## Install in short

With the game closed, in the package folder:

```
powershell -NoProfile -ExecutionPolicy Bypass -File install.ps1     # -DryRun shows every step first
powershell -NoProfile -ExecutionPolicy Bypass -File check.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File uninstall.ps1
```

Nothing is deleted: every file or setting a script replaces goes to a dated backup folder first.

## Rules for items

- Admin changes (what the host may do to players, saves and backups) go to `AdminPanelEnchancements/docs/`; anything that changes play goes to `GameplayEnhancements/docs/<topic>/` (`HomeCheat-plugin`, `Spells`, `Transform`, `FX`, `Keybinds`, or a new topic folder).
- One file per change or topic, lowercase with hyphens, `description:` frontmatter on one line.
- Each item says: game version, goal, result (before and after), where the files and settings live, what failed first, method, change procedure, verification, undo.
- Command sources are kept next to their docs as `Cmd_<name>.cs.txt`; `src/` and `payload/` are generated.
- No account identifiers or IDs. Write `<steam-id>` in paths.

## Generated parts

`MANIFEST.md`, `src/`, `payload/`, `kickban/`, `skills/` and the `*.ps1` scripts in each package are written by the HomeOS build (`scripts/valheim/packages/build.js`); edit the docs here, the scripts in HomeOS. How the build works: [docs/packaging.md](docs/packaging.md).
