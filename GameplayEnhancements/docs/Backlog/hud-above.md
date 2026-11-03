---
description: backlog card hud-above (approved 2026-11-03) - the game's bars and build requirements stay visible above the spell panels
---
# hud-above: the game's bars and build requirements stay visible above the spell panels

Status: built 2026-11-03 (hc-menu), not run in game yet: probe and test list below · Verdict: extend `spells` · Nearest: spells (`SpellBar.cs:551-627` read)
Fingerprint: UI/input / self / screen / Harmony patch on `Hud`

- Problem: the panels draw in `OnGUI` at the bottom centre, over the game's canvas: stamina, eitr and adrenaline bars and the build-piece requirements are hidden.
- Behaviour: while the bar shows, those game elements move up by the panels' height plus a gap; back when it hides, on unload and on world exit. Panels stay put. `hudlift=0` in the spell settings turns it off.
- Probes first: new read-only `op=spells&probe=hud` lists the bottom `Hud` RectTransforms (names, anchors, positions, heights) in this game version.
- Persists: nothing. Who sees it: this PC (HUD).
- Docs: `Spells/` page, architecture doc Feature notes (HUD moved while the bar shows).
- Test: screenshots at 1920x1080 and one other aspect ratio, hammer out (requirements), sprinting (stamina), eitr food; nothing under a panel.

## Built (2026-11-03)

- `src/Features/Spells/SpellHudLift.cs` (new): targets read by reflection from `Hud` in this game version (IL read 2026-11-03, the game past 1.0.20): `m_staminaBar2Root`, `m_eitrBarRoot`, `m_adrenalineBarRoot` (one group, one shared lift, so their stacking stays) and the parent of `m_requirementItems[0]` (the build requirements). Each frame in `SpellBar.LateUpdate` (after the game's `Update` and animators): the lift = top of the highest row of panels + 10 px (at 1080p) minus the group's lowest bottom edge where the game has it, in screen pixels through `RectTransformUtility` (any canvas mode), turned into `anchoredPosition` units of each parent. A position the game or an animator changed since our last write becomes the new original, so the lift never stacks and a restore never undoes the game's own move.
- Restore: bar hidden (spells off, no local player, HUD hidden by the user, no pinned row), `hudlift=0`, `OnDestroy` (unload, replaced build); world exit drops the targets with the old `Hud`.
- `op=spells&hudlift=1|0` (saved in `spells.state`, default 1); `op=spells&probe=hud` (read only); `refs.txt` + `UnityEngine.UIModule.dll`.
- Checks: `bash src/build.sh spells` exit 0 (rebuilt 14:01 against hc-core's 13:33 core); cheat-path grep empty.

## Test list (in game, ClaudeTest, `op=cheatstate` before and after)

1. `op=reload` names the new spells DLL; `op=spells&reload=1`: 64 spells, 22 titles, 6 pins, 0 bad lines.
2. `op=spells&probe=hud`: the three bars and the requirements panel listed with screen pixels; targets named.
3. 1920x1080: sprint until stamina shows, eat eitr food, hammer out on a piece with requirements: screenshots, nothing under a panel; `probe=hud` says `lifted`.
4. `op=spells&hudlift=0`: everything back where the game draws it (screenshot); `hudlift=1` again.
5. `op=spells&enabled=0`: restored; `enabled=1`: lifted again.
6. Another aspect ratio (for example 2560x1080 or 1280x1024, windowed): steps 3-4 again.
7. World exit and back: no errors in `LogOutput.log`, lift works in the new world.

## Test log

| Time | Command | Answer (first line) | Observation | Verdict |
|---|---|---|---|---|
