---
description: coord, groundlevel, path and spawn commands of the HomeCheat Valheim plugin (2026-10-25) - stored points per world, flatten a circle or the start-end rectangle and keep away from buildings, paths from start to end around obstacles, spawn ahead, at the crosshair or into an inventory, at full quality, tamed, and constructions built as by the hammer; in-game test 2026-10-25 with what is still untested
---
# Build and terrain: coord, groundlevel, path, spawn

Built 2026-10-25 on Valheim 1.0.20, BepInEx 5.4.2351, in the unattended no-game session W1-world-build of the HomeCheat work plan (cards coord, flatten-current, path-coords, spawn-menu). Source: [src/Features/World](../../src/Features/World/) (`CoordCommand`, `GroundLevelCommand`, `PathCommand`, `SpawnCommand`, `TerrainOps`). Uses the wave 0 core helpers `Kit.Host`, `Kit.Coords`, `Kit.Constructions`, `Kit.Caps` and `GridRoute`. Own game and the worlds this PC hosts only.

**Status B: built 2026-10-25, not run in game.** The test list for Maksym is at the end of this page.

## Goal

The Build section of the radial (`G` > Build) needs points to work between (Coordinates), flatten presets that never dig under a wall (Flatten), paths from one point to another that go around trees (Paths), and a spawn that asks what, how many, which way and at whom (Spawn...).

## coord: two stored points

| Form | Does |
|---|---|
| `op=coord&set=start` / `set=end` / `set=<name>` | stores the point at your feet; `aim=1` takes the crosshair instead (60 m ray, else 8 m ahead) |
| `op=coord&show=1` (or `op=coord`) | every point of this world, and once both are set the start-end distance and the rectangle `dx x dz` (edges along world X and Z) |
| `op=coord&get=1` | machine-readable `start=x,y,z end=x,y,z` (read-only, unset points left out) |
| `op=coord&clear=1` | removes every point of this world; `name=<point>` removes only that one |

- The points are kept by the core (`Kit.Coords`) per world name in `BepInEx/config/homecheat/coords.state`, so they survive `op=reload` and game restarts. Only this PC knows them. At most 64 points per world, names up to 32 letters, digits, `_` or `-`.
- A local marker shows each point for 10 s on `set` and `show` (`marker=` any fx prefab, `marker=none` for none). Default `vfx_Potion_health_medium` (ASSUMPTION, to be chosen with `/valheim-spell-fx`). An fx prefab with its own network view is seen by everyone; most potion vfx are not.
- Answer: `coord start = x,y,z (your feet)` and, when the other point exists, `; start-end 20.3 m, rectangle 14.0 x 14.4 m (dx x dz)`.
- Spell bar: 6.2.1-6.2.4 (Start, End, Show, Clear), mirrored at 3.5.1-3.5.2 (Protect > Shield).

## groundlevel: flatten, keep away from buildings

New opt-in parameters (absent = the behaviour before):

| Parameter | Default | Meaning |
|---|---|---|
| `avoid` | 0 | 1 = look for player-built pieces (`Piece.IsPlacedByPlayer`) within the area the stamp changes and refuse when there is one: `ERROR: piece wood_wall at 3.2 m inside the radius (2 piece(s) within 5.0 m): move away, lower radius= or add shrink=1` |
| `shrink` | 0 | with `avoid=1`: level a smaller circle instead, ending 1.1 m before the nearest piece; refused below 1 m |

- The area checked is the larger of the tool's smooth radius (when it smooths) and `radius + 1` m: the level changes the slopes 1 m beyond the circle, and a piece's distance is to its pivot (a long wall can cross the circle with its pivot outside).
- The game keeps terrain within ±8 m of the original ground (`TerrainComp`). With `|delta| > 8` the answer says so. A stack of several passes over 8 m is not detected (the original height is not read).
- Answers now print numbers with a point on every PC (`Fmt`), and `shrink` adds `, radius shrunk 4.0 -> 2.3 m (wood_wall at 3.4 m)`.
- Presets 6.1.1-6.1.5 (here, +5, +8, -3, -5) all use `avoid=1` and are sticky (the Flatten level stays open).

