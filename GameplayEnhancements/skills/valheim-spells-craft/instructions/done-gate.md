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
6. **Spell**: the `spells.txt` line in the game folder `BepInEx/config/homecheat/spells.txt`, read fresh just before the edit. A planned "(soon)" line of this command: replace its `@dummy` line with the `# real form (<session>): ...` comment above it, same id (the approved plan is the ask); list the ids in the answer or handoff. Find the lines with `grep -n "real form (<your session>)"` over the whole file, not only the ids your brief names: a brief's id list can miss a second copy of the same entry in another section (Refresh 2 found coord's 6.6.1-6.6.2 left `@dummy`). The one allowed change to the comment's form: add a safety parameter the built command requires for that step (`&confirm=1` on a destructive step whose comment lacks it, as W2-dome did on 3.5.5 and 6.6.5); write it as an ASSUMPTION in the handoff. Anything else that differs from the comment (another target, a larger radius) is a new spell, not a swap. **One owner of `spells.txt` at a time**: while another session owns it (a test pass, the spell-bar owner), send it your line or swap and let it edit; never edit the file beside it. When the owner session is gone (ended, archived, or not answering), ask Maksym first: his yes makes you the owner for that edit. A new spell outside the plan: an `@dummy` "(soon)" line straight away, the real command only when Maksym asks. `spells-ui-test.txt` stays `@dummy`; `SpellFile.DefaultText` is left to the one pass after the last wave. Then `op=spells&reload=1` (0 bad lines; a no-game run says it was not run), and the row in `<pkg>/docs/Spells/README.md` for a spell with its own page.
7. **Preset only** (no code): items 2, 3, 6.
8. `node scripts/commands-check.js` exit 0.
9. **Package files that `build.js` generates** (`GameplayEnhancements/README.md`, `install.ps1`, `uninstall.ps1`, `check.ps1`, `send-command.ps1`): edit their sources in HomeOS `scripts/valheim/packages/GameplayEnhancements/`, never the submodule copies, which the next `build.js` overwrites.

## 2. Package

`node scripts/valheim/packages/build.js` (HomeOS): refreshes `payload/` (newest build of every feature, `spells.txt`), the skill copies, `docs/architecture/` and `MANIFEST.md`. A feature folder without a build stops it: build your own first; when the folder is another session's, wait for its build (never build or commit another session's feature) and say so. Outside HomeOS: skip with a note. `build.js` also refreshes other features' builds in `payload/`: commit only your own feature's DLL and leave the rest unstaged. While another session works in the submodule, `MANIFEST.md` lists its uncommitted DLLs too: leave both `MANIFEST.md` files unstaged and name them in the handoff; the last committer (the refresh after the wave) commits them with a fresh `build.js`.

## 3. Commit and push

1. Submodule (`references/ValheimCustomMods`): on `main` (a fresh checkout is detached: `git checkout main`), `git status`; add this task's paths by name (or `git add -A` only when status shows nothing from other work), commit with `git commit --only <your paths>` (the index is shared: another session's plain `git commit` takes whatever you staged: W1-ship's `0ac250e` swept in a staged change of W3-mines; a private `GIT_INDEX_FILE` works too) (one-line subject, short body), `git pull --rebase`, `git push origin main`. When `git pull --rebase` refuses because other sessions have unstaged changes: never stash them; `git fetch`, and if `git rev-list --left-right --count HEAD...origin/main` shows the remote has not moved, push; if it has moved, `git pull --no-rebase --no-edit` works when none of the incoming files is dirty here; otherwise stop and report.
2. HomeOS: `git add references/ValheimCustomMods` (+ any HomeOS files of this task by name), commit, push.
3. Report both shas, the push state and the list of files that went to the public repository.
A rebase conflict or a failed push: stop, do not commit the pointer, report. Never force-push.

## 4. Refresh brief (trigger *done* only)

1. Read `baseline.md` for `knowledge-sha`. Look for an open brief (`status: open`): HomeOS `LocalOutputs/*/skill-refresh-brief.md`; outside HomeOS `skill-refresh-brief.md` in the folder that holds the unpacked `GameplayEnhancements`.
   - Found: extend its range to the new submodule HEAD and add this command to its list.
   - None: write `LocalOutputs/<ts>/skill-refresh-brief.md` (outside HomeOS: `<pkg>/../skill-refresh-brief.md`, never inside `<pkg>`, which a reinstall replaces):
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
2. Propose the refresh session: in the Claude Desktop app, `start_session` (initiation `own_initiative`, context `fresh`, title "Refresh valheim-spells-craft", the brief's path and content as the prompt): a card Maksym starts with one click. When a brief was only extended, say the existing card or brief covers it. Elsewhere: the refresh runs only in HomeOS; tell Maksym the brief's path and to copy it to HomeOS `LocalOutputs/<ts>/` and start a session there with "refresh valheim-spells-craft from <brief path>". The refresh is attended: its security record needs Maksym's yes (`refresh.md` step 9).
3. The start-of-run check of the next run reports a skipped refresh, so nothing is lost if the card is not started.
