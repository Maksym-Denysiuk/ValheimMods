---
description: repair and refill commands of the HomeCheat Valheim plugin (2026-10-24, refill fuel filter 2026-10-25) - restore building pieces to full health and top up fuel in fireplaces and smelters, at the crosshair or in a radius; parameters and defaults, what they write, limits, undo; built in CheatResearch, not yet run in game
---
# repair and refill (HomeCheat core ops)

Built 2026-10-24 on Valheim 1.0.20, BepInEx 5.4.2351, host `MDENYSIUK-PERS`, in the CheatResearch session at Maksym's request ("add refill ... first parameter aim or radius" and "add repair with the same signature"). Own game and own world only. Source: [src/Features/World](../../src/Features/World/) (`RepairCommand`, `RefillCommand`; until 2026-10-25 part of the core `HomeCheat.cs`).

## What they do

- **repair** restores every building piece that has a `WearNTear` component (walls, floors, roofs, doors, ships, ...) to full health. The health is the ZDO key `health`; the command claims ownership of the piece, writes the value and sends the game's own `RPC_HealthChanged`. Pieces already at full health are counted and skipped.
- **refill** sets the stored fuel of `Fireplace` (campfire, hearth, brazier, torches, sconces) and `Smelter` (kiln, smelter, furnace) to the object's own maximum. Fireplaces with infinite fuel are skipped. It writes the ZDO key `fuel`; no inventory items are used.

## Use

Drop `repair-1.cmd` into `BepInEx/config/homecheat/inbox/` with the text below (unique file name per call), read `outbox/repair-1.out`.

| Parameter | Default | Meaning |
|---|---|---|
| `aim` | 1 (true) | 1/true/yes = the object under the crosshair (ray 30 m); 0 = everything within `radius` of the player. Other words count as false even when the default is true (for example `on`) |
| `radius` | 10 | metres around the player, used only with `aim=0`; must be above 0 |
| `amount` | 0 | repair: HP to set (capped at the piece's maximum); refill: fuel units to set (capped at the maximum). 0 = full |
| `fuel` | any | refill only: part of the fuel item's prefab name (`Wood`, `Coal`, `Resin`, ...); only objects that burn it are refilled, the others are counted as skipped. See [Fuel by type](#fuel-by-type-since-2026-10-25) |

Examples: `op=repair` (piece under the crosshair), `op=repair&aim=0&radius=30`, `op=refill&aim=0&radius=10`. The first build of repair had default radius 0; it was changed to 10 in the same session (same defaults as refill). The spell-bar presets "Repair r10" and "Refill r10" use `aim=0&radius=10`.

Answers: `repaired N of M objects (K already at full health), crosshair|radius R m`; `refilled N of M objects (...)` with one line per object (`before -> after / max`); `nothing under the crosshair (30 m)`; `the object under the crosshair (<name>) cannot be repaired` / `has no fuel`.

## Limits

- Only loaded objects within the radius are found (`FindObjectsOfType`).
- repair covers placed pieces only. Item durability (swords, armour) is not touched.
- The crosshair ray stops at the first solid object on the layers Default, static_solid, Default_small, piece, piece_nonsolid and vehicle; an object behind a closer one is not reached.
- Ownership of a piece is claimed to write it. On someone else's server the effect depends on the server accepting it; not tested.

## Fuel by type (since 2026-10-25)

Built 2026-10-25 in the unattended, no-game session W3-world-plant of the Valheim work plan (backlog card 3.10 refill-fuel; Valheim 1.0.20, BepInEx 5.4.2351). Goal: refill only the objects that burn one fuel (for example only the smelters, which burn Coal, and not the campfires next to them). Before: `refill` filled every fireplace and smelter in range. Source: `src/Features/World/RefillCommand.cs`.

- `fuel=<text>` matches the prefab name of the object's own fuel item, `Fireplace.m_fuelItem` or `Smelter.m_fuelItem` (an `ItemDrop`: `Wood`, `Coal`, `Resin`, ...), case-insensitive, as part of the name. An object without a fuel item (a kiln) never matches. Without `fuel=` nothing changes.
- Radius form: objects with another fuel are skipped and counted: `refilled 1 of 3 objects (0 already full, radius 20 m, fuel 'Coal': 2 with other fuel skipped)`.
- Crosshair form: an object with another fuel is left alone: `the object under the crosshair (fire_pit) burns Wood, not 'Coal'`.
- No spell line uses it yet; Refill (aim), r10 and r50 (6.5.2, 6.5.4, 6.5.6) stay as they were.

ASSUMPTION A15 (on Maksym's review list): "fuel by type" in the backlog means this optional filter, not a separate fuel amount per type; `refill` already uses each building's own fuel and maximum.

### Test list for Maksym (not run yet)

World `ClaudeTest`, one command at a time, after `op=reload` names the new `HomeCheat.Cmd.world.v<stamp>.dll`.

1. `op=cheatstate` (before).
2. Next to a half-empty campfire and a half-empty smelter: `op=refill&aim=0&radius=20&fuel=Coal`: only the smelter line appears, `1 with other fuel skipped`.
3. `op=refill&aim=0&radius=20&fuel=Wood`: now the campfire.
4. Look at the campfire: `op=refill&fuel=Coal` answers `... burns Wood, not 'Coal'`.
5. `op=refill&aim=0&radius=20` (no filter): as before.
6. `op=cheatstate` (after); compare.

## Verification

Built and compiled without errors (DLL about 27.6 KB at the time). **Not yet run in game: no output line of either command was recorded in any session.** Ask for a run (`op=repair&aim=0&radius=10` next to a damaged wall, `op=refill&aim=0&radius=10` next to a half-empty fire) and record the answer here.

## Undo

There is no undo for restored health or fuel (both are normal in-game values; damage or burning changes them again). To remove the command altogether, remove the plugin: see [README.md](README.md#undo-and-uninstall). The cheat does not use the console, god mode or any cheat flag.
