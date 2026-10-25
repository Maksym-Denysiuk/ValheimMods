---
description: HomeCheat BepInEx plugin for our own Valheim (2026-10-24, restructured 2026-10-25) - overview, file command channel, install, build, uninstall, and the index of every command with its syntax and where it is documented; world ops (repair, refill, plant, field, cultivate, place, scale and others), heal/cleanse/bless; architecture in docs/homecheat-architecture.md of the ValheimMods repository
---
# HomeCheat plugin (overview and command index)

HomeCheat is a BepInEx 5 client plugin for Maksym's own Valheim game and world (Valheim 1.0.20, BepInEx 5.4.2351, host `MDENYSIUK-PERS`, test world `ClaudeTest`). It does not use the console, god mode or any cheat flag, and it opens no network port. Written in C# 5 because the only compiler on the machine is csc 4.0 (no `?.`, no `$""`).

## How it works

- **Command channel.** Write a file `<unique-name>.cmd` into `BepInEx/config/homecheat/inbox/` holding a form body, for example `op=give&prefab=Wood&amount=50`. The plugin polls every 0.5 s, runs the command on the Unity main thread and writes the answer to `outbox/<unique-name>.out`. Up to 20 files per poll.
- **Core and features.** The core plugin `HomeCheat.dll` holds the channel, the command registry, the timers, the shared helpers (`Kit`) and six built-ins (`ping`, `status`, `reload`, `commands`, `help`, `harmeffect`). Every other command lives in a feature DLL `BepInEx/homecheat-commands/HomeCheat.Cmd.<feature>.v<yyyyMMddHHmmss>.dll`; only the newest build of each feature loads. `op=reload` loads newer builds without a game restart and unloads the build they replace (its patches, objects and timers go with it). `op=commands` lists every command with its feature. Classes, inheritance and how to add a command: [homecheat-architecture.md](../../../docs/homecheat-architecture.md); sources in [src/](../../src/).
- **Parameters.** `key=value` pairs joined with `&` (or new lines), URL-decoded, keys case-insensitive. Flags accept `1`, `true`, `yes` (and `aim`); any other word is false even when the default is true.
- **Defaults** are chosen so a command works with only its essential parameter. Parameters with no safe default (a prefab name, a tree filter) are required and return an error when missing.

## Install

BepInEx 5 in the Valheim folder, `HomeCheat.dll` in `BepInEx/plugins/`. Built and installed in the CheatResearch session on 2026-10-24 on a throwaway copy of the world first.

## Build

With Git Bash: `bash src/build.sh <feature>` compiles `src/Features/<Feature>/*.cs` with csc 4.0 against the game's `Managed` assemblies and the installed core into `HomeCheat.Cmd.<feature>.v<yyyyMMddHHmmss>.dll` and stages it in `BepInEx/homecheat-commands/`; then `op=reload`. `bash src/build.sh core` builds and installs `HomeCheat.dll` (game closed: the core only loads at start), `all` does both, `selftest` runs the logic tests without the game. Extra game assemblies a feature needs are listed in its `refs.txt`. Details: [homecheat-architecture.md](../../../docs/homecheat-architecture.md#build).

## Undo and uninstall

- One feature: delete its `HomeCheat.Cmd.<feature>.*.dll` from `BepInEx/homecheat-commands/` and restart the game (a reload only loads newer builds).
- The whole plugin: delete `BepInEx/plugins/HomeCheat.dll`, the DLLs in `BepInEx/homecheat-commands/` and the folder `BepInEx/config/homecheat/`. BepInEx itself stays until its files are removed from the Valheim folder. Nothing in the game's own files is changed.
- Changes made in the world (planted, placed, resized objects) are normal world data: see the undo section of each page, or restore a world backup.

## Command index

Syntax in short; defaults and details on the linked pages. Status: V = run in game and recorded, B = built, no run recorded, ? = not checked in the audit.

