---
description: plant, field, cultivate, harvest and place commands of the HomeCheat Valheim plugin (2026-10-24, areas, harvest, grow rules and batches 2026-10-25) - saplings, crops or any prefab in a grid from you, the crosshair, the start-end rectangle or the patch you stand on; harvest into the inventory; parameters, spacing and grow rules, caps per cast, limits, undo; in-game test 2026-10-25
---
# plant, field, cultivate, place (HomeCheat core ops)

Built 2026-10-24 on Valheim 1.0.20, BepInEx 5.4.2351, host `MDENYSIUK-PERS`, in the CheatResearch session over several of Maksym's requests (grow crops only on cultivated soil, place cultivated soil as a separate command, then a parameterised `plant` for birches "5 in a row, 3 rows, the 3rd birch of row 1 where the crosshair points", then a general `place`). Own game and own world only. Source: [src/Features/World](../../src/Features/World/) (`PlantCommand`, `FieldCommand`, `CultivateCommand`, `PlaceCommand`, cell math in the core `Kit/Anchor.cs`; until 2026-10-25 part of the core `HomeCheat.cs`).

## What they do

Every command builds a block of `rows` x `perrow` objects. Row 1 starts at the crosshair point (the camera ray, 60 m; if nothing is hit, 8 m ahead of the player). Further rows run away from the player. Column `aimcol` (0-based) of row 1 stands on the crosshair point; default `(perrow-1)/2`, so one object (`rows=1&perrow=1`) stands exactly where the crosshair points, and with `perrow=5` the 3rd one does. The ground height is read at every spot.

- **plant** instantiates a sapling prefab (for example `Birch_Sapling`) in the block. Spacing defaults to twice the sapling's own grow radius times the scale, which is the free space the game's `Plant` needs to be allowed to grow. Clearance defaults to half the grow radius (at least 0.5 m); a spot with a solid object inside that radius (buildings, trees, bushes, rocks, other plants) is skipped. The grown tree copies the sapling's scale.
- **field** plants crops (`Pickable_*`) only on cells that already have cultivated soil. Cells without soil are skipped, so asking for 100 rows with soil on 5 plants 5. `crops=Pickable_Carrot:2,Pickable_Onion:1` makes bands of rows with one crop each, so several crops share one field. Default spacing 1.5 m, clearance 0.4 m.
- **cultivate** places one `cultivate` piece (what the Cultivator tool makes) per cell of the grid, default spacing 1.5 m, max 2500.
- **place** puts any prefab (item, building piece, creature, object) in the block.

## Use

