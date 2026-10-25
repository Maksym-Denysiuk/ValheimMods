---
description: HomeCheat (GameplayEnhancements) architecture since 2026-10-25 - class diagrams, core plugin vs feature DLLs, the main classes, what is inherited and what is composed, command lifecycle and hot reload, conventions (C# 5), parameter conventions, target model, Kit helpers by need, how to add a command, build and selftest, extension points, safety guards, test aids, known defects; command catalog in homecheat-commands.md
---
> Copy of `docs/homecheat-architecture.md` from the ValheimMods repository, written by the HomeOS package build; edit the original.

# HomeCheat architecture

Restructured 2026-10-25 from one 880-line plugin class plus flat command files. Valheim 1.0.20, BepInEx 5.4.2351, C# 5 (csc 4.0 that ships with Windows). Package: [GameplayEnhancements](../../README.md), sources: [GameplayEnhancements/src/](../../src/), command index: [docs/HomeCheat-plugin/README.md](../HomeCheat-plugin/README.md), command catalog (what each command changes, extend-or-new rules): [homecheat-commands.md](homecheat-commands.md), class diagrams: [diagrams/](diagrams/) (PlantUML sources and SVG). Paths below are relative to `GameplayEnhancements/`.

## Two kinds of DLL

| DLL | Holds | Changes need |
|---|---|---|
| `BepInEx/plugins/HomeCheat.dll` (core) | contracts, services, shared helpers (`Kit`), six built-ins, two core patches (tree scale on load, clean-up on world exit) | a game restart: BepInEx loads it once |
| `BepInEx/homecheat-commands/HomeCheat.Cmd.<feature>.v<yyyyMMddHHmmss>.dll` (one per feature) | the commands of one feature folder | `op=reload`, no restart |

Rules:
- The core holds what is stable. A helper moves into the core only when two or more features use it and it has stopped changing; until then it stays in its feature.
- Features never reference each other's types. They are separate assemblies, reloaded independently. One feature calls another only through `HomeApi.Exec("op=...")`, the same path the inbox uses (the spell bar does this for every cast).
- Only the newest stamp of each feature loads, compared as a number (old 6-digit `hhmmss` stamps lose to new 14-digit ones, and a build after midnight beats one before it).
- .NET 4 cannot unload an assembly. A replaced build stays in memory until the game restarts; it is unloaded logically (its patches, objects and timers are removed).

```
HomeCheat.dll (core)                                      HomeCheat.Cmd.<feature>.dll (hot reload)
 HomeCheatPlugin      Awake/Update only, wires services    Features/World        give spawn list place plant field cultivate
 Contracts            IHomeCommand, ICommandLifecycle,                           repair refill groundlevel path inspect scale coord
                      CommandBase, HarmEffect, PatchSet,                         + FieldAnchor CropSpacing TerrainStamp
                      HomeApi                              Features/Diag         cheatstate cheatinfo
 Pure (no Unity)      Args, CommandException, FormParser,  Features/Earthquake   earthquake      (HarmEffect)
                      BuildStamp, GridMath, GridPoint,     Features/Slime        slime           (HarmEffect)
                      GridRoute, GridCell, PointStore      Features/Spellshot    spellshot
 Services             CommandRegistry, CommandDispatcher,  Features/Heal         heal cleanse bless
                      FileInboxChannel, Scheduler          Features/Spells       spells (bar, radial, spell file, patches)
 Kit                  Players, Aim, Ground, Masks,         Features/Transform    transform + TransformState/Remote/Landing
                      Anchor, Prefabs, Fx, Trees, Refl,    Features/HideBody     hidebody + 3 patches, PublicPositionGuard
                      Fmt, Host, Coords, Constructions,    Features/KillMobs     killmobs
                      Caps                                 Features/VisualEffect visualeffect
 Builtins             ping status reload commands help     Features/FxDump       fxdump
                      harmeffect, TreeScalePatch,          Features/Keybinds     keybinds + 2 patches
                      WorldExitPatch                       Features/Ship         ship
```

Flow of one command: `BepInEx/config/homecheat/inbox/<name>.cmd` -> `FileInboxChannel` (poll 0.5 s, at most 20 files per poll, main thread) -> `CommandDispatcher` (form -> parameters, `op` lower-cased -> command) -> `CommandRegistry` lookup -> `CommandBase.Run` (sub-action or `Execute`) -> answer text -> `outbox/<name>.out` (written as `.tmp`, then moved).

## Class diagrams

Sources are PlantUML in [diagrams/](diagrams/). After a change, render them with `java -jar plantuml.jar -tsvg docs/diagrams/*.puml`; every file starts with `!pragma layout smetana`, so no Graphviz is needed.

### Core: contracts and services

![HomeCheat core: contracts, services, Pure, Kit](diagrams/homecheat-core.svg)

### Who inherits what, by DLL

Every command derives from `CommandBase`; the two effects aimed at a player derive from `HarmEffect`. Nothing goes deeper. One box is one DLL; the helper types of a feature sit in its box.

![HomeCheat commands by DLL](diagrams/homecheat-features.svg)

Each command in the single-command boxes is its own DLL (`earthquake`, `slime`, `spellshot`, `spells`, `transform`, `hidebody`, `killmobs`, `visualeffect`, `fxdump`, `keybinds`, `ship`), except `heal`, `cleanse` and `bless`, which share the `heal` DLL.

### Composition: the spell bar (the largest feature)

![Spell bar composition](diagrams/homecheat-spells.svg)

A cast goes `SpellBar.CastSpell` -> the spell's `Cmd` from the spell file (the Shift form appended when Shift is held), split on `;;` -> `HomeApi.Exec("op=heal")` for each part -> the dispatcher -> `HealCommand` in another DLL: features never reference each other's types.

Spell file v4 (2026-10-25): `SpellParse` (no Unity reference) reads the file into a tree of `SpellNode`s (dotted ids of any depth, titles, sticky inherited from a title) holding `Spell`s; `SpellGroupConfig` shows one title's level (the game lays out a long level as a spiral). A spell with `ask=` flags opens one `SpellAskConfig` level per question (`SpellAsk`, answers `SpellAskOption`, dynamic answers from `SpellAskSources`); the answers collect in a `SpellAskChain`, and the last one calls `SpellBar.CastChain` -> `CastSpell`, which puts each answer into `{key}` (`SpellParse.Substitute`, URL-escaped) before the `;;` split. So a command that needs a choice from the player (a prefab, a count, an area) takes plain parameters, and the choice is made in the spell file; it holds no menu code of its own. Format and limits: [spellbar.md](../Spells/spellbar.md#spell-file-v4).

## Main classes

| Class | Role |
|---|---|
| `HomeCheatPlugin` | BepInEx entry point. Creates the services and the dispatcher, fills `HomeApi`, hands the loaded `HomeCheat` assembly to the command DLLs (`AssemblyResolve`), applies the core patches `TreeScalePatch` and `WorldExitPatch`, registers the built-ins, loads the feature DLLs, ticks the scheduler and polls the inbox. Nothing else. |
| `CommandRegistry` | every command by name and the feature that provides it; loads the newest build per feature from bytes (the file is never locked), unloads the build it replaces, refuses a name another feature already owns (the rest of that DLL still loads); the feature name `core` is reserved for the built-ins |
| `CommandDispatcher` | form text -> parameters -> command -> answer; main thread only |
| `FileInboxChannel` | the file channel (`inbox/*.cmd` -> `outbox/*.out`); no network port |
| `Scheduler` | main-thread timers with an owner, on `Time.time` (they wait while the game is paused); unloading the owner cancels its timers, leaving the world cancels all of them (`CancelAll`) |
| `WorldExitPatch` | core postfix on `ZNetScene.OnDestroy` (logout, lost connection, quit); calls `CommandRegistry.WorldExit`: each command's `OnWorldExit`, its timers and its `OwnInWorld` objects are cleared, then any timer left |
| `HomeApi` | the one static entry point for feature code: `Exec`, `Log`, `LogError`, `Scheduler` |
| `Args` | typed parameters: `Str`, `Int`, `Float` (InvariantCulture), `Flag` (1/true/yes/aim; empty or missing = the default), `Has`, `Required` (throws), `Raw` (a copy, to forward to another command) |
| `CommandBase` | base of every command (below) |
| `HarmEffect` | base of effects aimed at a player: `Target(a)`, `Ring(...)`; reachable as `op=harmeffect&kind=<name>` too |
| `PatchSet` | the Harmony patches of one command under one id (`com.homeos.homecheat.<command>`); applied per patch class (`Apply`) or per method found by reflection (`Patch`), removed on unload |
| `Kit.Host` | world and host guards: `RequireWorld()` (net scene, zones and ObjectDB loaded; returns the world name), `RequireServer()` (also: this game hosts the world), `IsServer` |
| `Kit.Coords` + `PointStore` | named points per world (`start`, `end`, any name) in `BepInEx/config/homecheat/coords.state`; in the core so they survive feature reloads; the text format and limits are the Unity-free `PointStore` (64 points per world, names up to 32 letters, digits, `_`, `-`) |
| `GridRoute` | A* on an integer grid (8 neighbours, no corner cutting, start and end always passable, grids over `MaxCells` = 40 000 refused); `Corners` turns the cells into the polyline to stamp along |

## Inherit or compose

OOP is used, but shallow. Unity and Valheim are built from components, and their types are not ours to subclass. The exceptions are `BaseUnityPlugin` (the plugin), `MonoBehaviour` (`SpellBar`, `TransformState`, `TransformRemote`, `TransformLanding`) and the game's `IRadialConfig` (the five `Spell*Config` classes: `SpellRootConfig`, `SpellHotbarConfig`, `SpellGroupConfig`, `SpellAskConfig`, `SpellLeafConfig`).

- **Inherit** when something is a kind of command with a shared lifecycle: every command derives from `CommandBase`; player-targeted effects derive from `HarmEffect : CommandBase`. Concrete commands are `sealed`. Nothing goes deeper than two levels.
- **Compose** when a command *has* something: Harmony patches (`Patches`), a GameObject (`Own(...)` until unload, `OwnInWorld(...)` until the player leaves the world), timers (`Later(...)`), effects (`Kit.Fx`), a grid (`Kit.Anchor`), a spell file (`SpellFile` inside the spell bar).

`CommandBase` is a template method. `RepairCommand` (shortened) shows every part:

```csharp
public sealed class RepairCommand : CommandBase
{
    public RepairCommand()                     // constructors only register sub-actions: no side effects
    {
        SubFlag("report", "report=1 [radius=10] (health of every piece around you, changes nothing)",
            delegate (Args a) { return Report(a.Float("radius", 10f)); });
    }

    public override string Name { get { return "repair"; } }
    protected override string Usage { get { return "repair [aim=1] [radius=10 (with aim=0)] [amount=0 (= full)] - building pieces back to full health"; } }

    protected override string Execute(Args a)  // the main action
    {
        bool aim = a.Flag("aim", true);
        Player p = Players.Local;              // throws "no local player: load a world first"
        float radius = a.Float("radius", 10f);
        if (!aim && radius <= 0f) throw new CommandException("radius must be > 0");   // answered as "ERROR: radius must be > 0"
        return "repaired ...";
    }
}
```

`Run` (not overridable) builds `Args`, tries the sub-actions in registration order (`SubAction` = the key has a non-empty value, `SubFlag` = the key is 1, true, yes or aim), else calls `Execute`. A `CommandException` becomes `ERROR: <message>`; any other exception is logged with its stack trace and answered the same way. `Help` = `Usage` plus the help texts of the sub-actions, joined with ` | `.

## Lifecycle and hot reload

1. The registry loads the new DLL from bytes and constructs every non-abstract `IHomeCommand` type with a public parameterless constructor. Constructors only set fields and register sub-actions. A command whose name another feature (or a built-in) owns is dropped here, with a `FAILED` line in the answer.
2. It unloads every command of the feature's previous build: `OnUnload()` first, then the base class cancels the command's timers, removes its patches and destroys its owned GameObjects (even when `OnUnload` throws).
3. It registers the new commands and calls `OnLoad()` on each: apply patches, create GameObjects, read settings files. A command whose `OnLoad` throws stays registered; the answer shows `FAILED load of <name>`.

Built-ins are registered once by the plugin and get `OnLoad()` at that moment; they are never replaced.

Leaving the world (logout, a lost connection, quit) is a second, smaller clean-up. The core's `WorldExitPatch` is a postfix on `ZNetScene.OnDestroy`, which runs on every exit (`Game.Logout` is not enough: it can wait on a disk-space dialog, and a client that loses its host never calls it; checked with `il-reader.js` on 1.0.20). It asks the registry to call `WorldExit` on every command: `OnWorldExit()` first, then the command's timers are cancelled and its `OwnInWorld(...)` objects destroyed; last, the scheduler cancels every timer that is left, whatever its owner. Patches and `Own(...)` objects stay, because the next world uses them again (the spell bar is an `Own` object that lives across worlds). The net scene is going away, so world objects are destroyed locally: a networked object's ZDO stays in the save, which is why persistent objects carry a ZDO mark for a later `clear=1`. Log line: `world exit: <n> commands, <n> timers and objects cleared, <n> other timers cancelled`.

State lives in instance fields. Harmony patch classes are static, so they reach the loaded command through `static <Command> Current`, set in `OnLoad` and cleared in `OnUnload`; a patch does nothing while `Current` is null. Example: `KeybindsCommand.Current`, `HideBodyCommand.Current`, `SpellsSettings.Current`.

Consequences to know:
- Leaving the world cancels every pending timer: an effect started in one world never finishes in the next. Earthquake stones placed just before then stay until `clear=1`, as after a reload.
- A reload cancels the pending timers of the old build. Earthquake stones placed before the reload then never break on their own; `op=earthquake&clear=1` removes them (they are marked in their ZDO).
- `transform` takes its components off the players on unload, so a new build starts from the human form.
- `hidebody` switches itself off on unload and on quit.
- Harmony patches on game UI methods are applied only once a world is loaded, never during plugin start: the spell bar's `RadialBase.Open` / `UseHotbarItem` patches at start froze the main-scene load (2026-10-25). `SpellPatches.EnsureApplied` applies them from `SpellBar.Update` when the local player exists, or right away on an `op=reload` inside a world; `op=spells&enabled=1` inside a world applies them through `SpellPatches.Apply`. If applying fails, spells switch off and `spells.state` remembers it.
- A feature build that fails to compile leaves the previous build loaded: check that `op=reload` names the new file before testing it.

## Feature notes (2026-10-25)

Behaviour that shapes the code, checked in game on 2026-10-25. The full parameters are in each command's help (`op=help&cmd=<name>`) and the [command index](../HomeCheat-plugin/README.md).

- **World grid anchors.** `cultivate` and `field` lay their grid from the player (`FieldAnchor.AheadOfPlayer`): row 1 is your own cell (`dist=0` by default), centred on your flat look direction, rows running away from you. Both use the same anchor, so the crops land exactly on the cultivated cells. `plant` and `place` still start at the crosshair (`Kit.Anchor`).
- **field spacing.** Without `spacing=`, `field` uses the crops' minimum legal distance from the game's data (`CropSpacing`): the sapling's `m_growRadius` plus its collider radius, the largest over all crops in `crops=` (carrot 0.68 m). Fallback 1.5 m.
- **field plants only connected soil.** Only cultivated cells joined to your own cell through cultivated cells (all 8 neighbours) are planted. A gap of uncultivated soil ends the field there; with your cell uncultivated nothing is planted. `map=1` prints the grid (`@` your cell, `#` connected, `o` cut off, `.` no soil). Limits: 40 000 cells examined, 2 500 crops planted.
- **Terrain commands** reuse the game's hoe pieces (`TerrainStamp`): the piece's `TerrainModifier` radii are changed for the spawn and restored right after.
- **earthquake damage.** `damage=0` (default) only pushes. With `damage>0` every slam carries a hammer's damage: `weapon=SledgeStagbreaker` by default; `mode=strong` takes `SledgeDemolisher` and pushes 8.4 instead of 2.1; `weapon=auto` = the strongest `Sledge*` item (not `SledgeCheat`); `quality=max` by default, damage = base + per level x (quality - 1).
- **slime** casts effect set 2 by default (`fx=2`).
- **repair** has `report=1 [radius=10]`: the health of every piece around you, read from the ZDO, changing nothing.
- **cheatstate** reads, besides the achievement, terminal and profile flags: the player's debug-fly, god and ghost state, inventory items marked cheated, and cheated items, ragdolls and creatures within `radius=25`.

Built in wave 1 (2026-10-25), not yet run in game; the test lists are on the command pages ([build-terrain.md](../HomeCheat-plugin/build-terrain.md), [ship.md](../HomeCheat-plugin/ship.md), [spellshot.md](../Spells/spellshot.md)):

- **coord** is the player's front end to `Kit.Coords`: `set=start|end|<name>` (your position, or the crosshair with `aim=1`), `show=1`, `get=1`, `clear=1`. Other commands read the points with `Coords.Get` / `Coords.Require`; the command itself only stores them.
- **Keep away from buildings.** `groundlevel avoid=1` measures from the centre to the nearest player-built piece's pivot (`Constructions.Near`) over the area the stamp really changes: the larger of the hoe piece's smooth radius and radius + 1 m, not the level radius. It refuses, or with `shrink=1` levels a circle that stops 1.1 m short of that pivot (at least 1 m). A new keep-away check uses the same rule: the radius the game changes, plus a margin to the pivot.
- **path routing.** `path from=coords` stamps from the stored start to the stored end; `avoid=1` routes on a 1 m grid over both points plus a 10 m margin (`GridRoute`): a cell is blocked by `Ground.IsBlocked(cell, 0.6)` or a piece pivot within 1 m. Both stamp along the corner polyline (`GridRoute.Corners`).
- **Cut or refuse at a cap.** The old straight `path` refuses over 400 stamps; the opt-in `from=coords` and `avoid=1` modes stamp up to 400, stop, and name the cut in the first answer line. Cutting is allowed only in a new opt-in mode whose card decided it and only when the part made is useful on its own (a path that stops); an existing mode keeps refusing (no silent widening).
- **spawn at another player.** `player=<nick>` spawns ahead of that player; another player than you needs `Host.RequireServer()`. `yaw=` / `pitch=` are world angles. Item prefabs fall to the ground like any spawn.
- **Status effect on a projectile hit (harpoon).** The game's `Attack` puts the weapon's `m_attackStatusEffect` into `HitData.m_statusEffectHash`; `Projectile.Setup` keeps it when the prefab has none; `Character.RPC_Damage` adds it and calls `SetAttacker`. `spellshot` builds its own `HitData`, so `type=harpoon` sets `m_statusEffectHash` itself and the vanilla `SE_Harpooned` does the pull (IL, 1.0.20).
- **Temporary tuning of a game object (ship).** `ship` multiplies instance fields (`m_sailForceFactor`, `m_backwardForce`) of `Ship.GetLocalShip()`, always from the originals it kept per ship (x5 then x10 = x10), restores them in `OnUnload` and forgets them in `OnWorldExit`. Ship physics run on the ship's owner, so a change needs `Ship.IsOwner()`.

## Conventions

- One public type per file, named after the type. Namespaces: `HomeCheat` (contracts and the Unity-free `Pure` types), `HomeCheat.Kit`, `HomeCheat.Services`, `HomeCheat.Builtins`, `HomeCheat.Features.<Feature>`, except where the name would hide a game or Unity type (`World` -> `WorldCommands`, `Transform` -> `TransformCommands`, `Ship` -> `ShipCommands`). A new feature named after a game type takes the `<Name>Commands` form.
- Feature types are `internal` unless Harmony or Unity needs them public (`SpellPatches`, whose prefixes are found with `GetMethod`; the `MonoBehaviour` classes). Feature commands are `public sealed`; the registry creates them by reflection and needs a public parameterless constructor. The six built-ins are `internal sealed`: the plugin creates them itself.
- C# 5 only: no `?.`, no `$""`, no expression-bodied members, no `nameof`.
- Unity objects are compared with `== null`, never with `??`: `??` skips Unity's destroyed-object check.
- No capture of a `for` loop variable in a lambda: copy it to a local first (C# 5 fixes this only for `foreach`).
- User errors throw `CommandException` (`Args.Required`, `Players.Local`, `HarmEffect.Target`, `Prefabs.Require` do it for you). Older command code that returns an `"ERROR: ..."` answer directly gives the same result and is left as it is.
- `Fmt.Vec` and `Args` use InvariantCulture. Numbers printed with `ToString("F1")` in older answers still follow the PC's culture.
- All game access stays on the main thread; the dispatcher refuses other threads.

## Parameter conventions

How `Args` reads a form (`src/Core/Pure/Args.cs`):

| Call | Returns |
|---|---|
| `Str(key, def)` | the value when the key exists and is not empty, else `def` |
| `Int`, `Float` | parsed with InvariantCulture (`0.5`, never `0,5`), else `def` |
| `Flag(key, def)` | empty or missing = `def`; `1`, `true`, `yes`, `aim` = true; any other word = false, even when `def` is true |
| `Has(key)` | the key exists, even with an empty value (`Anchor.FromArgs` uses it for `dist`) |
| `Required(key, hint)` | the value, or `CommandException "<key> required, <hint>"` |
| `Raw` | a copy of all parameters, to forward to another command (`harmeffect`) |

Rules for a command's parameters:
- Keys are lower case and mean the same thing in every command. Families in use: placement `aim=1|0` then `radius=` (repair, refill), `at=aim|self|x,y,z` (spellshot, visualeffect), `dist=` (metres ahead instead of the crosshair, `Anchor.FromArgs`), `from=aim|coords` (`coords` = the stored start and end of `op=coord`, path), `player=<nick>` (where: ahead of that player, spawn; not whom an effect hits, which is `target=`); keep-away `avoid=1`, `shrink=1` (groundlevel, path); orientation `yaw=`, `pitch=` in world degrees (spawn); grid `rows`, `perrow`, `spacing`, `aimcol`, `scale`, `clear` (plant, place, field, cultivate); ring `rings`, `gap`, `step`, `itemsize`, `delay` (slime, visualeffect); target `target=<nick>`, `players=`, `boss=`, `tamed=`; strength `damage=`, `push=`, `force=`, `mode=`; time `duration=`, `delay=`, `lifetime=`; read-only sub-actions `list=1`, `status=1`, `report=1`, `find=<text>`, `dump=<prefab>`, `probe=`.
- Defaults make the command work with only its essential parameter (`op=heal`, `op=plant&prefab=Beech_Sapling`). A parameter with no safe default is read with `Required` and the command fails with `ERROR: <key> required, <hint>`.
- A new option is opt-in: it keeps the old behaviour when absent. Changing an existing default that changes who is hit, how much, or what stays in the world is a decision for Maksym, never a side effect of another change.
- `Usage` states every parameter with its default and cap; test aids and read-only sub-actions say so in their help text.
- The answer's first line is a summary with counts (`planted 24 (Pickable_Carrot=24) ...`), and it names any cap or clamp that applied. Failures throw `CommandException` (answered as `ERROR: ...`). Numbers through `Fmt` (InvariantCulture).

## Target model

Every effect that touches characters decides five classes: **self**, **enemies** (untamed creatures), **tamed** creatures, **other players**, **bosses**.

- Self should be safe by default. `earthquake`, `spellshot` and `killmobs` never hit the caster; `slime` with its default `hurtself=1` hits the target player too once they leave the free gap (`SlimeEffect.cs:73`, see Known defects).
- Other players are reached only when this game hosts the world (`ZNet.instance.IsServer()`): `slime` and `earthquake` refuse `target=<nick>` otherwise (`SlimeEffect.cs:65`, `EarthquakeEffect.cs:56`), `transform` refuses other players on a client and allows only the raven form on them (`TransformCommand.cs:111-118`). Newer commands use `Host.RequireServer()`: `spawn player=<nick>` for another player, `ship` for the shared ship.
- `target=<nick>` centres a `HarmEffect` on another player (`HarmEffect.Target`, unknown nick = `ERROR`); `Players.Find` matches the nickname case-insensitively.
- Tamed creatures and bosses are excluded unless asked: `killmobs` has `tamed=1`, `boss=1`.
- Hitting other players or friendly creatures should be an opt-in parameter. Two older defaults do not follow this yet (see Known defects): `slime hurtself=1`, `earthquake players=1`.
- Who sees an effect: objects spawned with a `ZNetView` on the host are synced to every player; local-only objects (`visualeffect share=0`, `transform` models, the spell bar) are seen only on this PC; `scale` on trees is re-applied on load only where HomeCheat is installed (`TreeScalePatch`).

## Kit by need

Shared helpers in `src/Core/Kit/` (namespace `HomeCheat.Kit`). Use them instead of writing a fourth copy; a helper written inside a feature moves here only when a second feature needs it and it has stopped changing (a core change: restart).

| Need | Helper |
|---|---|
| the local player, or a clear error when no world is loaded | `Players.Local` (throws `no local player: load a world first`) |
| another player by nickname | `Players.Find(name)` (null when offline); in a `HarmEffect`: `Target(a)` |
| the crosshair point | `Aim.Point(p, out aimed)` (60 m ray on solid objects and terrain, else 8 m ahead) |
| the solid object under the crosshair | `Aim.SolidHit(range, out hit)` (no terrain, triggers ignored) |
| a grid of cells from the crosshair or `dist=` metres ahead | `Anchor.AtAim(p)`, `Anchor.FromArgs(p, a)`, `anchor.Cell(row, col, centre, spacing)`; maths in `GridMath` (Pure) |
| a grid from the player's own cell | `FieldAnchor.AheadOfPlayer` (feature World, not Kit yet) |
| ground height, is a spot free, is it cultivated | `Ground.Height`, `Ground.IsBlocked(pos, radius)`, `Ground.IsCultivated` |
| physics masks | `Masks.Solid` (buildings, trees, rocks, plants), `Masks.SolidAndTerrain` |
| a prefab by name | `Prefabs.Get` (null), `Prefabs.Require(name, hint)` (throws), `Prefabs.Spawn(name, pos, rot)`; `Prefabs.BaseName(go)` = the name without `(Clone)` for answers |
| a hoe piece stamped along a polyline, cut at a cap | `PathCommand` stamping (feature World, not Kit yet; a second user, for example an area command, makes it a promotion candidate) |
| prefab discovery for `find=` / `dump=` / `list` | `Prefabs.All()`, `Matching(text, tag)`, `Find(text, max, tag)`, `Dump(prefab, fieldsOf...)`, `Lines` |
| a visual or sound effect that removes itself | `Fx.Spawn(owner, prefab, pos, life)` (timer owned by the command), `Fx.Destroy(go)` (networked or local) |
| a non-public game field | `Refl.Get/Set/GetFloat/SetFloat/GetStatic/Field` (missing member = default, never a crash) |
| object or tree scale saved with the world | `Trees.SetScale(inst, scale)` (ZDO key `scale`), `Trees.ApplyScale(tree, factor)` |
| numbers in answers | `Fmt.Vec(v)`, `Fmt.F(f, digits)` |
| a world must be loaded | `Host.RequireWorld()` (throws `no world loaded ...` in the menu or while loading; returns the world name) |
| this game must host the world (other players, shared world data) | `Host.RequireServer()` (throws `host only: ...`); `Host.IsServer` to branch |
| a point the player stored earlier (CoordStart, CoordEnd, any name) | `Coords.Get(name, out pos)`, `Coords.Require(name)` (throws with the `op=coord&set=<name>` hint), `Coords.Set`, `Clear`, `ClearAll`, `Names`; `Coords.Start` / `Coords.End` name the pair; kept per world in `coords.state` |
| player-built pieces near a spot (keep away from buildings) | `Constructions.Near(pos, radius, out nearest)` (count and the closest), `Constructions.All(pos, radius)`, `Constructions.Describe(piece, from)` for the answer (`wood_wall at 3.2 m`); distance is to the piece's pivot, so pass your radius plus a margin |
| cells of a rectangle between two points (edges along world X and Z) | `GridMath.RectSize(...)`, `GridMath.RectCells(ax, az, bx, bz, spacing, max)` (Pure: centred, both edges included, at most `max`; compare with `RectSize` to report a cap) |
| a route around obstacles on a grid | `GridRoute.Find(width, height, blocked, start, end, out path)` (Pure: A*, 8 neighbours, cap 40 000 cells; `Found`, `NoRoute`, `OverCap`, `Invalid`), `GridRoute.Corners(path)` |
| a cap on a count or size | `Caps.Check(name, value, max)` (refuses: `<name>=<v> is over the cap <max>`), `Caps.Clamp(name, value, max, out note)` (lowers it; put `note` in the first answer line) |
| a delayed action bound to the command | `Later(seconds, act)` on `CommandBase` (cancelled on unload and when the player leaves the world) |
| a GameObject removed on unload | `Own(go)` on `CommandBase` (stays across worlds) |
| a GameObject removed when the player leaves the world (or on unload) | `OwnInWorld(go)` on `CommandBase` |
| your own clean-up when the player leaves the world | override `OnWorldExit()` on `CommandBase` |
| Harmony patches removed on unload | `Patches.Apply(typeof(X))`, `Patches.Patch(method, prefix, postfix)` on `CommandBase` |
| run another command | `HomeApi.Exec("op=...")` |
| log | `HomeApi.Log`, `HomeApi.LogError` |

## Add a command

1. Pick the feature folder it belongs to (or create `src/Features/<Feature>/`; the folder name in lower case is the DLL name).
2. Add `<Name>Command.cs`: a `public sealed class` deriving from `CommandBase` (or `HarmEffect` for an effect centred on a player), with `Name`, `Usage` and `Execute`; sub-actions in the constructor.
3. Patches: one `[HarmonyPatch]` class per file, applied with `Patches.Apply(typeof(...))` in `OnLoad` (or when the command switches the feature on, as `hidebody` does), state through `Current`. A patch whose target must be found by reflection, or that may only go in once a world is loaded, uses `Patches.Patch(method, prefix, postfix)` (see `SpellPatches`).
4. Extra game assemblies (Animator, AudioSource, ZInput, IMGUI ...): one `valheim_Data/Managed` file name per line in the folder's `refs.txt` (`# comment` lines and blank lines are skipped); csc names the missing one.
5. `bash src/build.sh <feature>` (needs the core installed: `bash src/build.sh core` once), then `op=reload` in game, then `op=help&cmd=<name>`.
6. Document it: a page under `GameplayEnhancements/docs/<topic>/`, a row in the [command index](../HomeCheat-plugin/README.md) and a row in the [command catalog](homecheat-commands.md#catalog) (feature, class, fingerprint, side effects, caps, test aids, spell-bar entries). `commands-check.js` of the `valheim-spells-craft` skill fails until all three exist.

## Build

From Git Bash. The game folder defaults to `/d/steam/steamapps/common/Valheim`; set `VALHEIM_DIR=/c/...` for another install path. Builds go to `${TMPDIR:-/tmp}/homecheat-build` first and are then copied into the game; csc's CS0618 (obsolete game API) warnings are hidden.

```bash
bash src/build.sh selftest     # Core/Pure + SelfTest only, no game references: form parser, Args, build stamps, grid math, rectangles, routes, point store; exit 0 = all passed
bash src/build.sh core         # BepInEx/plugins/HomeCheat.dll; refuses while valheim.exe runs
bash src/build.sh <feature>    # e.g. heal (folder name, any case); stages HomeCheat.Cmd.heal.v<stamp>.dll, then op=reload
bash src/build.sh all          # core (if the game is closed) and every feature
```

Every build of a feature gets a new file name; the older builds stay in `homecheat-commands/` but do not load. The HomeOS package build (`scripts/valheim/packages/build.js`) takes the newest build of each feature and `src/` as it is, and stops when a feature folder has no build or a build has no folder.

## Extension points

Where new behaviour goes, from the smallest change to the largest. Prefer the first row that fits; see [homecheat-commands.md](homecheat-commands.md) for the extend-or-new rules and the commands each point already serves.

| Want | Extend | Where | Notes |
|---|---|---|---|
| a new spell-bar entry for an existing command (a preset) | one line in the spell file | `BepInEx/config/homecheat/spells.txt` (spell file v4, format in its header: `id \| label \| command \| flags`, dotted ids of any depth under a title line `id \| label`, `;;` chains commands, flags `sticky`, `pin=`, `shift=`, `ask=`) | no build; `op=spells&reload=1` (0 bad lines). Never reuse an id with a new meaning. A long level needs no splitting: the game continues it as a spiral. The game folder copy is the source; `build.js` copies it into `payload/`; `install.ps1` installs it only when missing |
| a spell that asks the player first (which prefab, how many, which area) | `ask=<key>:<answers>` flags on the spell line, `{key}` in its command | `spells.txt` | no build; the command only takes plain parameters. Answers fixed (`Label=value,...`) or dynamic (`@players`, `@crops`, `@trees`, `@prefabs`) |
| a planned command's spell line | swap its `@dummy` "(soon)" line for the `# real form (<session>): ...` comment above it, same id | `spells.txt` (fresh read just before the edit) | by the session that builds the command; `spells-ui-test.txt` stays `@dummy`; `SpellFile.DefaultText` is refreshed once after the last wave |
| a new dynamic answer source for `ask=` | a `case` in `SpellAskSources.Resolve` and the name in `SpellParse.Sources` | `Features/Spells/` | spell-bar owner only (one owner of the Spells folder at a time); read-only |
| a new option of an existing command | a parameter read in `Execute`, default = old behaviour | the command's class in `src/Features/<F>/` | `op=reload`; document it in `Usage`, the page and the catalog |
| a read-only helper of a command (discovery, report) | `SubAction(key, help, run)` or `SubFlag(...)` in the constructor | the command's class | runs instead of `Execute` when the key is present |
| a new projectile or strike type | a branch in `Candidates(type)` and the type check | `Features/Spellshot/SpellShotCommand.cs:31,58` | `type=probe&filter=` lists projectile prefabs and the items' attack status effect (`se=`); a status effect on the hit (pull, slow) goes into `HitData.m_statusEffectHash` in `Fire`, as `type=harpoon` does |
| a temporary change to a game object's tuning (speed, force, size while loaded) | instance fields through the game type or `Refl`, originals kept per instance | the command's class | multiply from the kept originals, never from the current value; restore in `OnUnload`, forget in `OnWorldExit`; nothing saved (model: `ship`) |
| a new named visual effect | a `CatalogEntry` | `Features/VisualEffect/VisualEffectCommand.cs:36` | designed with `/valheim-spell-fx`; `list=1` shows the catalog |
| a new animal form | a `case` in `Candidates(type)` and the form's movement in `TransformState` | `Features/Transform/TransformCommand.cs:28`, `TransformState.cs` | local player only; other players: raven only |
| a new boss blessing | nothing: `bless name=` matches any guardian power by part of its name | `Features/Heal/BlessCommand.cs` | `bless&list=1` lists the status effects |
| a new effect centred on a player (rings, clouds, slams) | `public sealed class <Name>Effect : HarmEffect` | new `src/Features/<Name>/` | `Target(a)`, `Ring(...)`; also reachable as `op=harmeffect&kind=<name>` |
| any other new command | `public sealed class <Name>Command : CommandBase` | an existing feature folder when it shares helpers with it (World: grid and terrain commands), else a new folder | steps in "Add a command" |
| behaviour the game decides internally (UI, network sync, input) | a Harmony patch class applied through `Patches` | the command's feature folder | `static Current` for state; UI patches only once a world is loaded (see spells) |
| clean-up when the player leaves the world | override `OnWorldExit()`; own world objects with `OwnInWorld(...)` | the command's class | timers are cancelled for you; `Own` objects and patches stay |
| a helper two features need | move it to `src/Core/Kit/` | core | core change: one owner, rebuild `core`, game restart |
| a new channel, registry or lifecycle behaviour | core services | `src/Core/Services/`, `src/Core/Contracts/` | core change, as above |

## Safety guards

What protects the game today. The wave 0 core pass (2026-10-25) put the host guard, the world check, the cap helper and the world-exit clean-up into the core; `confirm=1` for destructive actions is still per command. The last column is what a new command does.

| Guard | State | Where today | A new command |
|---|---|---|---|
| no console cheat path (`Terminal`, `Console`, god/ghost/fly, no-cost, profile cheat fields are never written) | in place | plugin header; `Features/Diag` only reads them | never write these members |
| no network port: commands only from the local inbox | in place | `FileInboxChannel` | - |
| main thread only | in place | `CommandDispatcher` | - |
| other players only when this game hosts the world | in place: `Kit.Host.RequireServer()`; older features keep their own check | `Kit/Host.cs`; `SlimeEffect.cs:65`, `EarthquakeEffect.cs:56`, `TransformCommand.cs:111` | `Host.RequireServer()` before touching another player or shared world data |
| spell bar only in single player or a world this PC hosts | in place | `SpellBar.cs:486` (`Exec`: every cast stops with an error when `ZNet.instance` is missing or not the server) | a command cast from the spell bar inherits it; a command sent through the inbox does not, so it still needs the check above |
| world loaded | in place | `Kit.Host.RequireWorld()` checks `ZNet`, `ZNetScene`, `ZoneSystem` and `ObjectDB`; `Players.Local`, `Prefabs.Require` throw a clear error too | `Host.RequireWorld()` (or `RequireServer()`) first, then `Players.Local` |
| caps on counts and sizes | in place: `Kit.Caps.Check` / `Clamp`; the values stay per command | `cultivate` 2 500, `field` 40 000 examined / 2 500 planted, `place` 400, `plant` 400, `path` 400 stamps (straight refuses; `from=coords` and `avoid=1` cut and say so) and 40 000 route cells, `spellshot` 81 shots and 300 strike points per volley, `groundlevel` radius 12, `list` 200, `spawn` count 100, `ship` speed 10, `coord` 64 points per world | a `const` cap, stated in `Usage`, applied with `Caps.Check` (refuse) or `Caps.Clamp` (lower, note in the answer); cutting a long job at the cap only in a new opt-in mode, named in the answer (Feature notes, "Cut or refuse at a cap") |
| timers and objects of a replaced build | in place | `CommandBase` (`ICommandLifecycle.Unload`): timers cancelled, patches removed, owned objects destroyed | use `Later`, `Own`, `Patches` only |
| timers and objects when the player leaves the world | in place | `WorldExitPatch` (`ZNetScene.OnDestroy`) -> `CommandRegistry.WorldExit`: every timer cancelled, `OwnInWorld` objects destroyed, `OnWorldExit` called | `Later`, `OwnInWorld`; mark persistent objects in their ZDO (earthquake `hc_earthquake`) so a `clear=1` finds them later |
| destructive or world-wide actions need `confirm=1` | missing | earthquake `kill=` and `clearitems=1` act at once | require `confirm=1` and a radius with a default |

## Test aids

- Commands meant for testing: `killmobs` (clear the arena; skips players, tamed and bosses unless asked), `heal` (restore the caster), `spawn` (enemies to test on), `give`, `cheatstate` (before and after a test), `status`, `inspect`, `list`, `fxdump`.
- Read-only sub-actions inside commands: `find=`, `dump=`, `probe=1`, `report=1`, `list=1`, `near=`, `weapons=1`.
- Rule: a command never gets its own heal, kill or clear-the-area mode for testing; a missing test helper becomes its own small command (feature `KillMobs` is the model) and is marked "test helper" in its `Usage`. Older exceptions are listed under Known defects.
- Test world `ClaudeTest`; second player `Claude2` (Maksym's own account). One command at a time through `send-command.ps1`, unique inbox names. Evidence: the answer, new `HomeCheat` errors in `BepInEx/LogOutput.log`, probe numbers, what Maksym saw, `op=cheatstate` before and after. The log of [homecheat-test-run-2026-10-25.md](homecheat-test-run-2026-10-25.md) is the format to follow.

## Known defects (2026-10-25)

| Defect | Where | Why it matters |
|---|---|---|
| `slime` defaults to `hurtself=1`: the clouds hit every player and NPC outside the gap, the target too when it leaves the gap | `SlimeEffect.cs:73` | a widened default hit a second player on 2026-10-24; the target model asks for opt-in |
| `earthquake` defaults to `players=1` (pushes other players and tamed creatures) | `EarthquakeEffect.cs:71` | same rule: other players should be opt-in |
| `earthquake` carries test aids `kill=<radius>` and `clearitems=1`, without `confirm=1`; `slime` carries `heal=1`, `keephealed=`, `healstats=1` | `EarthquakeEffect.cs:41,52`, `SlimeEffect.cs:38-51` | test aids belong in `killmobs`/`heal`; destructive actions need `confirm=1` |
| older features repeat their own `IsServer` check instead of `Host.RequireServer()` | `SlimeEffect.cs:65`, `EarthquakeEffect.cs:56`, `TransformCommand.cs:111` | same result; move to the Kit guard when the feature is next changed |
| older answers format numbers with the PC's culture (`ToString("F1")`) | World and Earthquake answers | a comma may appear instead of a point |
| `cheatinfo` is a discovery leftover (code review: delete) | `Features/Diag/CheatInfoCommand.cs` | - |
