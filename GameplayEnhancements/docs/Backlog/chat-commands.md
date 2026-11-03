---
description: backlog card chat-commands (approved 2026-11-03) - a Chat commands group, /Intro sends two shouts
---
# chat-commands: a Chat commands group, /Intro sends two shouts

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

## Test list (not run: the game was not running on 2026-11-03, 19:47-19:57)

Built `HomeCheat.Cmd.chat.v20261103194832.dll` (staged in `BepInEx/homecheat-commands/`), selftest 96/0, spell 10.1 real in the game's `spells.txt`. World `ClaudeTest`, Claude2 joined.

1. `op=cheatstate` (before).
2. `op=reload`: the answer names `HomeCheat.Cmd.chat.v20261103194832.dll`. `op=spells&reload=1`: 0 bad lines. `op=help&cmd=chat`: the Usage.
3. `op=chat&list=1` -> `chat: 1 section(s): intro (2)`.
4. `op=chat&msg=nope` -> `ERROR: no section [nope] in chat.txt ...`.
5. G > 10 Chat commands > 10.1 Intro -> `chat: [intro] 2 line(s), 2 shout(s), 1.5 s apart`; LogOutput.log: `line 1 sent as Shout`, `line 2 sent as Shout`, no HomeCheat errors. Ask Maksym: both shouts in order on this PC and on Claude2? About 1.5 s apart?
6. `op=cheatstate` (after), compare.

Kick=ban rejoin test (AdminPanelEnchancements, `check` = patched): Claude2 joins ClaudeTest; Escape > players list > Kick Claude2 -> `Banning user` in `Player.log` and one new line in `bannedlist.txt`; Claude2's rejoin refused; F5 `unban <id>` -> Claude2 joins again; then `bannedlist.txt` back to the comment line only. Record in `AdminPanelEnchancements/docs/kick-bans-lessons-learned.md`, no ids.

## Test log

| Time | Command | Answer (first line) | Observation | Verdict |
|---|---|---|---|---|
