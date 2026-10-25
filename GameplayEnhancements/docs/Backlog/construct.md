# construct: build a saved blueprint, with a pillar preview and a panel

Status: building (milestone 1: place at the crosshair)
Verdict: new
Nearest: `place` (any prefab at the crosshair, no creator), `dome` (vanilla piece as if built with the hammer, ZDO mark, `step=` stages, `confirm=1`), `groundlevel area=coords` (batched rectangle flattening, `TerrainStamp`); catalog rows, pages and sources read: yes
Fingerprint: effect place object (a whole build) + terrain / target ground / placement crosshair or self, rotated to the player / mechanism the blueprint's prefabs placed as by the hammer (`Piece.SetCreator`, `WearNTear.OnPlaced`), hoe pieces for the flattening

## What it does
- Trigger: `op=construct&step=<list|place|status|undo>` (milestone 1); later `preview|rotate|start|remove` and `panel=1`. Spell bar under the existing `6.7 | Construct` title in Build: `6.7.1 Construction...` (`op=construct&panel=1`), `6.7.2 Construction status` (`op=construct&step=status`), added in milestone 2.
- Inputs: `.blueprint` files (PlanBuild text format: `#Name`, `#Creator`, `#Description`, `#Category`, `#SnapPoints`, `#Pieces`; one piece per line `prefab;category;x;y;z;qx;qy;qz;qw;"extra";sx;sy;sz`) in `BepInEx/config/homecheat/blueprints/`. A file there = one construction; a subfolder = one construction made of all its files, placed around one origin (ASSUMPTION A2). The files stay in the game folder, never in this repository (other creators' content).
- Targets: self off, enemies off, tamed off, other players off, bosses off: no character is touched.
- Placement: the centre of the build's footprint at the crosshair (`at=aim`, default) or at your feet (`at=self`); turned to your facing, snapped to 22.5° (`yaw=` world degrees overrides); the lowest piece pivot of the build on the ground there (ASSUMPTION A5, milestone 1 without flattening).
- Magnitude: the whole build; `material=blueprint` (milestone 2: family swap, ASSUMPTION A4).
- Persists in the world: every placed piece, ZDO mark `hc_construct` = build id (`<name>-<yyyyMMddHHmmss>`); milestone 2: marker pillars (`hc_construct_pillar`), terrain change, `BepInEx/config/homecheat/construct.state`.
- Who sees it: everyone (networked pieces spawned on the host). The panel (milestone 2): this PC only.
- Visual reference: the blueprint's own pieces; pillars `wood_pole_log_4` (ASSUMPTION A3).
- Probes: `op=construct&step=list` (pieces, parts, footprint, missing prefabs per construction), `op=list&filter=_67` (the `*_67` pieces of the gumdrop files), `op=inspect&prefab=wood_pole_log_4`.
- Defaults: `step=place&name=<n>` works with only the name (`at=aim`, yaw from your facing).
- Caps: 4 000 pieces per construction, footprint 64 m per side (both refused, `Caps.Check`), 50 constructions in a list (more named as "first 50 of n").
- Refusals: host only (`Host.RequireServer`); a player-built piece within the footprint circle + 1 m (`Constructions.Near`) unless the opt-in `overlap=1`; an unknown name (the answer lists the names).
- Undo: `op=construct&step=undo&confirm=1` removes the pieces of the last build (or `id=<build id>`); without `confirm=1` the answer says what it would remove. The hammer works too: the pieces are yours.

## How it is built
- Feature folder: `src/Features/Construction/` (new, DLL `construction`); class `ConstructCommand : CommandBase`.
- Files: `ConstructCommand.cs` (steps), `Blueprint.cs` + `BlueprintPiece.cs` + `BlueprintParse.cs` (Unity-free reader, tested in a scratch console build: selftest covers only `Core/Pure`), `BlueprintLibrary.cs` (scans the folder), `refs.txt` (`Splatform.dll` for `SetCreator`).
- OnLoad needs: nothing (milestone 1). Timers: `Later` batches of 100 pieces 0.1 s apart, bottom-up, and a check 5 s after the last batch. OnWorldExit: forgets the last build in memory (the pieces keep their ZDO mark).
- Kit reused: `Host.RequireServer`, `Players.Local`, `Aim.Point`, `Ground.Height`, `Prefabs.Get`, `Constructions.Near/Describe`, `Caps.Check`, `Trees.SetScale`, `Notify.Center`, `Fmt`; the hammer steps copied from `DomeCommand.Place`.
- Core change: none.
- Guards: `Host.RequireServer()` first, `Players.Local`, `const` caps, ZDO mark on every piece, `confirm=1` on `undo`.
- Docs: page `docs/HomeCheat-plugin/construct.md`, index row, catalog row and a worked example; architecture doc Feature notes, Extension points (a feature-owned panel, A1), Parameter conventions (`step=list|place|preview|start|remove|undo`, `panel=1`).

## Milestone 2 (the rest of the card, after milestone 1 passes in game)
- `step=preview` places 5 columns of 5 `wood_pole_log_4` at the corners of the turned footprint and its centre (ASSUMPTION A3); `step=rotate&by=` moves them; `step=remove` takes them down; the plan is kept in `construct.state` per world (one active plan, A6).
- `step=start&confirm=1`: pillars removed, the footprint + 1 m levelled to the ground height under the centre (3 m stamps every 4 m in batches, `TerrainStamp` copied from World, a promotion candidate), then the pieces as in milestone 1; resume after leaving the world mid-build.
- Material swap (A4): `blueprint` or a family (darkwood, stone, woodiron ...) where the same shape exists; the others stay and are counted.
- `ConstructPanel` (A1): own IMGUI panel opened by `op=construct&panel=1`: list, material, the state block read from `construct.state`, buttons by state; every button calls `op=construct&step=...`.

## Open questions
Questions were not answered in the planning session (2026-10-25); the recommended answers were taken and are reviewed by Maksym:
1. A1 panel: own construct panel, a logged exception to the "choices through `ask=`" rule (stateful flow). ASSUMPTION.
2. A2 a folder of blueprints: parts placed together around one origin, optional `group.txt` offsets. ASSUMPTION.
3. A3 pillars: 5 columns x 5 core-wood 4 m poles. ASSUMPTION.
4. A4 material: piece-family swap, building free; `cost=1` shows the bill only. ASSUMPTION.
5. A5 height: the lowest piece pivot on the ground under the centre; flattening to that height (milestone 2). ASSUMPTION.

## Test plan (milestone 1)
1. `op=cheatstate` (before).
2. `op=construct&step=list` -> `2 construction(s)`: EarlySpiralStairs (1 part, 65 pieces, 3.6 x 3.8 x 3.9 m), brueplint-gumdropboth (2 parts, 3 466 pieces) with its missing prefabs named.
3. Aim at open flat ground, `op=construct&step=place&name=EarlySpiralStairs` -> `placed 65 of EarlySpiralStairs ...`; 5 s later the centre message `... 65 of 65 standing`. Look: on the crosshair, turned to you, steps in order, the first step on the ground, nothing falls.
4. `op=construct&step=status` -> the build id, 65 standing.
5. The same place again -> refused, naming a piece of the stairs; with `&overlap=1` it builds.
6. `op=construct&step=undo` -> `would remove 65 ...`; `&confirm=1` -> removed.
7. `op=cheatstate` (after); no new `HomeCheat` errors in `LogOutput.log`.

## Test log
| Time | Command | Answer (first line) | Observation | Verdict |
|---|---|---|---|---|
| 2026-10-25 17:24 | `bash src/build.sh construction` | `construction: built and staged HomeCheat.Cmd.construction.v20261025172406.dll` | compiled, not loaded yet (another session was testing in game) | pass |
| 2026-10-25 17:30 | scratch console build of the five Unity-free files over the game's `blueprints/` folder (outside selftest) | `EarlySpiralStairs ... pieces=65 bad=0 snaps=66 x=3.6 z=3.8 y=-0.26..3.61` | gumdrop folder: 2 parts, 3 466 pieces, 206 with extra data, 19 scaled; find by #Name, file and folder name; quoted extra with `;`, short lines, unknown section: pass | pass |
| 2026-10-25 18:03 | `op=cheatstate` (before) | `Achievements.IsCheatedAtAll( static) = True` | god mode and `Terminal.m_cheat` already on before the test (not from this command) | baseline |
| 2026-10-25 18:03 | `op=reload`, `op=help&cmd=construct` | `No new builds ...` / the Usage of construct | build v20261025172406 was already loaded by another session's reload | pass |
| 2026-10-25 18:03 | `op=construct&step=list` | `2 construction(s)` | EarlySpiralStairs 65 pieces 3.6 x 3.8 x 3.9 m; gumdrop 2 parts 3 466 pieces 31.4 x 32.2 x 21.0 m, no missing prefabs (the `*_67` pieces exist in 1.0.20) | pass |
| 2026-10-25 18:04 | `op=construct&step=place&name=EarlySpiralStairs` (ClaudeTest, Maksym aiming at open ground) | `placed 65 of EarlySpiralStairs (65 in the blueprint) at -393.0,32.5,148.4 (crosshair), yaw 292.5, id EarlySpiralStairs-20261025180400` | log 5 s later: `65 of 65 standing`; no construct errors in `LogOutput.log` | pass (look to confirm) |
| 2026-10-25 18:04 | `op=construct&step=status`, `op=cheatstate` (after) | `last build EarlySpiralStairs (...): 65 of 65 standing` | cheat state unchanged | pass |
