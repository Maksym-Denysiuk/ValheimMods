---
description: spell bar of the HomeCheat Valheim plugin (2026-10-24) - G opens the game's own radial menu with our spell groups, slot 1 is a Hotbar of 10 cells (Shift+1..5, F1..F5) shown as an on-screen bar, Shift+click places a spell, Shift+RMB empties a cell; spell files, placeholder mode, patches, tests, limits and undo
---
# spell bar (HomeCheat command `spells`)

Built 2026-10-24 on Valheim 1.0.20, BepInEx 5.4.2351, host `MDENYSIUK-PERS`, world `ClaudeTest`. Own game and own hosted world only. Source: [src/Features/Spells](../../src/Features/Spells/) (the bar) and [src/Features/Heal](../../src/Features/Heal/) (`heal`, `cleanse`, `bless`, slot 5 of the real spell set). Full design, slot map and open questions: [spellbar-design.md](spellbar-design.md).

## What it does

- **G** opens the game's own radial menu (the game's `Valheim.UI` classes, so animation, input blocking, Back and closing stay vanilla) filled with our groups. **Shift+G** is the vanilla radial, **T** the emotes.
- Sections: **1 Hotbar** (the 10 cells), 2 Attack, 3 Protect, 4 Fun, 5 Heal and bless, 6 Build, 7 Field, 8 Repair. A group's spells are its sections; sticky spells keep the radial open.
- **Bar on screen**: one row at the bottom showing only the filled cells (packed, centred; empty cells are hidden): 1-5 fire on Shift+1..5, F1-F5 on the F keys. A cell shows the spell icon (the icon of its group, spells have no own icon yet) and its name under it. Scale constant `PanelScale` (0.70).
- **Hotbar section** (radial slot 1): the same 10 cells. Click casts. **Shift+click** on a spell in any group picks it up and opens the Hotbar; a click on a cell there places it (a spell sits in one cell, it moves). **Shift+RMB** on a filled cell empties it (plain RMB is the radial's Back; Back is suppressed while Shift is held).
- Placements are saved next to the spell file as `<file>.pins` (`S1=2.1`, `F3=5.1`, one line per cell). `op=spells&pins=reset` forgets them.

## Use

Load with `op=reload`, then `op=spells&enabled=1` (saved in `spells.state`). Commands: `status=1`, `open=1`, `reload=1`, `list=1`, `reset=1` (rewrite the active file with the defaults, old one `.bak`), `pins=reset`, `pin=F1&spell=7.2` (this session only), `placeholder=1|0`.

**Placeholder mode (default ON)**: the bar reads `BepInEx/config/homecheat/spells-ui-test.txt`, every spell is `@dummy`, a cast only shows a toast and a log line. `placeholder=0` switches to the real `spells.txt` (needs the command DLLs; `op=bless&list=1` lists blessing names).

Spell file, one spell per line: `slot.entry | label | command | flags`. A slot alone with two fields is a group title. Flags: `sticky`, `pin=F1..F5|S1..S5` (S = Shift+digit), `shift=<form added when Shift is held while casting>`. Casting from the bar cells 1-5 never adds the shift variant (Shift is part of their key).

## How it works

- Two Harmony prefixes: `RadialBase.Open(config, back)` swaps the vanilla main config (`ValheimRadialConfig`) for ours when spells are on and Shift is not held; `Player.UseHotbarItem` is blocked in the frame a Shift+digit cell fired, so no weapon switch.
- Radial classes: `SpellRootConfig`, `SpellHotbarConfig`, `SpellGroupConfig`, `SpellLeafConfig` implement `IRadialConfig`; elements are the game's `GroupElement` prefab with replaced `Interact` / `CloseOnInteract`.
- The bar is a MonoBehaviour drawn with IMGUI (`OnGUI`), `[DefaultExecutionOrder(-1000)]`, input read in `Update` through `ZInput`.
- Each real cast takes a read-only snapshot of the cheat flags (`Terminal.m_cheat`, `PlayerProfile.m_usedCheats`, god/ghost/fly) before and after; a change switches spells off.
- A radial that still holds a config of a replaced build (after `op=reload`) opens the vanilla radial instead of failing; any other build failure switches spells off and saves `enabled=0`.

## Tests (2026-10-24, placeholder mode, ClaudeTest)

Seen working in game: radial with groups and Hotbar, pins firing with toasts, Shift+click placing a spell into a cell, cells saved to `.pins` (confirmed by Maksym). Built but not yet confirmed in game: Shift+RMB emptying a cell and the empty look. Not verified: real (non-placeholder) casts through the bar, gamepad, the 10-section radial with other layouts.

## Start-up fix (2026-10-25)

With `spells.state` at `enabled=1`, the two Harmony patches (`RadialBase.Open`, `Player.UseHotbarItem`) used to be applied while the plugin loaded. That froze the main-scene load at "Unloading 3 Unused Serialized files" (verified: with `enabled=0` the same build reached the menu). The patches now wait until a world is loaded (`SpellPatches.EnsureApplied`, log line `spells: world loaded, patches applied`); before 2026-10-25 they were only ever applied by `op=reload` inside a world, which is why the hang never showed.

Spell file changes the same day: 3.2 Stone circle is `op=earthquake&damage=1` with `shift=mode=strong`; 7.4 Place wall uses the real prefab name `woodwall` (`wood_wall` does not exist, so the entry never worked).

## Files and limits

- Files: `BepInEx/homecheat-commands/HomeCheat.Cmd.spells.v<time>.dll` (build line in the header of the source), config in `BepInEx/config/homecheat/` (`spells-ui-test.txt`, `spells.txt`, `spells.state`, `*.pins`).
- The core fixes of the plugin review are not applied (core is owned by another session).
- Undo: `op=spells&enabled=0` removes the patches and hides the bar; delete the DLL from `homecheat-commands/` and `op=reload` (or restart) to remove the command.
