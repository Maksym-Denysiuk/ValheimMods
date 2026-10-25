---
description: dome command of the HomeCheat Valheim plugin (2026-10-25) - a protected zone of vanilla wards (guard_stone) from two points plus 10 %, one ward or a group of up to 50 with minimal overlap, placed as if built with the hammer so every player sees the standard ward; one spell-bar entry opens the ward menu (New or Group-N, allowed players from the world's join history, Active / Deactivated / Deleted); new wards that meet your group join it; the host copies on/off and the permitted list across a group; monsters: vanilla ward rules only (trader-style guard researched and parked); card, assumptions, how it works, tests, limits, undo
---
# dome (ward zone, feature Dome)

Built 2026-10-25 in phase 4, wave 2 (session W2-dome) on Valheim 1.0.20, BepInEx 5.4.2351, unattended and without the game; first run in game 2026-10-25 (one ward from the Shield entries); the ward menu, Group-N names and the join history added the same day in an attended session (section Ward menu). Own game and own world only. Source: [src/Features/Dome/](../../src/Features/Dome/), DLL `HomeCheat.Cmd.dome.v<stamp>.dll`. The requirements and the plan card (backlog plan 3.8) are kept privately in HomeOS; this page holds only what the code does.

## Goal

A protected zone like a trader camp, built from what every vanilla player already has: normal wards. Before: a ward is placed by hand and covers its own radius. After `op=dome&from=coords`: the zone between two marked points is covered by one ward at its centre, or by a group of wards without gaps when it is wider than one ward; the group acts as one ward. Other players need no mod: they see normal wards and their game applies the normal ward rules.

This is build order steps 2 and 3 (single ward, then groups with synced on/off and permitted list) plus the ward menu, which replaces the owner panel of step 5 for the permitted list. Parked: the monster guard (step 4, Maksym 2026-10-25: wards are vanilla). Not built: hover text (step 5), extra effects and scale (step 6).

## Ward menu (2026-10-25, attended, session "dome step 4")

Maksym's redesign after the first in-game test: the Shield entries (start, end, place, list, remove) became **one** spell-bar entry, **Protect and Lights > Shield** (3.5) and **Build > Shield** (6.6), both `op=dome&step=menu`. It opens a one-screen window in the look of the mines form:

| Row | Answers | Default |
|---|---|---|
| Wards placed | `New`, then your groups `Group-1` ... `Group-X` (only groups whose wards you created) | New |
| Allowed | you (always: the creator has access, so you are shown ticked and greyed) and every player who has joined this world; for a group, its current list is ticked | you only |
| Status | `Active`, `Deactivated`, `Deleted` (greyed for New) | Active; a group shows its state |

Under Wards placed one line says what the pick means: for New the zone from the coord points (centre, radius + 10 %, how many wards at most, distance) or what is missing; for a group its ward count, centre, state, allowed count and distance. The button reads Place (New), Apply, or Delete (Status Deleted, with a red line naming how many wards go). Enter applies, Esc or Cancel closes; the answer shows as the game's centre text and in `LogOutput.log` (`dome menu: ...`).

- **New**: covers the circle between the coord start and end points plus 10 % (`DomeLayout.Margin` 1.1) with wards, the hexagonal lattice at `Fill` 0.95 (overlaps as small as possible, 5 % of the ward radius left to move a ward off a blocked spot). Lattice spots whose cell your own dome wards already cover are left out. New wards whose circle meets a ward of one of your groups **join** that group (it keeps its on/off; its list gains the players ticked); meeting several groups merges them into the lowest number. Otherwise a new `Group-<max+1>` with the ticked players and the status.
- **A group**: Apply writes the ticked list and Active / Deactivated to every ward of the group; Deleted removes all its wards (no materials back; the Apply click is the confirmation).
- **Players who joined**: the host records every player it sees, per world, in `BepInEx/config/homecheat/dome-players.state` (this PC only, never in the repository): the players online (the host's player list, id from the character's ZDO `ZDOVars.s_playerID`) and every player on a ward's permitted list, at each rescan (every 30 s and every dome command). A player who has never been online while the host ran HomeCheat is not offered yet.
- **Group numbers**: `Group-N`, N saved on every ward of the group (ZDO key `hc_dome_no`); a group built before the numbers got the next free number on the first rescan, in build order.
- `op=dome&step=apply&group=new|<n>[&allow=<name>,<name>][&status=active|deactivated|deleted]` is the window's Apply as a command (tests, scripts): without `allow=` a group keeps its list (New: only you), without `status=` it keeps its state (New: active); an unknown name is refused with the known names.
- **Monsters**: parked by Maksym's decision (2026-10-25, "wards are vanilla"). The research that led there: the trader camp keeps monsters out with an `EffectArea` of type `NoMonsters` that every game registers locally (`EffectArea.Awake`: its collider's bounds, a static list); `MonsterAI.UpdateAI` (untamed only) flees from a target inside the area and flees from the area's centre when itself within the bounds plus 8 m. Only the game that owns a creature runs its AI, and no networked vanilla prefab carries a working `NoMonsters` area (only `piece_shieldgenerator` / `charred_shieldgenerator`, switched off and never switched on by game code), so it cannot reach creatures run by another player's vanilla game. What the wards already do on every game: `guard_stone` carries a `PlayerBase` area of 20 m, and `SpawnSystem.IsSpawnPointGood` skips natural spawns inside one.

