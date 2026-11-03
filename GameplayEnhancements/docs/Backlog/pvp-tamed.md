---
description: backlog card pvp-tamed (approved 2026-11-03) - with PvP on, attack spells hit other players' tamed creatures
---
# pvp-tamed: with PvP on, attack spells hit other players' tamed creatures

Status: B: built 2026-11-03 (core installed), not run in game: test list below (Maksym tests later) · Verdict: extend the core PvP rule (0-core) · Nearest: `AttackCommand`, `Kit.PvpHits` (read)
Fingerprint: damage / tamed / as each attack / `Aoe`, `HitData`, `Projectile`

- With `pvp=1` and your PvP on: tamed creatures that are not yours are hit; yours are spared. Yours = it follows you (`MonsterAI.GetFollowTarget()`) or you named it (the name-author ZDO key). PvP off: every tamed creature spared.
- Today an armed `Aoe` (`m_hitFriendly`) may already hit every tamed creature, yours included; the probe settles it.
- Where: `PvpHits.HitsTamed(c)` plus a core prefix beside `PvpProjectilePatch` that drops a marked attack's hit on your own tamed creature and lets the others through. Every attack command (spellshot, slime, earthquake, trap, blink, bowcharge) gets it with no change of its own.
- Probes: `Aoe.ShouldHit`, `Projectile.IsValidTarget`, `Character.RPC_Damage` for tamed targets; the name-author ZDO key.
- Core change: yes (restart). Docs: architecture doc Target model, Safety guards; catalog Target cells.
- Test: Claude2 tames a wolf, Maksym a boar; PvP on: slime and arrow over both → wolf hit, boar spared; PvP off → both spared.

## Probes (2026-11-03, il-reader on the game DLL of 2026-10-28, 2 577 920 bytes)

- `Aoe.ShouldHit(Collider)`: unchanged from 1.0.20. Without `m_hitFriendly` only enemies (or aggravatable creatures hit by a player) are hit; `BaseAI.IsEnemy` never makes a tamed creature a player's enemy, so an unarmed Aoe spares every tamed one and an armed Aoe (`ArmAoe` with hitPlayers) hits every tamed one, **yours included** (the card's suspicion confirmed).
- `Projectile.IsValidTarget(IDestructible)`: unchanged. A shot whose owner is a player with PvP on may target any character, tamed ones included, yours too; PvP off: enemies only.
- `Character.RPC_Damage`: no tamed check (only the player-on-player PvP drop unless `m_ignorePVP`).
- Own tamed: `Tameable.RPC_Command` sets ZDO `follow` (`ZDOVars.s_follow`) to the player name when it follows, `""` when it stays; `MonsterAI.m_follow` (`GetFollowTarget()`) on the creature's owner. Rename: `Tameable.SetText` sends `SetName` with the local platform user id (`Splatform...LocalUser.PlatformUserID.ToString()`, or `"host"` before sign-in), stored as ZDO `TamedNameAuthor` (`ZDOVars.s_tamedNameAuthor`).

## Built (2026-11-03, hc-core)

- `PvpHits.HitsTamed(c)` (not tamed: true; tamed: your PvP on and not yours) and `PvpHits.IsOwnTamed(c, me)` (follow ZDO = your name, `m_follow` = you, or name author = your platform id; core has no Splatform reference, read by reflection).
- `PvpHits.ArmAoe` also marks the Aoe (instance id, like `MarkProjectile`).
- `src/Core/Builtins/PvpTamedPatch.cs`: postfixes on `Aoe.ShouldHit` and `Projectile.IsValidTarget` turn a marked attack's hit on a tamed creature into a miss when `HitsTamed` says no; they never add a hit. Log line `pvp tamed rule: area|shot spared tamed <prefab> (yours|your PvP off)`, once per creature per 2 s. Registered in `HomeCheatPlugin.cs` beside the projectile patch.
- Covered with no feature change: Aoe hits of spellshot (strikes), slime, earthquake; spellshot projectiles fired with `pvp=1` and your PvP on.
- Not covered (feature code decides its own targets, outside this session's files): blink's shove (`BlinkCommand.Shove` skips every tamed creature unless `tamed=1`, which then hits all, yours too) and trap blasts (`TrapCommand.Boom` hit every character in range with no attacker; closed 2026-11-03: `TrapCommand.Spared` in the trigger and the blast, for mines laid with `pvp=1`, test 17 in [traps.md](../Spells/traps.md)). A spellshot shot with `pvp=0` while your PvP is on is unmarked and hits tamed creatures as a vanilla bow does. Blink got `PvpHits.HitsTamed(c)` in its target filter from hc-teleport.

## Test list (ClaudeTest as host, Claude2 joined; one command at a time through `send-command.ps1`; `LogOutput.log` after each)

1. `op=cheatstate` (before).
2. `op=status`: the core loads; `LogOutput.log` has no `Harmony PvpTamedPatch:` error.
3. Your PvP **on** (inventory screen). `op=spawn&prefab=Boar&tamed=1`, then press E on the boar: it follows you (yours). `op=spawn&prefab=Wolf&tamed=1&dist=8`; Claude2 presses E on the wolf: it follows Claude2 (not yours; a wolf that follows no one is not yours either).
4. Aim at the wolf: `op=spellshot&type=arrow&pvp=1` → the wolf takes damage. Aim at the boar: same → no damage; log `pvp tamed rule: shot spared tamed Boar (yours)`.
5. Both where the slime clouds land (the ring around you, outside its gap): `op=slime&pvp=1` → the wolf is poisoned, the boar is not; log `pvp tamed rule: area spared tamed Boar (yours)`.
6. Your PvP **off**: repeat 4 and 5 → neither is hit; log lines `(your PvP off)` for both (`Wolf` and `Boar`).
7. Optional: rename the boar (yours by name) and send it to stay (E): with PvP on it is still spared.
8. `op=killmobs&tamed=1&radius=20` to clear; `op=cheatstate` (after), compare with 1.

## Test log

| Time | Command | Answer (first line) | Observation | Verdict |
|---|---|---|---|---|
