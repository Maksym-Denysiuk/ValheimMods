---
description: lessons learned 2026-10-22 - how every host-side kick in Valheim (F5 console `kick` and Escape > players list > Kick) was made to also ban, by changing two call tokens in assembly_valheim.dll with a Node script; why BepInEx was skipped, the method, the gaps checked, what the first test missed, verification, undo, and re-checking after a Steam update
---
# Valheim kick also bans: lessons learned

Done 2026-10-22 on `MDENYSIUK-PERS`, Valheim 1.0.20 (network version 41), world `VanilaPVPpass=1 to 5`, hosted from the game (no dedicated server). One game file was changed: `assembly_valheim.dll`, two call tokens, one byte each.

## Goal

Every player the host kicks should land on the ban list. Vanilla `kick` only disconnects, and the person can walk straight back in.

## Result

| Kick path | Before | After |
|---|---|---|
| F5 console `kick <name>` | `ZNet.Kick`: disconnect | `ZNet.Ban`: ban-list entry, then disconnect |
| Escape > players list > Kick user (confirm popup) | `ZNet.Kick`: disconnect | `ZNet.Ban`: ban-list entry, then disconnect |

`ZNet.Ban` resolves the player by name, stores their platform ID in `bannedlist.txt` at once, and prints `Banning user <id>`. `ZNet.UpdateBanList` runs every 5 s and kicks any connected banned peer, so the player is gone within seconds.

The plain kick (disconnect only) no longer exists on the host. The way back in is `unban <id>` in the F5 console.

## Where things are

| What | Path |
|---|---|
| patched file | `D:\Steam\steamapps\common\Valheim\valheim_Data\Managed\assembly_valheim.dll` |
| script | `scripts/valheim/kick-ban-patch.js` (`check`, `apply`, `undo`) |
| IL reader the script uses | `scripts/valheim/lib/il-reader.js` |
| original DLL and list files, before the change | `D:\ValheimBackups\2026-10-22-kick-ban\` |
| ban, admin and permitted lists | `%USERPROFILE%\AppData\LocalLow\IronGate\Valheim\bannedlist.txt`, `adminlist.txt`, `permittedlist.txt` |
| game log | `%USERPROFILE%\AppData\LocalLow\IronGate\Valheim\Player.log` |

## Why not BepInEx

Smart App Control is on and enforced on this PC. BepInEx's loader DLL is unsigned and would likely be blocked. Turning Smart App Control off is a security setting and Maksym's call, so it was not part of this change.

The same Smart App Control state blocks .NET reflection on the game DLL (`An Application Control policy has blocked this file`), so no decompiler or `Assembly.LoadFrom` was possible either.

## Method

1. **Read the IL with Node, without running any .NET code.** `scripts/valheim/lib/il-reader.js` is a dependency-free ECMA-335 reader: PE sections, the `#~`, `#Strings`, `#US` and `#Blob` streams, tables 0x00 to 0x0A, and an IL decoder that resolves tokens.
2. **Follow the console command to its code.** In `Terminal.InitTerminal`, `ldstr "kick"` is followed by `ldftn` of the lambda that handles it (`<>c::<InitTerminal>b__7_11`). The `ban` lambda (`b__7_12`) is the same code but calls `ZNet::Ban`. Several nested types are called `<>c`, so the reader scans all of them.
3. **Read the ZNet code.** `Kick` and `Ban` have the same signature. `Ban` goes to `InternalBan`, which finds the peer, stores `peer.m_socket.GetHostName()` in `m_bannedList` and prints `Banning user`.
4. **Patch idea:** change the token of the `callvirt` in the kick lambda from `ZNet::Kick` (MethodDef 0x060011DA) to `ZNet::Ban` (0x060011E0). Same stack shape, so no other byte changes.
5. **Find every caller of `ZNet::Kick`, not just the one that was asked about.** A scan of all method bodies for the token found a second caller after the first test (see below).

Byte-level tips that were useful: `ldstr` is `72 <token>`, `ldftn` is `FE 06 <token>`, `callvirt` is `6F <token>`. Tokens are little-endian: MethodDef `0x06xxxxxx`, user string `0x70xxxxxx`.

## What the first test missed

The first in-game kick was done with the Escape > players list > Kick button, not the console. It disconnected the player but wrote nothing to `bannedlist.txt`.

- **Why:** the button runs `SessionPlayerListEntry.OnKick`, which opens a confirm popup whose "yes" lambda (`<OnKick>b__112_0`) calls `ZNet.Kick` directly. The console patch did not touch it.
- **How it showed:** the log had `Kicking <name>` (written by `InternalKick`) and no ban, and the file date did not move.
- **Fix:** the script now patches both call sites. Both are found by meaning, never by offset.

The planning session had read the console path only. Lesson 1 below.

## Gaps checked

