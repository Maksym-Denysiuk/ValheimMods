---
description: despawn command of the HomeCheat Valheim plugin (2026-10-25) - removes the one networked object under the crosshair (creature, dropped item, piece, tree, rock, bush, ship, cart) for every player through the game's own destroy path, host only; never players, tombstones, traders or location objects; tamed, bosses and other players' builds opt-in; inloc=1 (2026-11-03, spell 6.8) frees everything inside a location radius except the start temple's and trader camps' own objects; parameters, refusals, how it works, tests, limits, undo
---
# despawn (remove the object under the crosshair)

Built 2026-10-25 in an attended session with the game open (world `ClaudeTest`, hosted on `MDENYSIUK-PERS`), Valheim 1.0.20, BepInEx 5.4.2351. Own game and own hosted world only. Source: [src/Features/Despawn/DespawnCommand.cs](../../src/Features/Despawn/DespawnCommand.cs) (feature DLL `HomeCheat.Cmd.despawn`).

## Goal

Maksym: "add despawn command. It should use croshair parameter and despawn selected item", placed in the Build tab of the spell bar. Before: no command could remove an arbitrary object. Every existing removal acts only on objects its own command marked (`trap remove=1`, `dome step=remove`, `construct` undo, `earthquake clear=1`), and `killmobs` kills with damage, so drops fall. After: `op=despawn` removes the one object you look at, with no drops, for everyone.

## Use

`op=despawn[&range=60][&tamed=1][&boss=1][&others=1][&inloc=1]`, `op=despawn&look=1[&inloc=1]` (read-only). Spell bar: Build tab, 6.8 "Despawn", which sends `op=despawn&others=1&tamed=1&boss=1&inloc=1` (since 2026-11-03).

| Parameter | Default | Meaning |
|---|---|---|
| `range` | 60 | length of the crosshair ray in metres; cap 100 (`range=150` answers `ERROR: range=150.00 is over the cap 100.00`) |
| `tamed` | 0 | 1 = a tamed animal may go |
| `boss` | 0 | 1 = a boss may go |
| `others` | 0 | 1 = a piece built by another player may go (your own pieces and pieces with no creator always may) |
| `inloc` | 0 | 1 = inside a location radius only the start temple's and trader camps' own objects stay (section "inloc=1"); added 2026-11-03 |
| `look` | - | 1 = say what is under the crosshair and whether it would go; changes nothing; with `inloc=1` a second line says which location rule applied |

Answers: `despawned Beech1 at 53.5 m (tree)`; kinds are `creature`, `tamed`, `boss`, `item`, `piece`, `ship`, `cart`, `tree`, `rock or bush`, `object`. A refusal is an error that names the object: `ERROR: refused Player Claude2 at 2.1 m: players are never despawned`. With nothing in front of the ground: `ERROR: nothing under the crosshair within 60 m`.

## What may go, what is refused

Decided by Maksym on 2026-10-25.

