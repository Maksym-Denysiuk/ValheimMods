# Done gate

Three triggers:

| Trigger | Signal | Runs |
|---|---|---|
| **iteration** | The owner is satisfied with an iteration: "ok, works", "good", "отлично", "работает", "меня устраивает", "давай зафиксируем", "комить". A mid-test "ok" (meaning "next test") is not one; unsure: ask "фиксируем?" | steps 1-3 |
| **done** | The owner says the command, spell or extension is created or done ("готово", "done", "it's finished") | steps 1-4 |
| **built** | a no-game session finished its cards (build and selftest pass, `implement.md` 5b) | steps 1-3 with status B; step 4 only writes or extends the refresh brief (no session proposed: the plan schedules refresh sessions) |

"Done" is not accepted, and not said back, until step 3 is complete. Anything that cannot be done blocks and is named with the reason.

## 1. Docs (in this order, each listed in the answer with its file)

1. **Command page** in `<pkg>/docs/<Topic>/` (an existing topic folder; a new one only after the owner picks it), with the item checklist of the ValheimMods README: game version, goal, result before and after, files and settings, what failed first, method, change procedure, verification, undo. For a card from `docs/Backlog/`, the card becomes this page and the backlog file is removed in the same commit; a source comment that names the card (`grep -rn "docs/Backlog/<name>" src/`) is repointed to the page in that commit (refresh 10 found four dangling ones).
2. **Command index** row in `<pkg>/docs/HomeCheat-plugin/README.md`: syntax, status (V only with a recorded answer or the owner's confirmation, else B and the line "run X once and record the answer"), page link. Every `|` inside a table cell is written `\|`, also inside code (`group=new\|<n>`): an unescaped one splits the row (docs audit 2026-10-25: the dome, construct and harmeffect rows did not render). A new page also gets its row in the folder's Contents block (`<!-- folder:start -->`, the page's `description:` line), and a changed `description:` is copied there: nothing regenerates those blocks inside the submodule (despawn had no row).
3. **Catalog** row in `<docs>/homecheat-commands.md`: feature, class, fingerprint, side effects, caps, test aids, spell-bar entries (the ids as they stand in the game's `spells.txt`, read, not remembered); a new worked example when the verdict was not obvious.
4. **Architecture doc** `<docs>/homecheat-architecture.md` when the change adds or changes a class others can derive from or call, a Kit helper, an extension point, a convention, a guard, a patch pattern or a `refs.txt` need; also Known defects when one was found or fixed; a `File.cs:<line>` reference anywhere in the doc that points into a file you changed is re-checked against the new line numbers. Class diagrams: a new namespace-level type (a patch class included) goes into the DLL overview block and its box in `homecheat-features.puml`; edit `<docs>/diagrams/*.puml` and render (`java -jar <plantuml jar> -tsvg docs/diagrams/*.puml` from the repository root; find the jar with `ls /d/plantuml*.jar`, ask when there is none; the package copy has only the rendered `.svg`: name the diagram change for the owner). Unsure whether it changed: ask.
5. **Source in place**: `git status` in the repository (the package copy without a clone: the list of files you changed) shows only `src/Features/<F>/` and docs paths of this task; no stray copies of them anywhere else.
6. **Spell**: the `spells.txt` line in the game folder `BepInEx/config/homecheat/spells.txt`, read fresh just before the edit. A planned "(soon)" line of this command: replace its `@dummy` line with the `# real form (<session>): ...` comment above it, same id (the approved plan is the ask); list the ids in the answer or handoff. Find the lines with `grep -n "real form (<your session>)"` over the whole file, not only the ids your brief names: a brief's id list can miss a second copy of the same entry in another section (Refresh 2 found coord's 6.6.1-6.6.2 left `@dummy`). The one allowed change to the comment's form: add a safety parameter the built command requires for that step (`&confirm=1` on a destructive step whose comment lacks it, as W2-dome did on 3.5.5 and 6.6.5); write it as an ASSUMPTION in the handoff. Anything else that differs from the comment (another target, a larger radius) is a new spell, not a swap. **One owner of `spells.txt` at a time**: while another session owns it (a test pass, the spell-bar owner), send it your line or swap and let it edit; never edit the file beside it. When the owning session is gone (ended, archived, or not answering), ask the owner first: the owner's yes makes you the owner for that edit. A new spell outside the plan: an `@dummy` "(soon)" line straight away, the real command only when the owner asks. `spells-ui-test.txt` stays `@dummy`; `SpellFile.DefaultText` is left to the one pass after the last wave. Then `op=spells&reload=1` (0 bad lines; a no-game run says it was not run), and the row in `<pkg>/docs/Spells/README.md` for a spell with its own page.
7. **Preset only** (no code): items 2, 3, 6.
8. `node scripts/commands-check.js` exit 0.
9. **Generated package files** (`GameplayEnhancements/README.md`, `install.ps1`, `uninstall.ps1`, `check.ps1`, `send-command.ps1`, `skills/`, `docs/architecture/`, `MANIFEST.md`) are never edited in place: the next package build overwrites them.
   The package copy: name the needed fix in the answer for the owner of the package.

## 2. Package

The package copy: there is no package build here; skip this step and say so.

## 3. Commit and push

The package copy without a ValheimMods clone: nothing to commit; list the changed files in the answer for the owner, and skip to step 4. In a clone of someone else's repository (no push rights): commit on a branch, push it to your fork and open a pull request instead of pushing `main`.
1. The ValheimMods repository: on `main` (a fresh checkout is detached: `git checkout main`), `git status`; add this task's paths by name (or `git add -A` only when status shows nothing from other work), commit with `git commit --only <your paths>` (the index is shared: another session's plain `git commit` takes whatever you staged: W1-ship's `0ac250e` swept in a staged change of W3-mines; a private `GIT_INDEX_FILE` works too) (one-line subject, short body), `git pull --rebase`, `git push origin main`. When `git pull --rebase` refuses because other sessions have unstaged changes: never stash them; `git fetch`, and if `git rev-list --left-right --count HEAD...origin/main` shows the remote has not moved, push; if it has moved, `git pull --no-rebase --no-edit` works when none of the incoming files is dirty here; otherwise stop and report.
2. Report every sha (the repository's, and the pointer's where there is one), the push state and the list of files that went to the public repository.
A rebase conflict or a failed push: stop, commit nothing further (no pointer), report. Never force-push.

## 4. Refresh brief (trigger *done* only)

1. Read `baseline.md` for `knowledge-sha`. Look for an open brief (`status: open`):
   the package copy or a clone: `skill-refresh-brief.md` in the folder that holds the unpacked `GameplayEnhancements` or the clone.
   - Found: extend its range to the new repository HEAD and add this command to its list.
   - None (the package copy or a clone): write `skill-refresh-brief.md` beside `<pkg>` or the clone, never inside it (a reinstall replaces `<pkg>`; a clone would publish it):
     ```markdown
     # valheim-spells-craft refresh brief
     status: open
     from: <knowledge-sha>   to: <repository HEAD, or "package <MANIFEST date>" without git>
     ## What was added or changed
     - <command>: <new | extended | preset>, feature <F>, docs <pages>; architecture doc changed: <sections or no>
     ## For the refresh
     - new classes, helpers, extension points, conventions: <list or none>
     - evals likely affected: <numbers or new scenario>
     Run: /valheim-spells-craft refresh with this brief (instructions/refresh.md).
     ```
2. (the package copy) Tell the owner the brief's path; the refresh of this skill is done where the skill is maintained, not in the package.
3. The start-of-run check of the next run reports a skipped refresh, so nothing is lost if the card is not started.
