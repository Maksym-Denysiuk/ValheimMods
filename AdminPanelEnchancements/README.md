# AdminPanelEnchancements

Two host-side admin changes for Valheim, as a standalone package. Built and tested on Valheim 1.0.20 (network version 41), world hosted from the game (no dedicated server).

| Change | What it does | How |
|---|---|---|
| Kick also bans | F5 console `kick <name>` and Escape > players list > Kick user put the player on the ban list (`bannedlist.txt`), then disconnect them within 5 s | two IL call tokens in `assembly_valheim.dll` changed from `ZNet.Kick` to `ZNet.Ban`, found by meaning, not by offset |
| World backups | 5 backups kept, a new one at most every 3 minutes (at a save) | three game settings in the registry, no game file changed |

## Requirements

- Windows 10 or 11, Windows PowerShell 5.1 (built in).
- Node.js (any current LTS) for the kick-ban patch. Not needed with `-SkipKickBan`.
- The game closed while installing or uninstalling: Unity rewrites its settings and files when it quits.
- Only the PC that hosts the world needs it. Each hosting PC runs it itself.

## Install

```
powershell -NoProfile -ExecutionPolicy Bypass -File install.ps1
```

Options: `-GamePath <folder with valheim.exe>` (found through Steam otherwise), `-BackupRoot <dir>` (default `D:\ValheimBackups` if it exists, else `%USERPROFILE%\ValheimBackups`), `-SkipKickBan`, `-SkipBackupSettings`, `-BackupCount 5`, `-BackupShort 180`, `-BackupLong 180`, `-DryRun` (prints every step, writes nothing).

Before it changes anything it makes `<BackupRoot>\<time>-admin-panel\` with the registry export, the existing world backups, the three list files and the previous backup values. The kick-ban patch keeps the original DLL in `<BackupRoot>\<date>-kick-ban\assembly_valheim.dll.orig`.

## Check

```
powershell -NoProfile -ExecutionPolicy Bypass -File check.ps1
```

Exit 0: both changes active. 1: something is not applied (run install). 2: the game code changed shape; do not force the patch, read `docs\kick-bans-lessons-learned.md`, section "After a Steam update".

**Run check after every Valheim update.** A Steam update or "Verify integrity of game files" puts the original DLL back and `kick` silently becomes a plain kick again. The registry settings survive updates.

## Use

- Ban: `kick <name>` in the F5 console, or the Kick button in the players list. `Banning user <id>` in the log means it worked. The name is exact and case-sensitive; a misspelt name is stored as a junk line and nobody is kicked.
- Unban: `unban <id>` in the console (the id is in `bannedlist.txt`).
- There is no plain kick (disconnect only) on the host any more.
- Backups: `%USERPROFILE%\AppData\LocalLow\IronGate\Valheim\worlds_local\<world>_backup_auto-<time>\`. None in the first 20 minutes of a session (hard-coded in the game), and only when the game saves (autosave every 30 minutes, or `save` in the console).

## Uninstall

```
powershell -NoProfile -ExecutionPolicy Bypass -File uninstall.ps1
```

Restores the original DLL from its backup (only if it is the same game version; otherwise use Steam > Valheim > Properties > Installed Files > Verify integrity of game files) and puts back the previous backup settings, or the game defaults (4 backups, 2 h / 12 h). Same options as install: `-SkipKickBan`, `-SkipBackupSettings`, `-DryRun`.

## Contents

| Path | What |
|---|---|
| `install.ps1`, `check.ps1`, `uninstall.ps1`, `common.ps1` | the package scripts |
| `kickban\kick-ban-patch.js` (`check`, `apply`, `undo`, `--dll <path>`) and `kickban\lib\il-reader.js` | the patcher: a dependency-free ECMA-335 reader in Node, no .NET code is run |
| `docs\kick-bans-lessons-learned.md` | how the kick paths were found, gaps checked, verification, lessons |
| `docs\backup-settings-lessons-learned.md` | how the registry names were found, the 20-minute rule, verification, lessons |
| `MANIFEST.md` | every file with size and sha256 |

In the docs, `scripts/valheim/kick-ban-patch.js` means `kickban\kick-ban-patch.js` here, and `D:\ValheimBackups` is the default `-BackupRoot`.
