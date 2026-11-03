---
description: backlog card materials-nearby (approved 2026-11-03) - build and plant take materials from you, then safe chests, then build free
---
# materials-nearby: build and plant take materials from you, then safe chests, then build free

Status: Kit helper B (built 2026-11-03, selftest; in-game test with its first user, hc-materials); the five command users: approved, wave 2 · Verdict: Kit helper (0-core) + extend five commands with `cost=nearby` · Nearest: dome `cost=1` (read)
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
- In-game test: hc-materials (no command uses the helper yet).

## Test log

| Time | Command | Answer (first line) | Observation | Verdict |
|---|---|---|---|---|
