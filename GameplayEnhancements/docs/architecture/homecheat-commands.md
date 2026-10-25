---
description: HomeCheat command catalog (2026-10-25) - every command once, with its feature DLL, class, what it changes, whom, where and how (the four-axis fingerprint), side effects, caps, test aids and spell-bar entries; the rules for deciding whether a new idea already exists, extends a command or needs a new one, with worked examples
---
> Copy of `docs/homecheat-commands.md` from the ValheimMods repository, written by the HomeOS package build; edit the original.

# HomeCheat command catalog

One row per command (34: `override string Name` in `GameplayEnhancements/src/`). Syntax, status and pages: [command index](../HomeCheat-plugin/README.md). Classes and extension points: [homecheat-architecture.md](homecheat-architecture.md). Spell bar: `BepInEx/config/homecheat/spells.txt` (copy in `GameplayEnhancements/payload/`).

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
| spawn | world | `SpawnCommand` | spawn creature | enemies | `dist=` ahead | creature prefab | creatures in the world | - | used as a test helper | 6.4.1 (soon: `ask=` prefab, count, yaw, pitch, player) |
| list | world | `ListCommand` | info | none | none | `Prefabs.All` | none | 200 names | - | - |
| inspect | world | `InspectCommand` | info | none | none | prefab components | none | - | - | - |
| place | world | `PlaceCommand` | place object | ground | grid at crosshair or `dist=` | any prefab, `Anchor` | objects in the world | 400 | - | 7.4, 7.5 |
| plant | world | `PlantCommand` | plant/grow | ground | grid at crosshair or `dist=` | sapling `Plant`, `Anchor`, `Trees.SetScale` | saplings, scale saved in ZDO | 400 | - | 7.3; 7.9 (soon: `area=`, `ask=` tree and size) |
| field | world | `FieldCommand` | plant/grow | ground (cultivated) | grid from your cell | `Pickable` crops, `FieldAnchor`, `CropSpacing` | crops in the world | 40 000 cells, 2 500 crops | `map=1` | 7.2; 7.7 (soon: `area=`, `ask=` area and crop) |
| cultivate | world | `CultivateCommand` | terrain | ground | grid from your cell | hoe piece `cultivate` (`Prefabs.Get`, one `Instantiate` per cell; not `TerrainStamp`) | terrain change saved | 2 500 | - | 7.1; 7.6 (soon: `area=coords`) |
| repair | world | `RepairCommand` | modify object | building pieces | crosshair or radius | `WearNTear` health | piece health | radius | `report=1` | 6.5.1, 6.5.3, 6.5.5, 6.5.7 |
| refill | world | `RefillCommand` | modify object | fireplaces, smelters | crosshair or radius | `Fireplace`/`Smelter` fuel | fuel levels | radius | - | 6.5.2, 6.5.4, 6.5.6, 6.5.7 |
| groundlevel | world | `GroundLevelCommand` | terrain | ground | radius around you | hoe piece `raise` via `TerrainStamp` | terrain change saved | radius 12 | - | 6.1.1-6.1.5 (soon: wait for `avoid=1`) |
| path | world | `PathCommand` | terrain | ground | line ahead | hoe pieces (`path`, `paved_road`) via `TerrainStamp` | terrain change saved | 400 stamps, width 1-8 m | - | 6.3.3, 6.3.4; 6.3.1, 6.3.2 (soon: `from=coords&avoid=1`) |
| scale | world | `ScaleCommand` | modify object | trees of a kind | radius | `TreeBase`, `Trees.ApplyScale`, core `TreeScalePatch` | scale, health and drops saved in ZDO; other players without HomeCheat see normal trees | radius | - | 4.5; 4.7-4.9 (soon: `aim=1`) |
| cheatstate | diag | `CheatStateCommand` | info | self, nearby objects | radius 25 | reads `Terminal.m_cheat`, profile, ZDO marks | none (read-only) | - | the before/after check of every test | - |
| cheatinfo | diag | `CheatInfoCommand` | info | none | none | reflection over game members | none | - | discovery leftover (known defect) | - |
| earthquake | earthquake | `EarthquakeEffect : HarmEffect` | push (+ damage with `damage>0`), place object (stones) | enemies; other players and tamed with `players=1` (default) | rings around self or `target=` | the hammer's slam `Aoe` (fallback `sledge_aoe`) with sledge weapon damage, `HarmEffect.Ring`, rock prefab stones with ZDO mark `hc_earthquake` | stones in the world (break by timer), drops | stages, rings | `find=`, `dump=`, `item=`, `weapons=1`, `demo=1`, `clear=1`; `kill=`, `clearitems=1` (known defect) | 2.5, 2.7, 2.8, 3.1, 3.2, 3.3 |
| slime | slime | `SlimeEffect : HarmEffect` | damage + status (poison) | enemies and, with default `hurtself=1`, players outside the gap | rings around self or `target=` | Blob `Aoe` clouds, `HarmEffect.Ring` | temporary clouds | rings | `near=`, `find=`, `weapon=`, `dump=`, `probe=1`, `onself=1`; `heal=1`, `keephealed=`, `healstats=1` (known defect) | 2.6, 3.4 |
| spellshot | spellshot | `SpellShotCommand` | damage + push | enemies | grid along the crosshair | `Projectile` (arrow, ballista bolt), lightning strikes with hidden damage carriers | projectiles, strikes | 81 shots, 300 strike points | `type=probe&filter=` | 2.1-2.4, 2.9; 2.10 (soon: `type=harpoon`) |
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
| "plant mushrooms in a field" | `field` | plant / cultivated ground / grid / `Pickable` | extend or exists: `field&prefab=<Pickable_Mushroom*>` when the prefab is a crop |
| "make birches 3x" | `scale` | same | exists: `op=scale&filter=Birch&factor=3` |
| "refill fires and repair walls at once" | `refill`, `repair` | modify object / pieces / radius | exists as a chained preset (spell 6.5.7) |
| "a spawn menu: pick the creature, how many, for which player" | `spawn` | spawn creature / enemies / ahead / creature prefab | preset with questions: a `spells.txt` line with `ask=prefab:@prefabs ask=count:...` and `{prefab}` in the form (6.4.1); the command takes plain parameters, no menu code in it |
| "kill the mobs after the test" | `killmobs` | test helper | exists; never a kill mode of the command under test |
| "show green lights on me" | `visualeffect` | visual only | not a command: catalog entry via `/valheim-spell-fx` |
| "turn into a wolf" | `transform` | form / self / on self / model swap | extend: new form in `TransformCommand.Candidates` and `TransformState` |
| "protection dome like the trader" | none | place + protect / zone / two points / ward | new (`dome`); requirements written 2026-10-24, kept privately in HomeOS |
