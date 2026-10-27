---
description: teleport and blink commands of the HomeCheat Valheim plugin (built 2026-10-26, tuned in game with Maksym 2026-10-27) - teleport to your last death point, a spot you left-click on the large map, next to another player, typed coordinates or the crosshair (the game's own portal path; the ward's blue dome with its sound and a slow sphere of smoke when the screen clears, 3 s fall guard); blink, a 10 m dash along your flat look direction through walls and creatures that shoves everyone on the line aside with tar and no hit, Hugin the raven poofing at both ends; spell group 9 Teleport (flat), Blink on F6; cards, how it works, probes, test log, limits, undo
---
# teleport and blink (feature Teleport)

> **Tuned in game 2026-10-27.** Maksym ran Last death, Blink (enemies shoved and tarred), to=point, to=aim and the appear look in `ClaudeTest`; Map point and To player have not been confirmed by him yet, and what Claude2 sees is still to check ([test log](#test-log)).

Valheim 1.0.20, BepInEx 5.4.2351, own game and own world only. Source: [src/Features/Teleport/](../../src/Features/Teleport/) (`TeleportCommand.cs`, `TeleportMapPatch.cs`, `BlinkCommand.cs`, `TeleportFx.cs`, `refs.txt`), DLL `HomeCheat.Cmd.teleport.v<stamp>.dll`, namespace `HomeCheat.Features.TeleportCommands` (the game has a type `Teleport`, the dungeon door).

## Goal

Before: the only ways to move far were portals and walking; nothing dashes. After:
- `op=teleport&to=death` takes you back to where you last died in this world;
- `op=teleport&to=map` opens the large map, and the next left click on it takes you there;
- `op=teleport&to=player&player=<nick>` puts you 2 to 5 m from that player;
- `op=teleport&to=point&at=x,y,z` and `op=teleport&to=aim` take you to typed coordinates or the spot under your crosshair;
- `op=blink` moves you 10 m forward at once, through walls and creatures, shoving everyone on the line aside and tarring them.

## Cards

### teleport

- Verdict: **new** (no command moved the player through space; nearest `spawn player=`, which reads another player's position, and `coord`, which stores points).
- Fingerprint: form/movement / self / a point / `Player.TeleportTo` (the portal path).
- Targets: only you; `to=player` only reads that player's position.
- Look (every mode, Maksym 2026-10-27): when the loading screen clears, the ward's blue dome `fx_guardstone_activate` at your feet with its "dome shield on" sound (the only sound), and a sphere of 18 smoke puffs `vfx_Place_HildirClothesRack` (2.6 m out from 1 m above your feet), played once at a third of the particle speed so it lingers. Picked from 22 sounds heard in game (cooking pop, loot poof, portal, bells, staves, ghosts, wind, potion chime and others); no lightning.
- Safeguard: no fall damage for 3 s after you land (Maksym 2026-10-27).
- Persists: nothing. Caps: `radius` 50; `at=` inside the world (10 400 m). Undo: `to=death` or walk back.
- Decisions: landing 2 m to `radius` (default 5) m from the player; items that portals refuse (ore, metal) are allowed in every mode (Maksym, plan 2026-10-26); `to=point` and `to=aim` added in the test session ("teleport me to that spot", "to the crosshair").

### blink

- Verdict: **new** (nearest `trap` tar kind: `Tared` and no damage).
- Fingerprint: form/movement + push + status / self + enemies / a line ahead / `AttackCommand`.
- Maksym: "it should pushback enemies to the sides and give them tar effect. no damage"; flat direction, follows the terrain; the look is "the crow that gives advice" (Hugin); 2026-10-27: twice the distance (10 m), no red hit flash on enemies ("use tar effect and make a knockback if enemy was on the user path, to the side"), smoke at a quarter size, the appear pop as its sound.
- Targets: enemies; tamed with `tamed=1`, bosses with `boss=1` (target model: excluded unless asked); other players only through the shared PvP rule (`pvp=1`, which the spell line passes); never you.
- Look: Hugin, the advice raven, poofs where you leave and where you appear: a local copy of the Hugin child of `Tutorial.m_ravenPrefab` (`Ravens`: Hugin and Munin, one `Raven` each) with the animator trigger `poff`, and his despawn effect `fx_raven_despawn` (local). Every player gets the networked fallback `vfx_odin_despawn` at a quarter size and the pop `sfx_cooking_station_take`. No portal.
- Safeguard: the 3 s fall guard as teleport.
- Persists: nothing. Caps: `dist` 20, `width` 5, `push` 500.
- Pin: F6, so it can be cast in a fight without opening the radial.

## Use

| Command | Parameters (default) |
|---|---|
| `op=teleport&to=death` | `fx=1` (0 = no dome, no smoke) |
| `op=teleport&to=map` | `fx=1` |
| `op=teleport&to=player&player=<nick>` | `radius=5` (2 to 50), `fx=1`; host only |
| `op=teleport&to=point&at=x,y,z` | `at=x,z` lets the game find the height (it then holds the black screen up to 15 s); `fx=1` |
| `op=teleport&to=aim` | the crosshair point (60 m, solid objects and terrain; nothing hit = refused); `fx=1` |
| `op=teleport&preview=1` | test aid: the appear look where you stand, no teleport |
| `op=blink` | `dist=10` (cap 20), `width=2` (metres to each side of the line, cap 5), `push=150` (cap 500), `tamed=0`, `boss=0`, `pvp=0`, `fx=1` |
| `op=blink&probe=1` | read-only: Hugin's prefab and its children, whether his despawn effect is networked, the fallback prefabs |

Spell bar (`spells.txt`), group 9 Teleport, flat (Maksym 2026-10-27: no "To coordinates" submenu): 9.1 Last death (`to=death`), 9.2 Map point (`to=map`), 9.3 Blink (`op=blink&pvp=1`, `pin=F6`), 9.4 To player... (`to=player&player={player}`, form `ask=player:@players`). Maksym's own pins: F7 = 9.1, F8 = 9.2, F9 = 9.4. The root is 1, 2, 4, 5, 6, 7, 8, 9: one ring; id 3 (the removed Protect) is not reused. `to=point` and `to=aim` have no spell line.

Answers (first line): `teleporting to your death point at x,y,z (n m)`, `map armed: left-click a spot to teleport there (closing the map cancels)`, `teleporting to <name>: 3.4 m from them at x,y,z`, `teleporting to x,y,z (n m)`, `teleporting to the crosshair at x,y,z (n m)`, `appear effect at x,y,z`, `blinked 10.0 m to x,y,z, shoved and tarred 4 (Greydwarf=4)` + the PvP note. Errors: `no death point in this world`, `player '<nick>' is not online`, `that is you: pick another player`, `at=<v>: write x,y,z or x,z`, `at=<v> is outside the world`, `nothing under the crosshair within 60 m`, `the world ends there: blink refused`, `the game refused the teleport (already teleporting?)`, `click inside the map to teleport`.

## How it works

- **teleport** calls `Player.TeleportTo(pos, rot, distantTeleport=true)`, the path portals use. `Player.UpdateTeleport` (IL 1.0.20) moves you to the target after 2 s and holds you there (8 s at least, 15 s when it has to find the height) until the area is ready, then sets your height to `ZoneSystem.GetSolidHeight(target) + 0.5` and clears `IsTeleporting()`. The loading screen stays black while `IsTeleporting()` and then fades (`Hud.UpdateBlackScreen`). A self-rescheduling `Later(0.1)` watches: the first moment you stand on the ground within 2 m of the target starts the fall guard (screen still black, logged `landed`); when `IsTeleporting()` clears, the fall guard runs again and the look plays (logged `appeared`). In the test the appear came after 7.9 s (death point, 512 m: 7.8 s; `at=x,z`: 14.9 s).
- **The look** (`Appear`, `SmokeWave`): `fx_guardstone_activate` at your feet (removed after 6 s); 18 puffs on a Fibonacci sphere, never below the ground + 0.1 m, each slowed to a third (`simulationSpeed`, this PC only) and muted (`ZSFX` off, `AudioSource` stopped: the puff's own poof sound, this PC only), removed after 6 s. `fx=0` skips the look, never the fall guard.
- **Fall guard** (`TeleportFx.FallGuard`): fall damage is the drop from `m_maxAirAltitude` when you touch ground (`Character.UpdateGroundContact`), so it is held at your height every 0.1 s for 3 s; a fall still going on after it counts from where you were then.
- **Death point**: `Game.instance.GetPlayerProfile().HaveDeathPoint()` / `GetDeathPoint()`, kept per world by the game.
- **Map click**: `to=map` arms one click, applies two patches the first time it is cast in a world (the UI patch rule) and opens the large map (`Minimap.SetMapMode(Large)`, by reflection). A prefix on `Minimap.OnMapLeftClick` takes the armed click: `Minimap.ScreenToWorldPoint(ZInput.pointerPosition)` (private, by reflection), closes the map, teleports, and skips the game's pin handling for that click. A postfix on `SetMapMode` disarms when the map closes; leaving the world and unloading disarm too.
- **Another player**: loaded on this PC through `Players.Find`; otherwise the host's copy of his character ZDO (`ZNet.GetPlayerList` -> `m_characterID` -> `ZDOMan.GetZDO`). Host only. You land at a random angle, facing them.
- **Point and crosshair**: `at=` parsed with the invariant culture (`x,z` leaves y = 0 for the game to fix); `to=aim` uses `Kit.Aim.Point` and refuses when nothing is hit instead of its 8 m fallback.
- **blink** moves you the way `UpdateTeleport` does, without `TeleportTo` (no fade, no wait): transform and body position, velocity zero, `m_maxAirAltitude`, then the fall guard. Target = your position + your flat look direction x `dist`; height = the higher of your own and the terrain there + 0.1 m (`Ground.Height`). Past 10 400 m from the world centre it is refused.
- **The shove**: before the move, every character within `width` of the line (and within 3 m above or below it) is pushed away from the line (a random side for one standing on it). A creature this PC owns gets `Tared` straight into its `SEMan` (`AddStatusEffect(hash, true, 0, 0)`) and the game's own push `Character.ApplyPushback(dir, push)` (private, by reflection): no hit, so no red flash, no hit sound, no damage number. Players (`pvp=1`) and creatures another peer owns get the zero-damage `HitData` (push, `m_statusEffectHash` Tared, `PvpHits.ArmHit`), whose owner applies both.
- **Smoke at a quarter** (`TeleportFx.Shrink`): blink's `vfx_odin_despawn` and `fx_raven_despawn` get the root scale (also the ZDO scale) with particles in Hierarchy scaling mode; a third copy of `TrapFxSets.Tune`, see Known defects in the architecture doc. Teleport's puffs stay full size (a quarter was not visible).
- Messages: failures only (an armed map click that fails shows its error through `Kit.Notify`); landings, appears and blinks are logged.

## Probes

| # | Command | Answer 2026-10-27 |
|---|---|---|
| P1 | `op=inspect&prefab=vfx_Place_HildirClothesRack`, `op=inspect&prefab=sfx_cooking_station_take` | both exist with a `ZNetView` (the puff also carries a sound: `ZSFX`, `AudioSource`) |
| P2 | `op=blink&probe=1` | `Hugin: prefab Ravens, ZNetView False, Animator True, Raven True; children: Hugin Raven Animator Munin Raven Animator; raven Hugin name=$npc_hugin visual=model; raven Munin ...; despawn effect: fx_raven_despawn (local); local only: fallback added` - the first build read `Raven` on the root and copied both birds with their brains: fixed (copy the Hugin child only) |
| P3 | `op=teleport&to=player&player=Claude2` with Claude2 indoors | not run (do you land on the roof? if yes: `distant=false` when the target is loaded here) |
| P4 | `op=spawn&prefab=Greydwarf&count=5&dist=5&where=ahead`, then Blink through them | `shoved and tarred 4 (Greydwarf=4)`, then `3 (Boar=3)`; Maksym: pushed aside and tarred, no flash ("works good") |

## Test log

World `ClaudeTest`, 2026-10-27. `op=cheatstate` before: `Achievements.IsWorldCheated False`, `Terminal.m_cheat False`, no god, ghost or debug fly.

| Command | Answer (first line) | Observation | Verdict |
|---|---|---|---|
| F6 Blink x5 (5 m build) | `blinked 5.0 m to -7.1,28.9,662.4, shoved and tarred 0; other players spared (your PvP off)` | Maksym: "Blink works"; asked for twice the distance and another sound | pass, tuned (10 m, pop) |
| `op=teleport&to=death` | `teleporting to your death point at ...` | Maksym: "teleport to last death works" | pass |
| `op=blink&probe=1` | see P2 | both ravens copied: fixed | fixed |
| Blink through 5 Greydwarfs | `shoved and tarred 4 (Greydwarf=4)` | first build showed the game's red hit flash: replaced by tar + push without a hit; then "works good" | pass |
| `op=teleport&to=death` (512 m) with the look | `teleporting to your death point at -187.7,32.8,-386.6 (512 m)` | the smoke came after the screen cleared but was too small to see and too short | tuned |
| `op=teleport&to=point&at=-208.4,31.1,-337.0` | `teleporting to -208.4,31.1,-337.0 (13 m)`; log `landed after 2.0 s` | the look played behind the black screen: moved to the moment it clears | fixed |
| `op=teleport&preview=1` (several) | `appear effect at ...` | sphere, slower, twice larger, one sound | tuned |
| 22 sounds via `op=visualeffect&name=_probe&prefab=<sfx>&at=self` | `probe <id> ... removed after 4 s` | Maksym picked the ward dome `fx_guardstone_activate` ("I like dome") | picked |
| `op=teleport&to=point&at=-192.0,52.7,-718.8` | `teleporting to -192.0,52.7,-718.8 (10 m)`; log `appeared after 7.9 s` | dome, smoke and sound: "all good" | pass |
| `op=teleport&to=aim` | `teleporting to the crosshair at -190.9,29.5,-346.1 (11 m)` | ran; once refused while looking at the sky (`nothing under the crosshair within 60 m`) | pass |
| `op=spells&reload=1` after flattening group 9 | `61 spells, 21 titles, 6 pins; 0 bad lines` | F6-F9 kept their spells | pass |

Still to run: Map point and To player with the final look (P3 indoors), the fall guard after a high drop, Esc on the armed map, Claude2 watching (dome, smoke size, puff sounds, Hugin or fallback), `op=cheatstate` after.

## Limits

- The teleports keep the game's black screen (2 s fade plus the area wait, 8 to 15 s); the look plays when it clears. Only blink is instant.
- Blink can end inside a building piece: the game pushes you out of the collider.
- On other players' screens the smoke runs at normal speed and every puff plays its own poof sound (slowing and muting are done on this PC); Hugin's model and his despawn effect are drawn on this PC only, others see the fallback smoke.
- A creature another peer owns, and a player, are shoved through a zero-damage hit, so they still show the hit flash.
- A map point on deep water drops you into the water.

## Undo

- Nothing is saved in the world. Remove the feature: delete `BepInEx/homecheat-commands/HomeCheat.Cmd.teleport.*.dll`, restart the game, and remove the 9.x lines from `spells.txt` and `F6=9.3` (and Maksym's F7-F9) from `spells.txt.pins`. Backups in the game folder: `spells.txt.pre-teleport.bak` (before the group), `spells.txt.pre-flat-teleport.bak` and `spells.txt.pins.pre-flat-teleport.bak` (before the flat group).
