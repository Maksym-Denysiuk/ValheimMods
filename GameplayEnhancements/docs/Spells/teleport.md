---
description: teleport and blink commands of the HomeCheat Valheim plugin (2026-10-26, built, not run in game) - teleport to your last death point, to a spot you left-click on the large map, or next to another player (the game's own portal path, a smoke puff and a little pop where you appear); blink, a 5 m dash along your flat look direction through walls and creatures that shoves everyone on the line aside with tar and no damage, Hugin the raven poofing at both ends; spell group 9 Teleport, Blink on F6; cards, how it works, probes and the test list, limits, undo
---
# teleport and blink (feature Teleport)

> **Built 2026-10-26, not run in game yet.** Compiled (`HomeCheat.Cmd.teleport.v20261026152544.dll`) and selftest 81/81; Maksym tests later. Run the [probes](#probes) and the [test list](#test-list) once and record the answers here; until then both commands are B in the command index.

Valheim 1.0.20, BepInEx 5.4.2351, own game and own world only. Source: [src/Features/Teleport/](../../src/Features/Teleport/) (`TeleportCommand.cs`, `TeleportMapPatch.cs`, `BlinkCommand.cs`, `refs.txt`), DLL `HomeCheat.Cmd.teleport.v<stamp>.dll`, namespace `HomeCheat.Features.TeleportCommands` (the game has a type `Teleport`, the dungeon door).

## Goal

Before: the only ways to move far were portals and walking; nothing dashes. After:
- `op=teleport&to=death` takes you back to where you last died in this world;
- `op=teleport&to=map` opens the large map, and the next left click on it takes you there;
- `op=teleport&to=player&player=<nick>` puts you 2 to 5 m from that player;
- `op=blink` moves you 5 m forward at once, through walls and creatures, shoving everyone on the line aside and tarring them.

## Cards

### teleport

- Verdict: **new** (no command moved the player through space; nearest `spawn player=`, which reads another player's position, and `coord`, which stores points).
- Fingerprint: form/movement / self / a point / `Player.TeleportTo` (the portal path).
- Targets: only you; `to=player` only reads that player's position.
- Look: `to=death` none of ours (the game's teleport fade stays: it loads the area); `to=map` and `to=player` a smoke puff `vfx_Place_HildirClothesRack` (`smoke_poof`) and a little pop `sfx_cooking_station_take` (`Poof_CloudDisappear`) at your feet once you are down, networked. Alternatives for Maksym's pick in the first test: `sfx_lootspawn` (`Magic_Poof3`), `fx_BonusYield` (`BonusPopV2`), `vfx_odin_despawn`. No lightning (Maksym).
- Persists: nothing. Caps: `radius` 50. Undo: `to=death` or walk back.
- Decisions (plan approved 2026-10-26): landing 2 m to `radius` (default 5) m from the player; items that portals refuse (ore, metal) are allowed in every mode (Maksym).

### blink

- Verdict: **new** (nearest `trap` tar kind: a hit with `Tared` and no damage).
- Fingerprint: form/movement + push + status / self + enemies / a line ahead / `AttackCommand`, `Character.Damage` with an empty `HitData`.
- Maksym: "it should pushback enemies to the sides and give them tar effect. no damage"; flat direction, follows the terrain; the look is "the crow that gives advice" (Hugin).
- Targets: enemies; tamed with `tamed=1`, bosses with `boss=1` (target model: excluded unless asked); other players only through the shared PvP rule (`pvp=1`, which the spell line passes); never you.
- Look: Hugin, the advice raven (`Tutorial.m_ravenPrefab`, class `Raven`), poofs where you leave and where you appear: his own despawn effect (`Raven.m_despawnEffect`) and the animator trigger `poff` (`Raven.FlyAway`). His model is a local copy (no texts, no chat, colliders off) removed after 2 s. When his despawn effect is not networked, every player also gets `vfx_odin_despawn` and the crow call `sfx_fallenvalkyrie_screech` (`probe=1` says which). No portal.
- Persists: nothing. Caps: `dist` 20, `width` 5, `push` 500.
- Pin: F6 (plan approved 2026-10-26), so it can be cast in a fight without opening the radial.

## Use

| Command | Parameters (default) |
|---|---|
| `op=teleport&to=death` | `fx` is ignored: no effect of ours |
| `op=teleport&to=map` | `fx=1` (0 = no smoke and pop) |
| `op=teleport&to=player&player=<nick>` | `radius=5` (2 to 50), `fx=1`; host only |
| `op=blink` | `dist=5` (cap 20), `width=2` (metres to each side of the line, cap 5), `push=150` (cap 500), `tamed=0`, `boss=0`, `pvp=0`, `fx=1` |
| `op=blink&probe=1` | read-only: Hugin's prefab, whether his model and his despawn effect are networked, the fallback prefabs |

Spell bar (`spells.txt`): 9 Teleport: 9.1 Last death (`to=death`), 9.2 To coordinates: 9.2.1 Map point (`to=map`), 9.2.2 To player... (`to=player&player={player}`, form `ask=player:@players`), 9.3 Blink (`op=blink&pvp=1`, `pin=F6`). The root is now 1, 2, 4, 5, 6, 7, 8, 9: eight entries, one ring; id 3 (the removed Protect) is not reused.

Answers (first line): `teleporting to your death point at x,y,z (n m)`, `map armed: left-click a spot to teleport there (closing the map cancels)`, `teleporting to <name>: 3.4 m from them at x,y,z`, `blinked 5.0 m to x,y,z, shoved and tarred 3 (Greydwarf=3)` + the PvP note. Errors: `no death point in this world`, `player '<nick>' is not online`, `that is you: pick another player`, `the world ends there: blink refused`, `the game refused the teleport (already teleporting?)`, `click inside the map to teleport`.

## How it works

- **teleport** calls `Player.TeleportTo(pos, rot, distantTeleport=true)`, the path portals use (IL 1.0.20): `UpdateTeleport` waits 2 s, moves you, waits until the area is ready, then sets your height to `ZoneSystem.GetSolidHeight(target) + 0.5`. A map point therefore needs no height, and a far, unloaded spot works. The appear effect waits with a self-rescheduling `Later(0.2)` until `IsTeleporting()` is false (at most 30 s).
- **Death point**: `Game.instance.GetPlayerProfile().HaveDeathPoint()` / `GetDeathPoint()`, kept per world by the game (the same point as the map's death marker).
- **Map click**: `to=map` arms one click, applies two patches the first time it is cast in a world (the UI patch rule) and opens the large map (`Minimap.SetMapMode(Large)`, by reflection). A prefix on `Minimap.OnMapLeftClick` takes the armed click: `Minimap.ScreenToWorldPoint(ZInput.pointerPosition)` (private, by reflection, as the game's own click does), closes the map, teleports, and skips the game's pin handling for that click. A postfix on `SetMapMode` disarms when the map closes; leaving the world and unloading disarm too. An unarmed click is the game's own.
- **Another player**: loaded on this PC through `Players.Find`; otherwise the host's copy of his character ZDO (`ZNet.GetPlayerList` -> `m_characterID` -> `ZDOMan.GetZDO`), so a player outside your area works. Host only (`Host.RequireServer`). You land at a random angle, facing them.
- **blink** moves you the way `UpdateTeleport` does, without `TeleportTo` (no fade, no wait): transform and body position, velocity zero, `m_maxAirAltitude` = the new height (no fall damage from the blink itself; a drop after it is a normal fall). Target = your position + your flat look direction x `dist`; height = the higher of your own and the terrain there + 0.1 m (`Ground.Height`), so a slope lifts you and walls, pieces and creatures are passed through. Past 10 400 m from the world centre it is refused.
- **The shove**: before the move, every character within `width` of the line (and within 3 m above or below it) gets one hit: no damage, `m_statusEffectHash` = `Tared`, `m_dir` = away from the line + 0.3 up (a random side for one standing on the line), `m_pushForce` = `push`, you as attacker, `PvpHits.ArmHit`. The hit is built as `TrapCommand.Boom` builds its tar hit (two users: a Kit candidate).
- Messages: failures only (an armed map click that fails shows its error through `Kit.Notify`); arrivals and blinks are logged.

## Probes

Run first, read-only:

| # | Command | Look for |
|---|---|---|
| P1 | `op=inspect&prefab=vfx_Place_HildirClothesRack`, `op=inspect&prefab=sfx_cooking_station_take` | both exist with a `ZNetView`; then preview the candidates with `op=visualeffect` and pick the smoke and the pop |
| P2 | `op=blink&probe=1` | Hugin's prefab found, `Animator True`; despawn effect networked or "local only: fallback added" |
| P3 | `op=teleport&to=player&player=Claude2` with Claude2 indoors | do you land on the roof (`GetSolidHeight`)? If yes: `distant=false` when the target is loaded here |
| P4 | `op=spawn&prefab=Greydwarf&count=1`, then `op=blink` through it | pushed and slowed (Tared) and no hp lost: does a hit with no damage carry both? |

## Test list

`op=cheatstate` before the first and after the last test; world `ClaudeTest`; read the new lines of `BepInEx/LogOutput.log` after every test.

1. Walk away from your last death point, `op=teleport&to=death`: you stand there after the fade, no effect of ours. A world where you never died: `no death point in this world`.
2. `op=teleport&to=map`, left-click a far unexplored spot: the map closes, you land on the ground there with the smoke and the pop. Again, but close the map with Esc: disarmed, and the next normal map click selects pins as before.
3. `op=teleport&to=player&player=Claude2` outdoors and indoors (P3): 2 to 5 m from Claude2, facing him; Claude2 sees the smoke and the pop. `player=<your own name>`: `that is you`; an unknown nick: `is not online`.
4. `op=blink` on flat ground (5 m), facing a wall (through it), facing a hill (on the slope), at a ledge (you fall), looking up or down (still flat), near the world edge (refused).
5. `op=spawn&prefab=Greydwarf&count=5` in front of you, `op=blink` through them: each shoved left or right and tarred, no hp lost; `op=killmobs` after.
6. `op=blink&pvp=1` through Claude2 with your PvP on (shoved and tarred) and off (spared).
7. Claude2 watches a blink: your jump, and which look he sees (Hugin or the fallback).
8. F6 casts Blink; the radial shows 9 Teleport with its three entries.

## Test log

| Time | Command | Answer (first line) | Observation | Verdict |
|---|---|---|---|---|

## Limits

- The teleports keep the game's 2 s fade (it loads the area); only blink is instant.
- Blink can end inside a building piece: the game pushes you out of the collider.
- Hugin's model is drawn on this PC only (the tutorial raven is a local object); other players see his despawn effect when it is networked, else the fallback smoke and crow call.
- A map point on deep water drops you into the water.

## Undo

- Nothing is saved in the world. Remove the feature: delete `BepInEx/homecheat-commands/HomeCheat.Cmd.teleport.*.dll`, restart the game, and remove the 9.x lines from `spells.txt` and `F6=9.3` from `spells.txt.pins` (backup of the spell file before this change: `spells.txt.pre-teleport.bak` in the game folder).
