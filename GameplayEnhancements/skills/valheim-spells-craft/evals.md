# valheim-spells-craft evals

Scenarios 1-19 come from the failures of the 2026-10-22..24 Valheim sessions (requirements, HomeOS `references/claude-code/valheim-spells-craft-requirements.md`), 20-23 from the work plan, 24-25 from the build plan. Run the first three, 4, 13, 20-23 on paper after every change to the skill.

| # | Scenario | Prompt | Expected | Failure looks like |
|---|---|---|---|---|
| 1 | Exact duplicate | "сделай команду, которая лечит меня" | check + catalog find `heal`; answer syntax `op=heal`, status, page; offer a run; no code | a new heal command or a heal mode in another command |
| 2 | Similar, extend | "залп огненных стрел" | reads architecture doc (Extension points), catalog, `spellshot.md`, `src/Features/Spellshot/`; proposes a new `type=` in `Candidates`, defaults kept | a new `firevolley` command |
| 3 | Borderline | "кольцо морозных облаков вокруг меня" | shows `slime&cloud=<frost prefab>` vs a new `HarmEffect` in its own feature, consequence of each, recommends one, waits | picks silently |
| 4 | Docs missing or stale | an anchor in `baseline.md` is gone, or `knowledge-sha` is behind a change in `src/Core/` | start-of-run check exits 1; the skill stops and offers the refresh | extends from memory of the code |
| 5 | Widening a target | "пусть slime бьёт и игроков" | reads the catalog: the default already hits players (`hurtself=1`, Known defects); says so, proposes making it opt-in, asks before changing the default; tests on Claude2 without asking | flips a default silently, or asks permission to hit Claude2 |
| 6 | Target classes | "earthquake должен работать и на игроков" during design | card lists self / enemies / tamed / other players / bosses with defaults; one question | discovers in the test that players are skipped |
| 7 | Ambiguous follow-up | "cast 9 9" after arrow and thunderbolt casts | asks which | guesses lightning |
| 8 | Unclear word | "ад" | asks what was meant | builds a "Hell" variant |
| 9 | Wrong layout | "здуфыу сщтештгу" | decodes "please continue", says so, continues | treats it as a new request |
| 10 | Test helper | "убей мобов после теста" | `op=killmobs`; no kill mode in the command under test | adds `kill=` to the command |
| 11 | Cap | `spellshot&type=thunderbolt&perrow=9&rows=9` | answer names the 300-point cap and what it dropped; the cap is in `Usage` and the page | silent thinning |
| 12 | Background job | a timed test sequence is "stopped" | checks no child script or loop is left running, says so | effects keep firing |
| 13 | Iteration delivered | "ok, good" after a successful tuning test | done gate trigger *iteration*: page, index, catalog, check, `build.js`, commit + push submodule then pointer, shas; **no** refresh brief | stops at "works", or starts a refresh |
| 14 | Out of scope | "сделай чтобы slime работал на сервере друга" | explains host-only and scope, builds nothing | builds it |
| 15 | Visual only | "зелёные огни вокруг меня" | routes to `/valheim-spell-fx` / a `visualeffect` catalog entry | a new command |
| 16 | Commit hygiene | other sessions have uncommitted files in the submodule | commits only its own paths, names the others | sweeps them in |
| 17 | Install | "установи GameplayEnhancements" with the game running | follows the Install runbook in `packaging.md`: sees the game running, asks to close it, dry run, install, `check.ps1` exit 0 | installs against a running game or improvises |
| 18 | Push after satisfaction | "отлично, меня устраивает" after a tuning loop | as 13, file list of what went to the public repository | waits for an explicit "push", or pushes after a mid-test "ok" |
| 19 | Package copy | run from the unpacked package, no HomeOS | `commands-check.js` finds `<pkg>` and `docs/architecture/`; skips reindex, pointer and the refresh session with a note, still writes the refresh brief on "done" | fails on a HomeOS path |
| 20 | Plan mode | a list of 10+ commands | cards, verdicts, wave 0 list, sessions with briefs, questions in one message; writes no code, sends nothing to the game | starts coding the first item |
| 21 | Implement scope | an implement session given one card | edits only its `src/Features/<F>/`, page, index and catalog rows; asks before any `src/Core/` change | edits a Kit helper on its own |
| 22 | Done -> docs -> refresh | "spell X works, done" | done gate trigger *done*: docs listed with files, package, commit + push; then a refresh brief with `from` = `knowledge-sha`, `to` = new HEAD, and a proposed refresh session | stops at "works", or updates the skill before the docs |
| 23 | Refresh finds a new base class | refresh over a range that adds `abstract class ZoneEffect : CommandBase` | `homecheat-architecture.md` (Main classes, Inherit or compose, Extension points) gains it if missing, reported as a done-gate miss; an eval is added; `knowledge-sha` bumped; security review re-run | only the command list changes, or the class is described inside the skill |
| 24 | Refresh never started | next run after a "done" whose card was not started | start-of-run check reports the commits since `knowledge-sha`; with a core or architecture change it stops and offers the refresh | plans on stale evals without a word |
| 25 | Two dones in one wave | two implement sessions finish within the hour | the second extends the open brief's range; one refresh session | two refresh sessions edit the skill at once |
