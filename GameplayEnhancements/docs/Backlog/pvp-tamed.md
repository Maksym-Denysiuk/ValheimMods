---
description: backlog card pvp-tamed (approved 2026-11-03) - with PvP on, attack spells hit other players' tamed creatures
---
# pvp-tamed: with PvP on, attack spells hit other players' tamed creatures

Status: approved (2026-11-03) · Verdict: extend the core PvP rule (0-core) · Nearest: `AttackCommand`, `Kit.PvpHits` (read)
Fingerprint: damage / tamed / as each attack / `Aoe`, `HitData`, `Projectile`

- With `pvp=1` and your PvP on: tamed creatures that are not yours are hit; yours are spared. Yours = it follows you (`MonsterAI.GetFollowTarget()`) or you named it (the name-author ZDO key). PvP off: every tamed creature spared.
- Today an armed `Aoe` (`m_hitFriendly`) may already hit every tamed creature, yours included; the probe settles it.
- Where: `PvpHits.HitsTamed(c)` plus a core prefix beside `PvpProjectilePatch` that drops a marked attack's hit on your own tamed creature and lets the others through. Every attack command (spellshot, slime, earthquake, trap, blink, bowcharge) gets it with no change of its own.
- Probes: `Aoe.ShouldHit`, `Projectile.IsValidTarget`, `Character.RPC_Damage` for tamed targets; the name-author ZDO key.
- Core change: yes (restart). Docs: architecture doc Target model, Safety guards; catalog Target cells.
- Test: Claude2 tames a wolf, Maksym a boar; PvP on: slime and arrow over both → wolf hit, boar spared; PvP off → both spared.

## Test log

| Time | Command | Answer (first line) | Observation | Verdict |
|---|---|---|---|---|