The window is drawn on this PC only (an interface, not world graphics, R0 holds). While it is open `TextInput.IsVisible` returns true (postfix in `DomePatches`), so the cursor is free and the player stands still, as with the spell form.

## Card

- Verdict: **new** command `dome` in a new feature `Dome` (catalog worked example "protection dome like the trader").
- Fingerprint: effect place object (protect) / target ground, a zone / placement two points (centre and radius) / mechanism vanilla `PrivateArea` (`guard_stone`) and its ZDO.
- Targets: no character is hit. Other players meet only the vanilla ward rules.
- Persists in the world: the wards (normal world objects, saved with the world) and the group keys in their ZDOs: `hc_dome` (zone id, the ZDO mark), `hc_dome_cx`, `hc_dome_cz`, `hc_dome_r`, `hc_dome_n` (wards in the group), `hc_dome_no` (N of Group-N), `hc_dome_sig`. On this PC only: the join history `BepInEx/config/homecheat/dome-players.state`.
- Who sees it: everyone (networked pieces spawned on the host; vanilla visuals only, nothing drawn on this PC alone).
- Visual reference: the vanilla ward (`guard_stone`), never a copy of the trader dome (ASSUMPTION A1).
- Probes: the ward radius (read live from the prefab, printed in every answer); the rest needs the game (open checks below).
- Caps: zone radius 150 m, 50 wards per dome (both refused above, `Caps.Check`).
- Undo: `op=dome&step=remove&confirm=1` at the dome, or demolish the wards with the hammer.

### Assumptions (unattended run, to review)