| Command | Syntax | Status | Documented in |
|---|---|---|---|
| repair | `op=repair[&aim=1][&radius=10][&amount=0]`, `op=repair&report=1[&radius=10]` (health of every piece, changes nothing) | V (2026-10-25) | [repair-refill.md](repair-refill.md) |
| refill | `op=refill[&aim=1][&radius=10][&amount=0]` | V (no fuel object in range) | [repair-refill.md](repair-refill.md) |
| plant | `op=plant&prefab=<Sapling>[&rows][&perrow][&spacing][&aimcol][&scale][&clear]` | V (2026-10-25) | [plant-field-place.md](plant-field-place.md) |
| field | `op=field&prefab=<Pickable_*>\|crops=<a:n,b:n>[&rows][&perrow][&spacing][&scale][&clear]` | V (carrots) | [plant-field-place.md](plant-field-place.md) |
| cultivate | `op=cultivate[&rows][&perrow][&spacing]` | V (with field) | [plant-field-place.md](plant-field-place.md) |
| place | `op=place&prefab=<name>[&rows][&perrow][&spacing][&aimcol][&scale][&amount][&level][&lift][&clear]` | V (woodwall) | [plant-field-place.md](plant-field-place.md) |
| scale (tree resize) | `op=scale&filter=<tree name>[&factor=2][&radius=50]` | V (oak, beech, birch) | [tree-scale.md](tree-scale.md) |
| heal, cleanse, bless | `op=heal`, `op=cleanse`, `op=bless&name=<boss>` | V (2026-10-25) | [heal-cleanse-bless.md](heal-cleanse-bless.md), source [src/Features/Heal](../../src/Features/Heal/) |
| earthquake, slime | `op=earthquake`, `op=slime` (also `op=harmeffect&kind=<name>`) | V | [../Spells/earthquake.md](../Spells/earthquake.md), [../Spells/slime.md](../Spells/slime.md) |
| spellshot | `op=spellshot&type=...` | V | [../Spells/spellshot.md](../Spells/spellshot.md) |
| transform, hidebody | `op=transform&type=...`, `op=hidebody&on=1` | V | [../Transform/transform.md](../Transform/transform.md), [../Transform/hidebody.md](../Transform/hidebody.md) |
| visualeffect | `op=visualeffect&name=<prefab>` (`list=1`, `name=reset`, `duration`, `scale`; see the FX README) | V | [../FX/README.md](../FX/README.md), [src/Features/VisualEffect](../../src/Features/VisualEffect/) |
| give | `op=give&prefab=<item>[&amount=1][&quality=1]` | V (50 wood) | this page only |
| spawn | `op=spawn&prefab=<name>[&count=1][&dist=4][&level=0]` | V (Greyling) | this page only |
| list | `op=list[&filter=<text>]` (prefab names, max 200) | V | this page only |
| groundlevel, path | `op=groundlevel[&delta=0][&radius=4][&tool=raise]`, `op=path[&type=dirt][&length=5][&width=2][&tool]` | V (2026-10-25) | this page only |
| inspect, status, ping, reload, commands, help | `op=inspect&prefab=<name>`, `op=status`, `op=ping`, `op=reload`, `op=commands[&builds=1]`, `op=help&cmd=<name>` | V (2026-10-25) | this page only |
| cheatstate, cheatinfo | discovery helpers that read the world's real cheat flags and game members containing "heat" (code review: delete cheatinfo) | V (2026-10-25) | this page only |
| killmobs | `op=killmobs[&radius=30][&boss=1][&tamed=1]`: hits every creature within the radius with 100000 normal damage (attacker = the local player); skips players, tamed animals and bosses unless asked; test helper, answer lists the kills per prefab | V (2026-10-25) | [src/Features/KillMobs](../../src/Features/KillMobs/) |
| fxdump | `op=fxdump[&out=<folder>]` (default `BepInEx/config/homecheat/fxdump`): read-only inventory of every fx_/vfx_/sfx_ prefab (components, particles, clips, lifetime, used-by) into `fxdump.csv` and `fxdump-usedby.csv`; basis of the FX description files | V (2026-10-25) | [src/Features/FxDump](../../src/Features/FxDump/), [../FX/README.md](../FX/README.md) |
| spells (spell bar) | `op=spells&enabled=1`; G opens the radial with spell groups, action bar with 10 cells | V: radial, bar, placeholder casts, reload (real casts through the bar not verified) | [../Spells/spellbar.md](../Spells/spellbar.md), [../Spells/spellbar-design.md](../Spells/spellbar-design.md) |
| keybinds | `op=keybinds`: stats window F2 to F6, console to backquote, F9 saves the world | V | [../Keybinds/keybinds.md](../Keybinds/keybinds.md) |

Statuses marked 2026-10-25 come from the test run after the restructure ([test log](../../../docs/homecheat-test-run-2026-10-25.md)); the others are taken from the session transcripts of 2026-10-24: V means an answer line or a user confirmation exists, B means compiled but no run recorded. Ask Maksym for a run and update the page when a B command is tried.

## Contents

<!-- folder:start -->
| Item | What it's about | Last modified |
|---|---|---|
| [heal-cleanse-bless.md](heal-cleanse-bless.md) | heal, cleanse and bless commands of the HomeCheat Valheim plugin (2026-10-24) - restore the local player's health, stamina and eitr, remove status effects, apply a boss guardian power or its status effect; parameters, defaults, how they work through plain game methods, limits, undo; built in the spells-design session, not yet run in game | 2026-10-24 |
| [plant-field-place.md](plant-field-place.md) | plant, field, cultivate and place commands of the HomeCheat Valheim plugin (2026-10-24) - saplings or crops or any prefab in a rows x per-row block anchored at the crosshair; parameters and defaults, spacing and clearance rules, scale, limits, undo; built in CheatResearch, field verified with carrots, plant and place not yet recorded as run | 2026-10-24 |
| [repair-refill.md](repair-refill.md) | repair and refill commands of the HomeCheat Valheim plugin (2026-10-24) - restore building pieces to full health and top up fuel in fireplaces and smelters, at the crosshair or in a radius; parameters and defaults, what they write, limits, undo; built in CheatResearch, not yet run in game | 2026-10-24 |
| [tree-scale.md](tree-scale.md) | scale command of the HomeCheat Valheim plugin (2026-10-24) - change the size of trees near the player (oak, beech, birch tested) with matching hit points and drops, saved with the world and re-applied on load by a Harmony patch; parameters, how it works, tests, limits, undo | 2026-10-24 |
<!-- folder:end -->
