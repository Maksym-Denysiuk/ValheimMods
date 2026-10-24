---
description: lessons learned 2026-10-22 - how Valheim's world backups were changed to 5 backups with 3-minute timers through the game's registry settings, found by reading assembly_valheim.dll; what failed first, the method, the hard 20-minute rule, verification and undo
---
# Valheim backup settings: lessons learned

Done 2026-10-22 on `MDENYSIUK-PERS`, Valheim 1.0.20 (Unity 6000.0, network version 41), world `VanilaPVPpass=1 to 5`, hosted from the game (no dedicated server). No mod and no game file was changed.

## Goal

Keep a backup of every save, after save 248 and 249 were lost: the game keeps only the newest save set in the world folder.

## Result

| Setting | Before | After | Where |
|---|---|---|---|
| backups kept | 4 | 5 | registry `AutoBackups_h2630279319` (REG_DWORD) |
| first backup after | 7200 s | 180 s | registry `AutoBackups_short_h1097457434` (REG_DWORD) |
| later backups every | 43200 s | 180 s | registry `AutoBackups_long_h1724334946` (REG_DWORD) |
| minimum session before any backup | 1200 s | 1200 s (unchanged) | hard-coded; console `test autowait <s>` per session |
| autosave interval | 1800 s | 1800 s (unchanged) | hard-coded field `m_saveInterval`, no setting |

All three values are under `HKCU\Software\IronGate\Valheim`.

**What it does now:** after the first 20 minutes of a session, every save at least 3 minutes after the last backup creates `<world>_backup_auto-<yyyymmdd-hhmmss>/` and a character backup. The oldest backup is deleted when there are already 5. Each backup holds the save before the one that triggered it.

Confirmed in `Player.log`:
- 20:59:39 `Creating new backup since we haven't reached our desired amount`
- 21:04:38 and 21:08:20 `Time to remove our last autobackup and make a new one`

## Where things are

| What | Path |
|---|---|
| worlds (local) and their backups | `%USERPROFILE%\AppData\LocalLow\IronGate\Valheim\worlds_local\` |
| game log (rotates to `Player-prev.log` on each start) | `%USERPROFILE%\AppData\LocalLow\IronGate\Valheim\Player.log` |
| character, Steam Cloud | `D:\Steam\userdata\<steam-id>\892970\remote\characters\` |
| game code | `D:\Steam\steamapps\common\Valheim\valheim_Data\Managed\assembly_valheim.dll` |
| settings | `HKCU\Software\IronGate\Valheim` |
| safety copies and registry undo file | `D:\ValheimBackups\` |

**World format since 1.0:** a world is a folder, not the old `.db` and `.fwl` pair:
- `_main.<N>.db2`, `.fwl2`, `.chunks`, `.ok`
- plus `*.chunk` files (only the changed ones are rewritten)
- plus `cacheMinimap*`

`N` is the save number, and `.ok` is written last. The previous set is deleted on each save, so there is no `.old` file anymore.

## What failed first, and why

1. **Steam launch options** `-saveinterval 180 -backups 5 -backupshort 180 -backuplong 180`.
   - **What happened:** the process command line showed them, but the log still said `short time: 7200, long time: 43200, backup count: 4`.
   - **Why:** the game reads them only on the dedicated-server path. Every web source about them was a server-hosting guide, and none confirmed the game client reads them.
2. **F5 `save` as a backup trigger.**
   - **What happened:** a save is not a backup. The log said `Skipping backup. World session not long enough.`
3. **Assumed `.old` files**, from older guides. Version 1.0 has none (see the world format above).
4. **Assumed the character was in `LocalLow\...\characters\`.** It is a Steam Cloud character under `Steam\userdata\...\remote\characters\`. The log line `Cloud Save: ... /characters/kozak.fch.new` gave it away.
5. **The plan missed a hard 20-minute minimum.** After the registry change, a save at 528 s of play still skipped the backup. Reading the backup decision code found `ldc.i4 1200` as the default minimum, which can be overridden only by `Terminal.m_testList["autowait"]`.

## Method: how the settings were found

### 1. Read the log first

`Player.log` prints the values the game actually uses at every save:

`Considering autobackup for World. World time: …, short time: …, long time: …, backup count: …`

That line is the ground truth for every test.

### 2. Scan the game code's text

Read the UTF-16 user strings in `assembly_valheim.dll` with a small Node script (no `strings` command in Git Bash). The names `AutoBackups`, `AutoBackups_short` and `AutoBackups_long` stood out next to `PlatformPrefs`. The registry already held `AutoBackups_h2630279319 = 4`.

### 3. Work out the registry names

Unity PlayerPrefs stores each key as `<key>_h<hash>`. The hash is djb2-xor over the key text, as an unsigned 32-bit number:

```js
const h = s => { let x = 5381; for (const c of s) x = (Math.imul(x, 33) ^ c.charCodeAt(0)) >>> 0; return x };
h("AutoBackups")        // 2630279319, matches the existing value
h("AutoBackups_short")  // 1097457434
h("AutoBackups_long")   // 1724334946
```

### 4. Read the bytes after each key, without a decompiler

1. Find the metadata root (`BSJB`) and the `#US` heap.
2. Turn each string's heap offset into its token (`0x70000000 | offset`).
3. Search for `ldstr <token>` (`72 xx xx xx 70`) and decode the next bytes by hand.

