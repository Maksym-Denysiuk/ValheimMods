---
description: heal, cleanse and bless commands of the HomeCheat Valheim plugin (2026-10-24) - restore the local player's health, stamina and eitr, remove status effects, apply a boss guardian power or its status effect; parameters, defaults, how they work through plain game methods, limits, undo; built in the spells-design session, not yet run in game
---
# heal, cleanse, bless (HomeCheat command DLL)

Built 2026-10-24 on Valheim 1.0.20, BepInEx 5.4.2351, host `MDENYSIUK-PERS`, in the spells-design session as slot 5 ("healing and blessings") of the spell bar. Own game and own world only. Source: [Cmd_heal.cs.txt](../Spells/Cmd_heal.cs.txt) (rename to `Cmd_heal.cs` and build with `bash build-cmd.sh heal`, see [README.md](README.md#build)). The three commands live in one DLL (`HomeCheat.Cmd.heal.v<time>.dll`).

None of them touches the console, god mode or a cheat flag: they call `Player.Heal`, `AddStamina`, `AddEitr`, the status effect manager and the guardian power methods.

## heal

`op=heal[&hp=<n>][&stamina=<n>][&eitr=<n>][&only=hp|stamina|eitr]` on the local player.

| Parameter | Default | Meaning |
|---|---|---|
| `hp`, `stamina`, `eitr` | 0 = fill to the maximum | amount to add |
| `only` | all | limit to one of the three values |

Refuses when the player is dead. Eitr is skipped when the player's maximum eitr is 0. Answer: `healed: hp 40 -> 100 / 100; stamina ...; eitr ...;`.

## cleanse

`op=cleanse[&names=Burning,Poison,...][&all=1][&list=1]` removes harmful status effects from the local player.

- Default list: Burning, Poison, Frost, Lightning, Spirit, Tared, Smoked, Wet.
- `names=` replaces the list (case-insensitive, matched against the effect's prefab name and its name key). `all=1` removes every status effect, including good ones (food, rested). `list=1` prints the current effects and changes nothing.

## bless

`op=bless&name=<bonemass|eikthyr|elder|moder|yagluth|queen|...>[&duration=<s>][&mode=effect|power]`, or `op=bless&list=1[&filter=<text>]`.

- `name` matches the prefab name of a guardian-power status effect (`GP_Eikthyr`, `GP_TheElder`, `GP_Bonemass`, `GP_Moder`, `GP_Yagluth`, `GP_Queen`): exact name, then `gp_<name>`, then any GP effect containing the text, then any effect containing it. The names are read from this game's ObjectDB at run time, nothing is hard-coded; `list=1` shows them with their key and time to live.
- `mode=effect` (default) adds the status effect straight to the player; `duration` overrides its time to live. `mode=power` sets it as the player's guardian power and starts it, so the game's own cooldown applies (the answer says "equipped, but not started" if the cooldown or conditions block it).

## Limits

- Local player only; there is no `target`.
- `mode=power` changes which guardian power is equipped, and it stays equipped afterwards.
- The status effect names depend on the game version.

## Verification

Compiled and staged. **Not yet run in game: the spells-design session recorded the files as "staged, untested" and no answer of `heal`, `cleanse` or `bless` appears in any session.** Run `op=heal`, `op=cleanse&list=1`, `op=bless&list=1` and `op=bless&name=bonemass` once and record the answers here.

## Undo

Healed values and cleansed effects are ordinary game state. A blessing ends with its time; `op=cleanse&all=1` removes it earlier. To remove the commands, delete the `HomeCheat.Cmd.heal.*.dll` files from `BepInEx/homecheat-commands/` and restart the game (a reload only adds commands). Nothing in the game files is changed.