| Object | Result |
|---|---|
| creatures, dropped items, trees, rocks, bushes | removed at once |
| player-built pieces (walls, workbenches, wards, furnaces), ships, carts | removed at once (a `confirm=1` step was built first and removed after the in-game test at Maksym's request) |
| a tamed animal, a boss | refused unless `tamed=1`, `boss=1` |
| a piece another player built | refused unless `others=1` |
| any player (you are skipped by the ray, others refused) | always refused, whatever the flags |
| a tombstone (a player's items) | always refused |
| a location's own object: trader, altar (`OfferingBowl`), runestone, vegvisir, anything under a `Location` or `DungeonGenerator`, and anything not player-built that stands inside a location | always refused |
| a trader (Haldor, Hildir, the Bog Witch: any `Trader`) | always refused, also with `inloc=1` |
| an object without a networked `ZNetView` (part of the world itself), terrain | refused / the ray stops at the ground |

## inloc=1 (2026-11-03)

Maksym: despawn should remove "anything besides default spawn structures and trader structures/creatures"; tombstones: "Keep refusing". Before, an object without a creator (spawned by a command, a dropped-in stone, a tree) that stood inside any location radius was refused (`Location.GetLocation`), so Maksym's own things at the spawn could not go. A typed `op=despawn` keeps that rule (no silent widening); `inloc=1` replaces it:

| With `inloc=1` | Result |
|---|---|
| players, tombstones, traders (`Trader`) | refused as before |
| an own object of a kept location: `StartTemple` (the start temple), `Vendor_BlackForest` (Haldor's camp), `Hildir_camp`, `BogWitch_Camp` | refused: `own object of StartTemple: kept with inloc=1` |
| anything else inside a location radius: your items and pieces without a creator at the spawn, other locations' runestones, altars, vegvisirs and their own objects | may go (bosses, tamed and other players' pieces still need their flags) |

"Own object" is decided per object, not per area: the location under the object (`Location.GetLocation`) must be a kept one, and its prefab (`ZoneSystem.m_locations`, the `ZoneLocation` whose `m_prefab.Name` is the location's name; `m_prefab.Load()`, `Asset`, `Release()`) must hold a `ZNetView` child with the same prefab name (`Utils.GetPrefabName`, the name the game hashes into the ZDO) within 0.5 m of where the game spawned it. `ZoneSystem.SpawnLocation` (IL read 2026-11-03, game after 1.0.20) moves the prefab root to the origin and spawns each `ZNetView` child at `location position + location rotation * child position`; despawn computes the same point from the live `Location` transform. The child list is read once per location name and game start and logged (`despawn: location StartTemple has <n> own networked objects`). If a kept location's prefab cannot be read, nothing inside it goes (the safe side). Location prefab names were read from the game's asset data (`StreamingAssets/SoftRef`) on 2026-11-03.

## How it works

1. `Host.RequireServer()` (host only), `Players.Local`, `Caps.Check("range", ...)`.
2. `Physics.RaycastAll` from the camera along its forward axis over `range` metres, triggers ignored, on the solid layers plus `character`, `character_net`, `character_ghost`, `character_noenv`, `hitbox` and `item` (the Kit's `Aim.SolidHit` does not see creatures or loose items). Hits are sorted by distance; colliders of the local player are skipped (the third-person ray can pass through your own body); a `Heightmap` hit ends the search.
3. The first other hit decides (with `inloc=1` the location rule is the own-object check of section "inloc=1" instead of the location refusals): a `Player` is refused; else the nearest `ZNetView` in its parents must be valid; then the refusal rules above, by the object's own components (`Character.IsTamed`/`IsBoss`, `ItemDrop`, `Piece.IsPlacedByPlayer`/`GetCreator`, `Ship`, `Vagon`, `TreeBase`/`TreeLog`, `MineRock`/`MineRock5`/`Destructible`, `Location.GetLocation`).
4. Removal is the game's own path: `ZNetView.ClaimOwnership()` then `ZNetScene.Destroy(go)`, so the ZDO is deleted for every player and nothing drops (the same path as `trap` and `dome` removal).

Nothing is patched, no timer runs, nothing is kept between casts.

## Verification (2026-10-25, in game)

`op=cheatstate` before the tests: `Terminal.m_cheat`, `m_usedCheats` and god mode were already on from earlier play; despawn does not touch them (self-check grep for cheat members: nothing).

| Command | Answer | Observation |
|---|---|---|
| `look=1`, no target | `ERROR: nothing under the crosshair within 60 m` | - |
| at a ward, with the confirm step | `would despawn guard_stone at 6.9 m (piece): add confirm=1 ...`, then with `confirm=1` `despawned guard_stone at 6.8 m (piece)` | it was a one-ward dome: `dome step=list` 2 -> 1; Maksym saw it vanish |
| at a tree | `despawned Beech1 at 53.5 m (tree)` | Maksym: gone, no logs dropped |
| at a rock | `despawned Rock_3 at 3.3 m (rock or bush)` | gone |
| at a blast furnace | `despawned blastfurnace at 3.1 m (piece)` | gone |
| at Claude2, `confirm=1&others=1&tamed=1&boss=1` | `ERROR: refused Player Claude2 at 2.1 m: players are never despawned` | Maksym: Claude2 still standing |

No HomeCheat errors in `BepInEx/LogOutput.log` after any of them. Not yet run: a creature, a dropped item, a ship or cart, the tamed, boss, tombstone and location refusals, and a piece with the build without the confirm step (v20261025175610).

### inloc=1 test list (built 2026-11-03, `HomeCheat.Cmd.despawn.v20261103195028`, not run in game: B)

`op=cheatstate` before and after. At the start temple:
1. `op=despawn&look=1&inloc=1` at a start temple stone (a boss stone or runestone) -> `would refuse ...: own object of StartTemple: kept with inloc=1`, second line `location: StartTemple (kept), own object, 0.0x m from its spawn point`; `LogOutput.log` has the `own networked objects` line with a count above 0.
2. `op=spawn&prefab=Rock_3` (or any item of yours placed at the spawn), then `look=1&inloc=1` -> `would despawn ...` with `not its own object`; then spell 6.8 -> `despawned ...`, gone.
3. Without `inloc`: `look=1` at the same kind of object -> `would refuse ...: inside the location StartTemple: never despawned (inloc=1 allows it)` (typed rule unchanged).
4. A wall Claude2 built -> 6.8 -> `despawned ... (piece)`.
5. At Haldor -> 6.8 -> `ERROR: refused Haldor ...: trader belongs to a location: never despawned`; one of his camp's own objects -> `own object of Vendor_BlackForest`.
6. A tombstone -> 6.8 -> `ERROR: refused ...: tombstones hold a player's items`.
7. A runestone of another location -> `look=1&inloc=1` -> `would despawn ... (runestone)`, `location: <name> (not kept ...)`.

## Limits

- One object per cast. A ward removed from a `dome` group leaves the rest of the group; use `dome step=remove` for a whole dome.
- Only objects loaded on this PC can be hit (the crosshair sees what is drawn).
- `others=1` checks the piece's creator id; a piece with no creator (placed by a command or the world) counts as yours.

## Undo

None: the ZDO is gone from the world. Restore a world backup (`AdminPanelEnchancements` keeps one every 3 minutes), or rebuild the piece. To remove the command: delete `HomeCheat.Cmd.despawn.*.dll` from `BepInEx/homecheat-commands/` and restart the game.
