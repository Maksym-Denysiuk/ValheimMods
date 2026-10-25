---
description: slime command of the HomeCheat Valheim plugin (2026-10-24) - two even rings of Blob poison clouds around the player with a free 2 m gap, Blob attack damage x3 so it kills goblins, the caster immune to the clouds; prefabs and numbers found, parameters, tests (goblins, standing and running), files, limits and undo
---
# slime (HomeCheat command)

Built and tested 2026-10-24 on Valheim 1.0.20 (network version 41), BepInEx 5.4.2351, host `MDENYSIUK-PERS`, throwaway world `ClaudeTest`. Own game and own world only.

## What it does

The slime attack of the Blob (the green ooze) as an area effect around the player.

1. Free circle: nothing is spawned closer than `gap` (2 m minimum) to the player.
2. Ring 1: 6 clouds, centres 4 m from the player (gap 2 m + cloud radius 2 m), neighbours edge to edge.
3. Ring 2: 12 clouds at 8 m, starting one cloud diameter (4 m) further out and turned half a step against ring 1.
4. Each cloud is Blob's own `blob_aoe` with the Blob attack damage multiplied by 3 (Poison 270 per hit). Poison damage brings the game's own Poison effect (damage over time), so it takes about 10 s to kill a goblin (175 hp).
5. Clouds are lifted 1 m off the ground so the effect is visible; they live 3 s and hit once a second.
6. By default (`hurtself=1`) the clouds have no owner and hurt every player and NPC they touch, the caster too once they leave the gap. With `hurtself=0` the target is set as the owner of every cloud and is immune, also when moving through the ring.

## Use

Drop a file into `BepInEx/config/homecheat/inbox/`, for example `slime-1.cmd` with the text `op=slime` (unique file name per call). Load a rebuilt DLL first with `op=reload`. `op=harmeffect&kind=slime` reaches the same code.

| Parameter | Default | Meaning |
|---|---|---|
| `gap` | 2 | free radius around the player in metres (below 2 is refused) |
| `rings` | 2 | circle layers; layer k sits one cloud diameter further out |
| `damage` | 3 | multiplier of Blob's attack damage (90 Poison); 1 = Blob original, 0 = visual only (the cloud prefab has no damage of its own) |
| `lift` | 1 | metres above the ground |
| `delay` | 0 | seconds between clouds, 0 = all at once |
| `lifetime` | prefab (3 s) | cloud life in seconds |
| `cloud` | `blob_aoe` | other cloud prefab; keeps that prefab's own damage (for example `oozebomb_explosion`: Poison 40, radius 4, 10 s) |
| `cloudr` | read from the prefab | cloud radius override in metres |
| `fx` | 2 | cast effects, one of five sets (0 = none); see "Effect sets" below |
| `stonefx` | 0 | 1 to 5: demo of that set's stone-break effects on 4 points of ring 2, 3 s after the cast |
| `hurtself` | 1 | 1 = clouds hit every player and NPC outside the gap (a player standing at the gap edge is hit too); 0 = the target is the cloud owner and immune |
| `target` | self | another player's nickname, only if this game is the host |