## path: from start to end, around obstacles

New opt-in parameters:

| Parameter | Default | Meaning |
|---|---|---|
| `from` | aim | `coords` = from the stored point start to end (`op=coord`); `length=` is then ignored and the answer says so. Without both points: `ERROR: set coord start and end first` |
| `avoid` | 0 | 1 = route around obstacles: a 1 m grid over the two points' bounding box plus 10 m; a cell is blocked when `Ground.IsBlocked(cell, 0.6)` finds a tree, rock, bush or building there, or a player-built piece's pivot is within 1 m. `GridRoute` (A*, 8 neighbours, no corner cutting) finds the route, and the strip is stamped along its corners |

- `from=aim` without `avoid` is the old straight path (refused over 400 stamps). The new modes stamp along a polyline every `width/2` metres and **cut** at 400 stamps: the answer then says `cut at 400 stamps (needed N)`.
- No route: `ERROR: no route from start to end around obstacles (...): nothing stamped`. Points too far apart for the grid (over 40 000 cells, about 180 m): refused before searching.
- Width 2 m as before (ASSUMPTION A12); stone uses `paved_road`, dirt `path`.
- Answer: `path stone from coords x,y,z to x,y,z: 24.1 m long (direct 20.0 m) x 2.0 m, around obstacles: 3 corner(s), 17 blocked cell(s), 25 stamps of r=1.0 (tool paved_road)`.
- Spell bar: 6.3.1 (dirt) and 6.3.2 (stone) start-end with `avoid=1`; 6.3.3-6.3.4 stay the 5 m straight paths.

## spawn: at a chosen player, turned

| Parameter | Default | Meaning |
|---|---|---|
| `player` | (you) | spawn `dist` metres ahead of that player. Another player only when this game hosts the world (`Host.RequireServer`); unknown or offline nick = `ERROR: player '<nick>' is not online` (ASSUMPTION A13: any online player, host only) |
| `yaw`, `pitch` | 0, 0 | rotation of each spawned object in degrees, world axes (before: no rotation, the same as 0/0) |
| `count` | 1 | **new cap 100**: `ERROR: count=150 is over the cap 100` (before: no cap) |

- Item prefabs (`ItemDrop`) fall to the ground there like any other spawn; the answer marks them `(items, dropped)`. Creatures get `level=` stars as before.
- Spell bar 6.4.1 `Spawn...` asks prefab (`@prefabs`), count (1, 5, 10, 50), yaw (0, 90, 180, 270), pitch (0, 90) and player (`@players`), then runs `op=spawn&prefab={prefab}&count={count}&yaw={yaw}&pitch={pitch}&player={player}`.

## Files and settings

- Code: `src/Features/World/CoordCommand.cs` (new), `GroundLevelCommand.cs`, `PathCommand.cs`, `SpawnCommand.cs`.
- Points: `BepInEx/config/homecheat/coords.state` (core, written as `.tmp` then moved).
- Spell lines: `BepInEx/config/homecheat/spells.txt` ids 3.5.1-3.5.2, 6.1.1-6.1.5, 6.2.1-6.2.4, 6.3.1-6.3.2, 6.4.1 (the `@dummy` "(soon)" lines swapped for the real forms).

## Method

The game's own terrain operations do the terrain work (saved `TerrainOp`s through `TerrainOps`, with the settings of the hoe pieces `raise`, `path`, `paved_road`; until 2026-10-25 the hoe pieces themselves through `TerrainStamp`, see below), the game's own `Piece.GetAllPiecesInRadius` finds buildings, physics overlap spheres find trees and rocks. Route search and the point store are Unity-free core code with selftests (straight, wall with a gap, no route, cap; point store format and limits).

## Assumptions (Maksym reviews)

