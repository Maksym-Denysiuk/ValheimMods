# Refresh (the skill updates itself)

Runs in its own session, started from a refresh brief (`done-gate.md` step 4), or when the start-of-run check says "refresh the skill first". HomeOS only: the package copy is generated and is never edited.

Goal: after commands were added, the docs this skill relies on cover them, the skill's evals cover the new patterns, and `baseline.md` points at the new submodule commit. The architecture knowledge stays in the docs; the refresh puts nothing about classes or commands into the skill's own files.

## Steps

1. Read the brief (or, without one, use `knowledge-sha` from `baseline.md` as `from` and the submodule HEAD as `to`). From this skill's folder run `node scripts/commands-check.js --baseline baseline.md` and note its differences.
2. `git -C references/ValheimCustomMods log --stat <from>..<to>` and `git diff <from>..<to> -- GameplayEnhancements/src docs GameplayEnhancements/docs`.
3. **Docs coverage.** For every new or changed public type, base class, Kit helper, extension point, convention, guard, patch pattern and command in the diff, check that the docs describe it:
   - `homecheat-architecture.md`: Main classes, Inherit or compose (a new base class), Kit by need, Extension points, Safety guards, Conventions, Known defects; diagrams when classes changed.
   - `homecheat-commands.md`: a catalog row per command, a worked example for each non-obvious verdict.
   - command index and the command page.
   A gap is closed in the docs now and listed in the report as a **done-gate miss** (what was missing, which done gate should have caught it).
4. **Evals.** Add a scenario to `evals.md` for each new pattern (a new base class, a new extension point, a new kind of target or a failure seen in the session); adjust scenarios whose expected answer changed (for example a command that now exists).
5. **Flow files.** Change `SKILL.md` or `instructions/*.md` only when the way of working changed (a new doc to read, a new gate, a new step). A changed heading in the docs: update the anchor in `baseline.md` and every instruction that names it.
6. **Evals on paper.** Run the scenarios the header of `evals.md` names for every change, plus each scenario added or changed in step 4. A failing scenario is fixed (flow file or docs) before going on.
7. **Commit the docs.** Submodule commit + push of the docs fixes from step 3 (`done-gate.md` step 3.1: own paths, `git pull --rebase`, no force-push). Nothing to fix: the submodule HEAD is the commit.
8. **Baseline.** Set `knowledge-sha` to the short sha of the step 7 commit (it must exist on the remote), `refreshed:` to today; keep the anchor table to headings that exist. `node scripts/commands-check.js --baseline baseline.md` exit 0. Order matters: a sha set before the docs commit points at docs without the fixes.
9. **Security.** `/skill-security-review valheim-spells-craft` (required after any change to the skill); its record must say pass. The review writes the record only after Maksym says yes, so a refresh is **attended**: a refresh started without him stops here with the brief at `status: awaiting review` and the proposed record in it, and nothing below runs until he answers.
10. **Deliver.** `node scripts/valheim/packages/build.js` (refreshes the package copy of the skill and `docs/architecture/`); submodule commit + push of the package copy (commits under `GameplayEnhancements/skills/` and `docs/architecture/` do not move the check, it ignores them); then HomeOS commit + push (skill, pointer). Same rules as `done-gate.md` step 3.
11. **Close the brief**: `status: consumed <submodule sha> <date>`. Report: range, docs gaps closed, evals added or changed and their paper results, new `knowledge-sha`, security result, shas.
