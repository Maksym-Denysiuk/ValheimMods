# Spec card

One card per command or change. Implement mode keeps it as `LocalOutputs/<ts>/<name>-spec.md` (HomeOS) and updates it after every step, so a new session can continue from it. Plan mode puts all cards in `backlog-plan.md`; approved cards are stored as `<pkg>/docs/Backlog/<name>.md` and become the command page when the command is done.

## Template

```markdown
# <name>: <one-line goal>

Status: draft | approved | building | testing | done (<date>)
Verdict: exists | preset | extend <command> with <parameter> | new | blocked (<why>)
Nearest: <command(s)> (catalog row, page, source read: yes/no)
Fingerprint: effect <...> / target <...> / placement <...> / mechanism <...>

## What it does
- Trigger: op=<name>[&...]; spell bar <group.entry> "<label>" [pin=] [sticky] [shift=]
- Targets: self <on/off>, enemies <on/off>, tamed <on/off>, other players <on/off, opt-in parameter>, bosses <on/off>
- Placement: self | crosshair | radius <m> | ring | grid | line | world
- Magnitude: damage / push / heal / count / size / duration, with defaults
- Persists in the world: <objects, terrain, ZDO values> or nothing
- Who sees it: everyone (networked on the host) | only this PC
- Visual reference: <prefab> | to choose with /valheim-spell-fx
- Defaults: <parameter=value ...> (works with only <essential parameter>)
- Caps: <limit, why, how reported>
- Undo: <parameter or way back>

## How it is built
- Feature folder: src/Features/<F>/ (existing | new); class <Name>Command : CommandBase | <Name>Effect : HarmEffect
- Sub-actions: <key=...> (read-only helpers)
- OnLoad needs: patches <...>, owned GameObjects <...>, timers <...>; refs.txt <dll>
- Kit reused: <helpers>; game objects reused: <Aoe, Projectile, ...>
- Core change: none | <what> (-> core owner, restart)
- Guards carried until the core pass: IsServer before other players, Players.Local, caps, ZDO mark for persistent objects, confirm=1 for destructive parts
- Docs: page <topic>/<file>.md, index row, catalog row, architecture doc <section> if a pattern changes

## Open questions
1. <question> - recommended: <answer> (what happens if chosen)

## Test plan
1. <command> -> expected <answer / measurement / what Maksym should see>
(cheatstate before and after; Claude2 for other-player tests)

## Test log
| Time | Command | Answer (first line) | Observation | Verdict |
|---|---|---|---|---|
```

## Ask or decide

| Ask Maksym (one message, recommended answer first) | Decide and state |
|---|---|
| target classes and whether other players are affected | parameter names and placement, by the parameter conventions |
| damage on/off and rough level; push strength when no game value fits | default counts and sizes inside caps |
| which visual or game reference is meant ("like Eikthyr", "the standard bar", "lightning or arrows") | the prefab once the reference is pinned (with `/valheim-spell-fx` for looks) |
| extend vs new when the catalog rules say borderline | extend vs new when the rule is clear |
| any change of an existing default | a new opt-in parameter that keeps old behaviour |
| anything that persists in the world or is destructive | timers, cleanup, caps, guards |
| a core change and the restart it needs | Harmony vs component vs reflection, which Kit helper |
| spell-bar slot, pin, sticky | the `Usage` text, answer format |
| testing in a world other than ClaudeTest; casting at a player who is not Claude2 | test order, spawns, probes |
| a new docs topic folder; "фиксируем?" when satisfaction is unclear | page placement in an existing folder, index and catalog rows |
| wrong-layout text that does not decode to a clear phrase | wrong-layout text that decodes cleanly |

When to build without asking (the practice of the existing commands): a new command or feature is built after Maksym's go on the card; tweaks inside a test loop ("multiplier 7", "half closer", "rotate 90°") are built and tested straight away; a default change or anything aimed at another player is asked even inside a loop.
