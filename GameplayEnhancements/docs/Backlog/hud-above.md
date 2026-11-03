---
description: backlog card hud-above (approved 2026-11-03) - the game's bars and build requirements stay visible above the spell panels
---
# hud-above: the game's bars and build requirements stay visible above the spell panels

Status: approved (2026-11-03) · Verdict: extend `spells` · Nearest: spells (`SpellBar.cs:551-627` read)
Fingerprint: UI/input / self / screen / Harmony patch on `Hud`

- Problem: the panels draw in `OnGUI` at the bottom centre, over the game's canvas: stamina, eitr and adrenaline bars and the build-piece requirements are hidden.
- Behaviour: while the bar shows, those game elements move up by the panels' height plus a gap; back when it hides, on unload and on world exit. Panels stay put. `hudlift=0` in the spell settings turns it off.
- Probes first: new read-only `op=spells&probe=hud` lists the bottom `Hud` RectTransforms (names, anchors, positions, heights) in this game version.
- Persists: nothing. Who sees it: this PC (HUD).
- Docs: `Spells/` page, architecture doc Feature notes (HUD moved while the bar shows).
- Test: screenshots at 1920x1080 and one other aspect ratio, hammer out (requirements), sprinting (stamina), eitr food; nothing under a panel.

## Test log

| Time | Command | Answer (first line) | Observation | Verdict |
|---|---|---|---|---|
