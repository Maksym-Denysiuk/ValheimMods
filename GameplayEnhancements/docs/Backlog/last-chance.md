---
description: backlog card last-chance (approved 2026-11-03) - a killing hit becomes a random blink
---
# last-chance: a killing hit becomes a random blink

Status: approved (2026-11-03) · Verdict: new `lastchance` in feature Teleport (reuses `BlinkCommand`) · Nearest: blink (catalog, page read)
Fingerprint: form/movement + push / self / a line in a random direction / damage-path patch + blink

- Behaviour: when a hit would bring your health to 0, the hit is cancelled (no damage, no healing: health stays as it was) and you blink as spell 9.3 does (10 m, shove and tar on the line, `pvp=1` rule) in a random direction (0-360 degrees, flat). Then 60 s cooldown, during which a killing hit kills as normal.
- On by default while HomeCheat is loaded (Maksym's choice). Spell 9.5 Last chance: `op=lastchance&on=1`, Shift = `on=0`; `status=1` shows on/off and the cooldown left. State per world in `teleport.state`.
- The blink's line avoids nothing; the world edge refuses as blink does, and then the hit goes through.
- Mechanism: a prefix on the local player's damage application (probe decides `Character.ApplyDamage` vs `RPC_Damage`), gated on `Current`, local player only.
- Gate 6: no god mode or cheat field is written; one save per 60 s.
- Probes: the damage path in this game version; blink from a patch without a spell-bar cast.
- Defaults: on; `cooldown=60` s (10-3600). Docs: `Spells/teleport.md`, index, catalog, Extension points (a guard on the local player's damage).
- Test: low health, `spawn prefab=Troll`, the killing hit → blink, health unchanged; a second killing hit within 60 s kills.

## Test log

| Time | Command | Answer (first line) | Observation | Verdict |
|---|---|---|---|---|
