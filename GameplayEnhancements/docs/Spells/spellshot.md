---
description: spellshot command of the HomeCheat Valheim plugin (2026-10-24, harpoon 2026-10-25) - a grid of arrows, ballista bolts, lightning beams or pulling harpoons fired along the crosshair; prefabs found, how a Projectile is launched, the thunderbolt beam (lightning strikes with hidden damage carriers), the harpoon type (vanilla SE_Harpooned through the hit's status effect, built but not yet run in game, with its test list), parameters, tests including the push-back measurements, files, limits and undo
---
# spellshot (HomeCheat command)

Built and tested 2026-10-24 on Valheim 1.0.20, BepInEx 5.4.2351, host `MDENYSIUK-PERS`, throwaway world `ClaudeTest`. Own game and own world only; the shots belong to the local player.

## What it does

`spellshot <type> <perrow> <rows>` fires a grid of projectiles that all fly along the crosshair. `perrow` x `rows` items, 1 to 9 each (even counts are allowed: the grid is then symmetric around the crosshair, with no centre item). `1 1` is one shot, `3 3` a 3x3 matrix, `9 9` 81 shots.

| Type | What it fires | Prefab (verified in game) |
|---|---|---|
| `arrow` | Needle arrow, speed 60 m/s, falls with gravity 5 | item `ArrowNeedle` -> projectile `bow_projectile_needle` |
| `balistashot` | Flametal ballista bolt, speed 90 m/s, straight, lives 12 s | item `TurretBoltFlametal` -> projectile `Turret_projectile` (shared with the other turret bolts) |
| `thunderbolt` | a lightning beam (below) | visual `fx_Lightning`, damage carrier `lightningAOE` |
| `harpoon` | the Abyssal Harpoon's throw; a hit enemy is pulled to you (below) | item `SpearChitin` (assumed, probe pending) -> its projectile, status effect from the item (`Harpooned` expected) |

Start of the grid: the player's head (eye point) plus `dist` metres along the camera direction, not the camera itself (in third person the camera sits behind the player and the volley would start inside the character). The grid plane is perpendicular to the shot direction, items `spacing` apart.

## thunderbolt = a beam

Eikthyr (the first boss) has no lightning projectile: his attacks (`Eikthyr_antler`, `Eikthyr_charge`, `Eikthyr_stomp`) have none, and the one Projectile that mentions him (`FrozenKing_P2_Projectile_Eikthyr`, the Frost King copying him) spawns a stag on hit, so it was rejected. `lightningAOE` (a sky strike with a ring) was tried and rejected as the look. The final design:

- A beam runs from the head towards the crosshair point and on, until a wall, terrain or a building piece, or `range` (40 m). Creatures do not stop it: everything on the line is hit.
- Every `step` metres (2) one lightning visual (`fx_Lightning`, laid along the beam) and one invisible `lightningAOE` appear, 0.03 s apart so the bolt sweeps forward. The invisible `lightningAOE` carries the damage (lightning type, `strikedamage` 20 per strike); its renderers, lights, sound and camera shake are switched off.
- The strikes skip the player (`m_hitFriendly` off, the player is the owner) and push enemies with the strike's force (100, `push` multiplies it).
- The visual has no timeout of its own and stayed forever, so each piece is destroyed after `life` seconds (1.2). `type=clear` removes leftovers.
- A volley is capped at 300 strikes, so a 9x9 gets only about 3 strikes per beam.
- `beam=0` (or `projectile=<prefab>`) gives the older single-spot strike at the crosshair instead.

## harpoon = the Abyssal Harpoon's pull

Built 2026-10-25 without the game (phase 4, wave 1): **status B, not run in game; test list below**.

- Fires the harpoon item's own projectile through the same ammo-item path as `arrow` (item `SpearChitin`; `projectile=<name>` overrides), with you as the owner, at the item's throw speed (`speed=` overrides).
- The pull is vanilla. Read from the game's IL (`assembly_valheim.dll`, 2026-10-25): the game's `Attack.FireProjectileBurst` puts the weapon's `m_attackStatusEffect` into `HitData.m_statusEffectHash` (by its chance); `Projectile.Setup` copies that hash when the prefab has none; on a hit `Character.RPC_Damage` adds the status effect to the target and calls `SetAttacker(<owner>)`. spellshot builds its own `HitData`, so it sets `m_statusEffectHash` itself, always (no chance roll). The effect: the item's own `m_attackStatusEffect`, else `se=<name>`, else the `ObjectDB` status effect named `Harpooned`.
- What `SE_Harpooned` then does (vanilla): pulls the target towards you (`Utils.Pull`), drains your stamina in proportion to the target's mass, breaks when it gets too far (`m_breakDistance`), when you run out of stamina, or when you block or attack; it refuses targets beyond `m_maxDistance` ("target too far") and bosses always break free. Bodies on ships or attached are not pulled.
- Targets as every spellshot type: enemies; you are the owner, so you are never hit. A grid (`perrow`, `rows`) gives several harpoons; each one hit drains stamina on its own.
- Nothing stays in the world: the projectile has its own lifetime; the status effect ends as above.

## Use

Drop `sp-1.cmd` into `BepInEx/config/homecheat/inbox/` with the text `op=spellshot&type=arrow&perrow=3&rows=3` (unique name per call; load a rebuilt DLL first with `op=reload`).

| Parameter | Default | Meaning |
|---|---|---|
| `type` | required | `arrow`, `balistashot`, `thunderbolt`, `harpoon`; also `probe`, `clear`, `pos` |
| `perrow`, `rows` | 1, 1 | items per row and rows, 1 to 9 |
| `dist` | 1 (beam: 6) | start distance from the head, minimum 0.5 |
| `spacing` | 0.6 (beam and strikes: 1.5) | metres between items |
| `speed` | per type (harpoon: the item's throw speed) | projectile speed |
| `se` | harpoon only: the item's own status effect | status effect carried by a harpoon hit, by name (`op=bless&list=1` lists names) |
| `projectile` | per type | prefab override (projectile or ammo item) |
| `damage` | 1 | multiplier of the ammo's damage (beam: of `strikedamage`) |
| `range`, `step` | 40, 2 | beam length and strike spacing along it |
| `strikedamage` | 20 (beam), 40 (single strike) | lightning damage per strike |
| `fx`, `fxscale`, `along`, `life` | `fx_Lightning`, 1, 1, 1.2 | beam visual, its scale, laid along the beam, seconds it stays |
| `scale` | 1 | size (damage radius) of a strike; 3 gave about 7 m |
| `push` | 1 | multiplier of the strike push force on enemies, 0 = no push |
| `at` | `aim` | single strikes only: `self` puts them in front of the player |

Helpers: `type=probe&filter=<text>` lists projectile prefabs, the ammo items that fire them (with the status effect their attack carries, `se=<name> (<chance>)`) and the default items of creatures; `type=pos` prints the player position and speed; `type=clear` removes leftover `fx_Lightning` objects.

## How a Projectile is launched

`Projectile` implements `IProjectile.Setup` (the method is not listed under `Projectile` itself when reflecting; look it up by name including the explicit-interface form). The command builds the argument list from the parameter types, so a changed signature still works: owner (the player), velocity (direction x speed), hit noise, a `HitData` (damage from the ammo item, falling back to the prefab), weapon item (null is accepted), ammo item. The prefab is instantiated with `Object.Instantiate` first, as the game's own attack does.

## Tests (ClaudeTest)

- arrow and balistashot: 1x1, 3x3, 5x5, 9x9, a row of 9 and a column of 9 all ran without error and looked right.
- thunderbolt as a single spot: it only hit a boar with `scale=3` (radius about 7 m); the aim ray ignored creatures, so strikes aimed at a troll landed behind it. Fixed with an aim ray that counts creatures (the player's own colliders are skipped).
- thunderbolt as a beam: 1x1, 2x2, 3x3, 5x5, 9x9, 9x1, 1x9 ran without error; spacing was halved from 3 m to 1.5 m at the player's request.
- Push-back on the player: with creatures around, the troll's own hits were mistaken for it. With the area cleared and the player standing still, a beam starting 0.5 m ahead moved the player about 4.5 m every time (4 of 4); 1 m and 2 m never did; 1.5 m and 2.5 m sometimes. After setting `m_hitFriendly` off, 3 of 3 at 0.5 m and 3 of 3 at 1.5 m left the player untouched (0.01 m drift).
- `op=cheatstate` after the run: `Achievements.IsCheatedAtAll` True, `IsWorldCheated` False, `Terminal.m_cheat` True, `PlayerProfile.m_usedCheats` True, `Inventory.AnyCheatedItem` False, nearest pickable not marked cheated. The True flags were already set in this session by the ghost command and other effect commands, not by spellshot.

## harpoon: card, assumptions and test list (2026-10-25)

Card 3.6 of the phase 3 backlog plan (verdict: extend `spellshot` with `type=harpoon`). Unattended run, no game; Maksym reviews the assumptions.

| # | Assumption | Why |
|---|---|---|
| A5 | Harpoon = the `SpearChitin` item's projectile with the vanilla `SE_Harpooned`, no own pull physics | game mechanics first |
| A5a | The status effect goes on the hit through `HitData.m_statusEffectHash` (from the item's `m_attackStatusEffect`), not through `Projectile.m_statusEffect` as the card first said | the IL shows that is how the game's own attack does it; the projectile prefab has no effect of its own (to confirm with the probe) |
| A5b | Always applied (no `m_attackStatusEffectChance` roll) | a harpoon spell that sometimes does not pull reads as broken |
| A5c | The card stays on this page (no `docs/Backlog/harpoon.md`) | the change is one type of an existing command; this page is where the next session looks |

Test list for Maksym (ClaudeTest, one at a time; `op=reload` first, the answer names `HomeCheat.Cmd.spellshot.v20261025110055.dll` or newer):

1. `op=cheatstate` -> note the flags.
2. Probe: `op=spellshot&type=probe&filter=harpoon` -> a `PROJECTILE` line for the harpoon projectile (name it here).
3. Probe: `op=spellshot&type=probe&filter=chitin` -> `ITEM SpearChitin -> <projectile> vel=<n> ... se=Harpooned (<chance>)`. If the item has another name or no `se=`, the default in `Candidates` or `se=` needs fixing.
4. `op=spawn&prefab=Boar&dist=15`, aim at it, `op=spellshot&type=harpoon` -> first line `spellshot harpoon 1x1 = 1 projectiles (prefab <projectile> via SpearChitin, status effect Harpooned) ...`. Look: the boar is hit, a line appears and the boar is pulled towards you; your stamina drops while it pulls; block releases it.
5. `op=spellshot&type=harpoon&perrow=3` at three boars -> three lines; stamina drains faster.
6. Out of range: a boar farther than the harpoon's max distance -> the message "target too far", no pull.
7. Clean-up: `op=killmobs`; `op=cheatstate` -> same flags as in step 1 (spellshot sets none).

Test log: none yet (not run in game).

## Limits and open points

- The thunderbolt visual `fx_Lightning` is a guess at the look; `fx_eikthyr_forwardshockwave`, `fx_eikthyr_stomp` and `lightningAOE` are the other candidates found.
- Damage from the beam to creatures was checked only on a boar (killed at `scale=3`, `strikedamage=200`); damage to a troll with the final beam was not measured.
- Arrows and bolts hitting and damaging a creature was not measured.
- A creature inside several overlapping strikes is hit several times (about 2 to 3 for a normal beam).
- The core `op=list` throws `TargetException` in the log (see `earthquake.md`); not from this command.
- harpoon: built and compiled only; the item name `SpearChitin`, the projectile it fires and the status effect name are unverified until the probes run (test list above). The pull strength, stamina drain and break distance are the vanilla values of the status effect; nothing is tuned.

## Files

`Cmd_spellshot.cs` (C# 5, built with `bash build-cmd.sh spellshot` into `HomeCheat.Cmd.spellshot.v<time>.dll`, staged in `BepInEx/homecheat-commands/`, loaded with `op=reload`). Since 2026-10-25 the source is [src/Features/Spellshot](../../src/Features/Spellshot/) (build: `bash build.sh spellshot`, see [homecheat-architecture.md](../../../docs/homecheat-architecture.md)); the working copy, the plan (`plan-spellshot.md`) and the build script are in `LocalOutputs/2026-10-24--00-48/`, which is not kept.

## Undo

Delete the `HomeCheat.Cmd.spellshot.*.dll` files from `BepInEx/homecheat-commands/` and restart the game (a reload only adds commands). Leftover lightning visuals: `op=spellshot&type=clear`. Nothing in the game files is changed.

## In-game test 2026-10-25

Maksym's test pass of the phase 4 commands, world `ClaudeTest` hosted on this PC, Claude2 (no mod) as the second player. Result: **V** for arrow, Arrow 3x3, balista, thunderbolt, harpoon and the Harpoon 3x3 fan.

- **Thunderbolt pushed the caster** (fixed): `lightningAOE` has a second `Aoe`, `AOE_ROD` (a 100 m box collider, attack force 100); only the biggest `Aoe` got an owner, so the rod hit everyone. Now every `Aoe` of a strike gets `Setup` (owner) and the rod's damage is 0.
- **PvP.** `SpellShotCommand : AttackCommand`: `pvp=1` hits other players while your PvP is on, whatever their own, spares them while it is off, never you. Projectiles lose `m_ignorePVP` inside the game's `Projectile`, so shots are marked (`PvpHits.MarkProjectile`) and the core `PvpProjectilePatch` puts the flag back on their hits. Area spells V ("all works"); Arrow 3x3 on Claude2 failed before the patch and was fixed; the projectile check after the rule moved into the core is still B.
- **Fan** (new): `spread=<deg>` (default 0 = parallel, cap 45) turns each shot of the grid by its offset from the centre, like a numpad (centre straight, 8 up, 2 down, 4 left, 6 right). Spell 2.6.2 Harpoon 3x3 fan = `perrow=3&rows=3&spread=15` (Maksym tried 45, 25, 5, then 15); Shift on 2.1.2 and 2.2.2 = the fan at 10. V ("Harpoon logic works now").
- Harpoon (T-harpoon): `projectile_chitinharpoon`, `SpearChitin` with `se=Harpooned` (probe), pull works. V.
- Spell bar: 2.1 Arrows, 2.2 Balista (2.2.2 Balista 3x3 new), 2.3 Thunderbolt (2.3.2 wall 5x2), 2.6 Harpoon; all `pvp=1`.
