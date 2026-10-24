---
description: spellshot command of the HomeCheat Valheim plugin (2026-10-24) - a grid of arrows, ballista bolts or lightning beams fired along the crosshair; prefabs found, how a Projectile is launched, the thunderbolt beam (lightning strikes with hidden damage carriers), parameters, tests including the push-back measurements, files, limits and undo
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

Start of the grid: the player's head (eye point) plus `dist` metres along the camera direction, not the camera itself (in third person the camera sits behind the player and the volley would start inside the character). The grid plane is perpendicular to the shot direction, items `spacing` apart.

## thunderbolt = a beam

Eikthyr (the first boss) has no lightning projectile: his attacks (`Eikthyr_antler`, `Eikthyr_charge`, `Eikthyr_stomp`) have none, and the one Projectile that mentions him (`FrozenKing_P2_Projectile_Eikthyr`, the Frost King copying him) spawns a stag on hit, so it was rejected. `lightningAOE` (a sky strike with a ring) was tried and rejected as the look. The final design:

- A beam runs from the head towards the crosshair point and on, until a wall, terrain or a building piece, or `range` (40 m). Creatures do not stop it: everything on the line is hit.
- Every `step` metres (2) one lightning visual (`fx_Lightning`, laid along the beam) and one invisible `lightningAOE` appear, 0.03 s apart so the bolt sweeps forward. The invisible `lightningAOE` carries the damage (lightning type, `strikedamage` 20 per strike); its renderers, lights, sound and camera shake are switched off.
- The strikes skip the player (`m_hitFriendly` off, the player is the owner) and push enemies with the strike's force (100, `push` multiplies it).
- The visual has no timeout of its own and stayed forever, so each piece is destroyed after `life` seconds (1.2). `type=clear` removes leftovers.
- A volley is capped at 300 strikes, so a 9x9 gets only about 3 strikes per beam.
- `beam=0` (or `projectile=<prefab>`) gives the older single-spot strike at the crosshair instead.

## Use

Drop `sp-1.cmd` into `BepInEx/config/homecheat/inbox/` with the text `op=spellshot&type=arrow&perrow=3&rows=3` (unique name per call; load a rebuilt DLL first with `op=reload`).

| Parameter | Default | Meaning |
|---|---|---|
| `type` | required | `arrow`, `balistashot`, `thunderbolt`; also `probe`, `clear`, `pos` |
| `perrow`, `rows` | 1, 1 | items per row and rows, 1 to 9 |
| `dist` | 1 (beam: 6) | start distance from the head, minimum 0.5 |
| `spacing` | 0.6 (beam and strikes: 1.5) | metres between items |
| `speed` | per type | projectile speed |
| `projectile` | per type | prefab override (projectile or ammo item) |
| `damage` | 1 | multiplier of the ammo's damage (beam: of `strikedamage`) |
| `range`, `step` | 40, 2 | beam length and strike spacing along it |
| `strikedamage` | 20 (beam), 40 (single strike) | lightning damage per strike |
| `fx`, `fxscale`, `along`, `life` | `fx_Lightning`, 1, 1, 1.2 | beam visual, its scale, laid along the beam, seconds it stays |
| `scale` | 1 | size (damage radius) of a strike; 3 gave about 7 m |
| `push` | 1 | multiplier of the strike push force on enemies, 0 = no push |
| `at` | `aim` | single strikes only: `self` puts them in front of the player |

Helpers: `type=probe&filter=<text>` lists projectile prefabs, the ammo items that fire them and the default items of creatures; `type=pos` prints the player position and speed; `type=clear` removes leftover `fx_Lightning` objects.

## How a Projectile is launched

`Projectile` implements `IProjectile.Setup` (the method is not listed under `Projectile` itself when reflecting; look it up by name including the explicit-interface form). The command builds the argument list from the parameter types, so a changed signature still works: owner (the player), velocity (direction x speed), hit noise, a `HitData` (damage from the ammo item, falling back to the prefab), weapon item (null is accepted), ammo item. The prefab is instantiated with `Object.Instantiate` first, as the game's own attack does.

## Tests (ClaudeTest)

- arrow and balistashot: 1x1, 3x3, 5x5, 9x9, a row of 9 and a column of 9 all ran without error and looked right.
- thunderbolt as a single spot: it only hit a boar with `scale=3` (radius about 7 m); the aim ray ignored creatures, so strikes aimed at a troll landed behind it. Fixed with an aim ray that counts creatures (the player's own colliders are skipped).
- thunderbolt as a beam: 1x1, 2x2, 3x3, 5x5, 9x9, 9x1, 1x9 ran without error; spacing was halved from 3 m to 1.5 m at the player's request.
- Push-back on the player: with creatures around, the troll's own hits were mistaken for it. With the area cleared and the player standing still, a beam starting 0.5 m ahead moved the player about 4.5 m every time (4 of 4); 1 m and 2 m never did; 1.5 m and 2.5 m sometimes. After setting `m_hitFriendly` off, 3 of 3 at 0.5 m and 3 of 3 at 1.5 m left the player untouched (0.01 m drift).
- `op=cheatstate` after the run: `Achievements.IsCheatedAtAll` True, `IsWorldCheated` False, `Terminal.m_cheat` True, `PlayerProfile.m_usedCheats` True, `Inventory.AnyCheatedItem` False, nearest pickable not marked cheated. The True flags were already set in this session by the ghost command and other effect commands, not by spellshot.

## Limits and open points

- The thunderbolt visual `fx_Lightning` is a guess at the look; `fx_eikthyr_forwardshockwave`, `fx_eikthyr_stomp` and `lightningAOE` are the other candidates found.
- Damage from the beam to creatures was checked only on a boar (killed at `scale=3`, `strikedamage=200`); damage to a troll with the final beam was not measured.
- Arrows and bolts hitting and damaging a creature was not measured.
- A creature inside several overlapping strikes is hit several times (about 2 to 3 for a normal beam).
- The core `op=list` throws `TargetException` in the log (see `earthquake.md`); not from this command.

## Files

`Cmd_spellshot.cs` (C# 5, built with `bash build-cmd.sh spellshot` into `HomeCheat.Cmd.spellshot.v<time>.dll`, staged in `BepInEx/homecheat-commands/`, loaded with `op=reload`). Since 2026-10-25 the source is [src/Features/Spellshot](../../src/Features/Spellshot/) (build: `bash build.sh spellshot`, see [architecture.md](../HomeCheat-plugin/architecture.md)); the working copy, the plan (`plan-spellshot.md`) and the build script are in `LocalOutputs/2026-10-24--00-48/`, which is not kept.

## Undo

Delete the `HomeCheat.Cmd.spellshot.*.dll` files from `BepInEx/homecheat-commands/` and restart the game (a reload only adds commands). Leftover lightning visuals: `op=spellshot&type=clear`. Nothing in the game files is changed.
