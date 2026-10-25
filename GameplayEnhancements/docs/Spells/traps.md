---
description: trap command of the HomeCheat Valheim plugin (2026-10-25, reworked in game the same day) - mines on a floor-tile look laid at the crosshair, in front of you as a scattered matrix, or as a minefield of rings around you; kinds explosive and tar (random per mine), a random blast look per mine, hammer-like push, detonate all, the shared PvP rule taken when laid; host only, cap 6 000; card, first design, how it works, the in-game test (passed: "Mines are good") with what is still untested, limits, undo
---
# trap (mines, feature Traps)

> **Played and reworked in game (2026-10-25).** The first design below (assumptions T1-T8, B1-B7) was drafted in the W3-mines session; Maksym tested and
> reworked it the same day ("Mines are good"). The current behaviour is in [In-game test 2026-10-25](#in-game-test-2026-10-25), which overrides the sections above where they differ.

Built 2026-10-25 in phase 4, wave 3 (session W3-mines) on Valheim 1.0.20, BepInEx 5.4.2351, unattended and without the game: compiled and staged, **not run in game yet**. Own game and own world only. Source: [src/Features/Traps/](../../src/Features/Traps/) (`TrapCommand.cs`, `TrapKind.cs`, `TrapWeapon.cs`, `TrapPatches.cs`), DLL `HomeCheat.Cmd.traps.v<stamp>.dll`. The design and the plan card are kept privately in HomeOS.

## Goal

Mines you lay in front of you. Before: nothing in the game blows up when stepped on. After `op=trap&type=mine`: a troll trap appears at the crosshair with the trap's arming flash; whoever comes within 1.5 m first (an enemy, a tamed wolf, another player, you) sets it off: a dynamite blast hits everyone within 3 m, and the mine is gone. A mine nobody steps on vanishes in a puff of smoke after 5 minutes.

The same command lays a field (`perrow=3&rows=3`) and will lay other kinds: the electric fence is planned as `type=fence` on the same placement.

## Card

- Verdict: **new** command `trap` in a new feature `Traps` (no command places something that waits for a character; nearest is `spellshot`, whose matrix model it copies).
- Fingerprint: effect damage + place object / target everyone near the trap (players, enemies, tamed, bosses) / placement grid at the crosshair or ahead of you / mechanism a vanilla `Trap` prefab as the look, the host's own watcher, `Character.Damage` with an item's damage.
- Shape: one placement layer (the matrix), one kind table (`TrapKind`: look, trigger, blast, damage, effects, lifetime), one host watcher. A new kind is a row in `TrapKind.Get` plus, for a shape other than a point, its check in `TrapCommand.Triggered`.
- Persists in the world: the trap objects (networked, ZDO-marked) until they go off, expire or are removed.
- Who sees it: everyone (networked prefab and networked effects).
- Probes: `op=trap&type=probe&filter=trap` first (the default look `piece_trap_troll` was found in the game's effect dump as the owner of `fx_trap_arm`; not yet confirmed as a loadable prefab in game).
- Caps: `perrow`, `rows` 9; 50 live traps in the loaded area (refused over); `life` 3600 s; `trigger` 5 m; `blast` 10 m; `mult` 10; `remove radius` 100 m.
- Undo: `op=trap&remove=1&radius=50`, or wait 5 minutes.

### Assumptions (Maksym approved the design 2026-10-25; unattended build)

| # | Assumption | Instead of |
|---|---|---|
| T1 | your own mines hit you too (a real mine) | caster safe, as the target model asks |
| T2 | tamed creatures are hit too | tamed safe |
| T3 | bosses are hit like any creature | bosses excluded |
| T4 | damage = the bow item's own damage x2 (`BowAshlands`, quality max), no arrow added | bow + best arrow, x2 |
| T5 | trigger radius 1.5 m, blast radius 3 m | one radius for both |
| T6 | 2 m between mines in a field | spellshot's 0.6 m |
| T7 | a blast does not set off neighbouring mines | chain reaction |
| T8 | removing gives nothing back | a trap item dropped |
| B1 | a mine arms 3 s after placement (`arm=`) | live at once (a mine aimed at your feet would go off on you) |
| B2 | "damage like from a skeleton" = the hit has no player attacker and the hit type of an enemy hit, so players take it without PvP | a skeleton's own attack |
| B3 | look: `piece_trap_troll`, else `fuling_trap`; its vanilla `Trap` stays inert on every PC | own trap logic hidden under any prefab |
| B4 | effects: placed `fx_trap_arm`; set off `fx_dynamite_explosion` + `sfx_bombdynamite_explosion`; vanished `vfx_odin_despawn` (picked from the game's fx dump; `/valheim-spell-fx` not run, each is a parameter) | a designed set |
| B5 | `remove=1&radius=` needs no `confirm=1`: it removes only traps, which vanish on their own within 5 min | `confirm=1` as for dome removal |
| B6 | only the host's loaded area is watched: a mine far from the host does nothing until the host comes near | a world-wide scan |
| B7 | the push is the bow's attack force x `mult` | no push |

T1-T3 change who is hit; Maksym decided them (HomeOS decisions log, 2026-10-25).

## Use

`op=trap&type=mine`, `op=trap&remove=1[&radius=]`, `op=trap&list=1`, `op=trap&type=probe&filter=trap`

| Parameter | Default | Meaning |
|---|---|---|
| `type` | required | `mine`; `probe` lists prefabs with a vanilla `Trap` (read-only); `fence` answers "planned, not built yet" |
| `perrow`, `rows` | 1, 1 | matrix size, 1 to 9 each, centred on the point; rows run along your flat look direction, columns across it |
| `spacing` | 2 | metres between cells |
| `at` | aim | `aim` = the crosshair point (60 m ray; nothing hit = 8 m ahead), `self` = `dist` metres ahead of you |
| `dist` | 3 | with `at=self` |
| `life` | 300 | seconds, then it vanishes (cap 3600) |
| `arm` | 3 | seconds before it can go off |
| `trigger` | 1.5 | metres: a living character this close (horizontally, within 2.5 m height) sets it off (cap 5) |
| `blast` | 3 | metres: everyone this close is hit (cap 10) |
| `weapon`, `quality`, `mult` | BowAshlands, max, 2 | the blast carries `mult` x that item's damage at that quality (base + per level x (quality - 1)); cap `mult` 10 |
| `prefab` | piece_trap_troll, else fuling_trap | the look; must have a vanilla `Trap` and a `ZNetView` |
| `fxplace`, `fxboom`, `fxend` | see B4 | comma lists of effect prefabs; `none` = no effect |
| `remove` | - | `remove=1`: the trap within 2 m of the crosshair point; with `radius=` > 0 every trap within that many metres of you |
| `list` | - | `list=1`: live traps in the loaded area, position, distance, seconds left, `arming`; read-only |

Every trap keeps its own settings in its ZDO, so a mine laid with `blast=5` still blasts 5 m after a reload.

Answers (first line):
- `placed 1 mine (1x1, spacing 2.0 m) at x,y,z, visual piece_trap_troll, life 300 s, arms after 3.0 s, trigger 1.5 m, blast 3.0 m, damage <n> (2.0 x BowAshlands quality q/q); live traps 1/50; hits players and creatures alike`
- `removed 3 traps within 10 m of you; 0 live left`
- errors: `type required: mine (or probe)`, `type=fence is planned, not built yet; built: mine`, `host only: ...`, `live traps 48 + 9 new is over the cap 50 (...)`, `perrow=12 is over the cap 9`, `weapon 'X' not found or has no damage (...)`, `prefab 'X' has no vanilla Trap or no ZNetView; ...`, `no trap prefab found (tried ...)`
- log lines (`BepInEx/LogOutput.log`): `trap: mine at x,y,z set off by <player name or creature prefab>, <n> hit`

Spell bar (`spells.txt`, section 2 Attack): `2.11 Place mine` = `op=trap&type=mine`, `2.12 Remove mines` = `op=trap&remove=1&radius=10`.

## How it works

Read offline with `scripts/valheim/lib/il-reader.js` (HomeOS) on `assembly_valheim.dll` 1.0.20:

- `Trap` keeps its state in the ZDO (`ZDOVars.s_state`: 0 unarmed, 1 armed, 2 triggered). `IsArmed` is state 1. `Interact` arms an unarmed trap unless `IsCoolingDown`, which is `s_triggered + m_rearmCooldown > ZNet.GetTimeSeconds()`. `Update` (owner only) only resets a triggered trap. `TriggerTrap` sets state 0, `s_triggered`, activates the `m_AOE` child and plays `m_triggerEffects`.
- `m_triggeredByPlayers` / `m_triggeredByEnemies` are prefab fields, not ZDO data, so they cannot be set per instance for every PC.

So the command does not use the vanilla trap logic. Each placed trap gets `s_state = 0` and `s_triggered = 1e9` in its ZDO: it is unarmed and "cooling down" until world time 10^9 s, on every PC, with or without HomeCheat, and nobody can arm it by hand (the hover text shows the cooldown). Its look stays.

The marks in the trap's ZDO: `hc_trap` (kind), `hc_trap_end` and `hc_trap_arm` (world time in ms, `ZNet.GetTimeSeconds`, which stops while the world is not running: a mine laid a minute before logout has four minutes left next time), `hc_trap_trigger`, `hc_trap_blast`, `hc_trap_weapon`, `hc_trap_quality`, `hc_trap_mult`, `hc_trap_fxboom`, `hc_trap_fxend`.

The watcher runs on the host only: a self-rescheduling `Later` every 0.2 s while traps are live, every 3 s with none. It is started from `OnLoad`, by every placement and by a `Player.OnSpawned` postfix (`TrapPatches`, the dome pattern), because leaving a world cancels every timer. Every 3 s it finds the loaded traps again (`FindObjectsOfType<Trap>()` with the `hc_trap` mark: traps are few, so this is cheap). Each step, per trap: past `hc_trap_end` -> the vanish effect and `ZNetScene.Destroy` (after `ClaimOwnership`); armed and a living character (`Character.GetAllCharacters`) within the trigger radius -> the blast: a `HitData` with the weapon's damage x `mult`, no attacker, `HitType.EnemyHit`, push away from the trap, `Character.Damage` on every living character within the blast radius (the game routes each hit to the PC that owns that character), the blast effects, then the trap is destroyed.

Change procedure: edit the files in `src/Features/Traps/`, `bash src/build.sh traps`, `op=reload`, `op=help&cmd=trap`. A new kind: a row in `TrapKind.Get` and its name in `TrapKind.Names`.

## Test list for Maksym (not run yet)

World `ClaudeTest`, hosted on this PC. `op=cheatstate` before the first and after the last test. Enemies with `op=spawn`, clean-up with `op=killmobs`. Heal yourself with `op=heal` (T1: mines hurt you).

1. `op=reload` -> names `HomeCheat.Cmd.traps.v<stamp>.dll`, no `FAILED` line. `op=help&cmd=trap` -> the usage above.
2. **Probe** `op=trap&type=probe&filter=trap` -> a `TRAP piece_trap_troll ...` line (and `fuling_trap`). If `piece_trap_troll` is missing, rerun with `prefab=<a listed name>` in the next steps and note it.
3. `op=trap` -> `ERROR: type required ...`; `op=trap&type=fence` -> `planned, not built yet`.
4. `op=trap&type=mine` aimed at open ground 6-8 m away -> `placed 1 mine ...`; do you see the trap and the arming flash and hear it? Note the damage number. Hover over it: does it show a cooldown and refuse to arm with E?
5. `op=trap&list=1` -> 1 live trap, about 300 s left (`arming` in the first 3 s).
6. `op=spawn&prefab=Greyling&count=1` near the mine, let it walk over -> blast effect and sound; the Greyling dies or is hurt; `LogOutput.log`: `trap: mine at ... set off by Greyling, 1 hit`; `list=1` -> none.
7. A mine, then walk onto it yourself -> you take the blast (T1): note your health before and after.
8. A mine, `Claude2` walks onto it (PvP off) -> Claude2 takes damage; how much, and was it pushed?
9. A mine and stand 2-3 m from it while a Greyling steps on it -> you are hit too (blast 3 m); at 4 m -> not hit.
10. `op=trap&type=mine&perrow=3&rows=3` -> `placed 9 mines (3x3, spacing 2.0 m)`; is the field centred on the crosshair, rows along your look? One Greyling steps on one: only that mine goes off (T7).
11. `op=trap&type=mine&life=20` -> after about 20 s the smoke puff (`vfx_odin_despawn`) and it is gone.
12. `op=trap&remove=1` aimed at a mine -> `removed 1 trap within 2 m of the crosshair`; `op=trap&remove=1&radius=10` -> the rest near you.
13. Cap: `perrow=9&rows=6` (54) -> `ERROR: live traps 0 + 54 new is over the cap 50`.
14. Reload: lay a mine, `bash src/build.sh traps` + `op=reload`, then `list=1` -> the mine is still there with its time left, and still goes off.
15. Logout with a mine laid, log back in -> `list=1` shows the time left that was left at logout; it goes off or expires as before.
16. Spell bar: 2.11 Place mine and 2.12 Remove mines (section Attack).
17. A tamed wolf steps on a mine -> it is hit (T2). A boss (only if one is at hand) -> hit (T3).

Record each first answer line and what was seen in the table below, then set the index status to V and review the design (T1-T8, B1-B7).

| Time | Command | Answer (first line) | Observation | Verdict |
|---|---|---|---|---|

## Limits

- Host only: placing, removing and the watcher need a world this PC hosts. On a client, `list=1` answers that traps work only in a hosted world.
- Only the host's loaded area is watched: a mine near another player far from the host waits (and does not count down any faster) until the host comes near.
- One blast per mine; mines do not chain (T7).
- The look is a vanilla troll trap: players can still deconstruct it with the hammer like a piece, which just removes it.
- Selftest covers no part of this feature (no Unity-free maths of its own); everything is checked only by the test list above.
- Not tested: flying creatures (they set it off when within 2.5 m height), creatures that dig or swim, many mines at once.

## Undo

`op=trap&remove=1&radius=50` around the mines, or wait until they vanish (default 5 min). To remove the command, delete `HomeCheat.Cmd.traps.*.dll` from `BepInEx/homecheat-commands/` and restart the game ([../HomeCheat-plugin/README.md](../HomeCheat-plugin/README.md#undo-and-uninstall)); traps left in the world then stay as inert troll traps that can be deconstructed with the hammer.

## In-game test 2026-10-25

Maksym's test pass of the phase 4 commands, world `ClaudeTest` hosted on this PC, Claude2 (no mod) as the second player. The design was reworked in game with Maksym; this section is the current behaviour, and it overrides the Use table, the assumptions B3, B4, B7, T5, T6 and the cap above where they differ.

Result: **V**, "Mines are good" (place, random blast look, minefield in both layouts, detonate, expiry with a breaking sound, remove at the crosshair).

| Changed | Now | Why (Maksym) |
|---|---|---|
| kind | no `type=` = `mine` (`explosive` is the same kind); new kind `tar` (the tar slow `Tared` on everyone in the blast, no damage, no push, own look); `type=random` = a kind of `TrapKind.RandomPool` (mine, tar) per trap | "many mine kinds in the future"; Place mine = random, Shift = tar |
| look (B3) | `wood_floor_1x1` (a floor square), else `piece_trap_troll`, `fuling_trap`; any networked piece or plant as `prefab=` (a sapling's `Plant` is removed on the host so it never grows); the vanilla trap model is shown open (armed) while its ZDO state stays 0 | "floor square"; only plates laid by `trap` are mines (the `hc_trap` mark), never every floor tile |
| trigger (T5) | the square of the plate's footprint + 0.3 m, x1.25 (2.0 x 2.0 m on the floor tile); `trigger=<m>` still gives a circle | "extend activation radius by 25 %" |
| blast look (B4) | `fxboom=random` (default): each mine draws one of 10 sets when laid (dynamite, grenade, lava, siege, fireball, glacier, wrath, blue, shock, gas; `TrapFxSets`, stored in the ZDO); `fxboom=<set>` fixes one; a prefab list still works | 10 sets picked with `/valheim-spell-fx`; ice sets at scale 0.5, speed 2 |
| push (B7) | one hit of `SledgeStagbreaker` (attack force 150, read live); `push=` overrides, 0 = none | "small knockback like after one hammer" |
| expiry | `vfx_odin_despawn` + `sfx_wood_destroyed` (the floor tile breaking), also on remove | "must expire and disappear with a breaking sound" |
| PvP | `TrapCommand : AttackCommand`; `pvp=1` is taken when the mine is laid (ZDO `hc_trap_pvp`): with your PvP on other players are hit, off = they neither set it off nor take the blast; you, creatures and tamed are hit either way (T1-T3) | "all attacking spells inherit this logic" |
| minefield (new) | `field=1&safe=10&radius=30`: rings from `safe` to `radius` (cap 100), step 2 x the trigger half-width + 1 m, circumference / step mines per ring, every second ring shifted half a step, one random start angle, each mine facing the centre, none on water | passable, but no straight lane |
| rows (new) | `scatter=1`: the matrix with the same step, every second row shifted half a step; `at=front`: the first row 5 m ahead, rows run away from you | "in front of the user, not around him" |
| detonate (new) | `detonate=1[&radius=100]`: every live mine within the radius goes off as if stepped on (same hits, PvP rule), nearest first, waves of 25 every 0.1 s; look `fx_JotunWitch_LightningBolt_Explosion` + `vfx_spawn_small` + `sfx_staffthunderblood_cast` + `sfx_FireAddFuel`, `fxscale=2`; `fx=own` = each mine's own blast | picked from 10 previews ("3 + 10 together") |
| caps | 6 000 live, 5 000 per minefield (refused with the count), laid in batches of 250 every 0.1 s | "raise the cap" |
| remove | spell 2.7.2 Remove mine = the mine at the crosshair (`remove=1`), Shift = within 50 m | "Remove mine = the mine where you aim" |

Damage measured: 218 = 2 x `BowAshlands` at quality 4 (Pierce), push 25 x 2 before the hammer push.

Spell bar (2 Attack > 2.7 Mines): 2.7.1 Place mine (`type=random&pvp=1`, Shift tar), 2.7.2 Remove mine, 2.7.3 Place mine field (form: Layout Radius/Rows, Mine type with Fence greyed, Safe radius, Minefield radius above safe, Rows, Items in row, Mine effect, Mine texture; button Set mines), 2.7.4 Detonate mines.

Still untested (B): the tar mine's slow on a creature and on Claude2; `pvp=1` at placement with Claude2 (their PvP off, yours on and off); a mine across `op=reload` and across logout (time left kept); frame rate while laying and with thousands of live mines, and Claude2's load; flying creatures. Known: `op=help&cmd=trap` still says "cap 50 live traps" (the cap is 6 000).
