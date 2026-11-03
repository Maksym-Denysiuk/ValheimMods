---
description: backlog card last-chance (approved 2026-11-03; built 2026-11-03, not run in game) - a killing hit becomes a random blink
---
# last-chance: a killing hit becomes a random blink

Status: **built 2026-11-03, not run in game** (B; hc-teleport) · Verdict: new `lastchance` in feature Teleport (reuses `BlinkCommand`) · Nearest: blink (catalog, page read)
Fingerprint: form/movement + push / self / a line in a random direction / damage-path patch + blink

- Behaviour: when a hit would bring your health to 0, the hit is cancelled (no damage, no healing: health stays as it was) and you blink as spell 9.3 does (10 m, shove and tar on the line, `pvp=1` rule) in a random direction (0-360 degrees, flat). Then 60 s cooldown, during which a killing hit kills as normal.
- On by default while HomeCheat is loaded (Maksym's choice). Spell 9.5 Last chance: `op=lastchance&on=1`, Shift = `on=0`; `status=1` shows on/off and the cooldown left. State per world in `teleport.state`.
- The blink's line avoids nothing; the world edge refuses as blink does, and then the hit goes through.
- Mechanism: a prefix on the local player's damage application (probe decides `Character.ApplyDamage` vs `RPC_Damage`), gated on `Current`, local player only.
- Gate 6: no god mode or cheat field is written; one save per 60 s.
- Probes: the damage path in this game version; blink from a patch without a spell-bar cast.
- Defaults: on; `cooldown=60` s (10-3600). Docs: `Spells/teleport.md`, index, catalog, Extension points (a guard on the local player's damage).
- Test: low health, `spawn prefab=Troll`, the killing hit → blink, health unchanged; a second killing hit within 60 s kills.
- Also (wave 0 Known defect): blink's shove takes `PvpHits.HitsTamed(c)` in its target filter.

## What was built (2026-11-03)

- Probe (offline, il-reader on the updated game): `Character.ApplyDamage` is the method that sets the health after `RPC_Damage`'s blocking, armour and resistances; one method, no `Player` override; a player's total is damage x `Game.m_localDamgeTakenRate`, below 0.1 = no damage. So the guard is a prefix on `ApplyDamage` (`LastChancePatch`), not `RPC_Damage`.
- `LastChanceCommand` (`on=`, `cooldown=`, `status=1`, `probe=1`; state in `teleport.state`, cooldown in world time), `LastChancePatch`; `BlinkCommand.Dash` split out of blink's `Execute` (same dash, also called by the guard), `BlinkCommand.Current`.
- Blink's tamed filter: `!tamed && c.IsTamed() && !(hitPlayers && PvpHits.HitsTamed(c))` skips; `tamed=1` unchanged (all, yours too).
- Spell 9.5: the "(soon)" placeholder swapped for its real form (`op=lastchance&on=1`, Shift `on=0`), no other line changed.
- Build `HomeCheat.Cmd.teleport.v20261103195038.dll`; `build.sh selftest` 96 passed; hygiene grep clean; `commands-check.js` rows agree (only the expected "knowledge-sha behind": refresh R13).
- ASSUMPTION: the guard saves every killing hit through `ApplyDamage`, fall, drowning and burning included (the card says "a hit"; all take the same path). Change if Maksym wants enemy hits only.
- ASSUMPTION: the blink always follows the `pvp=1` rule of 9.3 (no `pvp=` key on lastchance), as the card says "as spell 9.3 does".

## Test list (not run: the game was closed)

Page: [teleport.md, lastchance tests](../Spells/teleport.md#lastchance-tests), L1-L7: probe, status, the Troll's killing hit -> blink with health unchanged, a second killing hit within 60 s kills, `on=0` kills, blink over your own tamed creature with PvP on spares it (and shoves one that is not yours), clean-up.

## Test log

| Time | Command | Answer (first line) | Observation | Verdict |
|---|---|---|---|---|
