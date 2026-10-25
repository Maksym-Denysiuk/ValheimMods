---
description: spell bar of the HomeCheat Valheim plugin (2026-10-24, spell file v4 and in-game test 2026-10-25) - G opens the game's own radial with sections and sub-sections, slot 1 Hotbar = Panel 1-5 and Panel 6-0 (20 cells, a two-row bar F1-F10 and Shift+1..0), Shift versions on Shift+F, one form screen for a spell with questions, failures-only messages; spell file v4 format and form flags, patches, tests, limits and undo
---
# spell bar (HomeCheat command `spells`)

Built 2026-10-24 on Valheim 1.0.20, BepInEx 5.4.2351, host `MDENYSIUK-PERS`, world `ClaudeTest`. Own game and own hosted world only. Source: [src/Features/Spells](../../src/Features/Spells/) (the bar) and [src/Features/Heal](../../src/Features/Heal/) (`heal`, `cleanse`, `bless`, slot 5 of the real spell set). Full design, slot map and open questions: [spellbar-design.md](spellbar-design.md).

## What it does

- **G** opens the game's own radial menu (the game's `Valheim.UI` classes, so animation, input blocking, Back and closing stay vanilla) filled with our groups. **Shift+G** is the vanilla radial, **T** the emotes.
- Sections (spell file v4, 2026-10-25): **1 Hotbar** (the 10 cells), 2 Attack, 4 Fun, 5 Heal, defence, bless, 6 Build (Flatten..., Coordinates, Paths, Repair and refill, Ward, Construct, Despawn), 7 Field, 8 Spawn... (3 Protect and Lights removed 2026-10-25: its one entry, the ward menu, is Build > Ward). Plan each level to fit one ring (8 entries is safe, `MaxElementsRange` [8,12]): more entries go into sub-groups. A section's entries are spells or sub-sections, at any depth; a level with more entries than the game's per-layer limit continues as a **spiral** (the emote wheel's layout). Sticky spells keep the radial on their level.
- **Questions** (answer chains): a spell with `ask=` flags opens one radial level per question (spawn, plant food, plant trees). Pick an answer to go on, right click for the previous question, close the radial to cancel; the last answer casts.
- **Bar on screen**: one row at the bottom showing only the filled cells (packed, centred; empty cells are hidden): 1-5 fire on Shift+1..5, F1-F5 on the F keys. A cell shows the spell icon (the icon of its group, spells have no own icon yet) and its name under it. Scale constant `PanelScale` (0.70).
- **Hotbar section** (radial slot 1): the same 10 cells. Click casts. **Shift+click** on a spell in any group picks it up and opens the Hotbar; a click on a cell there places it (a spell sits in one cell, it moves). **Shift+RMB** on a filled cell empties it (plain RMB is the radial's Back; Back is suppressed while Shift is held).
- Placements are saved next to the spell file as `<file>.pins` (`S1=2.1`, `F3=5.1`, one line per cell). `op=spells&pins=reset` forgets them.

## Use

Load with `op=reload`, then `op=spells&enabled=1` (saved in `spells.state`). Commands: `status=1` (also the radial's `MaxElementsRange`), `open=1`, `reload=1` (the answer counts spells, titles, pins and bad lines with their reasons), `list=1` (the tree), `reset=1` (rewrite the active file with the defaults, old one `.bak`), `pins=reset`, `pin=F1&spell=6.5.3` (this session only), `placeholder=1|0`.

**Placeholder mode (default ON)**: the bar reads `BepInEx/config/homecheat/spells-ui-test.txt`, every spell is `@dummy`, a cast only shows a toast and a log line (with the answers of its questions). `placeholder=0` switches to the real `spells.txt` (needs the command DLLs; `op=bless&list=1` lists blessing names).

## Spell file v4

One entry per line: `id | label | command | flags`. Lines starting with `#` are comments.

| Part | Rule |
|---|---|
| id | dotted numbers of any depth: `2`, `2.10`, `6.1.3` (each 1-999, depth 6 at most). `1` is the Hotbar and cannot be used. Siblings are ordered by their last number; any number of entries per level |
| title | a group or sub-section: `6.1 \| Flatten`, or with flags `6.1 \| Flatten \| \| sticky` (empty command). Its entries are the ids below it (`6.1.1`, `6.1.2` ...). A title with nothing below it shows "(nothing here yet)" |
| spell | `id \| label \| command [\| flags]`. Command = the form of an inbox file (`op=...&k=v`); `;;` chains commands; `@dummy` only shows a toast (used for "(soon)" lines whose command is built later, the real form sits in a comment above) |
| `sticky` | the radial stays on its level after the cast. On a title: every spell below it |
| `pin=` | `F1..F5` or `S1..S5` (S = Shift+digit): the spell's bar cell when there is no `.pins` file |
| `shift=` | a form added when Shift is held while casting from the radial or an F cell. Cells 1-5 never add it (Shift is part of their key) |
| `ask=<key>:<answers>` | one question; several `ask=` flags are asked in order. Answers `Label=value` or `value`, comma separated, `_` in a label shows as a space: `ask=area:Start-end=coords,This_patch=patch`. `{key}` in the command (and in `shift=`) takes the answer, URL-escaped |
| dynamic answers | `ask=player:@players` (players online; others only when this PC hosts), `@crops` (`Pickable_*` crops that grow from a plant), `@trees` (`*_Sapling` prefabs), `@prefabs` (categories: Creatures, then Items by item type). Read from the game when the level opens, sorted by label |

A line that breaks a rule is skipped and named in the `reload=1` answer (`line 12 (pin F9 is not one of ...)`): an id under a spell (make it a title), an id used twice, a `{key}` with no `ask=key`, an unknown source, a pin, shift or ask flag on a title.

**Long levels.** A level hands all its entries to the game's `RadialBase.ConstructRadial`; past `MaxElementsPerLayer` (picked from `RadialDataSO.MaxElementsRange`) the game adds layers: moving round the circle continues into the next layer, as on the emote wheel. Question levels split at 40 answers into alphabetical blocks named by their first and last answer ("Abomination - Bat"); `@prefabs` first shows its categories.

**Questions in the radial.** Each answer is a leaf: the click stores it and opens the next question with the current level on the game's Back stack, so right click returns to the previous question (pick again to change the answer). The last answer casts. Closing the radial drops the answers. A pinned spell with questions opens the radial on its first question; Shift+click places it on the bar like any spell.

## How it works

- Two Harmony prefixes: `RadialBase.Open(config, back)` swaps the vanilla main config (`ValheimRadialConfig`) for ours when spells are on and Shift is not held; `Player.UseHotbarItem` is blocked in the frame a Shift+digit cell fired, so no weapon switch.
- Radial classes: `SpellRootConfig`, `SpellHotbarConfig`, `SpellGroupConfig` (a title at any depth, by id), `SpellAskConfig` (one question), `SpellLeafConfig` implement `IRadialConfig`; elements are the game's `GroupElement` prefab with replaced `Interact` / `CloseOnInteract`.
- Data: `SpellParse` (the v4 parser, no Unity reference, so it can be checked outside the game), `SpellNode` (tree), `Spell`, `SpellAsk`, `SpellAskOption`, `SpellAskChain` (the answers of one run), `SpellAskSources` (the dynamic answers, read only).
- The bar is a MonoBehaviour drawn with IMGUI (`OnGUI`), `[DefaultExecutionOrder(-1000)]`, input read in `Update` through `ZInput`.
- Each real cast takes a read-only snapshot of the cheat flags (`Terminal.m_cheat`, `PlayerProfile.m_usedCheats`, god/ghost/fly) before and after; a change switches spells off.
- A radial that still holds a config of a replaced build (after `op=reload`) opens the vanilla radial instead of failing; any other build failure switches spells off and saves `enabled=0`.

## Tests (2026-10-24, placeholder mode, ClaudeTest)

Seen working in game: radial with groups and Hotbar, pins firing with toasts, Shift+click placing a spell into a cell, cells saved to `.pins` (confirmed by Maksym). Built but not yet confirmed in game: Shift+RMB emptying a cell and the empty look. Not verified: real (non-placeholder) casts through the bar, gamepad, the 10-section radial with other layouts.

## Spell file v4 build (2026-10-25, not run in game)

Built without the game in phase 4 wave 0 (design and assumptions: [spellbar-design.md](spellbar-design.md) section 15). Verified offline: the feature builds (`bash src/build.sh spells`), `bash src/build.sh selftest` passes, and the parser compiled into a console check reads `spells.txt` and `spells-ui-test.txt` with 0 bad lines (78 spells, 14 titles, 9 pins each) and rejects 10 of 10 broken sample lines. Status B: run the test list below once in game.

Test list for Maksym (ClaudeTest, `op=cheatstate` before and after):
1. `op=reload` (names `HomeCheat.Cmd.spells.v<stamp>.dll`), `op=spells&reload=1`: `78 spells, 14 titles, 9 pins; 0 bad lines`.
2. `op=spells&status=1`: note the `MaxElementsRange` values (probe).
3. `op=spells&list=1`: the tree of 2-8 with 3.5, 6.1-6.6 as sub-sections.
4. G: Hotbar + 7 sections. Attack (12 entries) and Fun (12) continue as a spiral when 12 is past the layer limit; going round reaches 2.12.
5. Build > Flatten: a "(soon)" cast keeps the radial on Flatten (sticky title). Build > Repair and refill: same.
6. Build > Spawn > Spawn...: categories (Creatures, Items: ...), a category over 40 opens alphabetical blocks; then count, yaw, pitch, player; right click goes back one question; the last answer shows `placeholder: Spawn... (6.4.1) answers: prefab=... count=...`. Close the radial halfway: nothing is cast.
7. Field > Plant food...: area, then crops from `@crops`; Plant trees...: area, trees, size.
8. 8 Construct: one "(nothing here yet)" entry.
9. Shift+click Plant food... and place it on a free cell; press that key: the radial opens on the area question.
10. `op=spells&placeholder=0`, then 5.10 Remove all effects and 2.6 Slime ring with a goblin (`op=spawn&prefab=Goblin&count=1`): real casts through the new tree; `op=killmobs` after.

## Start-up fix (2026-10-25)

With `spells.state` at `enabled=1`, the two Harmony patches (`RadialBase.Open`, `Player.UseHotbarItem`) used to be applied while the plugin loaded. That froze the main-scene load at "Unloading 3 Unused Serialized files" (verified: with `enabled=0` the same build reached the menu). The patches now wait until a world is loaded (`SpellPatches.EnsureApplied`, log line `spells: world loaded, patches applied`); before 2026-10-25 they were only ever applied by `op=reload` inside a world, which is why the hang never showed.

Spell file changes the same day: 3.2 Stone circle is `op=earthquake&damage=1` with `shift=mode=strong`; 7.4 Place wall uses the real prefab name `woodwall` (`wood_wall` does not exist, so the entry never worked).

## Files and limits

- Files: `BepInEx/homecheat-commands/HomeCheat.Cmd.spells.v<time>.dll` (build line in the header of the source), config in `BepInEx/config/homecheat/` (`spells-ui-test.txt`, `spells.txt`, `spells.state`, `*.pins`).
- The core fixes of the plugin review are not applied (core is owned by another session).
- Undo: `op=spells&enabled=0` removes the patches and hides the bar; delete the DLL from `homecheat-commands/` and `op=reload` (or restart) to remove the command.

## In-game test 2026-10-25

Maksym's test pass of the phase 4 commands, world `ClaudeTest` hosted on this PC, Claude2 (no mod) as the second player. Real casts (placeholder mode off) through the whole tree. Result: **V** for the casts, Panel 6-0, and the form screens of Spawn..., Plant food..., Plant trees..., Flatten..., Place mine field.

What changed (Maksym's requests, built and loaded the same day):
- **Hotbar = two panels.** Radial slot 1 opens Panel 1-5 (Shift+1..5, F1..F5) and Panel 6-0 (Shift+6..9, Shift+0, F6..F10): 20 cells, 10 per panel, so each panel fits one ring (the game's ring holds 8-12 entries, `MaxElementsRange` [8,12]; the first 14-cell panel spiralled). The on-screen bar has two rows of 10: F1-F10 above, Shift+1..0 below. Pins `pin=F1..F10|S1..S10`. Minus, Equals, F11 and F12 are free again (F11 is the stats window of `keybinds`). **V** ("panel works").
- **Sections regrouped** into sub-groups instead of long spirals: 2 Attack (2.1 Arrows ... 2.7 Mines), 3 Protect and Lights (3.5 Shield, one ward-menu entry; Push away, Strong push, Stone circle and the duplicate Slime moat removed), 4 Fun (Transform, Trees, Ship, Reset sky), 5 Heal (Heal, Cleanse, Blessings, Remove all effects), 6 Build (6.1 Flatten..., 6.2 Coordinates, 6.3 Paths, 6.5 Repair and refill, 6.6 Shield, 6.7 Construct (empty), 6.8 Despawn), 7 Field (7.1 Cultivate..., 7.2 Plant food..., 7.3 Harvest patch, 7.4 Plant trees...), 8 Spawn... (the old Construct section's place; Construct moved under Build). Every attack line passes `pvp=1`.
- **Shift versions.** `shift=@<id>` (Shift casts that spell) and `shiftlabel=<name>`: Shift+F on an F cell casts the Shift version; the cell keeps a small box under it (no text, Maksym); `list=1` and the radial subtitle name it.
- **One form screen per spell with questions** (`SpellForm`, like the hammer's build menu; the radial answer chain `SpellAskConfig` is gone): fixed answers as buttons (the first, or the one marked `*`, picked), `@players` as names, `@crops` / `@trees` / `@prefabs` as an icon grid (`@prefabs` with a search line and categories, including "Constructions: <category>" from every tool's piece table), Cast or Enter, Cancel or Esc. Flags: `ask=key/Label_text`, `ask=key>other` (only answers above the other answer; the rest greyed), `ask=key?other=value` (only while that answer is chosen, else greyed and left out of the command), value `@off` (a greyed answer, "(soon)"), `button=<text>`. An area answer (`coords`, `patch`) greys `count`, `rows`, `perrow`, `radius`. While the form is open the game sees a text input: free cursor, the player stands still.
- **Messages: failures only.** The bar's own toast is gone; a failure (an unanswered question, a changed cheat flag, a command `ERROR`) shows in the screen centre in the game's style (`Kit.Notify`; the wrap at 50 characters a line and 70 % size is built and installs at the next core restart); success, pin hints and placeholders go only to `LogOutput.log`. Built 17:32: B.

Spell file header: the game copy and `SpellFile.DefaultText` do not describe the form flags yet (Known defects of the architecture doc); this page is the reference until the spell-file owner's pass.

Still untested (B): the failures-only messages; Shift strips; `spells-ui-test.txt` against the new tree; a world exit and back with the bar (T-core 4).
