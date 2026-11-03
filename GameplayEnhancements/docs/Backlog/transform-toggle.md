---
description: backlog card transform-toggle (approved 2026-11-03) - a second cast of the same form turns you back to human
---
# transform-toggle: a second cast of the same form turns you back to human

Status: built 2026-11-03 (B, not run in game; `HomeCheat.Cmd.transform.v20261103195037`, selftest 96/0) · Verdict: extend `transform` with `toggle=1` · Nearest: transform (`TransformCommand.cs:60-111` read)
Fingerprint: form/movement / self / on self / `TransformState`

- Trigger: `op=transform&type=raven&toggle=1`; spells 4.1.1-4.1.3 get `&toggle=1`; 4.1.4 Human stays.
- With `toggle=1`, a `TransformState` of the same form on you runs the `type=human` path; another form switches as today. `target=<nick>` ignores it.
- Defaults: `toggle=0` (typed commands unchanged). Probes, Measure: none.
- Docs: `Transform/` page, index, catalog.
- Test: 4.1.1 twice → raven, then `human: restored`; 4.1.1 then 4.1.2 → serpent; Claude2 sees the stand-in go.

## Test log

| Time | Command | Answer (first line) | Observation | Verdict |
|---|---|---|---|---|

Test list (run when the game is up): [transform.md, toggle=1](../Transform/transform.md#toggle1-2026-11-03).