Helpers on the same command: `find=<text>` (prefabs with an `Aoe` are marked), `dump=<prefab>` (Aoe fields, collider radii, scale), `weapon=<character prefab>` (default items, damage, attack settings). Test aids that are not part of the effect and never run during a cast (separate modes that return before the cast code; checked in the source): `keephealed=<nick>&seconds=N` (tops a player up to full health every 0.2 s through the game's Heal rpc, works on other players when this game is the host), `healstats=1`, `heal=1` (full health for the local player), `near=<r>` (characters around with hp), `probe=1` (particle systems of live clouds), `onself=1` (one cloud on the player, use a small `damage=`).

## Facts

| Prefab | Aoe radius | Life | Damage |
|---|---|---|---|
| `blob_aoe` | 2 m (diameter 4) | 3 s, hit every 1 s | none of its own (`m_useAttackSettings` = true: it takes the attacker's damage) |
| `oozebomb_explosion` | 4 m | 10 s, hit every 1 s | Poison 40 |

- Blob attack item `blob_attack_aoe`: Poison 90, attack type Area. BlobElite: `blobelite_attack_aoe`, Poison 115. Neither has a status effect; the poison damage type does the effect.
- To make `blob_aoe` deal damage, the command sets `Aoe.m_useAttackSettings` = false and `Aoe.m_damage` = the item's damage (scaled with `DamageTypes.Modify`, which changes the boxed struct in place and returns nothing).
- The prefab has `m_hitOwner` = false but no owner when spawned alone, so an ownerless cloud also hurts the player. Setting the field `Aoe.m_owner` to the caster fixes that.
- Goblin (Fuling) has 175 hp, Greydwarf 40.

## Effect sets (cast visuals and sounds)

Chosen with the `valheim-spell-fx` skill from the description files in `../FX/`; the cast prefabs are spawned at the caster (and on the 6 ring-1 clouds where marked) and removed after 8 s. Only prefabs marked ✔ were seen in game before; the others are inferred from the dump. All five were fired in ClaudeTest on 2026-10-24; Maksym looked at them; on 2026-10-25 he chose set 2 (all three effects once, at the caster's spot, at the start), and it is the default since (`fx=2`).

| Set | Cast (t) | Stone-break demo (`stonefx`) |
|---|---|---|
| 1 Faithful | `fx_Bonemass_aoe_start` + `sfx_blob_attack` (0 s), `vfx_blob_attack` on ring-1 clouds (0.2 s) | `vfx_BombBlob_explode_poison`, `sfx_oozebomb_explode` |
| 2 Epic (default since 2026-10-25) | under the caster, once: `fx_fader_meteor_hit` ✔ (0 s: big green blast, camera shake 1.3) + `fx_Abomination_attack2` (0 s: grey stones, dirt, camera shake 1.2), `fx_Fader_Roar` ✔ (0.3 s, green lights) | none |
| 3 Subtle | `fx_Fader_Roar` ✔ (0 s), `vfx_blob_hit` on ring-1 clouds (0.2 s) | `vfx_GuckSackSmall_Destroyed`, `sfx_GuckSackDestroyed` |
| 4 Frost | `fx_fenring_icenova` ✔ (0 s), `vfx_blob_frost_attack` on ring-1 clouds (0.2 s) | `vfx_ice_destroyed`, `sfx_ice_destroyed` |
| 5 Layered | set 1 cast plus `fx_Fader_Roar` ✔ (0.5 s) | `vfx_GuckSackDestroyed`, `vfx_BombBlob_explode_poison`, `sfx_gdking_rock_destroyed` |

Changes made while showing them: `sfx_gdking_rock_destroyed` was removed from set 2, and `fx_Abomination_attack2` moved from the stones to the caster at the start; on 2026-10-25 Maksym confirmed it there: once, at the caster's spot, at the start. The stone-break effects are only a demo on ring 2: firing them when an earthquake stone really breaks needs a `breakfx=` parameter in `Cmd_earthquake.cs` (not built). The stones' own break effect and sound are the game's.

## Test results (ClaudeTest)

| Test | Result |
|---|---|
| default `op=slime`, ring count | 6 clouds on R=4 m; with `rings=2` 6 + 12 = 18 (measured radius 2.00) |
| 6 Greydwarf 4 to 5 m ahead, Blob damage x1 | 40 hp to 14.3 to 1.5 to dead in about 3 s; one standing exactly on the 6 m line between the rings was missed |
| 4 goblins 5 m ahead, `damage=3` | 175 to 143 to 112 to 64 to dead in about 10 s |
| one ownerless cloud on the player, Poison 4.5 | hp 25 to 22.9 to 21.5: the player is hurt without an owner |
| one cloud on the player with owner set, Poison 45 | hp stayed 25/25 |
| three casts while running (about 3.4 m/s through the rings) | hp 25/25 after every cast |
| `gap=1`, unknown `cloud=` | refused with a clear error |
| default cast (ownerless clouds), CLAUDE2 standing 1.9 m from the caster | CLAUDE2 hp 25 to 17.7 to 10.4 to 3.1 to 0 (about 4 s, poison ticking); caster in the centre untouched |
| `target=CLAUDE2` (second player on this hosted world, 1 to 5 m away), `damage=0.1` then default `damage=3` | 18 clouds around CLAUDE2; CLAUDE2 saw the clouds on their own screen (confirmed by Maksym); host and CLAUDE2 stayed at 25/25 |

`op=cheatstate` was identical before and after every test.

## Lessons

- A gap alone does not protect a moving caster; the owner field does (`hurtself=0`). Maksym chose the default where players are hurt too (`hurtself=1`).
- The first enemy test missed because the enemies stood 8 to 9 m out, beyond the 6 m reach of ring 1: spawn test targets inside the rings.
- A spawned `blob_aoe` with no damage looks like a working effect but hurts nobody: always check hp of a target, not just the spawn message.
- The first version spawned clouds on the ground, where they were hard to see; the 1 m lift fixed it (particle systems were playing all along).

## Files

`Cmd_slime.cs` (C# 5, built with `build-cmd.sh slime` into `HomeCheat.Cmd.slime.v<time>.dll`, staged in `BepInEx/homecheat-commands/`, loaded with `op=reload`). Final build `HomeCheat.Cmd.slime.v213831.dll`. Since 2026-10-25 the source is [src/Features/Slime](../../src/Features/Slime/) (build: `bash build.sh slime`, see [homecheat-architecture.md](../../../docs/homecheat-architecture.md)); the working copy and plan are in `LocalOutputs/2026-10-24--00-48/` (`Cmd_slime.cs`, `plan-slime.md`), which is not kept.

## Undo

Delete the `HomeCheat.Cmd.slime.*.dll` files from `BepInEx/homecheat-commands/` and restart the game (a reload only adds commands). Nothing in the game files is changed, and clouds disappear by themselves after 3 s.

## Open

- Not tested: `delay`, `lifetime`, poison damage on a second player (CLAUDE2 is the owner of their own ring, so it is immune; a third player or a mob would show it).
- Seen by another client: confirmed with CLAUDE2 on the hosted world; the look on screen was confirmed by Maksym ("all good") after the lift, not measured.

## In-game test 2026-10-25

Maksym's test pass of the phase 4 commands, world `ClaudeTest` hosted on this PC, Claude2 (no mod) as the second player. Result: **V**.
- `hurtself=0` made the caster the cloud owner, and Valheim then never hits other players, even with PvP on (Claude2 at 3.1 m stayed 25/25). Replaced in the spell lines by the shared rule `pvp=1` (`SlimeEffect : HarmEffect : AttackCommand`, `PvpHits.ArmAoe`): your PvP on = Claude2 is hit, off = spared, never you. V ("all works", before the rule moved into the core).
- Slime moat: `gap=3&cloudr=3.5` (Maksym: a damage area about 3x the ring, annulus 3-17 m against 2-10 m).
- Spells: 2.4.1 Slime ring, 2.4.2 Slime moat (the duplicate 3.4 in Protect was removed), Shift = 3 rings.
- The default `hurtself=1` is unchanged (Known defects); the spell lines always pass `pvp=1`.