| # | Assumption | Why |
|---|---|---|
| A1 | vanilla `guard_stone` visuals (settled in the plan review) | everyone sees the same; a trader-dome copy would show only on this PC |
| A2 | menu: Protect > Shield (3.5.3-3.5.5), mirrored in Build > Shield (6.6.3-6.6.5) (settled; replaced 2026-10-25 by one entry each, the ward menu) | Protect would be empty; a shield protects |
| D1 | the ward radius is the prefab's `PrivateArea.m_radius`, read at run time (the code default is 10 m; the asset sets the real value, about 32 m, to confirm) | no hard-coded radius to go stale |
| D2 | coverage: a hexagonal lattice with spacing sqrt(3) x 0.9 x ward radius (0.95 since 2026-10-25, Maksym: overlaps minimal; the slack is then 5 %); a blocked spot may move up to 10 % of the radius (rings at half and all of the slack, 8 directions) and still leaves no gap; checked offline for ward radius 32 m and 10 m, zone radius 5-150 m, nudged too | each vanilla client checks wards by horizontal distance with the ward's own radius, so the circles must overlap |
| D3 | all or nothing: a spot that cannot take a ward refuses the whole dome and names the spots; `partial=1` places the rest and says how many gaps | a gap is an unprotected hole for other players |
| D4 | "inside another player's ward" = the two ward areas overlap (centres closer than 2 x radius) for any ward whose creator is someone else, on or off | the hammer refuses overlap with enabled foreign wards (`PrivateArea.CheckAccess` with `wardCheck`); counting switched-off ones too is the safer reading |
| D5 | height from the terrain (`ZoneSystem.GetGroundHeight`) at the spot and 8 points at 0.6 m (the base edge): a drop over 0.5 m is refused with the excess; the ward sits 0.05 m below the lowest edge, upright, turned to your facing | never in the air, never tilted; a sunk edge is better than a floating one |
| D6 | "on a building" = a player-built piece within 2.1 m of the spot (`Kit.Constructions`, base plus 1.5 m pivot margin); a tree, rock or other solid object at the base (`Ground.IsBlocked`) refuses too | the requirement names buildings; a ward inside a rock would be useless |
| D7 | a spot whose terrain is not loaded on this PC is refused ("stand nearer the zone") | the height cannot be read there |
| D8 | host only for build, list and remove (`Host.RequireServer`); `step=mark` needs only a world | the wards are shared world data |
| D9 | `step=mark` writes the same points as `op=coord` (start, then end, alternating; `point=` picks one) | one point store (`Kit.Coords`) |
| D10 | a new group takes the first ward's state (on when the prefab is enabled by default) for every ward | one state from the start |
| D11 | sync every 2 s on the host: a ward whose on/off or permitted list differs from the state last copied (`hc_dome_sig`) was changed by a player; its state goes to every ward of the group; two in one tick: the newer ZDO data wins. The host takes each ward's ZDO before writing it | only the host runs HomeCheat; the ZDO is what every client reads |
| D12 | a far ward (not loaded on this PC) changes through its ZDO only: its state follows, but its activate / deactivate effect does not play; a loaded ward goes through the game's own `SetEnabled` | the effect is created where the change runs |
| D13 | `step=remove` without `confirm=1` only says what it would remove; the spell-bar lines carry `confirm=1`; no materials are returned | destructive actions need `confirm=1` (architecture, safety guards); the menu click is the confirmation |
| D14 | `cost=1` takes the ward recipe x the ward count, after checking all of it is there; free by default | the requirement: free by default, `cost=1` takes the normal materials |

## Use

| Form | What it does |
|---|---|
| `op=dome&step=menu` | the ward menu (section Ward menu): the one spell-bar entry |
| `op=dome&step=apply&group=new|<n>[&allow=<names>][&status=active|deactivated|deleted]` | the menu's Apply as a command |
| `op=dome&step=mark` (twice) | stand on a point on one edge, then on the opposite edge; the second answer shows the centre, the radius and how many wards it needs. Same points as `op=coord&set=start` / `end` |
| `op=dome&from=coords` (or `step=build&from=coords`) | builds the dome between the coord start and end points |
| `op=dome&from=x,z&to=x,z` | the same between two typed points (X and Z, the ground plane) |
| `op=dome&step=list` | every dome of the world: id, centre, radius, wards found / placed, on, permitted players, distance |
| `op=dome&step=remove[&confirm=1]` | the dome at the crosshair (a ward of it, or a point inside its zone) with all its wards |

| Parameter | Default | Meaning |
|---|---|---|
| `from` | required (build) | `coords` (the start and end points) or `x,z` |
| `to` | required with `from=x,z` | the point on the opposite edge |
| `cost` | 0 | 1 = take the ward materials from your inventory (all wards at once, refused when short) |
| `partial` | 0 | 1 = skip spots that cannot take a ward (gaps), place the rest |
| `preview` | 0 | 1 = only say where the wards would go and which spots are refused; places nothing |
| `point` | alternates | `step=mark`: `start` or `end` |
| `confirm` | 0 | `step=remove`: 1 = remove; without it the answer only says what would go |

