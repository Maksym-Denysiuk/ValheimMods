---
description: ship command of the HomeCheat Valheim plugin (2026-10-25) - sail force multiplier x1 to x10 for the ship you are on, only while this PC owns the ship and hosts the world; multiplies the ship's sail force factor and backward paddle force, speed=1 restores, nothing saved; card, assumptions, how it works, test list for Maksym, limits, undo; built without the game, not yet run
---
# ship (sail force multiplier, feature Ship)

Built 2026-10-25 in phase 4, wave 1 (session W1-ship) on Valheim 1.0.20, BepInEx 5.4.2351, unattended and without the game: compiled and staged, **not run in game yet**. Own game and own world only. Source: [src/Features/Ship/ShipCommand.cs](../../src/Features/Ship/ShipCommand.cs), DLL `HomeCheat.Cmd.ship.v<stamp>.dll`. Plan card: backlog plan 3.7 (kept privately in HomeOS).

## Goal

Sail faster: the ship you are on gets x1 to x10 of its normal sail push, then back to normal with one command. Before: a Karve or Longship sails at the game's speed. After `op=ship&speed=5`: the same wind pushes it five times as hard; `speed=1` puts it back.

## Card

- Verdict: **new** command `ship` in a new feature `Ship` (no command touches ships; the catalog has no form/movement command for vehicles).
- Fingerprint: effect form/movement / target the ship you are on / placement none / mechanism `Ship.m_sailForceFactor`, `Ship.m_backwardForce`.
- Targets: no character is hit; the ship moves for everyone on it (position sync).
- Persists in the world: nothing. The two fields live on this PC's ship instance and are never written to the ZDO.
- Who sees it: everyone (the owner's physics drive the synced position).
- Probes: none needed before coding; the members were read offline with `il-reader.js` (below).
- Caps: `speed` 10 (refused above, `Caps.Check`).
- Undo: `speed=1`, `op=reload`, or leaving the world.

### Assumptions (unattended run, Maksym reviews)

| # | Assumption | Why |
|---|---|---|
| A8 | works only while this PC owns the ship (`Ship.IsOwner`), i.e. you steer it; otherwise the answer says so and nothing changes | ship physics run on the owner; on any other PC the changed fields are never used |
| S1 | host only: `Host.RequireServer()` before any change (`status=1` needs only a loaded world) | gate "own game, host only": the ship is shared world data |
| S2 | `speed` is required (`op=ship` alone is an error with the hint); fractional values allowed (`2.5`); below 1 refused | no safe default for a multiplier; slowing a ship down was not asked for |
| S3 | above 10 is refused, not lowered | refusing changes nothing unexpected |
| S4 | the backward paddle force is multiplied too (card); the rudder (`m_stearForce`) is not | the card names the two forces; at x10 turning may feel weak, see the test list |
| S5 | the multiplier always applies to the values before the first change (no compounding: x5 then x10 = x10) | predictable, and `speed=1` returns exactly the original |
| S6 | `speed=1` restores even when another player owns the ship | restoring local fields is harmless |

## Use

`op=ship&speed=<1..10>`, `op=ship&status=1`

| Parameter | Default | Meaning |
|---|---|---|
| `speed` | required | multiplier 1 to 10 (cap 10, refused above); 1 = normal (restores) |
| `status` | - | `status=1`: the ship you are on, its owner (this PC or another player), the current multiplier, speed in m/s, the two force values and their normal values; read-only |

Answers (first line):
- `<ship prefab>: speed x5.0 (sail force factor <normal> -> <new>, backward force <normal> -> <new>)`
- `<ship prefab>: restored to normal (sail force factor <normal>)`, or `already normal (speed=1)`
- errors: `not on a ship: board one first`, `<ship>: another player owns this ship; take the helm first ...`, `speed=12.00 is over the cap 10.00`, `speed must be 1..10 ...`, `host only: ...`, `speed required, ...`

Spell bar (`spells.txt`, section 4 Fun): `4.10 Ship x5` = `op=ship&speed=5`, `4.11 Ship x10` = `op=ship&speed=10`, `4.12 Ship normal` = `op=ship&speed=1`.

## How it works

Read offline with `scripts/valheim/lib/il-reader.js` (HomeOS) on `assembly_valheim.dll` 1.0.20:

- `Ship.GetLocalShip()` (static) returns the last ship in `s_currentShips`, the ships the local player is standing in; null when on none.
- `Ship.IsOwner()` = `m_nview.IsValid() && m_nview.IsOwner()`. The owner is the player at the helm (`UpdateOwner`), else the host.
- `Ship.GetSailForce` builds the wind push as `... * m_sailForceFactor * sailSize`, smoothed into `m_sailForce`; `Ship.CustomFixedUpdate` uses `m_backwardForce` for paddling back. Both are public instance floats.
- `HaveControllingPlayer()` is private in 1.0.20, so `status=1` reports the owner only.

The command keeps a dictionary ship -> original values. Each change sets `field = original x speed`. `speed=1` writes the originals back and forgets the ship. `OnUnload` (every `op=reload` of a newer build) restores every changed ship that still exists; `OnWorldExit` forgets them (the ships go with the world).

Change procedure: edit `ShipCommand.cs`, `bash src/build.sh ship`, `op=reload`, `op=help&cmd=ship`.

## Test list for Maksym (not run yet)

World `ClaudeTest`, hosted on this PC, a Karve or Longship in the water. `op=cheatstate` before the first and after the last test.

1. `op=reload` -> names `HomeCheat.Cmd.ship.v<stamp>.dll`, no `FAILED` line. `op=help&cmd=ship` -> the usage above.
2. On land: `op=ship&speed=5` -> `ERROR: not on a ship: board one first`.
3. Board, do not steer: `op=ship&status=1` -> `<Karve>: speed x1.0, owner ...` (note which owner it reports while nobody steers; the host may own it).
4. Take the helm, sail up: `op=ship&status=1` -> owner this PC, note the m/s at full sail.
5. `op=ship&speed=5` -> `speed x5.0 (sail force factor a -> 5a, ...)`; sail the same heading: is it clearly faster? `status=1` m/s.
6. `op=ship&speed=10` -> x10.0 from the original (not x50). Watch stability: does the ship tip, bounce or drift? Does steering still work?
7. Paddle back (backward setting) at x10: faster backward, still controllable?
8. `op=ship&speed=12` -> `ERROR: speed=12 is over the cap 10`; `op=ship&speed=0.5` -> `ERROR: speed must be 1..10 ...`; `op=ship` -> `ERROR: speed required ...`.
9. `op=ship&speed=1` -> `restored to normal`; speed back to step 4.
10. `op=ship&speed=5`, then `op=reload` after a rebuild (or `bash src/build.sh ship` + `op=reload`) -> `LogOutput.log` line `ship: restored 1 ship(s) on unload`; the ship sails normally.
11. Spell bar: casts 4.10, 4.11, 4.12 from section Fun give the same answers as steps 5, 6, 9.
12. Optional, with `Claude2` steering the ship: `op=ship&speed=5` from this PC -> `another player owns this ship`.

Record each first answer line and what was seen in the table below, then set the index status to V.

| Time | Command | Answer (first line) | Observation | Verdict |
|---|---|---|---|---|

## Limits

- Only the ship you stand in, and only while this PC owns it. If someone else takes the helm, they sail at normal speed; the changed values stay on this PC's instance and work again when you take the helm back.
- Nothing is saved: after leaving the world or restarting the game every ship is normal.
- Wind still matters: the factor multiplies the wind push, so no wind still means no sail force.
- Not tested: behaviour at x10 in storms, collisions at high speed (hull damage), the Drakkar and other ship types.

## Undo

`op=ship&speed=1` on the ship. `op=reload` of a newer `ship` build restores every changed ship; leaving the world forgets them (nothing was saved). To remove the command, delete `HomeCheat.Cmd.ship.*.dll` from `BepInEx/homecheat-commands/` and restart the game ([README.md](README.md#undo-and-uninstall)).
