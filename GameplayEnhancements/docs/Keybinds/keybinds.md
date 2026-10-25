---
description: keybinds 2026-10-24 - stats window F2 to F6, then F11 on 2026-10-25 with the vanilla F11 screenshot off (HomeCheat patches), console F5 to backquote (the game's own Console button setting), F9 = save (console bind); before-state of every key, how each was found in assembly_valheim.dll, verification from the logs and undo
---
# Keybinds: stats window F11, console `~`, F9 saves the world

Done 2026-10-24 on `MDENYSIUK-PERS`, Valheim 1.0.20 (network version 41), BepInEx 5.4.2351, HomeCheat plugin. World `ClaudeTest`, hosted from the game. Own game only.

## Result

| Key | Before | After | How |
|---|---|---|---|
| F2 | stats window (World, Port, UID, ZDO, Connections, Players) | nothing (spell-bar pin only) | Harmony patch |
| F6 | nothing in the game; spell pin on an old line of `spells.txt` | stats window (2026-10-24), then free for the spell hotbar (Panel 6, 2026-10-25) | Harmony patch; pin removed |
| F11 | vanilla: saves a game screenshot (`GameCamera.ScreenShot`, in game and in the main menu) | stats window (2026-10-25) | Harmony patches; screenshot check off |
| F5 | console | nothing (spell-bar pin only) | game setting |
| `~` (backquote, left of 1) | nothing | console | game setting |
| F9 | vanilla: cycles the gamepad controller layout | saves the world | layout key patched off; console bind `F9 save` |

## What was bound before (undo list)

| Key | Vanilla | `bind` list | Mod |
|---|---|---|---|
| F2 | `ConnectPanel.Update`: `ZInput.GetKeyDown(KeyCode.F2)` toggles the stats window | none | spell-bar pin F2 (kept) |
| F5 | button `Console`, registry `kbmBinding_Console = <Keyboard>/F5` | none | spell-bar pin F5 (kept) |
| F6 | none | none | `spells.txt` line 8.4 `sticky pin=F6` (removed; the current spell bar knows F1 to F5 only) |
| F9 | `KeyHints.Update`: F9 = next gamepad layout, shows "Changed controller layout to: ..." | none | none |
| F11 | `GameCamera.LateUpdate` and `FejdStartup.LateUpdate`: `ZInput.GetKeyDown(KeyCode.F11)` calls `GameCamera.ScreenShot` | none | none |
| `~` | none | none | none |

The registry value `ConsoleBindings` was empty. No other `kbmBinding_*` value used backquote. The spell bar's F2 and F5 pins stay: the request was that F2 and F5 stop opening the stats window and the console.

## How each was found

1. **Hard-coded keys.** A Node scan of every method in `assembly_valheim.dll` (`scripts/valheim/lib/il-reader.js`) for `ldc.i4 <KeyCode>` followed by `ZInput.GetKeyDown`. F2 (283) is only in `ConnectPanel.Update`; F9 (290) only in `KeyHints.Update`; F5, F6 and backquote are not hard-coded anywhere. F11 (292), scanned 2026-10-25, is in `GameCamera.LateUpdate` and `FejdStartup.LateUpdate`, both the game screenshot.
2. **Console.** `Console.Update` asks `ZInput.GetButtonDown("Console")`, a named, remappable button. `ZInput.Save` writes every button as `kbmBinding_<name>` through `PlatformPrefs` (registry), `ZInput.Load` reads it back. So the console key is a game setting, not code. Needs the `-console` launch option, which is already set.
3. **Binds.** `Terminal.updateBinds` stores `bind` lines (`<KeyCode> <command>`) in the registry value `ConsoleBindings`; `Chat.Update` runs them on `GetKeyDown` while the console is closed.
4. **Save.** Console `save` = `ZNet.SaveWorldAndPlayerProfiles` (host: `RPC_Save`, a client sends the Save RPC to the server).
5. **Cheat flags.** `bind` and `save` are created with `isCheat = false`. The cheated flag (`PlayerProfile.m_usedCheats`) is set only in `ConsoleCommand.RunAction` for `IsCheat` commands. Neither command, nor the console key, touches it.
6. **The `[CheatCleanSlate]` tag in `Player.log`** (`WorldVersion: 42 [CheatCleanSlate]`) is the name of the newest world save-format version (enum `World` member), not a "no cheats" mark. No key change affects it.

## What changed

| What | Where |
|---|---|
| command DLL `HomeCheat.Cmd.keybinds.v221027.dll` (source: [src/Features/Keybinds](../../src/Features/Keybinds/)) | `D:\Steam\steamapps\common\Valheim\BepInEx\homecheat-commands\` |
| `kbmBinding_Console` `<Keyboard>/F5` -> `<Keyboard>/backquote` | registry `HKCU\Software\IronGate\Valheim`, written by the game (`op=keybinds&console=backquote` calls the game's own `Rebind` and `ZInput.Save`) |
| `ConsoleBindings` `` -> `F9 save` | same registry key, typed in the console: `bind F9 save` |
| spell pin F6 removed | `BepInEx\config\homecheat\spells.txt` |

The DLL patches on load (game start or `op=reload`): a transpiler changes F2 to F11 in `ConnectPanel.Update` and turns the F9 check in `KeyHints.Update` and the F11 checks in `GameCamera.LateUpdate` and `FejdStartup.LateUpdate` into a constant false (the free-fly camera's mouse-click screenshot and Steam's F12 stay). Each logs a warning to `BepInEx/LogOutput.log` if the game code no longer has exactly one such check. `op=keybinds&status=1` shows the patch state; `op=keybinds&on=0` removes all four patches.

## Verification

- 2026-10-25, F11: build `v20261025171549` loaded with `op=reload`; `op=keybinds&status=1` answered `ON, patched ConnectPanel::Update KeyHints::Update GameCamera::LateUpdate FejdStartup::LateUpdate`, no WARNING in `LogOutput.log` (each check found exactly once). Maksym confirmed in game: F11 opens and closes the stats window, F2 and F6 do not; `LogOutput.log` shows `keybinds: F11 pressed, stats window visible = True/False` on each press and no F2 or F6 presses after the reload.
- 2026-10-24, F6: `LogOutput.log` `keybinds: F6 pressed, stats window visible = True/False` on each press; Maksym confirmed F2 does nothing (F2 presses are logged as `F2 pressed (ignored)` and the window did not change).
- Console and save, confirmed by Maksym in game: `~` opens the console, F5 does not, F9 saves. `Player.log` 22:12:25: `World save (5/5) done`, save number 16, with an auto backup in the same second.
- Registry after the change: `kbmBinding_Console_h3187837224 = <Keyboard>/backquote`, `ConsoleBindings_h3167151616 = F9 save`.

## Backup

`D:\ValheimBackups\2026-10-24-keybinds\`: `valheim-registry-before.reg` (full export), `spells.txt`, `spells.state`, `spells-ui-test.txt`, `Player-before.log`.

## Undo

- **Console back to F5:** in game `op=keybinds&console=F5` (drop `op=keybinds&console=F5` as an inbox file), or with the game closed set `kbmBinding_Console_h3187837224` to `<Keyboard>/F5`.
- **F9 bind:** console `unbind F9`.
- **F2, F9 and F11 as in vanilla:** delete `HomeCheat.Cmd.keybinds.*.dll` from `homecheat-commands\` and restart the game (a reload only adds), or `op=keybinds&on=0` for this session.
- **Everything:** with the game closed `reg import D:\ValheimBackups\2026-10-24-keybinds\valheim-registry-before.reg`, restore `spells.txt` from the backup, delete the DLL.

## Lessons

1. **Check the game's own settings before writing code.** The console key was a saved button setting; only the two hard-coded keys needed a patch.
2. **A running game cannot take registry edits, but it can write its own.** Calling the game's `Rebind` and `Save` live persisted the change with no restart.
3. **Find what a key does by meaning.** The scan found F9's real vanilla job (gamepad layout), which no list of defaults mentions.

## Open

- The spell hotbar took F6 to F10 on 2026-10-25 (Panel 6-0), which is why the stats window moved to F11. Pins on F9, F11 or `~` would clash.
- After a Valheim update: `op=keybinds&status=1` and the log warning show whether the two patches still match.
