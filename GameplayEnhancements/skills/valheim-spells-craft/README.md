# valheim-spells-craft: independent copy

A Claude Code skill that plans, builds, tests and documents HomeCheat commands, spells and extensions for GameplayEnhancements (repository ValheimMods). It always works from the repository docs and the code in `src/`, never from memory. `SKILL.md` is the entry point. This copy works without the maintainer's own setup.

## What you need

- Claude Code.
- Node.js 18 or newer, for `scripts/commands-check.js`.
- One of: the unpacked `GameplayEnhancements` package (docs in `docs/architecture/`), or a clone of ValheimMods (docs in `docs/`).
- To build and test commands: Git Bash with the .NET Framework `csc` (`src/build.sh`), Windows PowerShell 5.1 (`send-command.ps1`), and Valheim with GameplayEnhancements installed (`install.ps1`). You play on a world you host.

## Install

Claude Code finds skills in a project's `.claude/skills/` folder (or in `~/.claude/skills/` for every project).

1. Copy this folder to `<GameplayEnhancements>/.claude/skills/valheim-spells-craft/`, or to `<ValheimMods clone>/.claude/skills/valheim-spells-craft/`.
2. Start Claude Code in that `GameplayEnhancements` folder or clone.
3. Check: `node .claude/skills/valheim-spells-craft/scripts/commands-check.js` prints the commands it compared and exits 0 or 1. Exit 2 means it did not find the package: pass `--pkg <GameplayEnhancements folder>` or `--mods <ValheimMods clone>`.

From `GameplayEnhancements/skills/valheim-spells-craft/`, where the package ships it, the check finds the package by itself too.

## What this copy skips

| Step | Here |
|---|---|
| package build (`payload/`, `skills/`, `docs/architecture/`, `MANIFEST.md`) | not run; the done gate says so. These files are generated: never edit them in place |
| commit and push | only in a ValheimMods clone (`main`, own paths only; without push rights a branch on your fork and a pull request); in an unpacked package the done gate lists the changed files instead |
| refresh of this skill after new commands | not run here; the done gate writes `skill-refresh-brief.md` next to the package folder for the skill's maintainer |
| `knowledge-sha` in `baseline.md` | compared only in a clone (git); the package copy prints "not compared" |

## Words used

| Word | Means |
|---|---|
| the owner | you: the person who runs the game, hosts the test world and decides on the cards |
| the test world | a world you host only for tests (the skill never acts on a world this PC does not host) |
| the second test account | a second game account you own, without the mod, used as the "other player" in tests; without one, other-player cases stay untested (status B) |

Some trigger phrases in the instructions are Russian ("фиксируем"); any language works.

## Where changes to the skill go

This copy is generated with every package build: the copy here is replaced. Send a fix to the ValheimMods repository (an issue or a pull request that names the file and the change) instead of editing the copy. Local edits are lost at the next package update.

## Version

Skill v1.15 (2026-10-28): the first independent copy. Earlier versions were a plain copy of the maintainer's original.
