---
description: earthquake command of the HomeCheat Valheim plugin (2026-10-24) - expanding rings of knockback around the player plus a tilted stone circle that breaks outer-first; parameters, how it works, test results on goblins and trolls, weapon push ranking, files, limits and undo
---
# earthquake (HomeCheat command)

Built and tested 2026-10-24 on Valheim 1.0.20 (network version 41), BepInEx 5.4.2351, host `MDENYSIUK-PERS`, throwaway world `ClaudeTest`. Own game and own world only.

## What it does

A ground slam like a stag hammer, repeated in growing rings around the player, so that enemies are pushed out of a safe circle.

1. Stage 0: 2 slams right under the player, 0.003 s apart (that is under one frame: the second slam runs on the next frame).
2. Stages 1 to 5: a ring of slam points at radius k x 4 m (4, 8, 12, 16, 20 m), points about 5.6 m apart so neighbouring slams overlap, every point slammed twice. One stage every 0.4 s so the wave reads as expanding.
3. Stones: `Rock_7` placed all round the circle of every 2nd ring from ring 2 (R = 8 m: 25 stones, R = 16 m: 51 stones), leaning 45 degrees away from the player, wide side along the circle.
4. After `stonelife` seconds the stones break the game's own way (break effect, removed for everyone). The innermost circle breaks last, each circle further out `stonegap` seconds earlier.

The slam is push only: damage is multiplied by 0, the player and friends are not hit, props and terrain are not hit.

## Use

Drop a file into `BepInEx/config/homecheat/inbox/`, for example `eq-1.cmd` with the text `op=earthquake` (use a unique file name per call). First load a rebuilt DLL with `op=reload`.