| Question | Answer from the code and tests | Effect |
|---|---|---|
| Does hosting from the game run the ban check? | `ZNet.Update` > `IsServer` > `UpdateBanList` every 5 s | works without a dedicated server |
| Does the game still start with a changed DLL under Smart App Control? | yes: Unity's Mono reads managed DLLs itself; the game started and ran normally | not a blocker |
| When is the list written? | `SyncedList.Add` reloads the file, skips duplicates, adds, saves at once | file changes on the kick, not on quit; kicking the same name twice adds nothing |
| Which file? | `Utils.GetSaveDataPath() + "/bannedlist.txt"` | the existing file under `LocalLow\IronGate\Valheim` |
| How is the name matched? | `GetPeerByPlayerName`: exact `==`, case-sensitive, ready peers only | a misspelt or offline name is stored as typed: a junk line, nobody kicked. An ID after `Banning user` means it worked |
| Names with spaces? | the console handlers take only `args[1]` | already broken in vanilla; use the players-list button or the platform ID from `Player.log` |
| Is a "modded" flag set? | no `*odded*` member in `Game` | nothing to handle |
| Another admin's kick? | goes through `RPC_Kick` on the host: plain kick | fine while the host is the only admin |
| Connection icon / `PlayFab ... code 4098` in the log after a kick? | also present after the earlier plain kick | normal aftermath of any kick, not caused by the patch |

## Verification

Evidence from the test on 2026-10-22 (player name `Curly`):

- **Console `kick Curly`:** a line appeared in `bannedlist.txt` at 22:01:23 and `Kicking Curly` was logged at 22:01:27. The banned ID matched the player's entry in the log's player history, and was not in `adminlist.txt` or `permittedlist.txt`.
- **Players-list button:** a line appeared at 22:59:15 and `Kicking Curly` was logged at 22:59:17.
- **Not tested in game:** that a banned player's rejoin is refused, and that the in-game `unban <id>` lets them back in. Both are vanilla `ZNet` behaviour that the patch does not touch.
- Both unbans after the tests were done by restoring `bannedlist.txt` from its backup with the game closed. `bannedlist.txt` is again the comment line only.

## Change procedure (what was run)

1. Game closed (`tasklist` shows no `valheim.exe`). The script refuses to write while it runs.
2. `sha256` and the bytes at the call site matched the known original (SHA256 `94998885…4f3a`, 2,575,360 bytes) before the change.
3. Backups to `D:\ValheimBackups\2026-10-22-kick-ban\`: `assembly_valheim.dll.orig` (hash verified) plus the three list files.
4. `node scripts/valheim/kick-ban-patch.js apply`. It writes the 4-byte `ZNet::Ban` token at each unpatched site, re-reads the file and checks that only those windows changed.
5. `node scripts/valheim/kick-ban-patch.js check` reports `patched`.

Before the real run, `apply` and `undo` were rehearsed on copies of the DLL: from the original, from the one-site state, and the refusal when a one-site DLL has no matching backup. (Rehearsal on a copy needs `--dll <copy>` plus `VALHEIM_ALLOW_RUNNING=1` and `VALHEIM_BACKUP_ROOT=<folder>`.)

## Undo

With the game closed:

```bash
node scripts/valheim/kick-ban-patch.js undo
```

It restores the backup only if it is the same game version: the current DLL with the patch windows put back must equal the backup byte for byte. Otherwise it tells you to use Steam: Valheim > Properties > Installed Files > Verify integrity of game files.

## After a Steam update

A game update or "Verify integrity" puts the original DLL back, and `kick` silently becomes a plain kick again. After any Valheim update run:

```bash
node scripts/valheim/kick-ban-patch.js check
```

- `patched`: nothing to do.
- `original` or `partial`: close the game and run `apply`. The script finds the handlers by meaning, so a moved offset is fine. If the backup is for an older version, `apply` makes a new dated backup folder from the new original.
- `unknown`: the code changed shape. Stop and re-read the IL with `node scripts/valheim/lib/il-reader.js <dll> <TypeName> <methodRegex>`. Do not force the patch.

Another machine that hosts needs the same script run there. The patch covers only the PC it was applied on.

## Lessons

1. **Find every caller before you promise a behaviour.** The plan covered the console `kick`; the user's real flow was a button that calls the same method from somewhere else. Scanning all method bodies for the callee token took seconds and would have found both on day one.
2. **Test the flow the person actually uses.** The first test used a different path from the one the plan covered, and the log line (`Kicking` without `Banning`) gave it away.
3. **Find code by meaning, not by offset.** Anchoring on the command string and the callee name keeps the tool useful after an update.
4. **A reusable `check` beats a one-off patch.** A Steam update silently reverts the file; a one-line `check` makes that visible.
5. **Rehearse on a copy.** `apply`, `undo` and the refusal paths were tested on throwaway copies before the real DLL was touched.
6. **Back up before editing a game file, and keep the list files too.** The test adds a real entry to the ban list; the backup made the cleanup a plain copy.

## Open options (not done)

- Pick the player without typing the name (Tab completion or a numbered list): needs a BepInEx plugin, so it is blocked by Smart App Control for now.
- Keep a plain "disconnect only" kick next to the ban: would need a new console command, which means a plugin or a larger IL change.
