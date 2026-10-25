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
6. **Baseline.** Set `knowledge-sha` to the submodule commit that holds the updated docs, `refreshed:` to today; keep the anchor table to headings that exist. `node scripts/commands-check.js --baseline baseline.md` exit 0.
7. **Security.** `/skill-security-review valheim-spells-craft` (required after any change to the skill); its record must say pass.
8. **Deliver.** `node scripts/valheim/packages/build.js` (refreshes the package copy of the skill and the docs copy); submodule commit + push (docs fixes and package copy), then HomeOS commit + push (skill, pointer). Same order and rules as `done-gate.md` step 3.
9. **Close the brief**: `status: consumed <submodule sha> <date>`. Report: range, docs gaps closed, evals added or changed, new `knowledge-sha`, security result, shas.
