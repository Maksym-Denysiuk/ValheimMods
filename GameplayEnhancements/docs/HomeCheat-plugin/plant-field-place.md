---
description: plant, field, cultivate and place commands of the HomeCheat Valheim plugin (2026-10-24) - saplings or crops or any prefab in a rows x per-row block anchored at the crosshair; parameters and defaults, spacing and clearance rules, scale, limits, undo; built in CheatResearch, field verified with carrots, plant and place not yet recorded as run
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
| `op=plant` | `prefab` (find names with `op=list&filter=sapling`) | `rows` (1), `perrow` (1, alias `cols`), `spacing` (auto), `aimcol` ((perrow-1)/2), `scale` (1), `clear` (auto) |
| `op=field` | `prefab` or `crops` | `rows` (1), `perrow` (1), `spacing` (1.5), `scale` (1), `clear` (0.4), `dist` (anchor in metres ahead of the player instead of the crosshair) |
| `op=cultivate` | none | `rows` (1), `perrow` (1), `spacing` (1.5), `dist` |
| `op=place` | `prefab` | `rows` (1), `perrow` (1), `spacing` (1.5), `aimcol`, `scale` (1), `amount` (1, stack size for items), `level` (0, star level for creatures), `lift` (0 m above ground), `clear` (0 = no check, so pieces can be placed anywhere) |

Examples: `op=plant&prefab=Birch_Sapling&rows=3&perrow=5`, `op=cultivate&rows=3&perrow=5`, `op=field&crops=Pickable_Carrot:2,Pickable_Onion:1&perrow=5`, `op=plant&prefab=Birch_Sapling&scale=2`.

Limits: plant and place refuse above 400 objects, field examines at most 40000 cells and plants at most 2500. `scale` above 0 only. A missing `prefab` returns an error on purpose (no default, so a typo cannot plant the wrong thing). Answers: `planted N <prefab> x<scale> (R rows x C), B blocked by objects (clear radius ...), spacing ... m (growRadius ...), tree K of row 1 at x,y,z (crosshair)`; `placed N ...`; `nothing planted: no cultivated soil in the RxC grid (use op=cultivate first)`.

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

## Undo

Planted and placed objects are ordinary world objects: remove them in game (Hoe/Cultivator for soil, an axe or the build tool for the rest) or restore a world backup (make a fresh one before big runs; see [backup-settings-lessons-learned.md](../backup-settings-lessons-learned.md)). There is no mod-side delete command. To remove the commands themselves see [README.md](README.md#undo-and-uninstall).