Answers (first line):
- `dome mark start = x,y,z (your feet); next: op=dome&step=mark (the end point, on the opposite edge)`
- `dome mark end = ...; zone centre x,z, radius R m -> N ward(s) of radius r m; build: op=dome&step=build&from=coords`
- `dome d<id>: placed N ward(s) (centre x,z, radius R m, ward radius r m), state on[, cost: ...][; one group: on/off and the permitted list are copied to every ward]`, then one line per ward (`moved 1.6 m from a blocked spot` where it was nudged)
- `preview: N ward(s) would be placed (...)`
- `<k> of <N> ward spot(s) cannot take a ward, nothing placed (...): x,z on water (ground 1.2 m below the water line); x,z on a building: wood_wall at 1.4 m; x,z slope too steep: ...; x,z overlaps another player's ward area (...)`
- `would remove dome d<id> with N ward(s): add confirm=1`, `removed dome d<id>: N ward(s)`
- errors: `coord 'start' is not set ...`, `the two points are 0.4 m apart ...`, `radius=180.00 is over the cap 150.00`, `wards=61 is over the cap 50`, `cost=1: ...`, `host only: ...`, `no dome at the crosshair ...`

Spell bar (`spells.txt`, 2026-10-25): Protect and Lights > Shield 3.5 and Build > Shield 6.6, both `op=dome&step=menu`. Start and end points: Build > Coordinates (`coord`). The former entries 3.5.1-3.5.5 / 6.6.1-6.6.5 (start, end, place, list, remove) are gone; their commands still work typed.

## How it works

Read offline with `scripts/valheim/lib/il-reader.js` (HomeOS) on `assembly_valheim.dll` 1.0.20:

- `PrivateArea.IsInside(point, radius)` = `Utils.DistanceXZ(ward, point) < m_radius + radius`: the ward area is a **vertical column**, horizontal distance only (half of open check 2, answered from the code). Tunnels and building under the zone are inside it.
- `PrivateArea.CheckAccess(point, radius, flash, wardCheck)` walks every loaded ward (`m_allAreas`): an enabled ward whose area holds the point and where the local player has no access blocks the action. Each client runs it on its own game, so it only knows wards loaded there, with their synced ZDO.
- The state is the ZDO: `ZDOVars.s_enabled`; the permitted list `ZDOVars.s_permitted` (count) and `pu_id<i>` / `pu_name<i>` (`SetPermittedPlayers`). `Interact`: the creator toggles on/off (RPC `ToggleEnabled`, run by the ward's owner, only for the creator's id); anyone else, only while the ward is off, toggles themselves on the list (RPC `TogglePermitted`).
- `Awake` switches a new ward on when `m_enabledByDefault` and this PC owns it. `Setup(name)` writes the creator name.
- `Player.PlacePiece` does, for a hammer placement: `Piece.SetCreator(playerId, platform user id)`, `PrivateArea.Setup(profile name)`, `WearNTear.OnPlaced()`, the place effect. `dome` does the same steps on its own instance.

Build: zone from the two points (`DomeLayout.Zone`), lattice (`DomeLayout.Wards`: the centre first, then rings), each spot checked (`CheckSpot`) and nudged if needed (`FindSpot`), then the wards are created and marked. The group's ward ZDOs are found with `ZDOMan.GetAllZDOsWithPrefabIterative("guard_stone")` (on the host: every ward of the world, loaded or not), rescanned every 30 s and on each `list` / `remove`.

Sync (`DomeCommand.Tick`, host only, every 2 s): see D11 and D12. It starts with the command (`OnLoad` inside a world), with every dome command, and when the local player spawns (postfix on `Player.OnSpawned`, `DomePatches`); leaving the world stops it, the next spawn starts it again. A sync writes the log line `dome <id>: ward at x,z changed to on|off, N permitted; copied to k other ward(s)`.

Files: `DomeCommand.cs` (command, build, groups and their numbers, apply, list, remove, sync), `DomeMenu.cs` (the ward menu, IMGUI), `DomePlayers.cs` (the join history), `DomeWards.cs` (ward ZDO keys, read / write / remove), `DomeLayout.cs` (Unity-free maths: zone, lattice, a coverage check), `DomePatches.cs` (the spawn postfix, `TextInput.IsVisible` while the menu is open), `refs.txt` (`Splatform.dll` for the platform user id, `UnityEngine.IMGUIModule.dll` and `UnityEngine.TextRenderingModule.dll` for the menu).

