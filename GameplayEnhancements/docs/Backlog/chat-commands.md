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

## Test log

| Time | Command | Answer (first line) | Observation | Verdict |
|---|---|---|---|---|
