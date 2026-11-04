---
description: materials of the HomeCheat build and plant commands (cost=nearby, 2026-11-03, verified in game 2026-11-04) - construct, spawn, dome, plant and field take materials from you, then safe chests within 50 m, the rest free; cost=check says what would be taken; card, probes, how it was built, test list M1-M14
---
# Materials: build and plant take materials from you, then safe chests, then build free

Status: V (Maksym 2026-11-04: ok). Recorded: spell Spawn..., `built 1 portal_wood at the crosshair ...; materials (radius 50 m): 21 from you, 11 from 3 chest(s), 0 free (GreydwarfEye 5/5/0, FineWood 15/5/0, SurtlingCore 1/1/0), no undo`. The log of that launch holds no materials line of `construct`, `dome`, `plant` or `field`: those four rest on Maksym's word · Verdict: Kit helper (0-core) + extend five commands with `cost=nearby` · Nearest: dome `cost=1` (read)
Fingerprint: give item (negative) / self, containers / radius 50 m / `Inventory`, `Container`

- Helper: `Kit.Materials.Take(player, requirements, radius, out report)`: per requirement, your inventory first, then containers within `radius` nearest first, the rest free. Per piece, never all or nothing. Report: from you, from N chests, free.
- Safe chests only: not open by another player (`Container.IsInUse`), not in a ward you lack access to (`PrivateArea.CheckAccess`), not another player's private chest. The helper claims the container ZDO before removing, as the game does.
- Users (wave 2): `construct step=start`, `spawn` (constructions), `dome` (`cost=1` stays as inventory-only, all-or-nothing), `plant` (sapling `m_resources`), `field` (crop seeds).
- Scope: HomeCheat commands only; the vanilla hammer and cultivator stay vanilla (a free vanilla build is a no-cost path, gate 6).
- Defaults: `cost=0` in code; every build and plant spell line passes `cost=nearby`. `radius=50`, cap 100. `cost=check` says what would be taken, changes nothing.
- Persists: items leave your inventory and chests. No undo (named in the answer).
- Probes: `Container.IsInUse`, `PrivateArea.CheckAccess`, the private-chest field, `m_resources` of saplings and crops in this game version.
- Docs: architecture doc Kit by need, each command's page, index, catalog.
- Test: 10 wood on you, 50 in a chest at 20 m, a 100-wood build → 10 / 50 / 40 free; a chest in Claude2's ward untouched.

## Probes (2026-11-03, il-reader on the game DLL of 2026-10-28)

- `Container.CheckAccess(long playerID)` is **private** in this version (called by reflection): switch on `m_privacy`, Private = `m_piece.GetCreator() == playerID`, the open setting = true, others false. This is the "another player's private chest" check.
- `Container.IsInUse()` returns the local `m_inUse`, which only the ZDO owner keeps; others see the synced ZDO int `InUse` (`ZDOVars.s_inUse`, written in `UpdateUseVisual`). The helper reads both, and a cart's `m_wagon.InUse()` (refused by `RPC_RequestOpen` too).
- `PrivateArea.CheckAccess(Vector3, float radius, bool flash, bool wardCheck)`: `Container.Interact` calls it with `(pos, 0, true, false)` when `m_checkGuardStone`; the helper calls `(pos, 0, false, false)` for every chest (stricter).
- Taking: `Container.RPC_RequestOpen` gives the opener the ZDO (`ForceSendZDO`, `SetOwner`); `OnContainerChanged` saves only on the owner. The helper claims the ZDO, reloads the inventory (`Container.Load`, private) and removes; the inventory's change event saves.
- `m_resources` of saplings and crops: left to hc-materials (its users read them in game).

## Built (2026-11-03, hc-core: Kit helper only)

