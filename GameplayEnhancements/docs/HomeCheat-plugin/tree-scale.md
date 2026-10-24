---
description: scale command of the HomeCheat Valheim plugin (2026-10-24) - change the size of trees near the player (oak, beech, birch tested) with matching hit points and drops, saved with the world and re-applied on load by a Harmony patch; parameters, how it works, tests, limits, undo
---
# scale (tree resize, HomeCheat core op)

Built in the CheatResearch session (first version 2026-10-23/24) on Valheim 1.0.20, BepInEx 5.4.2351, host `MDENYSIUK-PERS`, tested 2026-10-24 in our own world. Own game and own world only. Source: core `HomeCheat.cs` (`ScaleTrees`, `ApplyTreeScale`, `ZNetViewAwakePatch`), archived as [HomeCheat.cs.txt](HomeCheat.cs.txt).

## What it does

Changes the size of every tree (`TreeBase`) whose prefab name contains `filter` within `radius` metres of the player.

1. The tree's local scale becomes `factor` on all three axes.
2. Hit points follow: `m_health` = the prefab's health x factor, so a bigger tree takes longer to fell.
3. Drops follow: the minimum and maximum drop of the tree are the prefab's values x factor (rounded, at least 1).
4. The factor is written to the tree's ZDO key `scale`, so it is saved with the world and other clients that receive the object can read it.
5. The game itself ignores a saved scale on trees when they load. A Harmony postfix on `ZNetView.Awake` (`ZNetViewAwakePatch`) re-applies it (size, health, drops) for every tree whose saved scale differs from 1, so big trees stay big after a reload.

## Use

`op=scale&filter=<name part>[&factor=2][&radius=50]`

| Parameter | Default | Meaning |
|---|---|---|
| `filter` | required | part of the tree prefab name, case-insensitive: `Oak`, `Beech`, `Birch`, `Pine`, ... |
| `factor` | 2 | size multiplier; 1 = vanilla size |
| `radius` | 50 | metres around the player |

Answer: `scaled N 'Oak' trees within 50 m to x2`. The spell-bar preset "Oaks x2" is `op=scale&filter=Oak&factor=2&radius=30`. `scale=` on `plant`, `field` and `place` ([plant-field-place.md](plant-field-place.md)) sets the same ZDO key for new objects; a sapling's scale is copied by the grown tree.

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
