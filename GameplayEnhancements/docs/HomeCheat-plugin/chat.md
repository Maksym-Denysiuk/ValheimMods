---
description: chat command of the HomeCheat Valheim plugin (2026-11-03) - sends prepared chat lines from BepInEx/config/homecheat/chat.txt, one [section] per cast, 1.5 s apart; a line starting "/s " is a shout, others normal chat; spell bar group 10 "Chat commands", 10.1 Intro sends the two server intro shouts; list=1 lists the sections; caps 10 lines per section, 250 characters per line
---
# chat (prepared chat lines, feature Chat)

Built 2026-11-03 (session hc-chat, backlog plan 2026-11-03 item 9) against the game as updated on 2026-10-28, BepInEx 5.4.2351. Own game and own hosted world only. Source: [src/Features/Chat/ChatCommand.cs](../../src/Features/Chat/ChatCommand.cs), DLL `HomeCheat.Cmd.chat.v<stamp>.dll`.

## Goal

Say the same things to people who join, without typing them. Before: Maksym types the server rules into chat by hand each time. After: G menu > 10 Chat commands > 10.1 Intro sends the two intro shouts in order; a new message set is a new section in `chat.txt`, no build.

## Card

- Verdict: **new** command `chat` in a new feature `Chat` (no command sends chat).
- Fingerprint: effect system (message) / target everyone in chat range (shout: everyone in the world) / placement none / mechanism `Chat.SendText(Talker.Type, string)`.
- Targets: no character is touched. The lines go out as the local player's own chat, the same path as typing them.
- Persists in the world: nothing. Who sees it: every player, through the game's own chat RPCs.
- Probe (offline, `il-reader.js`, this game version): `Chat.SendText(Talker.Type type, string text)` is an instance method on `Chat.instance`. A `Shout` with a local player goes through `CheckPermissionsAndSendChatMessageRPCsAsync` (the game's own platform permission check before the RPC); `Normal` goes through `Talker.Say`.
- Caps: 10 lines per section (`Caps.Clamp`, the answer says so), 250 characters per line (cut, the answer counts the cut lines).
- Guards: `Host.RequireWorld()`, the local player alive, `Chat.instance` present.
- Kick=ban is not part of this feature: it stays the IL patch of the game's own kick in `AdminPanelEnchancements` ([kick-bans-lessons-learned.md](../../../AdminPanelEnchancements/docs/kick-bans-lessons-learned.md)).

## Use

`op=chat&msg=<section>`, `op=chat&list=1`

| Parameter | Default | Meaning |
|---|---|---|
| `msg` | required | the section name in `chat.txt` (case does not matter) |
| `list` | 0 | `1`: lists the sections and their line counts, sends nothing |

Spell bar: `10 | Chat commands` (title), `10.1 | Intro | op=chat&msg=intro`.

Answers (first line): `chat: [intro] 2 line(s), 2 shout(s), 1.5 s apart`; with the caps, `; lines capped at 10 (asked 12)` and `; 1 line(s) cut at 250 characters`. Each sent line is logged in `BepInEx/LogOutput.log` as `chat: [intro] line <n> sent as Shout`.

## chat.txt

`BepInEx/config/homecheat/chat.txt`, read at every cast (edit it while the game runs). `install.ps1` puts the sample there only when the file is missing; an existing file is the player's and is kept.

```
# comment
[intro]
/s first shout
/s second shout
[rules]
a normal chat line
```

- `[name]` starts a section; every non-empty, non-comment line under it is one message, in file order.
- `/s ` at the start: sent as a shout (the prefix is dropped); any other line: normal chat.
- Lines before the first section are ignored.

The shipped `[intro]` is Maksym's text, verbatim (typos included, his choice on 2026-11-03).

## How it works

1. `Host.RequireWorld()`, reads `chat.txt` (missing file: an error with the path).
2. `list=1`: answers with the sections; nothing else happens.
3. Finds the section, clamps it to 10 lines, then schedules line `i` with `Later(i * 1.5 s)`: the first goes at once, the rest 1.5 s apart, so the order is kept. Each timer checks `Chat.instance` and the local player again and calls `Chat.instance.SendText(type, text)`.
4. The timers belong to the command: leaving the world or `op=reload` cancels the lines not sent yet.

Nothing is patched, no file is written.

## Verification

Verified in game 2026-11-04. Self-check grep for cheat members: nothing. The test list is in the card below.

| Time | Command | Answer (first line) | Observation | Verdict |
|---|---|---|---|---|
| 2026-11-04 | spell 10.1 Intro | `chat: [intro] 2 line(s), 2 shout(s), 1.5 s apart` | Maksym: ok | pass |

## Limits

- The shout goes through the game's platform permission check: a player whose platform blocks chat for the sender (crossplay text-chat settings) may not see it.
- The text is sent as typed in the file; there is no variable substitution.

## Undo

Nothing to undo: chat is not saved. To remove the command: delete `HomeCheat.Cmd.chat.*.dll` from `BepInEx/homecheat-commands/` and `op=reload`; `chat.txt` can stay.

## Card: chat-commands (approved 2026-11-03)

Status: built 2026-11-03 (hc-chat), in-game test pending; page [chat.md](../HomeCheat-plugin/chat.md) · Verdict: new `chat` in a new feature Chat · Nearest: none
Fingerprint: system (message) / everyone in chat / none / `Chat.SendText`

- `op=chat&msg=intro` sends each line of `[intro]` in `BepInEx/config/homecheat/chat.txt`, 1.5 s apart; a line starting `/s ` is sent as a shout, others as normal chat. G menu (spell bar) group 10 "Chat commands", 10.1 Intro. `chat&list=1` lists sections. New messages = new sections, no build.
- `chat.txt`, verbatim (Maksym):
  ```
  [intro]
  /s Hi, that is my local server, it makes backups each 3 minutes so Im OK to do a restore any time. It stores enough backups to make me able to rollback anything within 24h
  /s Im ok if you use cheats. But keep in mind that players here just having Vanil;a-like fun. If you want some PvP - just tell
  ```
- Kick=ban stays the IL patch in AdminPanelEnchancements (the game's own kick), not part of Chat.
- Guards: `Host.RequireWorld`; caps 10 lines per section, 250 characters per line.
- Probes: `Chat.SendText(Talker.Type, string)` in this game version.
- Docs: new page `HomeCheat-plugin/chat.md`, index, catalog, payload `chat.txt`, `install.ps1` copies it when missing.
- Test: cast 10.1 → two shouts in order; Claude2 sees both.

### Test list (not run: the game was not running on 2026-11-03, 19:47-19:57)

Built `HomeCheat.Cmd.chat.v20261103194832.dll` (staged in `BepInEx/homecheat-commands/`), selftest 96/0, spell 10.1 real in the game's `spells.txt`. World `ClaudeTest`, Claude2 joined.

1. `op=cheatstate` (before).
2. `op=reload`: the answer names `HomeCheat.Cmd.chat.v20261103194832.dll`. `op=spells&reload=1`: 0 bad lines. `op=help&cmd=chat`: the Usage.
3. `op=chat&list=1` -> `chat: 1 section(s): intro (2)`.
4. `op=chat&msg=nope` -> `ERROR: no section [nope] in chat.txt ...`.
5. G > 10 Chat commands > 10.1 Intro -> `chat: [intro] 2 line(s), 2 shout(s), 1.5 s apart`; LogOutput.log: `line 1 sent as Shout`, `line 2 sent as Shout`, no HomeCheat errors. Ask Maksym: both shouts in order on this PC and on Claude2? About 1.5 s apart?
6. `op=cheatstate` (after), compare.