What it showed for each key:

```
ldstr "AutoBackups_short"; ldsfld m_backupShort; call GetInt; stsfld m_backupShort
```

So each setting is a whole-number read (REG_DWORD) with the current field as the default. The same call with `ldc.i4.4` as the default for `AutoBackups` proves the call is `GetInt`.

- **Does the code run when hosting from the game?** Yes: it sits next to `Steam initialized` and `Loading: ZNet Start`, which the game's own log prints. It runs at each world load.
- **Does the game overwrite the timers?** No: the only write is `SetInt("AutoBackups", slider)` from the settings menu.
- **Field names** came from the `#~` Field and TypeDef tables, giving `m_backupCount`, `m_backupShort`, `m_backupLong` and `Terminal.m_testList`.

### 5. Read the backup decision code

The bytes before `Skipping backup. World session not long enough.` decode to:

`if (worldTime > 0 && worldTime < minimum) skip`

The code that prepares the check starts with `minimum = 1200`. It then lets `m_testList` keys `autoshort`, `autolong` and `autowait` override short, long and minimum. `m_testList` is set by the console command `test <key> <value>`, lives in memory only and resets on every launch.

## Change procedure (what was run)

1. Check the game is closed (`tasklist`). Unity writes its settings when the game quits and would overwrite the change.
2. Copy all existing backups (world and character) to `D:\ValheimBackups\2026-10-22-before-change\`. Compare file counts and sizes, and the character files byte for byte.
3. `reg export "HKCU\Software\IronGate\Valheim" "D:\ValheimBackups\valheim-registry-before.reg"`
4. `reg add "HKCU\Software\IronGate\Valheim" /v <name> /t REG_DWORD /d <value> /f` for each of the three values, then read them back with `reg query`.
5. Start the game, play past 20 minutes, save, then read the log and the folders.

**Undo:** with the game closed, `reg import` the export file, then delete the `_short` and `_long` values.

## Lessons

1. **Measure before you trust a guide.** The game's own log and the process command line settled every question in seconds. Guides about dedicated servers are not about the game client.
2. **The game code is the specification.** A byte-level read of `ldstr` call sites was enough to find setting names, value types, defaults and when the code runs. No decompiler or mod was needed.
3. **A plan is only as good as the code path it read.** The first plan read where the settings come from, but not the backup decision itself, so it missed the 20-minute rule. Read the whole decision path before promising a behaviour.
4. **Back up before you change backup behaviour.** A new rotation deletes old backups. The 10-19 and 10-21 backups were deleted within minutes, but the safety copies kept them.
5. **Saves and backups are different events.** The game decides on a backup only at a save. A timer of 3 minutes is a minimum gap, not a schedule. Autosave stays at 30 minutes.
6. **Check where the data really lives.** World and character files sit in different places (local versus Steam Cloud). The log names the real paths.

## Open options (not done)

- **Real 3-minute automatic saves:** run Valheim Dedicated Server (a free Steam tool) with `-saveinterval 180 -backups 5 -backupshort 180 -backuplong 180`, or use a BepInEx plugin that patches `m_saveInterval` and the 1200 s minimum. Game updates tend to break the plugin.
- **Backups in the first 20 minutes:** type `test autowait 180` in the F5 console each session (may need `devcommands`; not tested).
- **After a big game update:** check the log line again, because the setting names could change.
