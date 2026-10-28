# valheim-spell-fx: independent copy

A Claude Code skill that takes a spell description for your own Valheim game and proposes five effect combinations built from real `fx_`, `vfx_` and `sfx_` prefabs (cast, travel, impact, area, linger, sound). Each combination comes with timing, placement, HomeCheat preview commands and what is still needed to make the spell work. Every prefab comes from the description files in `GameplayEnhancements/docs/FX/`, never from memory. `SKILL.md` is the entry point. This copy works without the maintainer's own setup.

## What you need

- Claude Code.
- Node.js 18 or newer, for `scripts/find-fx.js`.
- The unpacked `GameplayEnhancements` package or a ValheimMods clone, for `docs/FX/` and `docs/Spells/`.
- Only for previews in game: Valheim with GameplayEnhancements installed (`install.ps1`, the `visualeffect` command), a world you host, and Windows PowerShell 5.1 for `send-command.ps1`.

## Install

Claude Code finds skills in a project's `.claude/skills/` folder (or in `~/.claude/skills/` for every project).

1. Copy this folder to `<GameplayEnhancements>/.claude/skills/valheim-spell-fx/`, or to `<ValheimMods clone>/.claude/skills/valheim-spell-fx/`.
2. Start Claude Code in that `GameplayEnhancements` folder or clone.
3. Check: `node .claude/skills/valheim-spell-fx/scripts/find-fx.js ice nova` lists ranked prefabs. Exit 2 means the FX docs were not found: pass `--fx-dir <GameplayEnhancements>/docs/FX`, or set `VALHEIM_FX_DIR`.

From `GameplayEnhancements/skills/valheim-spell-fx/`, where the package ships it, the finder locates `docs/FX/` by itself too.

## Works with

`valheim-spells-craft` (shipped beside this skill) builds the mechanics of a chosen combination: the command, its tests and its docs.

## Where changes to the skill go

This copy is generated with every package build: the copy here is replaced. Send a fix to the ValheimMods repository (an issue or a pull request that names the file and the change) instead of editing the copy.

## Version

Skill v4 (2026-10-28): the first independent copy. Earlier versions were a plain copy of the maintainer's original.
