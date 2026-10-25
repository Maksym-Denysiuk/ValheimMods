---
description: coord, groundlevel, path and spawn commands of the HomeCheat Valheim plugin (2026-10-25, wave 1 W1-world-build) - two stored points per world (start, end) for other commands, flatten that keeps away from player-built pieces, paths from start to end routed around trees, rocks and pieces, spawn at a chosen player with rotation and a count cap; parameters, defaults, caps, how they work, assumptions, Maksym's test list, undo; built and selftested, not run in game
---
# Build and terrain: coord, groundlevel, path, spawn

Built 2026-10-25 on Valheim 1.0.20, BepInEx 5.4.2351, in the unattended no-game session W1-world-build of the HomeCheat work plan (cards coord, flatten-current, path-coords, spawn-menu). Source: [src/Features/World](../../src/Features/World/) (`CoordCommand`, `GroundLevelCommand`, `PathCommand`, `SpawnCommand`, `TerrainStamp`). Uses the wave 0 core helpers `Kit.Host`, `Kit.Coords`, `Kit.Constructions`, `Kit.Caps` and `GridRoute`. Own game and the worlds this PC hosts only.

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

- The area checked is the larger of the prefab's smooth radius and `radius + 1` m, because `TerrainStamp` smooths that far and a piece's distance is to its pivot (a long wall can cross the circle with its pivot outside).
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

The game's own pieces do the terrain work (hoe pieces `raise`, `path`, `paved_road` through `TerrainStamp`), the game's own `Piece.GetAllPiecesInRadius` finds buildings, physics overlap spheres find trees and rocks. Route search and the point store are Unity-free core code with selftests (straight, wall with a gap, no route, cap; point store format and limits).

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
- Terrain (flatten, paths) is saved with the world like any hoe work: level again by hand or with `op=groundlevel`, or restore a world backup (the admin package takes one every 3 minutes).
- Spawned creatures: `op=killmobs`; items: pick them up.
- The whole feature: delete `HomeCheat.Cmd.world.*.dll` from `BepInEx/homecheat-commands/` and restart; the plugin: [README.md](README.md#undo-and-uninstall).