| Command | Required | Parameters (default) |
|---|---|---|
| `op=plant` | `prefab` (find names with `op=list&filter=sapling`) | `rows` (1), `perrow` (1, alias `cols`), `spacing` (auto), `aimcol` ((perrow-1)/2), `scale` (1, cap 20), `clear` (auto), `area` (aim; coords = between the two stored points), `avoid` (0; 1 = keep away from buildings): see [Trees in an area](#trees-in-an-area-since-2026-10-25) |
| `op=field` | `prefab` or `crops` | `rows` (1), `perrow` (1), `spacing` (1.5), `scale` (1), `clear` (0.4), `dist` (anchor in metres ahead of the player instead of the crosshair) |
| `op=cultivate` | none | `rows` (1), `perrow` (1), `spacing` (1.5), `dist` |
| `op=place` | `prefab` | `rows` (1), `perrow` (1), `spacing` (1.5), `aimcol`, `scale` (1), `amount` (1, stack size for items), `level` (0, star level for creatures), `lift` (0 m above ground), `clear` (0 = no check, so pieces can be placed anywhere) |

Examples: `op=plant&prefab=Birch_Sapling&rows=3&perrow=5`, `op=cultivate&rows=3&perrow=5`, `op=field&crops=Pickable_Carrot:2,Pickable_Onion:1&perrow=5`, `op=plant&prefab=Birch_Sapling&scale=2`.

Limits: plant and place refuse above 400 objects, field examines at most 40000 cells and plants at most 2500. `scale` above 0 only (plant: at most 20). A missing `prefab` returns an error on purpose (no default, so a typo cannot plant the wrong thing). Answers: `planted N <prefab> x<scale> (R rows x C), B blocked by objects (clear radius ...), spacing ... m (growRadius ...), tree K of row 1 at x,y,z (crosshair)`; `placed N ...`; `nothing planted: no cultivated soil in the RxC grid (use op=cultivate first)`.

`scale` on these commands sets the object's local scale and the ZDO key `scale`. For trees see [tree-scale.md](tree-scale.md).

## Verification

- **field:** run in ClaudeTest, answers `planted 48 Pickable_Carrot in a 6x8 grid` and `planted 24 Pickable_Carrot in a 4x6 grid`. The rule "only on cultivated soil" was added after the first run at Maksym's request and the rerun was accepted.
- **plant:** the block logic is the same as `field` and compiled, but **no answer line of a sapling run was recorded in any session**. The three checks Maksym asked for (the 3rd birch of row 1 on the crosshair, one tree on the crosshair for `rows=1&perrow=1`, nothing placed inside buildings, trees or bushes) are not confirmed. Run once and record.
- **cultivate** was run as part of the field tests; **place** has a design (`place-design.md` in the run folder `LocalOutputs/2026-10-24--00-48/`, not archived) and no recorded run.

## cultivate and field since 2026-10-25

Changed with Maksym in the test run of 2026-10-25 ([test log](../../../docs/homecheat-test-run-2026-10-25.md)); `plant` and `place` are unchanged (crosshair, `aimcol`).

- **Where.** No crosshair any more: row 1 is the player's own cell (`dist=<m>` moves it forward), the block is centred on the look direction and the rows run away from the player that way (`Features/World/FieldAnchor.cs`).
- **Which cells field plants.** Only the cultivated cells connected to the player's cell through cultivated cells; neighbours are all 8 cells around. A gap ends the planting there: cells 1-4-7 in a line with 4 not cultivated plants only 1. If the player's own cell is not cultivated, nothing is planted ("the soil under you is not cultivated"). The answer counts planted, cultivated but not connected, without soil, and blocked (existing crops block, so nothing is planted twice).
- **Spacing.** Without `spacing=`, field uses the smallest legal crop distance from the game. The sapling of the crop (the prefab whose `Plant.m_grownPrefabs` holds it) needs no other plant within its `m_growRadius` (`Plant.HaveGrowSpace`), so the step is growRadius + the sapling's collider radius: carrot, turnip, onion, barley, flax, poteitr 0.5 + 0.18 = 0.68 m; magecap 0.98 m. Mixed crops take the largest of their minimums (`Features/World/CropSpacing.cs`). cultivate keeps 1.5 m; the soil is continuous, so the denser field grid fits.
- **map=1.** `op=field&prefab=<crop>&rows=<r>&perrow=<n>&map=1` plants nothing and prints the grid: `@` the player's cell, `#` cultivated and connected, `o` cultivated but cut off, `.` no soil.

Tested 2026-10-25: cultivate 1x1, 1x3, 3x3; field 3x3, 4x9, 9x2, 7x7, 9x9 (three times, one off the soil), 40x40 (twice), with carrots, Poteitr and turnips.

## Soil areas and harvest (since 2026-10-25)

Built 2026-10-25 in the unattended, no-game session W2-world-field of the Valheim work plan (backlog cards 3.11 cultivate-area and 3.12 field-area and harvest; Valheim 1.0.20, BepInEx 5.4.2351). Goal: cultivate and plant food between the two stored points or on a whole patch, and pick a ripe patch in one go, from the spell bar's Field group (7.6-7.8). Before: `cultivate` and `field` only laid a grid from your own cell (`rows` x `perrow`), and crops were picked one by one with E. Source: `src/Features/World/` (`CultivateCommand`, `FieldCommand`, `HarvestCommand`, `FieldPatch`).

| Command | Does |
|---|---|
| `op=cultivate&area=coords[&spacing=1.5]` | soil on the cells of the rectangle with `op=coord` start and end as opposite corners, edges along world X and Z, the grid centred in it (`GridMath.RectCells`, both edges included). Over 2 500 cells it is refused as a whole. |
| `op=field&prefab=<crop>&area=coords` | the crop grid (minimum crop spacing, as without `area=`) over the same rectangle; every cultivated cell is planted, no connection rule. |
| `op=field&prefab=<crop>&area=patch` | the whole cultivated patch you stand on: `FieldPatch.Sample` grows it from your cell through cultivated soil on a 0.5 m grid (8 neighbours, at most 40 000 cells examined), and the crop grid is laid over its bounding box; cells outside the patch (another patch, or no soil) are skipped and counted. |
| `op=harvest` | every ripe crop (`Pickable.CanBePicked()`) on the patch you stand on is picked with the vanilla `Pickable.Interact(you, false, false)`, the same call as pressing E. The items drop at each crop; auto-pickup takes the ones within reach, walk over the rest. Cap 2 500 picks. Wild pickables standing on the patch are picked too. |

With `area=` the parameters `rows`, `perrow` (`cols`) and `dist` are ignored and the answer says so; `crops=a:n,b:n` bands repeat along world Z (n grid rows each). `map=1` works only without `area=`. Without `area=` both commands behave as before: the flood fill of `field` moved unchanged into `FieldPatch.Connected`.

Answers (first line): `cultivated 35 cells (7 x 5, spacing 1.5 m, cap 2500) in the rectangle start x,y,z - end x,y,z`; `planted N (Pickable_Carrot=N) in the rectangle ... | the patch under you (about A m2, S samples of 0.5 m), grid C x R; K cultivated cells not in this patch, M cells without cultivated soil, B blocked by objects; spacing ...`; `harvested N (Pickable_Carrot=N) on the patch under you (about A m2); K not ripe or already picked; drops lie at each crop, ...`. Not on soil: `nothing planted: the soil under you is not cultivated ...`, `nothing harvested: stand on cultivated soil`. No points: `ERROR: coord 'start' is not set in world <world>: op=coord&set=start`.

Assumptions taken in the unattended run (Maksym reviews):

| # | Assumption | Instead of | Why |
|---|---|---|---|
| A16 | the rectangle's edges run along world X and Z | along the start-end line | simple, predictable (plan) |
| A17 | `field area=coords` plants every cultivated cell in the rectangle; `area=patch` = the connected patch; `harvest` = vanilla `Pickable.Interact` (drops on the ground) | a connection rule in rectangles too; items straight to the inventory | plan |
| W2-1 | `cultivate area=coords` over the cap is refused as a whole | cultivating the first 2 500 cells | a cut rectangle would leave a strip that was not asked for; same as the grid mode |
| W2-2 | `field area=patch` lays its crop grid along world X and Z over the patch's bounding box | along your look direction | a patch has no row direction; matches `area=coords` |
| W2-3 | `crops=` bands repeat along world Z with `area=` | one band each, the rest empty | an area has its own number of rows |
| W2-4 | `harvest` picks wild pickables on the patch as well, and takes no filter | crops only, or a `prefab=` filter | the card says every `Pickable` that `CanBePicked()`; a filter can be added opt-in later |
| W2-5 | `map=1` with `area=` is refused | a map of the area | the map shows field's own grid; an area map was not on the card |

### Test list for Maksym (not run yet)

World `ClaudeTest`, one command at a time. `op=reload` must name the new `HomeCheat.Cmd.world.v<stamp>.dll`; `op=help&cmd=harvest` shows the usage.

1. `op=cheatstate` (before).
2. `op=coord&set=start`, walk about 10 m along X and 6 m along Z, `op=coord&set=end`, then `op=cultivate&area=coords`: expect `cultivated 35 cells (7 x 5, ...)`; the soil matches the rectangle between the two markers.
3. Points 100 m apart: `op=cultivate&area=coords` answers `ERROR: cells=... is over the cap 2500` and changes nothing.
4. Stand in that rectangle: `op=field&prefab=Pickable_Carrot&area=coords`: crops on every soil cell in it, `0 cells without cultivated soil` or close.
5. Cultivate two touching patches (`op=cultivate&rows=3&perrow=3` twice, side by side) and a third 3 m away. Stand on the first: `op=field&prefab=Pickable_Carrot&area=patch` plants both touching patches, the third stays empty (counted as "not in this patch").
6. Same with `rows=5`: the answer adds `rows/perrow/dist ignored with area=`.
7. Grow or replace the crops (wait, or plant ripe `Pickable_Carrot` with `op=field` - they are ripe as placed), stand on the patch: `op=harvest` picks both touching patches and counts them, the third patch's crops stay; carrots near you go to the inventory, the far ones lie on the ground.
8. Step off the soil: `op=harvest` answers `nothing harvested: stand on cultivated soil`.
9. Without `area=`: `op=field&prefab=Pickable_Carrot&rows=4&perrow=4&map=1` prints the map as before (default unchanged).
10. Spell bar Field group: 7.6 Cultivate start-end, 7.7 Plant food... (asks area, then crop), 7.8 Harvest patch.
11. `op=cheatstate` (after); compare.

Undo: soil and crops as above (Hoe, Cultivator, pick or a world backup); picked crops are gone, replant with `op=field`.

## Trees in an area (since 2026-10-25)

Built 2026-10-25 in the unattended, no-game session W3-world-plant of the Valheim work plan (backlog card 3.13 plant-area; Valheim 1.0.20, BepInEx 5.4.2351). Goal: plant saplings between the two stored points, keep them away from buildings, and pick area, tree and size from the spell bar (7.9 Plant trees...). Before: `plant` only laid a `rows` x `perrow` block at the crosshair, with no size limit and no keep-away from buildings. Source: `src/Features/World/PlantCommand.cs`.

| Parameter | Default | Does |
|---|---|---|
| `area=aim\|self\|coords` | `aim` | `aim` = the block at the crosshair, as before. `self` = from you: row 1 one spacing ahead, rows running towards the crosshair. `coords` = the cells of the rectangle with `op=coord` start and end as opposite corners, edges along world X and Z, the grid centred in it (`GridMath.RectCells`, both edges included); `rows`, `perrow` (`cols`) and `aimcol` are ignored and the answer says so. Over 20 000 cells it is refused as a whole; at most 2 000 saplings are planted per cast (the answer says "cast again" for the rest). |
| `avoid=0\|1` | 0 | 1 = a cell with a player-built piece (`Kit.Constructions.Near`) within `clear` x `scale` + 2 m is skipped and counted; works with both areas. The 2 m margin is there because the distance is measured to a piece's pivot. |
| `scale` | 1 | now capped at 20: `scale=25` answers `ERROR: scale=25.00 is over the cap 20.00` and plants nothing. |

Spacing without `spacing=` stays twice the sapling's grow radius times the scale, in both areas. Answers (first line): `planted N Birch_Sapling x5.00 in the rectangle start x,y,z - end x,y,z (C x R cells), B blocked by objects (clear radius ...), K skipped near buildings (keep K m, e.g. wood_wall at 3.2 m), spacing ... m (growRadius ...)`; the crosshair form keeps its old answer, with the `skipped near buildings` part only when `avoid=1`. No points: `ERROR: coord 'start' is not set in world <world>: op=coord&set=start`.

The grown tree copies the sapling's scale (ZDO key `scale`). As with `op=scale`, only players with HomeCheat see a scaled tree at its size ([tree-scale.md](tree-scale.md), ASSUMPTION A7).

Assumptions taken in the unattended run (Maksym reviews):

| # | Assumption | Instead of | Why |
|---|---|---|---|
| A7 | size cap 20 on `plant`; scaled trees visible only with HomeCheat | no cap; sync-only prefabs | plan (Q3) |
| A16 | the rectangle's edges run along world X and Z | along the start-end line | plan; same as `cultivate area=coords` |
| W3-1 | keep-away radius = `clear` x `scale` + 2 m margin | `clear` x `scale` only (the card) | `Constructions` measures to a piece's pivot (its own note: pass your radius plus a margin); a wall's end reaches past its pivot |
| W3-2 | `plant area=coords` over 400 cells is refused as a whole | planting the first 400 | same rule as `cultivate area=coords` (W2-1) |
| W3-3 | `area=aim` is the explicit name of today's crosshair block | a new aim behaviour | the spell line 7.9 asks `Start-end=coords,Crosshair=aim`; the default stays unchanged |

### Test list for Maksym (trees, not run yet)

World `ClaudeTest`, one command at a time. `op=reload` must name the new `HomeCheat.Cmd.world.v<stamp>.dll`; `op=help&cmd=plant` shows the usage with `area` and `avoid`.

1. `op=cheatstate` (before).
2. Next to a wall: `op=coord&set=start`, walk about 15 m so the wall lies inside the rectangle, `op=coord&set=end`.
3. `op=plant&area=coords&prefab=Birch_Sapling&scale=5&avoid=1`: expect `planted N ... in the rectangle ... K skipped near buildings (keep ... m, e.g. <piece> at ... m)`; no sapling near the wall.
4. Same without `avoid=1` on fresh points: saplings also land next to the wall (default unchanged).
5. `op=plant&prefab=Birch_Sapling&scale=25`: `ERROR: scale=25.00 is over the cap 20.00`, nothing planted.
6. Points 300 m apart, `op=plant&area=coords&prefab=Birch_Sapling`: `ERROR: cells=... is over the cap 400`.
7. Crosshair form unchanged: `op=plant&prefab=Birch_Sapling&rows=1&perrow=5`: the 3rd sapling on the crosshair, answer ends `tree 3 of row 1 at ... (crosshair)`.
8. Spell bar 7.9 Plant trees...: asks area (Start-end / Crosshair), then tree, then size (1, 3, 5, 10, 20); always `avoid=1`.
9. `op=cheatstate` (after); compare.

Undo: saplings and grown trees are ordinary world objects (an axe, or a world backup).

## Undo

Planted and placed objects are ordinary world objects: remove them in game (Hoe/Cultivator for soil, an axe or the build tool for the rest) or restore a world backup (make a fresh one before big runs; see [backup-settings-lessons-learned.md](../../../AdminPanelEnchancements/docs/backup-settings-lessons-learned.md)). There is no mod-side undo for these commands; `op=despawn` removes the one object under the crosshair ([despawn.md](despawn.md)). To remove the commands themselves see [README.md](README.md#undo-and-uninstall).

## In-game test 2026-10-25

Maksym's test pass of the phase 4 commands, world `ClaudeTest` hosted on this PC, Claude2 (no mod) as the second player.

- **harvest** `collect=1` (new): the harvest goes into your inventory, amounts as the game's own `Pickable.RPC_Pick` (`Game.ScaleDrops`, extra drops), the crop marked picked through `RPC_SetPicked`, overflow at your feet one stack per item. Spell 7.3 Harvest patch. V ("all works").
- **Caps raised after real fields** (Maksym: "did not plant the whole field", "limit on Start-end"): `field` 10 000 per cast in batches of 500 (a 4 139 m2 patch had stopped at 2 500), `harvest` 10 000 in batches of 500, `plant` 2 000 saplings per cast in batches of 100 (`area=coords` refused only over 20 000 cells; 400 refused the whole rectangle before). A cut says "cast again to plant the rest". The runs that hit the old caps are V; the new caps are B.
- **Grow rules** (new, Maksym: "never plant what the game would not let grow"): `field` and `plant` skip cells where the plant would not grow (`GrowRules` from `Plant.UpdateHealth`: the biome must be in `Plant.m_biome`, cultivated ground where `m_needCultivatedGround`) and report "N not planted: the game would not let them grow there (<crop>: wrong biome (X; grows in Y))". B.
- **From me** (new): `plant area=self` = row 1 one spacing ahead of you, rows running towards the crosshair, columns centred; `cultivate area=self` and `field area=aim` name the old from-your-cell grid so a form can send them. B.
- Forms: 7.1 Cultivate... (Area, Rows, Items in row; greyed for Start-end), 7.2 Plant food... (Area Start-end / This patch / From me, crop, Rows, Items in row), 7.4 Plant trees... (Area, tree, Item Size 1-20, Rows, Items in row; `avoid=1`). Run in game (the cap reports above came from them).
- Spell bar: Oak grove, Carrots 3x5, Place (wall, campfire) and the Crops / Trees sub-groups were removed; `place` has no spell line now (constructions come from `spawn`).

Still untested (B): `plant avoid=1` next to a wall, the scale cap, `cultivate area=coords` cap, `field map=1` default, the grow-rule refusal on a wrong biome.
