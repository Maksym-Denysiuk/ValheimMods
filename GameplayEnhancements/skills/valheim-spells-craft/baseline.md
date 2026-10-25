# Baseline

knowledge-sha: `e187d24`
refreshed: 2026-10-25

`knowledge-sha` is the ValheimMods commit whose docs this skill was last checked against. `scripts/commands-check.js --baseline baseline.md` reports how far it is behind and stops the run when `src/Core/` or `homecheat-architecture.md` changed since (refresh first: `instructions/refresh.md`). Only the refresh session changes this file.

## Doc sections the skill relies on

Every row must exist as a heading in `<docs>`; the check fails when one is renamed or removed.

| Doc | Anchor | Used by |
|---|---|---|
| `homecheat-architecture.md` | `#two-kinds-of-dll` | implement: core vs feature |
| `homecheat-architecture.md` | `#main-classes` | refresh: docs coverage |
| `homecheat-architecture.md` | `#inherit-or-compose` | refresh: new base class, implement |
| `homecheat-architecture.md` | `#lifecycle-and-hot-reload` | implement: OnLoad, reload |
| `homecheat-architecture.md` | `#conventions` | implement: C# 5 rules |
| `homecheat-architecture.md` | `#parameter-conventions` | implement, spec card |
| `homecheat-architecture.md` | `#target-model` | spec card, implement |
| `homecheat-architecture.md` | `#kit-by-need` | implement |
| `homecheat-architecture.md` | `#add-a-command` | implement |
| `homecheat-architecture.md` | `#build` | implement |
| `homecheat-architecture.md` | `#extension-points` | plan, implement |
| `homecheat-architecture.md` | `#safety-guards` | plan (wave 0), implement |
| `homecheat-architecture.md` | `#test-aids` | implement: test |
| `homecheat-architecture.md` | `#known-defects-2026-10-25` | plan, done gate |
| `homecheat-commands.md` | `#catalog` | exists check, done gate |
| `homecheat-commands.md` | `#exists-extend-or-new` | plan, implement |
| `homecheat-commands.md` | `#worked-examples` | plan |
| `packaging.md` | `#install-runbook` | install |
| `packaging.md` | `#what-is-source-and-what-is-generated` | done gate |