- `src/Core/Pure/MaterialSplit.cs`: `MaterialSplit.Split(need, available[], out free)` (you first, then sources in order, the rest free), `Merge` (one item named twice in a recipe counts once), `MaterialLedger` (what a check-only run already counted). Selftest `MaterialSplitTests` (the card's 10 / 50 / 40 example, nearest first, empty sources, merge, a two-piece check run).
- `src/Core/Kit/Materials.cs`: `Materials.Take(player, requirements, radius, out report)` and `Take(..., checkOnly, out report)` for one piece; `Materials.Begin(player, radius, checkOnly)` returns a `MaterialsRun` for many pieces (chests found once, nearest first; `run.Take(reqs)` per piece returns the free count; `run.Report.Summary()` = `10 from you, 50 from 1 chest(s), 40 free (Wood 10/50/40); 1 chest(s) skipped (warded 1)`). `Materials.Usage`, `DefaultRadius` 50, `MaxRadius` 100 (clamped).
- Chests that are not built by a player (dungeon loot, tombstones) are never touched and not counted as skipped. A chest found safe at the start is checked again before each take; one opened since is skipped and that part is free.
- ASSUMPTION (hc-core, conservative): only player-built containers count as chests, and the ward check runs for every chest, not only those with `m_checkGuardStone`. Maksym may widen it.
- In-game test: with the users below.

## Built (2026-11-03, hc-materials: the five users)

Pages: [construct.md](../HomeCheat-plugin/construct.md#materials-costnearby-since-2026-11-03), [build-terrain.md](../HomeCheat-plugin/build-terrain.md#spawn-materials-for-constructions-costnearby-since-2026-11-03) (spawn), [dome.md](../HomeCheat-plugin/dome.md#materials-costnearby-since-2026-11-03), [plant-field-place.md](../HomeCheat-plugin/plant-field-place.md#materials-costnearby-plant-field-since-2026-11-03).

- Keys on all five: `cost=0` (default, free; typed commands unchanged) `|nearby|check`, `radius=50` (cap 100, lowered and named). Dome also keeps `cost=1` (inventory only, all or nothing).
- `construct` (`ConstructCommand.Cost`): `step=start` and `step=place`; materials per piece as `PlaceOne` places it (`TakeOne`, after the ground gate: a refused build costs nothing; poles and foundation pieces pay too). The start answer counts with a check-only run over the plan's pieces ("counted now"); the totals go to the log line `construct: <name> materials: ...` and `step=status` after the last batch. `cost=check` on start or place builds nothing, plan kept. The panel: `panel=1&cost=nearby` stores `&cost=nearby` on `ConstructPanel.CostForm`; Start sends `op=construct&step=start&confirm=1&cost=nearby`. `continue=1` stays free.
- `spawn` (World `MaterialCost`): constructions only, per piece once it stands; items and creatures ignore `cost=`; `cost=check` builds nothing.
- `dome` (`DomeCommand.DomeCost`): `nearby` per ward really placed (after placing); `check` = a preview with the counts; the menu (`step=menu&cost=nearby`) keeps a `DomeCost` on `DomeMenu.Cost` and New's Apply passes it; `step=apply` reads `cost=`/`radius=`; the menu refuses `cost=check`.
- `plant`, `field` (World `MaterialCost`): n x the recipe in one take (`MaterialCost.Times`, same split as n single takes); sapling: its `Piece.m_resources`; crop: the seed piece that grows it (`GrowRules.PlantOf`). Only what is planted pays. `cost=check` plants nothing.
- Spell lines (hc-menu, not edited here): 6.6 `op=dome&step=menu&cost=nearby`, 6.7.1 `op=construct&panel=1&cost=nearby`, 7.2 field, 7.4 plant, 8 spawn with `&cost=nearby` after the `{...}` answers: the forms pass it through as a plain key (no `ask=` touches it).
- Promotion candidate (Known defects row): the `cost=`/`radius=` reader exists three times (World `MaterialCost`, `DomeCost`, construct `Cost`); a `Kit.Materials` reader with the next core change.
- Probe left for the game: the `m_resources` of saplings and crops in this version (expected: `Birch_Sapling` BirchSeeds 1, `sapling_carrot` CarrotSeeds 1); the test reads them from the answers' item lists.

## Test list

World `ClaudeTest`; Claude2 is Maksym's own account. `op=reload` first (the staged world, dome and construction DLLs), `op=cheatstate` before and after; read `BepInEx/LogOutput.log` after each line.

| # | Setup | Command | Expected (first answer line) | Look at |
|---|---|---|---|---|
| M1 | none | `op=help&cmd=plant` (and field, spawn, dome, construct) | `Usage` lists `[cost=0 (nearby = ...; check = ...)] [radius=50 (cap 100)]` | the help of all five |
| M2 | 10 Wood on you, a chest you built 20 m away with 50 Wood, none other within 50 m; a construction preview of 100 Wood (or `step=place` of a 100-wood blueprint) | `op=construct&step=start&cost=check` | `cost=check, nothing built: <name> (N pieces) would take 10 from you, 50 from 1 chest(s), 40 free (Wood 10/50/40) ...` | inventory and chest unchanged, plan and poles still there |
| M3 | as M2 | `op=construct&step=start&confirm=1&cost=nearby` | `building <name>: ... materials (cost=nearby, radius 50 m), counted now: 10 from you, 50 from 1 chest(s), 40 free (Wood 10/50/40) ...` | log `construct: <name> materials: 10 from you, 50 from 1 chest(s), 40 free ...`; 0 Wood on you and in the chest; `op=construct&step=status` shows the same line |
| M4 | a chest with Wood inside a ward of Claude2 that does not permit you, within 50 m; Claude2 online | `op=spawn&prefab=piece_workbench&where=crosshair&cost=nearby` with no Wood on you | `built 1 piece_workbench ...; materials (radius 50 m): 0 from you, 0 from 0 chest(s), 10 free (Wood 0/0/10); 1 chest(s) skipped (warded 1), no undo` | Claude2's chest untouched (ask Maksym or open it as Claude2) |
| M5 | a chest Claude2 holds open (or private, built by Claude2) | as M4 | skipped with `in use 1` (or `private 1`) | chest untouched |
| M6 | BirchSeeds 5 on you, 20 in a near chest | `op=plant&prefab=Birch_Sapling&rows=2&perrow=5&cost=nearby` | `planted 10 ...; materials (radius 50 m): 5 from you, 5 from 1 chest(s), 0 free (BirchSeeds 5/5/0), no undo` (amounts from the sapling's recipe) | seeds left: 0 on you, 15 in the chest; probe: the item and amount per sapling |
| M7 | cultivated patch, CarrotSeeds 3 on you, none in chests | `op=field&prefab=Pickable_Carrot&rows=1&perrow=5&cost=nearby` | `planted 5 ...; materials ...: 3 from you, 0 from 0 chest(s), 2 free (CarrotSeeds 3/0/2)` | probe: the seed item name; 0 CarrotSeeds left |
| M8 | as M6 | `op=plant&prefab=Birch_Sapling&rows=2&perrow=5&cost=check` | `cost=check, nothing planted: would plant 10 ...; materials that would be taken (cost=check, nothing taken): ...` | no saplings, seeds unchanged |
| M9 | coord start and end 20 m apart, Stone on you | `op=dome&from=coords&cost=check`, then `op=dome&from=coords&cost=nearby` | check: `cost=check, nothing placed: N ward(s) would be placed ...; materials that would be taken ...`; nearby: `dome Group-N ...; ..., materials (radius 50 m): ...` | wards placed only by the second; items gone per ward |
| M10 | spell 6.6 Build > Ward | open the menu, New, Apply | opening answer in the log ends `New takes the ward materials from you, then safe chests within 50 m (cost=nearby)`; Apply's answer (on screen) names the materials | as M9 |
| M11 | spell 6.7.1 | the panel's Start construction | the panel's answer line names `counted now: ...` | as M3 |
| M12 | spell lines 7.2, 7.4, 8 | one cast each | the answer ends with a materials part | no `cost` error from a form |
| M13 | typed, no `cost=` | `op=plant&prefab=Birch_Sapling` | no materials part, nothing taken (default unchanged) | inventory unchanged |
| M14 | none | `op=plant&prefab=Birch_Sapling&cost=1` | `ERROR: cost must be 0, nearby or check ...` | nothing planted |
