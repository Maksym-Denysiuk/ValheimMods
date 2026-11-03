---
description: backlog card materials-nearby (approved 2026-11-03) - build and plant take materials from you, then safe chests, then build free
---
# materials-nearby: build and plant take materials from you, then safe chests, then build free

Status: approved (2026-11-03) · Verdict: Kit helper (0-core) + extend five commands with `cost=nearby` · Nearest: dome `cost=1` (read)
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

## Test log

| Time | Command | Answer (first line) | Observation | Verdict |
|---|---|---|---|---|
