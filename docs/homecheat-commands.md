---
description: HomeCheat command catalog (2026-10-25) - every command once, with its feature DLL, class, what it changes, whom, where and how (the four-axis fingerprint), side effects, caps, test aids and spell-bar entries; the rules for deciding whether a new idea already exists, extends a command or needs a new one, with worked examples
---
# HomeCheat command catalog

One row per command (39: `override string Name` in `GameplayEnhancements/src/`). Syntax, status and pages: [command index](../GameplayEnhancements/docs/HomeCheat-plugin/README.md). Classes and extension points: [homecheat-architecture.md](homecheat-architecture.md). Spell bar: `BepInEx/config/homecheat/spells.txt` (copy in `GameplayEnhancements/payload/`).

`scripts/commands-check.js` of the `valheim-spells-craft` skill compares this table with the code, the index and the spell file; keep one row per command name.

## Fingerprint axes

| Axis | Values |
|---|---|
| **Effect** (what changes) | damage, push, heal/buff, status, spawn creature, give item, place object, plant/grow, modify object, terrain, visual/sound, form/movement, UI/input, info, system |
| **Target** (who or what) | self, named player, enemies, objects of a kind, ground, world, none |
| **Placement** (where) | on self, crosshair, radius, ring, grid, line, whole loaded world, none |
| **Mechanism** (how) | the game object or base that does the work: `Aoe`, `Projectile`, `Pickable`, `TreeBase`, `WearNTear`, `Fireplace`/`Smelter`, `SE_*` status effects, terrain hoe pieces, fx prefab, Harmony patch, `HarmEffect.Ring`, grid (`Anchor`/`FieldAnchor`) |

## Catalog

