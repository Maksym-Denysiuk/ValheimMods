# despawn: remove the object under the crosshair

Status: testing (built 2026-10-25, HomeCheat.Cmd.despawn.v20261025173005)
Verdict: new (no command removes an arbitrary object; every existing removal acts only on its own ZDO-marked objects: `trap remove=1`, `dome step=remove`, `construct` undo, `earthquake clear=1`; `killmobs` kills with damage and leaves drops)
Nearest: `scale aim=1` (crosshair -> one object, `Aim.SolidHit`), `trap remove=1` / `DomeWards.Remove` (destroy path `ClaimOwnership` + `ZNetScene.Destroy`), `killmobs` (tamed/boss opt-in) (catalog rows, pages, source read: yes)
Fingerprint: effect remove object / target the one object under the crosshair / placement crosshair / mechanism `ZNetView` + `ZNetScene.Destroy` (ZDO deleted for everyone)

## What it does
- Trigger: `op=despawn`; spell bar Build tab 6.8 "Despawn" `op=despawn&confirm=1` (line sent to the spell-file owner session)
- Targets: self off (never), other players off (never), enemies on, tamed off (`tamed=1`), bosses off (`boss=1`); objects: dropped items, pieces, trees, rocks, bushes, ships and carts
- Refused always: players, tombstones (a player's items), objects that belong to a location (trader, altars, runestones, dungeon parts), objects without a networked `ZNetView`, terrain
- Placement: crosshair, camera ray `range=60` (cap 100); the first hit that has a networked `ZNetView` and is not the local player
- Magnitude: one object per cast
- Persists in the world: the object's ZDO is deleted, for every player; no drops
- Who sees it: everyone (host)
- Visual reference: none (later with /valheim-spell-fx if wanted)
- Probes: none
- Defaults: `range=60`, `tamed=0`, `boss=0` (works with only `op=despawn`)
- Caps: `range` 100 (`Caps.Check`)
- Undo: none (world backup)

## How it is built
- Feature folder: `src/Features/Despawn/` (new); class `DespawnCommand : CommandBase`
- Sub-actions: `look=1` (read-only: what would be removed)
- OnLoad needs: nothing; refs.txt none
- Kit reused: `Host.RequireServer`, `Players.Local`, `Masks`, `Prefabs.BaseName`, `Fmt`, `Caps`; own ray mask (Solid + character, item layers)
- Core change: none
- Guards: `Host.RequireServer()`, `Players.Local`, `Caps.Check(range)`; `confirm=1` for pieces, ships, carts
- Docs: page `HomeCheat-plugin/despawn.md`, index row, catalog row

## Open questions
Answered by Maksym 2026-10-25 (all as recommended):
1. Skill refresh due (start-of-run check): build despawn now, refresh afterwards in its own session.
2. Scope: creatures, dropped items, pieces, trees, rocks, bushes, ships, carts. Always refused: players, tombstones, location objects (trader, altars, runestones, vegvisir, dungeon parts, anything not player-built inside a location), pieces another player built unless `others=1`.
3. Tamed and bosses: opt-in `tamed=1` / `boss=1`.
4. Confirm: player-built pieces, ships and carts need `confirm=1`; the Build-tab spell line carries `confirm=1`.
5. Menu: Build tab (Maksym: "should be placed in a build tab"), id 6.8 "Despawn" (6.4 was the spawn menu, not reused).

## Test plan
1. `op=cheatstate`
2. `op=despawn&look=1` at a spawned Boar -> `would despawn Boar at <d> m (creature)`
3. `op=despawn` at the Boar -> `despawned Boar at <d> m`; Maksym: gone, no drops?
4. a dropped item, a tree, a rock, a wood wall (with confirm as decided), a ship
5. refusals: Claude2, a tamed wolf, a tombstone, Haldor/a runestone
6. `op=cheatstate`

## Test log
| Time | Command | Answer (first line) | Observation | Verdict |
|---|---|---|---|---|
