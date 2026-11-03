---
description: backlog card despawn-anything (approved 2026-11-03) - despawn everything except the spawn's own objects and traders
---
# despawn-anything: despawn everything except the spawn's own objects and traders

Status: approved (2026-11-03) · Verdict: extend `despawn` with `inloc=1`; preset 6.8 passes `others=1&tamed=1&boss=1&inloc=1` · Nearest: despawn (`DespawnCommand.cs:100-152` read)
Fingerprint: remove object / any object / crosshair / `ZNetScene.Destroy`

- Why your items are refused: an object without a creator falls to `Location.GetLocation(pos)`, and inside the spawn area (or any location radius) that refuses everything. Others' pieces need `others=1`, which 6.8 does not send.
- With `inloc=1` only these stay refused: players, tombstones, the start temple's own objects, trader camps' own objects and the traders themselves (Haldor, Hildir, the Bog Witch; prefab names from the probe). "Own objects" = matches a `ZNetView` child of that location's prefab by prefab name and position (0.5 m), so Maksym's items inside the spawn area are allowed. Other locations' objects (runestones, altars, dungeons elsewhere) are allowed.
- Defaults: a typed `op=despawn` keeps today's rules (gate 4); 6.8 sends the four flags.
- Probes: `despawn&look=1` on a spawn stone, Maksym's item at spawn, a Claude2 wall, Haldor; the location prefab names.
- Docs: `HomeCheat-plugin/despawn.md`, index, catalog.
- Test: your item at spawn → gone; Claude2's wall → gone; spawn runestone, Haldor, a tombstone → refused.

## Test log

| Time | Command | Answer (first line) | Observation | Verdict |
|---|---|---|---|---|
