---
description: backlog card despawn-anything (approved 2026-11-03) - despawn everything except the spawn's own objects and traders
---
# despawn-anything: despawn everything except the spawn's own objects and traders

Status: built 2026-11-03 (B, not run in game; `HomeCheat.Cmd.despawn.v20261103195028`, selftest 96/0) · Verdict: extend `despawn` with `inloc=1`; preset 6.8 passes `others=1&tamed=1&boss=1&inloc=1` · Nearest: despawn (`DespawnCommand.cs:100-152` read)
Fingerprint: remove object / any object / crosshair / `ZNetScene.Destroy`

- Why your items are refused: an object without a creator falls to `Location.GetLocation(pos)`, and inside the spawn area (or any location radius) that refuses everything. Others' pieces need `others=1`, which 6.8 does not send.
- With `inloc=1` only these stay refused: players, tombstones, the start temple's own objects, trader camps' own objects and the traders themselves (Haldor, Hildir, the Bog Witch; prefab names from the probe). "Own objects" = matches a `ZNetView` child of that location's prefab by prefab name and position (0.5 m), so Maksym's items inside the spawn area are allowed. Other locations' objects (runestones, altars, dungeons elsewhere) are allowed.
- Defaults: a typed `op=despawn` keeps today's rules (gate 4); 6.8 sends the four flags.
- Probes: `despawn&look=1&inloc=1` on a spawn stone, Maksym's item at spawn, a Claude2 wall, Haldor (the second answer line names the location rule). Location prefab names read from the game's asset data 2026-11-03: `StartTemple`, `Vendor_BlackForest`, `Hildir_camp`, `BogWitch_Camp`; traders refused by their `Trader` component, so their names need no list. Own objects: `ZoneSystem.m_locations` prefab, its `ZNetView` children at `location position + rotation * child position` (`ZoneSystem.SpawnLocation` IL read 2026-11-03).
- Docs: `HomeCheat-plugin/despawn.md`, index, catalog.
- Test: your item at spawn → gone; Claude2's wall → gone; spawn runestone, Haldor, a tombstone → refused.

## Test log

| Time | Command | Answer (first line) | Observation | Verdict |
|---|---|---|---|---|

Test list (run when the game is up): [despawn.md, inloc=1 test list](../HomeCheat-plugin/despawn.md#inloc1-test-list-built-2026-11-03-homecheatcmddespawnv20261103195028-not-run-in-game-b).