| # | Assumption | Instead of |
|---|---|---|
| A9 | Flatten presets refuse near buildings (`avoid=1`); `shrink=1` exists, no preset uses it | shrink by default |
| A10 | Flatten +8 is the top preset (the game's ±8 m clamp) | +10 |
| A11 | Points are your feet; `aim=1` for the crosshair; kept per world in a file | crosshair by default, memory only |
| A12 | Paths 2 m wide; `avoid` on a 1 m grid with A* | another width or a simple detour |
| A13 | Spawn at any online player, host only | Maksym and Claude2 only |
| W1-a | coord marker `vfx_Potion_health_medium`, 10 s, `marker=none` to switch off | no marker, or a chosen fx |
| W1-b | groundlevel `avoid` checks the larger of the prefab's smooth radius and radius + 1 m; `shrink` stops 1.1 m before the nearest pivot | exactly radius + 1 m |
| W1-c | The ±8 m note fires only for `|delta| > 8`; repeated passes are not detected | reading the original height from `TerrainComp` |
| W1-d | path `from=aim&avoid=1` also routes (crosshair to `length` ahead) | `avoid` only with `from=coords` |
| W1-e | New path modes cut at 400 stamps and say so (as the card asks); the old straight mode still refuses over 400 | refuse in every mode |
| W1-f | spawn `yaw`/`pitch` are world angles, not relative to the player | relative to the player's facing |
| W1-g | spawn items need no separate drop code: `Instantiate` of an `ItemDrop` prefab already lies on the ground like a dropped stack | a `place`-style stack drop |

## Test list for Maksym (not run yet)

`op=cheatstate` before the first test and after the last; then `op=reload` (the answer must name `HomeCheat.Cmd.world.v20261025...`), and `op=help&cmd=coord`. Test world `ClaudeTest`, second player `Claude2`.

| # | Command | Expected first answer line | Look at |
|---|---|---|---|
| 1 | `op=coord&set=start` | `coord start = x,y,z (your feet)` | a small marker at your feet for 10 s |
| 2 | walk about 20 m, `op=coord&set=end` | `coord end = ...; start-end 20.x m, rectangle ...` | distance about 20 |
| 3 | `op=coord&show=1` | `2 point(s) in world ClaudeTest: start=... end=...; start-end 20.x m` | both markers |
| 4 | `op=reload`, then `op=coord&get=1` | `start=x,y,z end=x,y,z` | points survived the reload |
| 5 | next to a wall: `op=groundlevel&avoid=1` | `ERROR: piece <wall> at <d> m inside the radius ...` | nothing changed |
| 6 | same spot: `op=groundlevel&avoid=1&shrink=1` | `ground levelled ... radius shrunk 4.0 -> <r> m (...)` | a smaller circle levelled, the wall untouched |
| 7 | in the open: `op=groundlevel&avoid=1` | `ground levelled around ... r=4.0 m` | same as before |
| 8 | `op=groundlevel&delta=10&radius=3` (open ground) | `... ; the game clamps terrain at ±8 m ...` | the raise stops at 8 m |
| 9 | points about 20 m apart with a tree on the line: `op=path&from=coords&avoid=1` | `path dirt from coords ...: <L> m long (direct 20.x m) ..., around obstacles: <n> corner(s) ...` | the strip bends around the tree |
| 10 | same points: `op=path&from=coords` | `path dirt from coords ...: 20.x m long ...` | straight through, under the tree |
| 11 | `op=path&type=stone&from=coords&avoid=1` | `path stone ... (tool paved_road)` | stone strip |
| 12 | `op=coord&clear=1`, then `op=path&from=coords` | `cleared 2 point(s) ...`, then `ERROR: set coord start and end first ...` | nothing stamped |
| 13 | `op=spawn&prefab=Greyling&count=2&player=Claude2` | `spawned 2 Greyling ahead of Claude2` | two greylings by Claude2; clear with `op=killmobs` |
| 14 | `op=spawn&prefab=Wood&count=5&yaw=90` | `spawned 5 Wood (items, dropped) (yaw 90, pitch 0)` | five logs on the ground, turned |
| 15 | `op=spawn&prefab=Greyling&count=150` | `ERROR: count=150 is over the cap 100` | nothing spawned |
| 16 | radial: Build > Flatten > Flatten here; Build > Coordinates; Build > Paths > Dirt path start-end; Build > Spawn > Spawn... | the same answers as above | Flatten stays open after a cast (sticky); Spawn asks five questions, Back goes one back |

## Undo

- Points: `op=coord&clear=1`, or delete `BepInEx/config/homecheat/coords.state`.
- Terrain (flatten, paths) is saved with the world like any hoe work: level again by hand or with `op=groundlevel`, or restore a world backup (the admin package takes one every 3 minutes). Old hoe stamps that `groundlevel` removed (the answer counts them) come back only from a backup.
- Spawned creatures: `op=killmobs`; items: pick them up.
- The whole feature: delete `HomeCheat.Cmd.world.*.dll` from `BepInEx/homecheat-commands/` and restart; the plugin: [README.md](README.md#undo-and-uninstall).

## In-game test 2026-10-25

Maksym's test pass of the phase 4 commands, world `ClaudeTest` hosted on this PC, Claude2 (no mod) as the second player. **coord**: set start and end, show, get after `op=reload`, clear: V. `path` refuses without points: V.

**spawn** gained (Maksym's requests):
- `tamed=1`: creatures spawn tamed (a test aid for tamed-animal hits). V.
- `where=ahead|crosshair|inventory` (default ahead, unchanged): crosshair = your camera's aim point + 0.5 m (no hit within 60 m: 8 m ahead, said in the answer); inventory = items only, into your own inventory, overflow at your feet; with `player=<another>` the items are dropped at their feet (their inventory lives on their PC; auto-pickup takes them); an unknown name answers "player 'x' is not online". The Spawn... form asks Where: Crosshair (default) or Inventory. V (Wood x5 inventory, Wood x3 crosshair, "items spawn works"); `player=<another>` with Inventory B.
- `quality=1|<n>|max`: an item's level, clamped to the item's maximum, with full durability (as `give`); the form passes `quality=max`. V (SwordIron max = quality 4, AxeBronze at the crosshair).
- **Constructions**: a prefab with a `Piece` (workbench, portal, furnace, ships) is built as the hammer builds it (`Player.PlacePiece` steps, as `dome`): on the ground, facing you (`yaw=` turns it, `pitch` ignored), a count side by side to the right, you as creator (the hammer can remove it), `WearNTear.OnPlaced`, the place effect; a ship on the water surface when the ground is under water; `tag=` sets a portal's pairing tag; host only. The Spawn... form lists "Constructions: <category>" groups from every tool's piece table (terrain and plant pieces left out). Persistence unchanged: spawn always left its objects in the world. V: a workbench at the crosshair.
- **Constructions never into an inventory**: `where=inventory` with a `Piece` is refused ("is a construction: it is built in the world only (Where = Crosshair)"). V for a workbench and a Karve. The form still offers Inventory; the cast answers with the error in the screen centre (Known defects).

Still untested (B): a ship on water, a portal pair with `tag=`, the form's "Constructions:" groups, `player=` / `yaw=` / `pitch=` / the count cap (T-build 13-15).

**groundlevel** gained `area=coords` (the Flatten... form, Start-end): the rectangle of the stored start and end, levelled to your ground height + `delta` with 3 m stamps every 4 m (cap 1 600, batches of 50 every 0.1 s); `area=self` = the circle as before. Spell 6.1 Flatten... (form: Area From me / Start-end, Flatten level -10..10, Radius greyed for Start-end) always sends `avoid=1`. Run in game: V.

**Known behaviour (Flatten next to buildings).** With `avoid=1` and `area=coords`, each 3 m stamp whose smoothing reaches within about 4 m of any piece is skipped, so a strip next to buildings stays unlevelled. Maksym saw part of the area left unlevelled next to buildings. Open, not built: a form question "Buildings: keep away / level all" (keep away first) and an area `shrink`.

Still untested (B): `avoid=1` / `shrink=1` of the circle next to a wall, the ±8 m clamp, path `from=coords` with `avoid=1` around a tree, the stone path (T-build 5-12). The 5 m paths 6.3.3-6.3.4 were removed from the spell bar.

## Saved terrain operations (since 2026-10-25)

**Problem.** `groundlevel` and `path` placed the Hoe's own pieces (`raise`, `path`, `paved_road`) with a changed radius (`TerrainStamp`). Those pieces carry the old `TerrainModifier`: it stays in the world and is re-applied from the prefab after the area reloads, with the prefab's radius (1.5 m level for `raise`, 2 m paint for `path`, 2.2 m for `paved_road`). A Start-end rectangle of 3 m stamps every 4 m then fell apart into 1.5 m circles, and a 2 m path grew to about 4 m. Found by `construct` (IL 1.0.20, lessons in HomeOS `references/ValheimModWork/construct-lessons-learned.md`).

**Now.** Both commands write saved `TerrainOp`s through `TerrainOps` (feature World, the approach of Construction's `TerrainLevel`). Parameters, defaults and caps are unchanged.

| Command | What it writes |
|---|---|
| `groundlevel` (circle and `area=coords`) | one single-vertex level operation (a 0.3 m circle) per terrain vertex (whole world metres) at your ground height + `delta`; the rectangle is rounded out to whole metres, so a strip thinner than 1 m still gets levelled. The tool's paint (`raise`: dirt) goes on at the old stamp points. With `avoid=1` the rectangle keeps the old rule for which parts stay (a 3 m part every 4 m is left out when a piece's pivot is within 4 m), and only vertices inside a kept part are levelled |
| `path` | one paint operation per stamp with the tool's paint type and smoothing (`path` dirt, `paved_road` paved and smoothed), radius `width/2 + 0.5 m` (the paint mask is a 1 m grid: `width/2` alone painted a visibly thinner strip). A tool that levels (`tool=raise`) also gets single-vertex level operations at each stamp's height + its level offset |

- Before the level, old hoe stamps whose level or smoothing reaches the area are removed (they would override the saved heights after a reload); the answer counts them. With `avoid=1` a stamp that reaches within 1 m of a building's pivot stays and is counted as kept. The operations start 1 s after a removal, so the terrain has rebuilt without the old stamps.
- 150 level operations every 0.1 s. The first answer line names the vertices and says when the flatness line reaches the log: `groundlevel: <area>: <n> of <m> vertices within 5 cm of the target ...`.
- `op=groundlevel&report=1` (read-only): the last levelled area of this session against its target; `area=coords` or `area=self [&radius=]` measures the start-end rectangle or the circle around you against the median height, at any time (after a restart too). Vertices more than 8 m from the world's own ground are named (the game's limit).
- `op=path&report=1` (read-only): the last path of this session, sampled on its axis and at 0.6 of the half width on each side (inside), and 0.8 m outside each edge (outside), read in the paint type's own channel of the paint mask (dirt red, cultivated green, paved blue; paint over paint mixes them). Outside points on the edge cells of the 1 m mask read as painted too; the number to watch is whether it changes after a reload.

**What failed first.** A 0.4 m wide Start-end strip held no whole-metre vertex and levelled nothing (the box is now rounded out). The first path check counted any paint, so the dirt of the levelled rectangle made the outside look painted (now the type's own channel). Paint radius `width/2` gave a strip Maksym saw as thin (now + 0.5 m).

**Verification (in game 2026-10-25, ClaudeTest, Maksym).** A 16 x 16 m Start-end rectangle (heights 31.95..37.84, 8 of 324 vertices level before): `groundlevel&area=coords` 324 of 324 within 5 cm of 32.32; a stone path start-end 22.7 x 2 m: 72 of 72 inside points paved, 24 of 48 outside points paved (edge cells), Maksym: "now 2m". Exit to the menu and back into the world: 324 of 324 at 32.32, the path 72 of 72 and 24 of 48 again. A 4 m circle with `delta=1` on open ground: 8 of 49 level before, 50 of 50 at 35.80 after. No `Failed to deserialize TerrainOp` errors in `LogOutput.log`.

**Not covered yet (B).** `avoid=1` with old hoe stamps next to a building (kept stamps), a dirt path, a path with `tool=raise`, Claude2 seeing the result (terrain is saved in the zone's `TerrainComp` and synced like any hoe work).
