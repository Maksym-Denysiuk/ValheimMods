---
description: backlog card bow-charge (approved 2026-11-03) - holding a drawn bow adds spell arrows
---
# bow-charge: holding a drawn bow adds spell arrows

Status: approved (2026-11-03) · Verdict: new `bowcharge` in feature Spellshot (`: AttackCommand`, reuses the arrow fire) · Nearest: spellshot
Fingerprint: damage + push / enemies (players by the PvP rule) / grid along the crosshair / bow `Attack` patch + spellshot `Projectile`

- Behaviour: while you hold a vanilla bow, a timer starts once the game's full draw is reached. Every whole second after that adds 1 extra arrow, up to 25 (a 5x5 matrix). On release, the normal bow shot fires, plus the extra arrows as spellshot `type=arrow`: a centred grid of n arrows (ceil(sqrt n) per row, rows filled in order, at most 5x5), from the crosshair centre, along the crosshair direction (aim at the sky and they go there). Example: held 10 s with about 2 s to full draw → 8 extra arrows.
- Arrows: spell arrows (no ammo, spellshot damage), as Maksym said ("standart spell logic").
- On by default while loaded ("bound to bow charge, not to bindings"). Spell 2.8 Bow charge toggles it (`op=bowcharge&on=1`, Shift = off); `status=1`.
- PvP: `pvp=1` in its settings (every attack spell carries it): with your PvP on other players are hit. Tamed per card pvp-tamed.
- Probes: the bow's draw in this game version (`Humanoid.GetAttackDrawPercentage`, `Attack.m_attackDrawPercentage`, the release method).
- Caps: 25 arrows (`Caps.Clamp`, named in the log line). Crossbows have no hold: not affected.
- Docs: `Spells/spellshot.md` (or a new `Spells/bowcharge.md`), index, catalog.
- Test: hold 3 s past full draw → 3 extra arrows; 30 s → 25; aim up → arrows go up; Claude2 with PvP: hit only when your PvP is on.

## Test log

| Time | Command | Answer (first line) | Observation | Verdict |
|---|---|---|---|---|
