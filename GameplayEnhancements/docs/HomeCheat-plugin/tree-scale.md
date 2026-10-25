---
description: scale command of the HomeCheat Valheim plugin (2026-10-24, aim and cap 2026-10-25) - change the size of trees near the player or the tree under the crosshair (oak, beech, birch tested) with matching hit points and drops, saved with the world and re-applied on load by a Harmony patch; parameters, how it works, tests, limits, undo
---
# scale (tree resize, HomeCheat core op)

Built in the CheatResearch session (first version 2026-10-23/24) on Valheim 1.0.20, BepInEx 5.4.2351, host `MDENYSIUK-PERS`, tested 2026-10-24 in our own world. Own game and own world only. Source: the command in [src/Features/World/ScaleCommand.cs](../../src/Features/World/ScaleCommand.cs); size, hit points and drops in the core [Kit/Trees.cs](../../src/Core/Kit/Trees.cs), the reload patch in the core [Builtins/TreeScalePatch.cs](../../src/Core/Builtins/TreeScalePatch.cs) (until 2026-10-25 `ScaleTrees`, `ApplyTreeScale`, `ZNetViewAwakePatch` in `HomeCheat.cs`).

## What it does

Changes the size of every tree (`TreeBase`) whose prefab name contains `filter` within `radius` metres of the player.

1. The tree's local scale becomes `factor` on all three axes.
2. Hit points follow: `m_health` = the prefab's health x factor, so a bigger tree takes longer to fell.
3. Drops follow: the minimum and maximum drop of the tree are the prefab's values x factor (rounded, at least 1).
4. The factor is written to the tree's ZDO key `scale`, so it is saved with the world and other clients that receive the object can read it.
5. The game itself ignores a saved scale on trees when they load. A Harmony postfix on `ZNetView.Awake` (`ZNetViewAwakePatch`) re-applies it (size, health, drops) for every tree whose saved scale differs from 1, so big trees stay big after a reload.

## Use

`op=scale&filter=<name part>|*[&factor=2][&radius=50]`, `op=scale&aim=1[&factor=2]` (factor cap 20)

| Parameter | Default | Meaning |
|---|---|---|
| `filter` | required (not with `aim=1`) | part of the tree prefab name, case-insensitive: `Oak`, `Beech`, `Birch`, `Pine`, ...; `*` = every tree |
| `factor` | 2 | size multiplier; 1 = vanilla size; cap 20 |
| `radius` | 50 | metres around the player |
| `aim` | 0 | 1 = only the tree under the crosshair (60 m); see [below](#the-tree-under-the-crosshair-since-2026-10-25) |

Answer: `scaled N 'Oak' trees within 50 m to x2`. The spell-bar preset "Oaks x2" is `op=scale&filter=Oak&factor=2&radius=30`. `scale=` on `plant`, `field` and `place` ([plant-field-place.md](plant-field-place.md)) sets the same ZDO key for new objects; a sapling's scale is copied by the grown tree.

## The tree under the crosshair (since 2026-10-25)

Built 2026-10-25 in the unattended, no-game session W3-world-plant of the Valheim work plan (backlog card 3.9 scale-aim; Valheim 1.0.20, BepInEx 5.4.2351). Goal: grow the one tree you look at x3, x5 or x10 from the spell bar (4.7-4.9) without a name filter. Before: only every tree matching `filter` in a radius, no size limit. Source: `src/Features/World/ScaleCommand.cs`.

| Parameter | Default | Does |
|---|---|---|
| `aim=0\|1` | 0 | 1 = only the tree under the crosshair (`Aim.SolidHit`, 60 m, then `GetComponentInParent<TreeBase>`); `filter` and `radius` are not needed and are ignored |
| `filter=*` | - | every tree in the radius, whatever its name; `filter` is still required when `aim` is off |
| `factor` | 2 | now capped at 20: `factor=25` answers `ERROR: factor=25.00 is over the cap 20.00` and changes nothing; `factor` must be above 0 |

Answers: `scaled 1 tree (Birch) under the crosshair at 6.2 m from you to x3.00 (seen only by players with HomeCheat)`; `ERROR: nothing under the crosshair (60 m): look at a tree`; `ERROR: the object under the crosshair (<name>) is not a tree`. Without `aim` the answer is as before.

Who sees it (ASSUMPTION A7, on Maksym's review list): the size is written to the tree's ZDO, but vanilla does not apply a saved scale to trees, so only players with HomeCheat (`TreeScalePatch`) see the big tree; the spell labels say "(only you see)". Cap 20 on `scale` and on `plant scale=` (A7).

### Test list for Maksym (not run yet)

World `ClaudeTest`, one command at a time, after `op=reload` names the new `HomeCheat.Cmd.world.v<stamp>.dll`.

1. `op=cheatstate` (before).
2. Look at a birch: `op=scale&aim=1&factor=3`: `scaled 1 tree (Birch...) under the crosshair ... to x3.00`; only that birch grows.
3. `op=scale&aim=1&factor=25`: `ERROR: factor=25.00 is over the cap 20.00`.
4. Look at the sky or a rock: `ERROR: nothing under the crosshair` / `... is not a tree`.
5. `op=scale&filter=*&radius=10`: every tree within 10 m at x2.
6. Old form unchanged: `op=scale&filter=Oak&factor=2&radius=30` (spell 4.5).
7. Spell bar 4.7-4.9 (Tree x3, x5, x10, "only you see") on a tree you look at.
8. Leave and reload the world: the scaled trees keep their size (`TreeScalePatch`).
9. `op=cheatstate` (after); compare.

## Verification

Run live in game on 2026-10-24, answers quoted from the CheatResearch transcript, accepted by Maksym ("worked on oaks", "works" for birch):

- `scaled 1 'Oak' trees within 100 m to x5` (one oak, confirmed in game)
- `scaled 10 'Oak' trees within 300 m to x3` and `scaled 17 'Beech' trees within 40 m to x5`
- `scaled 6 'Beech' trees within 40 m to x2`
- `scaled 2 'Birch' trees within 25 m to x3`

Not tested: reloading the world after scaling (the Harmony re-apply on load), how another connected player sees the resized trees, felling a big tree (hit points and drop counts were set from the prefab values, not measured).

## Limits

- Only trees that are currently loaded (near the player) are found; unloaded zones are not touched.
- Stumps and logs (`TreeLog`, `Stub`) are not `TreeBase` and keep their size.
- Ownership of the tree is claimed to write the value. On a server that is not ours this may be refused or reverted; not tested.

## Undo

Run the same filter and radius with `factor=1`: size, hit points and drops go back to the prefab values and the saved scale becomes 1 (the load patch ignores a scale of 1). Trees outside the radius or not loaded at that moment keep their size until you do it again near them. Restoring a world backup also works. To remove the command and the load patch, see [README.md](README.md#undo-and-uninstall) (per the code comment vanilla ignores a saved tree scale on load, so without the plugin scaled trees should load at normal size; not tested).
