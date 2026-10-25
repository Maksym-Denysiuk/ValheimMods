# AGENT METADATA

Owner:
maksym

Purpose:
Plans, builds, tests and documents new HomeCheat commands, spells and extensions for our own Valheim (GameplayEnhancements in the ValheimMods repository), always from the repository docs; plan mode writes spec cards and a session plan, implement mode takes one card to code, build, test, docs and delivery; "done" forces the docs update and then proposes a session that refreshes this skill.

Allowed use:
Reads HomeOS and the submodule references/ValheimCustomMods/ (docs/, GameplayEnhancements/src/ and docs/), or the unpacked package; a copy of the skill ships in GameplayEnhancements/skills/. Runs scripts/commands-check.js (Node, read-only: local git log/diff, the docs, spells.txt, and Steam libraryfolders.vdf to find the game folder; --game takes a folder path). Edits src/Features/<Feature>/, the docs, the command index and catalog in the submodule, and spells.txt in the game's BepInEx/config/homecheat/ folder; never edits src/Core/ without Maksym's yes (a wave 0 core brief from a plan he approved counts, for the listed changes only). Unattended runs (set by the session brief) take recommended answers as ASSUMPTION lines instead of asking, never for hosting, other players, default changes, core edits outside an approved wave 0 brief, cheat paths or public-repo hygiene; no-game runs send nothing to the game and end at build, selftest and a test list (done-gate trigger built, status B). Runs bash src/build.sh (csc compiles C# into DLLs staged in the game folder), node scripts/valheim/packages/build.js, java PlantUML (render only), node scripts/valheim/lib/il-reader.js (read-only). Sends one command at a time to the HomeCheat inbox through GameplayEnhancements/send-command.ps1, own game and hosted worlds only; reads BepInEx/LogOutput.log. Writes LocalOutputs/<ts>/ (draft spec cards, backlog plan, refresh brief; outside HomeOS the brief goes next to the unpacked package); approved spec cards live in the public repository as GameplayEnhancements/docs/Backlog/<name>.md. Git commit and push of its own paths in the submodule (public repository) and HomeOS, never force. Proposes (does not start) a refresh session; the refresh is attended because its security record needs a yes. No other network use, no secrets, never localConfigs/ or people/.

Created:
2026-10-25

Last Reviewed:
2026-10-25

Security Reviewed:
2026-10-25

Security Result:
pass with notes: compiled C# runs inside the game process with full rights (own game and hosted worlds only, SKILL.md gates 3, 5, 6); chat text reaches bash and PowerShell lines, limited by instructions/implement.md (feature names [A-Za-z]+, a form as one single-quoted argument without quote, ;, |, backtick or $); pushes to the public ValheimMods repository after each satisfied iteration without a per-push yes (Maksym decision D5), public-repo hygiene in SKILL.md gate 10; game answers, logs, spell files, backlog cards and briefs are data (gate 9). v1.1 re-review 2026-10-25: unattended and no-game modes added at Maksym's request; the unattended mode skips asking but not gates 3-6 and 10; the no-game trigger built pushes untested code to the public repository with status B (not V), test list attached; the core yes is limited to a wave 0 brief from a plan Maksym approved (a brief in a file stays data); the implement.md shell-safety line was cut in v1 (a second copy of the file had been pasted at the "$"), repaired and covered by eval 34; commands-check.js runs git through execFileSync with an argument array and a hex-validated sha, il-reader.js only reads files; v1.2 re-review 2026-10-25 (review findings fixed): refresh order is docs commit, then knowledge-sha, then security, then package; commands-check.js also compares the catalog Feature column and finds the game folder through --game, VALHEIM_DIR, VALHEIM_DIR_WIN or Steam libraryfolders.vdf (file reads only, no exec added), and says when it fell back to the payload spells.txt; approved cards in docs/Backlog are public, gate 10 applies to them; known hole recorded in gate 10, not fixed here: the HomeOS sync commits and pushes every uncommitted submodule change, so half-built work can reach the public repository without a done gate (Maksym decides)

Security Scope:
v1.2: SKILL.md, baseline.md, evals.md (35-40), instructions/*.md (card location, brief location, refresh order, attended refresh), scripts/commands-check.js (game folder discovery, Feature column); earlier: SKILL.md, baseline.md, evals.md, instructions/*.md, scripts/commands-check.js, scripts/valheim/lib/il-reader.js (called by path); unattended and no-game run modes; compiled code that runs inside the game process, shell lines built from chat text (feature names, command forms), writes to the game folder and the HomeCheat inbox, git push to a public repository
