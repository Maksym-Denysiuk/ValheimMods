# Plan mode

For a list of commands (a backlog, a menu group, "plan these"). Writes cards and a session plan. **Writes no code, builds nothing, sends nothing to the game.**

## Steps

1. **Start-of-run check** (SKILL.md). Read `homecheat-architecture.md` (Extension points, Safety guards, Known defects) and `homecheat-commands.md` whole.
2. **Intake per item.** Decode wrong-layout text, restate the item in one line, fingerprint it on the four axes. Merge duplicates in the list and say so.
3. **Verdict per item** by the catalog's exists / extend / new rules: exists, preset (a `spells.txt` line only), extend X with P, new, blocked (needs a decision or a missing core piece). For every extend or new, open the nearest command's page and its `src/Features/<F>/` before writing the verdict.
4. **Card per item** (`spec-card.md`). Presets get a short card: trigger, command form, spell-bar slot.
5. **Shared core needs.** Collect what two or more items need from the core (a coordinate store, a collision check, a dialog, radial menu changes, the planned guards). These become **wave 0**: one core owner, one rebuild of `core`, one game restart. Name every item that waits on them.
   - Wave 0 has two kinds. **0-core**: changes under `src/Core/`, one owner, one core rebuild, one restart. **0-shared**: work in one feature folder that later items depend on (for example the spell bar's file format, sub-sections or dialogs in `src/Features/Spells/`). 0-shared is not a core change: it may run beside 0-core, and later items wait for it. Radial menu changes are 0-shared, not core.
   - Exception to "two features before the core": Unity-free maths (grids, routes, geometry) may go to `src/Core/Pure/` in wave 0 so `bash src/build.sh selftest` covers it while the game is not available. Say so on the card.
6. **Questions.** All blocking questions in one message, grouped by item, recommended answer first. Items not blocked are planned with the stated decisions. Unattended (SKILL.md gate 2): write the questions with their recommended answers into an Assumptions table instead of sending them, and plan on the recommended answer.
7. **Waves and sessions.**
   - Items in the same feature folder never run in two sessions at the same time. Either one session takes them all, or several sessions (one per command is fine) run **in sequence**, each starting after the previous one pushed.
   - Shared files have one owner at a time: the game folder's `spells.txt` and `spells-ui-test.txt` and `SpellFile.DefaultText`. Write the whole planned menu once, in the session that owns the spell bar. A not-yet-built command gets an `@dummy` line in `spells.txt` (label ending in "(soon)"; `commands-check.js` flags any `op=` that names no command) and its real form in a comment above it. The session that builds the command swaps in the real form. Later sessions change only their own lines, with a fresh read of the file just before the edit, and list the lines in their handoff. Index and catalog: own rows only, `git pull --rebase` before the commit. `payload/`: commit only your own feature's DLL; `build.js` also refreshes other features' builds, so leave those unstaged.
   - After a wave that changed `src/Core/` or `homecheat-architecture.md`, a refresh session (`refresh.md`) runs alone before the next wave starts: the start-of-run check of every later session stops until `knowledge-sha` is bumped. Put it in the session table as **attended** (its security record needs Maksym's yes, `refresh.md` step 9): an unattended plan ends its unattended part at that point.
   - The HomeOS sync pushes every uncommitted submodule change (SKILL.md gate 10, known hole): briefs tell sessions to keep the submodule tree publishable at all times and to commit their own paths as soon as an iteration builds.
   - At most 2-3 implementation sessions at once: all tests share one game and Maksym's eyes.
   - Order: wave 0 alone; then presets and small extensions; then new commands; then items that need a design decision.
   - One brief per session (template below).
8. **Write** `LocalOutputs/<ts>/backlog-plan.md` (HomeOS; elsewhere next to the package): summary table, cards, wave 0 list, wave and session table, briefs, questions (or the Assumptions table), and a test list for Maksym that starts with every card's probes. Show the summary and the questions.
9. **After Maksym approves**: store each approved card as `<pkg>/docs/Backlog/<name>.md` (one file per card, `description:` frontmatter), commit and push them with the plan's own paths only. Never start implementation sessions without his go.

## Summary table

| # | Item | Verdict | Nearest | Feature folder | Shared files | Needs wave 0 | Questions / assumptions | Session |
|---|---|---|---|---|---|---|---|---|

## Session brief template

```markdown
Implement <card names> with /valheim-spells-craft implement mode.
Cards: <pkg>/docs/Backlog/<name>.md (read first; update the card's test log as you go).
Files you may edit: src/Features/<F>/, docs/<topic>/<page>.md, the index and catalog rows of these commands, your own spells.txt lines <ids> (fresh read before the edit).
Run mode: <attended | unattended> and <game | no game> (unattended: ASSUMPTION lines, SKILL.md gate 2; no game: implement.md 5b, done-gate trigger built).
Rules: no edits under src/Core/ (stop and ask); unique inbox names (<session-prefix>-<n>); one command at a time in the game;
no game restart; commit only your own paths; deliver after each iteration Maksym is satisfied with; "done" runs the done gate.
Shared game: other sessions may be testing; ask Maksym before spawning or clearing creatures near another test.
Done when: every card is V in the index, documented, pushed, and the refresh brief is extended.
(No-game run: every card built, selftest passes, docs with status B and the test list, pushed: done-gate trigger built.)
```