Change procedure: edit the files, `bash src/build.sh dome`, `op=reload`, `op=help&cmd=dome`.

## Open checks and test list (not run yet)

Test world hosted on this PC, `op=cheatstate` before the first and after the last test. The open checks come first; they decide steps 4-6 of the build order.

1. **Open check 1 (scale sync):** `m_syncInitialScale` on `guard_stone`, the tree prefabs and the effect prefabs. Needs an extra read-only column in `fxdump` (feature FxDump, not built in this session: a follow-up). Until then `op=inspect&prefab=guard_stone` lists its components.
2. **Open check 2 (ward radius and shape):** `op=dome&step=mark` twice 20 m apart -> the answer names the ward radius (`... of radius <r> m`). Expected about 32. The horizontal-only check is in the code (above); in game: a second player without permission digs under a dome ward, 10 m down -> refused (the ward flashes)?
3. **Open check 3 (vanilla ward rules):** inside an enabled dome ward, a second player without permission uses the hoe, the pickaxe on the ground and the cultivator -> refused? Stand inside a zone at night for a while: do monsters spawn inside the radius?
4. **Open check 4 (networked effects):** with a second player on a vanilla client: do they see the wards `dome` placed, the activate / deactivate effect when the host toggles a near ward, and a server-spawned networked effect (`op=visualeffect&name=<fx>&share=1`)?
5. `op=reload` -> names `HomeCheat.Cmd.dome.v<stamp>.dll`, no `FAILED` line. `op=help&cmd=dome` -> the usage above.
6. `op=coord&set=start`, walk 20 m, `op=coord&set=end`, `op=dome&from=coords&preview=1` -> `preview: 1 ward(s) would be placed (centre ..., radius 10.0 m, ward radius 32.0 m)`, one `would place at` line.
7. `op=dome&from=coords` -> `dome d<id>: placed 1 ward(s) ..., state on`. Look: a normal ward at the centre, upright, base not floating; hover shows you as the creator; the vanilla area circle when you hold the hammer.
8. `op=dome&step=list` -> `1 dome(s) ...: d<id>: centre ..., radius 10.0 m, wards 1/1, on 1/1, permitted 0`.
9. Group: mark two points 100 m apart, `preview=1` -> 7 wards (radius 32), then build -> `placed 7 ward(s) ...; one group: ...`. Walk the zone with the hammer out: no gap between the circles?
10. Group on/off: switch one ward off (E) -> within about 2 s every ward of the group is off (`step=list`: `on 0/7`); `LogOutput.log`: `dome d<id>: ward at ... changed to off, 0 permitted; copied to 6 other ward(s)`. Switch another one on -> all on.
11. Permitted list: group off, a second player interacts with one ward -> `step=list` `permitted 1` on every ward (hover on a far ward); group on; the second player builds next to another ward of the group -> allowed. Group off, the second player interacts again -> removed everywhere.
12. Refusals, each `preview=1` first: points on the sea -> `on water ...`; a point next to a wall -> `on a building: <piece> at ... m`; a steep hillside -> `slope too steep: ... over the limit`; next to a second player's own ward -> `overlaps another player's ward area ...`; a zone of 180 m radius -> `radius=... is over the cap 150.00`.
13. `partial=1` on a zone that reaches the shore -> `placed N ward(s) ..., k spot(s) skipped (gaps)` and the `skipped:` line.
14. `cost=1` with no stone in the inventory -> `ERROR: cost=1: ... need ..., you have 0; nothing placed`; with the materials -> `cost: ...` and the items gone.
15. Aim at a dome ward: `op=dome&step=remove` -> `would remove dome d<id> with N ward(s): add confirm=1`; with `confirm=1` -> `removed dome d<id>: N ward(s)`; `step=list` -> no domes.
16. Spell bar: Protect > Shield 3.5.3-3.5.5 and Build > Shield 6.6.3-6.6.5 -> the same answers as 7, 8 and 15.
17. Persistence: build a group, leave the world and load it again -> `step=list` shows it; switch one ward off -> the group follows (the sync restarted on spawn). The same after `op=reload` of a new build.

Record each first answer line and what was seen below, then set the index status to V.

| Time | Command | Answer (first line) | Observation | Verdict |
|---|---|---|---|---|

## In-game test 2026-10-25: ward menu

World `ClaudeTest`, hosted on this PC; after the first test pass (section In-game test 2026-10-25 below).

| Command | Answer (first line) | Observation | Verdict |
|---|---|---|---|
| `op=dome&step=list` (new build) | `Group-1 (d35e76e7b): centre -7.0,-3.5, radius 15.5 m, wards 1/1, on 1/1, permitted 0` | the older dome got its number on the first rescan | pass |
| `op=dome&step=apply&group=1&status=deactivated`, then `status=active` | `Group-1: 1 ward(s) deactivated, allowed: you` / `... active ...`; list `on 0/1` between | a far ward changed through its ZDO | pass |
| `op=dome&step=apply&group=1&allow=Nobody` | `ERROR: allow: 'Nobody' has not joined world ClaudeTest yet (known: Claude, Claude2)` | join history read | pass |
| `op=dome&step=menu` | `dome menu opened: 1 group(s) of yours, 2 known player(s)` | window as designed: New / Group-1, Claude (you) greyed ticked, Claude2, Active / Deactivated / Deleted (greyed for New), the New line names the zone; Maksym: "all good" | pass |
| `op=spells&reload=1` after the spell-file change | `56 spells, 21 titles, 5 pins; 0 bad lines` | Protect and Lights > Shield, Build > Shield | pass |

Offline: the lattice at `Fill` 0.95 covers every zone radius 5-150 m for ward radius 10, 20 and 32 m without a gap, and a second overlapping zone with its covered spots left out still covers itself (a scratch Node port of `DomeLayout`; exes are blocked on this PC).

## Limits

- Monsters: only what the vanilla ward does (no natural spawns within 20 m of a ward, its `PlayerBase` area); a creature that walks in or is spawned inside stays and attacks. The trader-style guard is parked (Ward menu, Monsters).
- Allowed offers only players the host has seen while running HomeCheat in this world (join history on this PC); a group's own list is always shown.
- The menu shows and changes only your own groups; wards placed by hand are not groups.
- The group follows within about 2 s and only while the host runs HomeCheat. Without it the wards stay normal, separate wards.
- A far ward (not loaded on the host) changes state without its effect.
- Build needs the zone's terrain loaded on this PC: stand near the centre of a large zone.
- Zone radius 150 m, 50 wards; a ward radius smaller than expected (open check 2) raises the ward count (10 m: a 40 m zone needs 37 wards).
- New, joining, merging and Deleted from the window: see the test log for what was run.

## Undo

`op=dome&step=remove&confirm=1` aimed at the dome removes all its wards (no materials returned). A single ward can also be demolished with the hammer like any ward; `step=list` then shows the group with fewer wards. To remove the command, delete `HomeCheat.Cmd.dome.*.dll` from `BepInEx/homecheat-commands/` and restart the game ([README.md](README.md#undo-and-uninstall)); the wards stay in the world as normal wards.

## In-game test 2026-10-25

Maksym's test pass of the phase 4 commands, world `ClaudeTest` hosted on this PC, Claude2 (no mod) as the second player. - Shield placed from the spell bar (Build or Protect > Shield: start, end, Place shield): dome `d35e76e7b`, centre -7.0,-3.5, radius 15.5 m, 1 ward, on, permitted 0. V.
- A Greyling spawned inside the shield stayed inside and attacked: as built. Only the vanilla ward rules apply; keeping monsters out is build step 4, which was not part of steps 2-3. Maksym had expected mobs to leave, which the page and `DomeCommand.cs` already said was not built.
- Step 4 (keep monsters out) was researched and parked; the Shield entries were redesigned as the ward menu: section Ward menu, test log In-game test 2026-10-25: ward menu.

Still untested (B): ward groups and their gaps, the on/off and permitted sync, the refusals (water, building, slope, another player's ward, the cap), `partial=1`, `cost=1`, remove with `confirm=1`, a group across reload and logout (T-dome 1-14 except placing one ward).
