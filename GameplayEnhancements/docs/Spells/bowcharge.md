---
description: bowcharge command of the HomeCheat Valheim plugin (built 2026-11-03, verified in game 2026-11-04) - hold a vanilla bow past its full draw and every whole second adds one spell arrow (cap 25, a 5x5 grid) fired on release with the normal arrow, from the crosshair centre along the crosshair; on by default, spell 2.8 (Shift = off), the pvp=1 rule; card, the bow draw probe, how it works, test list, limits, undo
---
# bowcharge (feature Spellshot)

> **Verified in game 2026-11-04** (status V): T1 passed; the PvP rows T6-T7 and the rest of the list are under [Tests](#tests).

Valheim after 1.0.20 (probed 2026-11-03), BepInEx 5.4.2351, own game and own world only. Source: [src/Features/Spellshot/](../../src/Features/Spellshot/) (`BowChargeCommand.cs`, `BowChargePatch.cs`; the arrow fire is `SpellShotCommand.Fire`), DLL `HomeCheat.Cmd.spellshot.v<stamp>.dll`.

## Goal

Before: a bow fires one arrow, however long you hold it. After: holding a vanilla bow past its full draw charges it. Every whole second at full draw adds one spell arrow, up to 25. On release the normal arrow flies as usual, plus the extra arrows as a centred grid along the crosshair. Example: held 10 s with about 2 s to full draw = 8 extra arrows.

## Card

Approved 2026-11-03 (plan item 5; Maksym: "timer starts after when max vanila hold is reached. Then 1 arrow is added once per second", "max arrows - matrix 5x5, spawn them following standart spell logic, from center of the croshair", "It is bound to bow charge, not to bindings by default").

- Verdict: new `bowcharge` in feature Spellshot, `BowChargeCommand : AttackCommand`; nearest command `spellshot`, whose arrow fire it reuses.
- Fingerprint: damage + push / enemies (other players by the PvP rule) / grid along the crosshair / a patch on the bow draw + spellshot `Projectile`.
- Arrows: spellshot `type=arrow` (`ArrowNeedle`'s projectile `bow_projectile_needle`, the needle arrow's damage, 60 m/s), no ammo taken. The normal shot is the game's own and uses your arrow as always.
- Grid: n arrows, ceil(sqrt n) per row, rows filled top to bottom, each row centred on its own count (a short last row stays centred); 0.6 m apart, 1 m ahead of your eye, all along the camera's forward (aim at the sky and they go there). 8 arrows = rows of 3, 3, 2; 25 = 5x5.
- On by default while loaded; `on=0|1`; `status=1`, `probe=1`. Spell 2.8 Bow charge sends `on=1&pvp=1`, Shift `on=0`.
- PvP: follows your PvP switch from the start, no spell needed (Maksym 2026-11-03: "Bow should follow user settings"). The `AttackCommand` rule, read at each release: your PvP on = other players (whatever their own setting) and their tamed creatures are hit, yours spared; off = other players and every tamed creature spared. The normal arrow is the game's own and is not under this rule.
- `pvp=0` opts out: unmarked arrows, as a vanilla bow. With your PvP on those hit players whose PvP is also on and every tamed creature, yours too (`Projectile.IsValidTarget`; `PvpTamedPatch` only sees marked shots).
- The settings live in memory only: a game restart or `op=reload` brings back on, `pvp=1`.
- ASSUMPTION: no extra arrows when the game's own shot does not start (no ammo, no stamina or eitr): "the normal bow shot fires, plus the extra arrows".
- Caps: 25 arrows (`Caps.Clamp`, named in the log line, `arrows capped at 25 (asked 28)`). Crossbows have no hold (`m_bowDraw` false, they load instead): not affected.

## Use

| Form | Does |
|---|---|
| `op=bowcharge` or `op=bowcharge&status=1` | `bow charge on (1 spell arrow per whole second past full draw, cap 25)` and, while you hold a bow at full draw, how long and how many extra so far |
| `op=bowcharge&on=1&pvp=1` | spell 2.8: on, following your PvP switch (the default) |
| `op=bowcharge&pvp=0` | unmarked arrows, as a vanilla bow |
| `op=bowcharge&on=0` | spell 2.8 with Shift: off (the bow is vanilla again) |
| `op=bowcharge&probe=1` | read-only: whether `Player.UpdateAttackBowDraw` and the two draw fields exist, the patch state, your weapon, its draw duration and the draw now |

Success goes to `BepInEx/LogOutput.log` only: `bowcharge: held 3.2 s past full draw = 3 extra arrows (prefab bow_projectile_needle) from (x, y, z) along (x, y, z)` plus the PvP note and any cap.

## How it works

`BowChargePatch` is a prefix and postfix on `Player.UpdateAttackBowDraw(ItemData weapon, float dt)`, applied in `OnLoad` and gated on `BowChargeCommand.Current`, the local player only.

1. While the attack is held and the draw time is above 0, the prefix starts the timer (`FullAt = Time.time`) the first time `Humanoid.GetAttackDrawPercentage()` reaches 1.
2. A call with the attack released and the draw time above 0 is the release: the prefix hands the postfix the seconds held past full (the Harmony `__state`) and clears the timer. Anything else (not drawing, blocking, a minor action, stamina gone mid-draw: the game resets the draw time) also clears it.
3. The postfix fires only when the game's own shot started: `m_currentAttack` holds a new object (`Humanoid.StartAttack` stores its clone there only when `Attack.Start` succeeds, so no ammo = no new object): `floor(seconds past full)` arrows, `Caps.Clamp` to 25, the grid from `GridOffsets(n)`, each through `SpellShotCommand.Fire` with `PvpHits.ArmHit`/`MarkProjectile` when the PvP rule says other players are hit.

## Probes

`il-reader.js` on `assembly_valheim.dll`, 2026-11-03 (the game had updated past 1.0.20):
- `Humanoid.GetAttackDrawPercentage()`: for a weapon whose attack has `m_bowDraw` and `m_attackDrawTime > 0`, `Clamp01(m_attackDrawTime / Lerp(m_drawDurationMin, m_drawDurationMin * 0.2, skill factor))` (1 when the duration is 0), else 0.
- `Player.PlayerAttackInput(dt)`: a weapon with `m_bowDraw` goes to `UpdateAttackBowDraw(weapon, dt)`; everything else (crossbows included) to the queued attack path.
- `Player.UpdateAttackBowDraw`: blocking, a minor action or attached sets `m_attackDrawTime = -1`; held with stamina: `StartDraw` once, then `m_attackDrawTime += fixedDeltaTime`; released (or no stamina) with draw time above 0: `StartAttack(null, false)` when stamina and eitr allow, then `m_attackDrawTime = 0`.
- `Attack.m_attackDrawPercentage` is the percentage the attack takes at its start; bowcharge does not need it.

## Tests

ClaudeTest, a vanilla bow and arrows in the inventory; `op=cheatstate` before and after; read the new `bowcharge:` lines of `BepInEx/LogOutput.log` after each test.

| # | Steps | Expect |
|---|---|---|
| T0 | `op=reload`, then `op=bowcharge&probe=1` | the new spellshot DLL named; `Player.UpdateAttackBowDraw found`, both fields ok, patched True |
| T1 | draw the bow, hold about 3 s past full draw, release | the normal arrow plus 3 spell arrows in a row of 2 over 1; log `= 3 extra arrows` |
| T2 | hold 30 s past full | 25 extra in a 5x5 grid; log `arrows capped at 25 (asked 30)` |
| T3 | aim at the sky, hold 5 s past full | the extra arrows fly up along the crosshair |
| T4 | release before full draw; and a crossbow shot | no extra arrows, no log line |
| T5 | `op=bowcharge&on=0`, hold 3 s past full | vanilla shot only; `on=1&pvp=1` brings it back |
| T6 | Claude2 in front, your PvP off, then on (no spell cast since the load) | Claude2 not hit with your PvP off, hit with it on |
| T7 | your own tamed creature, then Claude2's, in the grid with your PvP on | yours spared, Claude2's hit (core tamed rule) |
| T8 | no arrows in the inventory, hold past full | no shot, log `released without a shot ...: no extra arrows` |

## Test log

| Time | Command | Answer (first line) | Observation | Verdict |
|---|---|---|---|---|
| 2026-11-04, first build | release at full draw, twice | `released without a shot (no ammo, stamina or eitr): no extra arrows` | the check `InAttack()` reads the animator state, which changes only at a later animator update, so it said no shot on every release | fail, fixed |
| 2026-11-04, `HomeCheat.Cmd.spellshot.v20261104210347` | hold 3.4 s, 1.0 s, 4.2 s, 14.8 s past full | `held 14.8 s past full draw = 14 extra arrows (prefab bow_projectile_needle) ...; other players spared (your PvP off)` | Maksym: "the bow works" | pass |

## Limits

- Holding drains stamina at half the draw rate; when it runs out the game drops the draw without a shot and the charge is lost, so a long charge needs the stamina for it.
- The local player only; a player without the mod has a vanilla bow.
- Settings in memory: a restart or a reload gives on, `pvp=1`.
- Spell arrows use the needle arrow's damage, whatever arrow the bow fires.
- The extra arrows leave at the release; the game's own arrow leaves with the attack's animation, a moment later.

## Undo

`op=bowcharge&on=0` (spell 2.8 with Shift) for the session. To remove it: delete `BowChargeCommand.cs` and `BowChargePatch.cs` from `src/Features/Spellshot/`, rebuild `spellshot`, `op=reload`, and replace spell line 2.8 with an `@dummy` line. Nothing is saved in the world or in a file.