| Command | Feature (DLL) | Class | Effect | Target | Placement | Mechanism | Side effects | Caps | Test aids / read-only parts | Spell bar (`spells.txt`) |
|---|---|---|---|---|---|---|---|---|---|---|
| ping | core | `PingCommand` | system | none | none | - | none | - | - | - |
| status | core | `StatusCommand` | info | self | none | - | none | - | - | - |
| reload | core | `ReloadCommand` | system | none | none | `CommandRegistry` | loads newer feature builds, unloads the old | - | - | - |
| commands | core | `CommandsCommand` | info | none | none | registry | none | - | `builds=1` | - |
| help | core | `HelpCommand` | info | none | none | registry | none | - | - | - |
| harmeffect | core | `HarmEffectCommand` | system | (as the effect) | (as the effect) | forwards to a `HarmEffect` | as the effect | - | - | - |
| give | world | `GiveCommand` | give item | self | none | `ItemDrop` into the inventory | items in your inventory (world data) | stack size | - | - |
| spawn | world | `SpawnCommand` | spawn creature (or drop items) | enemies; ahead of another player with `player=<nick>` (host only, `Host.RequireServer`) | `dist=` ahead of you or that player; `yaw=`, `pitch=` | creature or item prefab, `Players.Find` | creatures or items in the world | count 100 (`Caps.Check`) | used as a test helper | 6.4.1 (`ask=` prefab, count, yaw, pitch, player) |
| coord | world | `CoordCommand` | info (stored points) | self | on self, or crosshair with `aim=1` | core `Kit.Coords` (`coords.state` per world), local marker `Fx.Spawn` | `BepInEx/config/homecheat/coords.state` (outside the world save) | 64 points per world (`PointStore`) | `show=1`, `get=1` (read-only) | 6.2.1-6.2.4, 3.5.1, 3.5.2, 6.6.1, 6.6.2 |
| list | world | `ListCommand` | info | none | none | `Prefabs.All` | none | 200 names | - | - |
| inspect | world | `InspectCommand` | info | none | none | prefab components | none | - | - | - |
| place | world | `PlaceCommand` | place object | ground | grid at crosshair or `dist=` | any prefab, `Anchor` | objects in the world | 400 | - | 7.4, 7.5 |
| plant | world | `PlantCommand` | plant/grow | ground | grid at crosshair (`area=aim`, default) or `dist=`; rectangle of the stored start and end (`area=coords`) | sapling `Plant`, `Anchor`, `GridMath.RectCells`, `Constructions.Near` (`avoid=1`), `Trees.SetScale` | saplings, scale saved in ZDO (grown trees at size only with HomeCheat) | 400 saplings (`area=coords` refused over it), scale 20 | - | 7.3, 7.9 |
| field | world | `FieldCommand` | plant/grow | ground (cultivated) | grid from your cell; `area=coords` = stored rectangle, `area=patch` = the cultivated patch you stand on | `Pickable` crops, `FieldAnchor`, `CropSpacing`, `FieldPatch` (flood fill, patch sampler), `GridMath.RectCells`, `Kit.Coords` | crops in the world | 40 000 cells, 2 500 crops | `map=1` (grid from your cell only) | 7.2, 7.7 (`ask=` area and crop) |
| cultivate | world | `CultivateCommand` | terrain | ground | grid from your cell; `area=coords` = stored rectangle (world X/Z) | hoe piece `cultivate` (`Prefabs.Get`, one `Instantiate` per cell; not `TerrainStamp`), `GridMath.RectCells`, `Kit.Coords` | terrain change saved | 2 500 (`area=coords`: refused as a whole) | - | 7.1, 7.6 |
| harvest | world | `HarvestCommand` | give item (picks crops) | crops (`Pickable`) on cultivated ground | the cultivated patch you stand on | vanilla `Pickable.Interact` per ripe `Pickable` (`CanBePicked`), `FieldPatch.Sample` | crops picked (replanting is `field`), drops on the ground at each crop | 2 500 picks, 40 000 soil samples | - | 7.8 |
| repair | world | `RepairCommand` | modify object | building pieces | crosshair or radius | `WearNTear` health | piece health | radius | `report=1` | 6.5.1, 6.5.3, 6.5.5, 6.5.7 |
| refill | world | `RefillCommand` | modify object | fireplaces, smelters; with `fuel=` only those burning that item | crosshair or radius | `Fireplace`/`Smelter` fuel, `m_fuelItem` for `fuel=` | fuel levels | radius | - | 6.5.2, 6.5.4, 6.5.6, 6.5.7 |
| groundlevel | world | `GroundLevelCommand` | terrain | ground | radius around you | hoe piece `raise` via `TerrainStamp`; `avoid=1` keeps away from player-built pieces (`Kit.Constructions`), `shrink=1` levels a smaller circle | terrain change saved | radius 12; the game's ±8 m clamp named in the answer | - | 6.1.1-6.1.5 (`avoid=1`) |
| path | world | `PathCommand` | terrain | ground | line ahead; `from=coords` = stored start to end | hoe pieces (`path`, `paved_road`) via `TerrainStamp`; `avoid=1` routes on a 1 m grid (`GridRoute`, `Ground.IsBlocked`, `Kit.Constructions`) | terrain change saved | 400 stamps (straight: refused; `from=coords` or `avoid`: cut, named in the answer), width 1-8 m, grid 40 000 cells | - | 6.3.1, 6.3.2 (`from=coords&avoid=1`), 6.3.3, 6.3.4 |
| scale | world | `ScaleCommand` | modify object | trees of a kind (`filter=*` every tree), or the tree under the crosshair (`aim=1`) | radius or crosshair | `TreeBase`, `Aim.SolidHit`, `Trees.ApplyScale`, core `TreeScalePatch` | scale, health and drops saved in ZDO; other players without HomeCheat see normal trees | radius, factor 20 | - | 4.5, 4.7-4.9 |
| cheatstate | diag | `CheatStateCommand` | info | self, nearby objects | radius 25 | reads `Terminal.m_cheat`, profile, ZDO marks | none (read-only) | - | the before/after check of every test | - |
| cheatinfo | diag | `CheatInfoCommand` | info | none | none | reflection over game members | none | - | discovery leftover (known defect) | - |
| earthquake | earthquake | `EarthquakeEffect : HarmEffect` | push (+ damage with `damage>0`), place object (stones) | enemies; other players and tamed with `players=1` (default) | rings around self or `target=` | the hammer's slam `Aoe` (fallback `sledge_aoe`) with sledge weapon damage, `HarmEffect.Ring`, rock prefab stones with ZDO mark `hc_earthquake` | stones in the world (break by timer), drops | stages, rings | `find=`, `dump=`, `item=`, `weapons=1`, `demo=1`, `clear=1`; `kill=`, `clearitems=1` (known defect) | 2.5, 2.7, 2.8, 3.1, 3.2, 3.3 |
| slime | slime | `SlimeEffect : HarmEffect` | damage + status (poison) | enemies and, with default `hurtself=1`, players outside the gap | rings around self or `target=` | Blob `Aoe` clouds, `HarmEffect.Ring` | temporary clouds | rings | `near=`, `find=`, `weapon=`, `dump=`, `probe=1`, `onself=1`; `heal=1`, `keephealed=`, `healstats=1` (known defect) | 2.6, 3.4 |
| spellshot | spellshot | `SpellShotCommand` | damage + push; harpoon: status (pull) | enemies | grid along the crosshair | `Projectile` (arrow, ballista bolt, harpoon `SpearChitin` with the vanilla `SE_Harpooned` set as the hit's status effect), lightning strikes with hidden damage carriers | projectiles, strikes; harpoon drains your stamina while it pulls | 81 shots, 300 strike points | `type=probe&filter=` (item lines show their attack status effect `se=`) | 2.1-2.4, 2.9, 2.10 |
| heal | heal | `HealCommand` | heal/buff | self | on self | `Player.Heal`, stamina, eitr | none | - | used as a test helper | 5.1 |
| cleanse | heal | `CleanseCommand` | status (remove) | self | on self | `SEMan` | removes status effects | - | `list=1`, `all=1` | 5.2, 5.10 |
| bless | heal | `BlessCommand` | status (add) | self | on self | guardian power `SE_*` by name | status effect or guardian power | - | `list=1` | 5.3-5.9 |
| spells | spells | `SpellsCommand` | UI/input | self | none | radial (`IRadialConfig`), `SpellBar` MonoBehaviour, Harmony patches on `RadialBase.Open` / hotbar; spell file v4 parsed by `SpellParse` (no Unity), questions through `SpellAskConfig` | spell file, pins, `spells.state` | 10 cells; ids depth 6, numbers 1-999, 1000 spells; 40 answers per question level (alphabetical blocks above) | `status=1`, `list=1`, `open=1`, `placeholder=1` | (the bar itself) |
| transform | transform | `TransformCommand` | form/movement | self; other players: raven only, host only | on self | model swap, `TransformState`/`Remote`/`Landing` MonoBehaviours | local look; `share=1` spawns a networked Crow | - | `type=models` | 4.1-4.4 |
| hidebody | hidebody | `HideBodyCommand` | form/movement | self | on self | Harmony patches on ZDO send and player list | other players see you 10 km up | - | `status=1` | - |
| killmobs | killmobs | `KillMobsCommand` | damage | enemies (tamed, bosses opt-in) | radius | `HitData` 100 000 damage | kills, drops | radius | test helper | - |
| visualeffect | visualeffect | `VisualEffectCommand` | visual/sound | none | crosshair, self, point, ring pattern | fx prefabs, catalog | temporary effects; `share=1` networked | count | `list=1`, `name=status`, `name=_probe`, `name=_envs` | 4.6 |
| fxdump | fxdump | `FxDumpCommand` | info | none | none | prefab and component inventory | writes two CSV files | - | read-only | - |
| keybinds | keybinds | `KeybindsCommand` | UI/input | self | none | Harmony patches (`ConnectPanel` key, F9 hints) | key behaviour while loaded | - | `status=1` | - |
| ship | ship | `ShipCommand` | form/movement | the ship you are on, steering or sitting (host only) | none | `Ship.GetLocalShip`, instance fields `m_sailForceFactor` and `m_backwardForce` (x speed from the originals), `ZNetView.ClaimOwnership` when another PC owns the ship | none saved; the ship sails faster for everyone on it; ship ownership (physics) moves to this PC; `OnUnload` restores | speed 10 (refused above) | `status=1` | 4.10-4.13 |
| dome | dome | `DomeCommand` | place object (protect) | ground, a zone between two points (host only) | centre and radius from two points (`from=coords` = `Kit.Coords` start/end, or `from=x,z&to=x,z`); one ward, or a hex group without gaps | vanilla `guard_stone` (`PrivateArea`) created as by the hammer (`Piece.SetCreator`, `PrivateArea.Setup`, `WearNTear.OnPlaced`), refusals via `Kit.Constructions`, `Ground.IsBlocked`, water level and other creators' ward ZDOs; host sync of the ward ZDOs (`s_enabled`, permitted list) every 2 s, postfix `Player.OnSpawned` | wards saved with the world, ZDO mark `hc_dome` (+ centre, radius, count, state signature); with `cost=1` the materials; the ZDO owner of every group ward moves to the host when it syncs | radius 150 m, 50 wards (refused above) | `step=list`, `preview=1` (read-only); `step=remove` needs `confirm=1` | 3.5.3-3.5.5, 6.6.3-6.6.5 |
| trap | traps | `TrapCommand` | damage + place object (a trap that waits) | everyone near the trap: you, other players, enemies, tamed, bosses (host only; Maksym's decision 2026-10-25) | matrix on the ground at the crosshair or `at=self` (`perrow` x `rows`, `spacing`) | vanilla `Trap` prefab as the look (`piece_trap_troll`), kept inert through its ZDO state; host watcher (`Later` 0.2 s, `Player.OnSpawned` postfix) over `Character.GetAllCharacters`; blast = `Character.Damage` with an item's damage (`BowAshlands` x2), no attacker, `HitType.EnemyHit`; kinds in `TrapKind` | trap objects in the world until they go off, expire (5 min) or are removed; ZDO marks `hc_trap`, `hc_trap_end`, `hc_trap_arm` and the trap's settings; damage to whoever is in the blast | 50 live, `perrow`/`rows` 9, `life` 3600 s, `trigger` 5 m, `blast` 10 m, `mult` 10, remove radius 100 m | `list=1`, `type=probe&filter=trap` (read-only); `remove=1[&radius=]` | 2.11, 2.12 |

## Exists, extend or new

Fingerprint the request on the four axes and compare it with every row.

| Match | Verdict |
|---|---|
| same command already does it with some parameters | **exists**: answer with the syntax and status, offer to run it; maybe a **preset** (a `spells.txt` line) |
| same effect and same target, any placement | **extend** that command with an opt-in parameter (`type=`, `mode=`, a placement option); old defaults stay |
| same mechanism and placement, different effect | **new command reusing the base or helper** (slime and earthquake both derive from `HarmEffect`) |
| two axes match | **borderline**: show both options with their consequence, recommend one, Maksym decides |
| visual or sound only | **not a command**: a `visualeffect` catalog entry, looks designed with `/valheim-spell-fx` |
| a helper for testing (kill, heal, clear) | reuse `killmobs`, `heal`, `spawn`; else a small separate test-helper command, never a mode of the command under test |
| nothing matches | **new** command, in the feature folder that shares its helpers or a new one |

Before deciding, read this catalog, the nearest command's page and its source in `src/Features/<F>/`.

### Worked examples

| Request | Nearest | Effect / Target / Placement / Mechanism | Verdict |
|---|---|---|---|
| "a command that heals me" | `heal` | same on all four | exists: `op=heal` |
| "volley of fire arrows" | `spellshot` | damage / enemies / grid along crosshair / `Projectile` | extend: new `type=` in `SpellShotCommand.Candidates` |
| "slime on Claude2" | `slime` | same, `target=` exists | exists: `op=slime&target=Claude2` |
| "make slime hit players too" | `slime` | same effect, wider target | already the default (`hurtself=1`); the target model asks for the reverse (known defect) |
| "ring of frost clouds around me" | `slime` | damage+status / enemies / ring / `Aoe` cloud | borderline: `slime&cloud=<frost prefab>` vs a new `HarmEffect`; recommend extending slime (`cloud=` exists) |
| "push enemies away in a circle" | `earthquake` | push / enemies / rings / `Aoe` slam | exists: `op=earthquake&noStones=1` (spell 3.1) |
| "stone wall circle without the slam" | `earthquake` stones | place object / ground / ring / `HarmEffect.Ring` | borderline: `earthquake` option vs a new command reusing `Ring`; recommend new, the effect differs |
| "plant food between my two points, or on the whole patch" | `field`, `cultivate` | plant or terrain / ground / stored rectangle or patch instead of from your cell / `Pickable`, hoe piece | extend: `area=coords` (both) and `area=patch` (field), opt-in; the default grid from your cell is unchanged |
| "harvest the whole patch" | `field` (same soil, opposite effect) | give item / ripe crops / the patch / `Pickable.Interact` | new (`harvest`, feature World): a different effect on the same patch; shares `FieldPatch` with field, picks as by hand (drops, no items straight to the inventory) |
| "plant mushrooms in a field" | `field` | plant / cultivated ground / grid / `Pickable` | extend or exists: `field&prefab=<Pickable_Mushroom*>` when the prefab is a crop |
| "make birches 3x" | `scale` | same | exists: `op=scale&filter=Birch&factor=3` |
| "refill fires and repair walls at once" | `refill`, `repair` | modify object / pieces / radius | exists as a chained preset (spell 6.5.7) |
| "a spawn menu: pick the creature, how many, for which player" | `spawn` | spawn creature / enemies / ahead / creature prefab | preset with questions: a `spells.txt` line with `ask=prefab:@prefabs ask=count:...` and `{prefab}` in the form (6.4.1); the command takes plain parameters, no menu code in it. The missing parameters (`player=`, `yaw=`, `pitch=`, a count cap) were an extend of `spawn` (wave 1); `player=` for someone else is host only |
| "store a start and an end point for later" | none (core `Kit.Coords` holds points, no command set them) | info / self / on self or crosshair / point store | new (`coord`, feature World): a thin command over the core helper; other commands read the points, they never store them |
| "flatten here but don't touch my house" | `groundlevel` | same effect, target and placement; narrower area | extend: `avoid=1` (refuse) and `shrink=1` (smaller circle), opt-in; keep-away = the area the stamp changes (smooth radius), measured to piece pivots |
| "a road from point A to point B around rocks and walls" | `path` | terrain / ground / two stored points instead of ahead / hoe pieces | extend: `from=coords` and `avoid=1` (`GridRoute`); the new modes cut at the stamp cap and say so, the old straight mode still refuses |
| "a harpoon that pulls enemies to me" | `spellshot` | damage + pull status / enemies / grid along crosshair / `Projectile` | extend: `type=harpoon`; the pull is the game's own `SE_Harpooned`, set as the hit's status effect, no own pull physics |
| "make my ship faster" | none (no command touched ships) | form/movement / the ship you are on / none / instance fields | new (`ship`, own DLL); a temporary multiplier from kept originals, restored on unload, host only; this PC takes the ship's ownership so it works steering or sitting |
| "kill the mobs after the test" | `killmobs` | test helper | exists; never a kill mode of the command under test |
| "show green lights on me" | `visualeffect` | visual only | not a command: catalog entry via `/valheim-spell-fx` |
| "turn into a wolf" | `transform` | form / self / on self / model swap | extend: new form in `TransformCommand.Candidates` and `TransformState` |
| "mines that blow up on whoever steps on them" | `spellshot` (matrix), none for waiting | damage + place object / everyone near / grid at the crosshair / vanilla `Trap` look, own watcher | new (`trap`, kind `type=mine`, built 2026-10-25): one placement for every trap kind, the electric fence planned as `type=fence`; design assumed, Maksym reviews |
| "protection dome like the trader" | none | place + protect / zone / two points / ward | new (`dome`, built 2026-10-25: vanilla wards, a group for a large zone; requirements kept privately in HomeOS) |
