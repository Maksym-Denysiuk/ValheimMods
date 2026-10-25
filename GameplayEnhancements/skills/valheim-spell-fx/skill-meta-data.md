# AGENT METADATA

Owner:
maksym

Purpose:
Takes a spell description for our own Valheim game and proposes five effect combinations built from real fx_, vfx_ and sfx_ prefabs (cast, travel, impact, area, linger, sound), each with timing, placement, HomeCheat preview commands and what is still needed to make the spell complete.

Allowed use:
Reads GameplayEnhancements/docs/FX/ (README, fx_/vfx_/sfx_ description files) and GameplayEnhancements/docs/Spells/, in HomeOS under references/ValheimCustomMods/ (submodule ValheimMods); a copy of the skill ships in GameplayEnhancements/skills/. Runs scripts/find-fx.js locally (Node, read-only, no network). Answers in chat. May send one command at a time to the HomeCheat inbox through GameplayEnhancements/send-command.ps1 only after Maksym says yes, own game and own world only. Never invents prefab ids, never uses DO-NOT-USE effects, never runs a background loop that keeps firing effects.

Created:
2026-10-24

Last Reviewed:
2026-10-25

Security Reviewed:
2026-10-25

Security Result:
pass with notes: search words from the spell text go on a bash command line (SKILL.md Step 2); pass plain single words (letters only)

Security Scope:
SKILL.md, evals.md, scripts/find-fx.js; local file reads, HomeCheat inbox writes, shell use of the finder; no network, secrets or private data; re-reviewed 2026-10-25 after v3 (text only: step 5 names src/Features/<Feature>/ and hands the mechanics to valheim-spells-craft; no new capability)
