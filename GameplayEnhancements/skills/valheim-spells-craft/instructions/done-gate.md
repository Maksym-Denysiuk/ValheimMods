# Done gate

Two triggers:

| Trigger | Signal | Runs |
|---|---|---|
| **iteration** | Maksym is satisfied with an iteration: "ok, works", "good", "отлично", "работает", "меня устраивает", "давай зафиксируем", "комить". A mid-test "ok" (meaning "next test") is not one; unsure: ask "фиксируем?" | steps 1-3 |
| **done** | Maksym says the command, spell or extension is created or done ("готово", "done", "it's finished") | steps 1-4 |
| **built** | a no-game session finished its cards (build and selftest pass, `implement.md` 5b) | steps 1-3 with status B; step 4 only writes or extends the refresh brief (no session proposed: the plan schedules refresh sessions) |

"Done" is not accepted, and not said back, until step 3 is complete. Anything that cannot be done blocks and is named with the reason.

## 1. Docs (in this order, each listed in the answer with its file)

1. **Command page** in `<pkg>/docs/<Topic>/` (an existing topic folder; a new one only after Maksym picks it), with the item checklist of the repository README: game version, goal, result before and after, files and settings, what failed first, method, change procedure, verification, undo. For a card from `docs/Backlog/`, the card becomes this page and the backlog file is removed in the same commit.
2. **Command index** row in `<pkg>/docs/HomeCheat-plugin/README.md`: syntax, status (V only with a recorded answer or Maksym's confirmation, else B and the line "run X once and record the answer"), page link.
3. **Catalog** row in `<docs>/homecheat-commands.md`: feature, class, fingerprint, side effects, caps, test aids, spell-bar entries; a new worked example when the verdict was not obvious.
4. **Architecture doc** `<docs>/homecheat-architecture.md` when the change adds or changes a class others can derive from or call, a Kit helper, an extension point, a convention, a guard, a patch pattern or a `refs.txt` need; also Known defects when one was found or fixed. Class diagrams: edit `<docs>/diagrams/*.puml` and render (`java -jar <plantuml jar> -tsvg docs/diagrams/*.puml` from the repository root; find the jar with `ls /d/plantuml*.jar`, ask when there is none). Unsure whether it changed: ask.
5. **Source in place**: `git status` in the submodule shows only `src/Features/<F>/` and docs paths of this task; no stray copies in `LocalOutputs/`.
6. **Spell**: the `spells.txt` line in the game folder `BepInEx/config/homecheat/spells.txt` (placeholder line straight away for a new spell, the real command only when Maksym asks), `op=spells&reload=1`, and the row in `<pkg>/docs/Spells/README.md`.
7. **Preset only** (no code): items 2, 3, 6.
8. `node scripts/commands-check.js` exit 0.

## 2. Package

`node scripts/valheim/packages/build.js` (HomeOS): refreshes `payload/` (newest build of every feature, `spells.txt`), the skill copies, `docs/architecture/` and `MANIFEST.md`. A feature folder without a build stops it: build first. Outside HomeOS: skip with a note. `build.js` also refreshes other features' builds in `payload/`: commit only your own feature's DLL and leave the rest unstaged.

## 3. Commit and push

1. Submodule (`references/ValheimCustomMods`): on `main` (a fresh checkout is detached: `git checkout main`), `git status`; add this task's paths by name (or `git add -A` only when status shows nothing from other work), commit (one-line subject, short body), `git pull --rebase`, `git push origin main`.
2. HomeOS: `git add references/ValheimCustomMods` (+ any HomeOS files of this task by name), commit, push.
3. Report both shas, the push state and the list of files that went to the public repository.
A rebase conflict or a failed push: stop, do not commit the pointer, report. Never force-push.

## 4. Refresh brief (trigger *done* only)

1. Read `baseline.md` for `knowledge-sha`. Look for an open brief: `LocalOutputs/*/skill-refresh-brief.md` with `status: open`.
   - Found: extend its range to the new submodule HEAD and add this command to its list.
   - None: write `LocalOutputs/<ts>/skill-refresh-brief.md`:
     ```markdown
     # valheim-spells-craft refresh brief
     status: open
     from: <knowledge-sha>   to: <submodule HEAD>
     ## What was added or changed
     - <command>: <new | extended | preset>, feature <F>, docs <pages>; architecture doc changed: <sections or no>
     ## For the refresh
     - new classes, helpers, extension points, conventions: <list or none>
     - evals likely affected: <numbers or new scenario>
     Run: /valheim-spells-craft refresh with this brief (instructions/refresh.md).
     ```
2. Propose the refresh session: in the Claude Desktop app, `start_session` (initiation `own_initiative`, context `fresh`, title "Refresh valheim-spells-craft", the brief's path and content as the prompt): a card Maksym starts with one click. When a brief was only extended, say the existing card or brief covers it. Elsewhere: tell Maksym to start a session with "refresh valheim-spells-craft from <brief path>".
3. The start-of-run check of the next run reports a skipped refresh, so nothing is lost if the card is not started.