| Parameter | Default | Meaning |
|---|---|---|
| `mode` | `simple` | `simple` = push x2.1 (the Demolisher sledge value, 210, against the slam prefab's 100); `strong` = x4.2 |
| `force` | from mode | own multiplier, overrides `mode` |
| `stages` | 5 | rings after stage 0 |
| `hits` | 2 | slams per point |
| `delay` | 0.003 | seconds between the hits of one point |
| `stagegap` | 0.4 | seconds between stages |
| `radius` | 4 | ring step in metres (the slam radius) |
| `spacing` | 1.4 x radius | metres between points on a ring |
| `aoe` | hammer's slam | slam prefab; falls back to `sledge_aoe` |
| `damage` | 0 | multiplier of the prefab's damage (0 = push only) |
| `props` | 0 | 1 lets the slam hit props |
| `stone` | `Rock_7` | stone prefab |
| `stonefrom` / `stonestep` | 2 / 2 | first ring with stones / every n-th ring |
| `tilt` / `stoneyaw` | 45 / 90 | lean away from the player / spin about the stone's own axis (degrees) |
| `stoney` | 0 | lift in metres |
| `noStones` | 0 | 1 = no stones |
| `stonelife` | 60 | seconds from the command until the innermost circle breaks |
| `stonegap` | 10 | seconds each outer circle breaks earlier |
| `dropcount` | `1` | stones dropped by each broken rock: a number, a range like `0-1`, or `game` (Rock_7: 3 to 6) |
| `players` | 1 | 1 = also push other players and friendly creatures, 0 = enemies only |
| `fx` | `earth` | effect set, `none` = no effect layers (see Effects) |
| `target` | self | another player's nickname, only if this game is the host |

Helpers on the same command: `kill=<radius m>` (kills untamed creatures around you), `clearitems=1` (removes items lying on the ground, not inventories), `clear=1` (removes leftover stones), `weapons=1&top=N` (weapon push ranking), `item=<prefab>`, `dump=<prefab>`, `find=<text>`.

## Facts

- The Stagbreaker (`SledgeStagbreaker`) has no `spawnOnTrigger` prefab on either attack, so there is no slam prefab to copy. `sledge_aoe` (Aoe radius 4, `m_attackForce` 100, damage from the weapon) is the sledge family's slam Aoe and is used as a fallback; the link to the Stagbreaker specifically is not verified. Stagbreaker weapon force is 150, Demolisher 210.
- `Rock_7`: the prefab name is capitalised, render size about 2.0 x 4.3 x 1.0 m, has Destructible and DropOnDestroyed (3 to 6 `Stone`, one of each draw).
- Push ranking of craftable weapons (weapon force x attack multiplier): sledges Demolisher, Gold variants, Wood and crossbows 210; SledgeIron, maces and Himmin Afl atgeir (secondary attack) 200.
- Pushing is ordinary game knockback: light enemies fly far, heavy ones (trolls) barely move at x2.1.

## Test results (ClaudeTest, 6 goblins at 6 m)

| Setting | Goblins pushed | Trolls pushed |
|---|---|---|
| x2.1 (`simple`) | 6.9 to 17.4 m, hp unchanged | 2.0 to 3.7 m |
| x4.2 (`strong`) | 7.0 to 37.4 m, hp unchanged | not tested |
| x7 | 19 to 42 m | 7 to 17 m |

Hits 1 versus 2 per point: no clear difference. Stone drops with `dropcount` 0, 1 and 3 and the range `0-1` behaved as set (`0-1` gave 32 stones from 76 rocks); the default is now exactly 1 per rock.

## Lessons

- Scanning all items after every broken stone made the game stutter; the sweep was removed, drops are controlled only by the drop table before breaking.
- Moving the stones down into the ground only moves them on the local client; breaking them removes them for everyone.
- Spawn tests in a world with other creatures: enemies of different factions hurt each other, which looks like damage from the effect.

## Files

`Cmd_earthquake.cs` (C# 5, built with `build-cmd.sh earthquake` into `HomeCheat.Cmd.earthquake.v<time>.dll`, staged in `BepInEx/homecheat-commands/`, loaded with `op=reload`). Since 2026-10-25 the source is [src/Features/Earthquake](../../src/Features/Earthquake/) (build: `bash build.sh earthquake`, see [homecheat-architecture.md](../../../docs/homecheat-architecture.md)); the working copy and the plan are in `LocalOutputs/2026-10-24--00-48/` (`Cmd_earthquake.cs`, `plan-earthquake.md`), which is not kept.

## Undo

Delete the `HomeCheat.Cmd.earthquake.*.dll` files from `BepInEx/homecheat-commands/` and restart the game (a reload only adds commands). Stones in the world: `op=earthquake&clear=1` first. Nothing in the game files is changed.

## Open

- `Terminal.m_cheat` and `PlayerProfile.m_usedCheats` are True in this session because cheats were switched on for the ghost command, not by this command.
- Core `op=list` throws `TargetException` (`m_namedPrefabs` read without an instance); belongs to the core plugin.

## Damage (2026-10-25)

Chosen by Maksym in the test run of 2026-10-25 (world `ClaudeTest`, client `Claude2`).

- `damage=0` (default) pushes only. `damage=<x>` gives every slam the damage of a hammer times `x`, instead of the slam prefab's own small damage (`sledge_aoe`: Blunt 15, Pierce 10).
- The hammer: `weapon=SledgeStagbreaker` (the first hammer) for a normal cast, `SledgeDemolisher` with `mode=strong`; `weapon=<item>` picks any item, `weapon=auto` the strongest `Sledge*` item of the game (today `SledgeGold_FrostFire`, 300). `quality=max` (default) or `1..max`: damage = base + per level x (quality - 1), the game's own rule. Stagbreaker 4/4 = Blunt 38 + Pierce 5.
- `mode=strong` also pushes harder: x8.4 the slam force (normal x2.1, the Demolisher value).
- The player in the centre owns every slam: the game records them as the attacker (verified per hit: "by PLAYER Claude") and the slams never hit them.
- `players=1` (default) also hits other players and tamed animals; `players=0` spares them. `props=1` also hits buildings, trees and rocks (default 0, so the base and the stone circle stay whole).

| Test (2026-10-25) | Result |
|---|---|
| goblins (175 hp), `weapon=auto` | one hit each: 222 per hit after resistances, 665 on a goblin that had not noticed us (the game's sneak bonus, x3) |
| goblins, Stagbreaker 4/4 | 33 per hit, 99 sneak; dead after 3 to 4 hits |
| troll (600 hp), normal cast | 8 hits, 204 total, max 61; survives with 396 |
| troll, `mode=strong` (Demolisher) | 6 hits, 502 total, max 125; left with 99 (the stronger push carries it out of the rings sooner) |
| 3 `woodwall` (400), `props=1`, Stagbreaker | 400 -> 76, 157, 76 |
| `props=0` | walls untouched |
| caster | full health in every test |
| `Claude2` 11 m away, `players=1` | killed (damage to a remote player is applied on their client, so the host's probe shows no hits) |

The probe in the answer logs to `BepInEx/LogOutput.log`, per creature: hits, damage after resistances, the attacker the game recorded and every single hit. A hit "by (none)" right after a slam is the tick of a status effect (fire, frost), which has no attacker.

Loot cheat mark: during the run, two items (`BlackMetalScrap`, `DeerMeat`) from kills by the quake were marked as cheated. The game sets the mark in `Character.ApplyDamage` when the attacking player has a cheated weapon equipped or is flying, in god mode or in ghost mode; all four read false, and every later kill gave clean loot. Cause not found. `op=cheatstate` now lists marked items on the ground and in the inventory.

## Players and NPCs

By default (`players=1`) the slam pushes everything: enemies, tamed and friendly creatures and other players, never the caster. Needed flags on the slam Aoe: `m_hitFriendly`, `m_ignorePVP` and `m_hitSame` (without `m_hitSame` the slam skips targets of the owner's own kind, i.e. other players). `players=0` = enemies only. Tested on a second client: pushed 6 to 31 m at the simple strength, no damage from the slam itself.

## Effects (visual and sound layers)

The spell plays the accepted effect set by default (`fx=earth`); `fx=none` switches all layers off. All prefabs come from the FX library (`../FX/`), nothing is invented.

| Layer | Default prefab | When | Parameter |
|---|---|---|---|
| Cast | `fx_goblinbrute_groundslam` (checked in game, small) | t = 0 under the caster | `castfx` |
| Rings | `vfx_troll_groundslam` | t = 0.4 to 2.0 on every point of every ring | `ringfx`, `ringevery` (every n-th point) |
| Stone break | `fx_tombstone_destroyed` + `sfx_rock_wall_crumble` (sound on every 4th stone) | when a stone breaks, on top of the built-in `vfx_RockDestroyed` and `sfx_rock_destroyed` of `Rock_7` | `breakfx`, `breaksfx`, `fxevery` |
| Linger | `vfx_darkland_groundfog` | after the last ring, 20 s | `lingerfx`, `lingerdur` |
| Stone rise | none (`fx_Abomination_arise` was tried and dropped) | when a stone appears | `stonefx` |

Other sets tried (all five were shown to Maksym one by one, see the list of prefabs in the FX folder): epic (`fx_eikthyr_stomp`, `fx_Abomination_attack3`, `vfx_gdking_stomp`), subtle (`vfx_lox_groundslam`), frost (`fx_frozenking_punch_aoe`, `fx_fenring_icenova`, `fx_icicle_destruction`, `vfx_Frost`) and a fire "hell" set (`fx_lavabomb_explosion`, `fx_fireskeleton_nova`, `fx_dynamite_explosion`). `fx=layered` is the same set without the cast layer.

`op=earthquake&demo=1` places five stones in a row 6 m ahead and breaks them one by one with the break effects of five sets (first after 5 s, then every 15 s; `demostart`, `demogap`, `demodist`).

Effect prefabs are removed again after a few seconds by the command (looping ones after `lingerdur`). Not tested with a second player watching; most of these prefabs are networked and should show for everyone when spawned on the host.
