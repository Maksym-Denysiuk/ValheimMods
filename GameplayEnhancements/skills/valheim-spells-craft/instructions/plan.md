# Plan mode

For a list of commands (a backlog, a menu group, "plan these"). Writes cards and a session plan. **Writes no code, builds nothing, sends nothing to the game.**

## Steps

1. **Start-of-run check** (SKILL.md). Read `homecheat-architecture.md` (Extension points, Safety guards, Known defects) and `homecheat-commands.md` whole.
2. **Intake per item.** Decode wrong-layout text, restate the item in one line, fingerprint it on the four axes. Merge duplicates in the list and say so.
3. **Verdict per item** by the catalog's exists / extend / new rules: exists, preset (a `spells.txt` line only), extend X with P, new, blocked (needs a decision or a missing core piece). For every extend or new, open the nearest command's page and its `src/Features/<F>/` before writing the verdict.
4. **Card per item** (`spec-card.md`). Presets get a short card: trigger, command form, spell-bar slot.
5. **Shared core needs.** Collect what two or more items need from the core (a coordinate store, a collision check, a dialog, radial menu changes, the planned guards). These become **wave 0**: one core owner, one rebuild of `core`, one game restart. Name every item that waits on them.
6. **Questions.** All blocking questions in one message, grouped by item, recommended answer first. Items not blocked are planned with the stated decisions.
7. **Waves and sessions.**
   - Items in the same feature folder share a session (they edit the same files).
   - At most 2-3 implementation sessions at once: all tests share one game and Maksym's eyes.
   - Order: wave 0 alone; then presets and small extensions; then new commands; then items that need a design decision.
   - One brief per session (template below).
8. **Write** `LocalOutputs/<ts>/backlog-plan.md` (HomeOS; elsewhere next to the package): summary table, cards, wave 0 list, wave and session table, briefs, questions. Show the summary and the questions.
9. **After Maksym approves**: store each approved card as `<pkg>/docs/Backlog/<name>.md` (one file per card, `description:` frontmatter), commit and push them with the plan's own paths only. Never start implementation sessions without his go.

## Summary table

| # | Item | Verdict | Nearest | Feature folder | Needs wave 0 | Questions | Session |
|---|---|---|---|---|---|---|---|

## Session brief template

```markdown
Implement <card names> with /valheim-spells-craft implement mode.
Cards: <pkg>/docs/Backlog/<name>.md (read first; update the card's test log as you go).
Files you may edit: src/Features/<F>/, docs/<topic>/<page>.md, the index and catalog rows of these commands, spells.txt lines <group.entry>.
Rules: no edits under src/Core/ (stop and ask); unique inbox names (<session-prefix>-<n>); one command at a time in the game;
no game restart; commit only your own paths; deliver after each iteration Maksym is satisfied with; "done" runs the done gate.
Shared game: other sessions may be testing; ask Maksym before spawning or clearing creatures near another test.
Done when: every card is V in the index, documented, pushed, and the refresh brief is extended.
```
