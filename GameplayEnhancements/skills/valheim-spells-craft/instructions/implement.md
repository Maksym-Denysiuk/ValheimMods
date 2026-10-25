# Implement mode

One card: code, build, test, docs. Each phase 4 session runs this for its own cards.

## 1. Card and questions

1. Start-of-run check (SKILL.md). Read the docs listed in SKILL.md, the nearest command's page and its `src/Features/<F>/`.
2. Use the given card, or write one (`spec-card.md`) to `LocalOutputs/<ts>/<name>-spec.md`.
3. Blocking questions in one message (Ask column). A new command or feature waits for Maksym's go on the card; tweaks inside a test loop do not; default changes and anything aimed at another player are always asked.
   Unattended: a card from an approved or unattended plan is the go; blocking questions found while building become `ASSUMPTION:` lines on the card (SKILL.md gate 2). Default changes and other players stay blocked (gates 3-4).

## 2. Discovery (what the game offers)

Prefer the game's own objects and values over custom physics or drawing (`Aoe`, `Projectile`, `SE_*` status effects, hoe pieces, `RadialBase`, ward rules). To find them:
- in game: `op=list&filter=<text>`, `op=inspect&prefab=<name>`, a command's `find=`/`dump=` sub-actions (`earthquake`, `slime`), `op=fxdump` for effects, `op=bless&list=1` for status effects;
- offline (HomeOS): `node scripts/valheim/lib/il-reader.js <Valheim>/valheim_Data/Managed/assembly_valheim.dll <TypeName> <methodRegex> [--list]` prints a method's IL with tokens resolved (.NET reflection on the game DLLs is blocked by Smart App Control);
- looks: `/valheim-spell-fx` gives prefab combinations; build the mechanics on the chosen one.
Pin every visual or game reference in the request ("like Eikthyr") to a prefab before coding; write it on the card.

## 3. Code

- Where: the card's feature folder `src/Features/<F>/` only. A new folder name in lower case is the DLL name. Anything under `src/Core/` (a Kit helper, a guard, a contract) stops here: ask, it is a core-owner task with a restart.
- Shape: `public sealed class <Name>Command : CommandBase` (or `<Name>Effect : HarmEffect` for an effect centred on a player); `Name` lower case; `Usage` with every parameter, default and cap; read-only helpers as `SubAction`/`SubFlag` in the constructor; constructors without side effects; patches, owned objects and files in `OnLoad`; state for Harmony patches through `static Current`.
- Parameters and answers: the architecture doc's Parameter conventions (opt-in for anything new, `Required` for no safe default, first answer line a summary with counts and any cap that applied, `CommandException` for user errors, `Fmt` for numbers).
- Targets: the Target model. Other players only through an opt-in parameter, and only when `ZNet.instance.IsServer()`.
- Safety until the core pass (Safety guards table): `Players.Local` first; `ObjectDB.instance` checked when items or status effects are used; a `const` cap per count or size, stated in `Usage`; `Later`/`Own`/`Patches` only, never a raw coroutine or static timer; a ZDO mark on every object that persists, so a `clear=1` finds it later; `confirm=1` and a default radius for destructive parts.
- Kit: use the Kit by need table; a helper you need from another feature is copied into your folder only when it is small, and named on the card as a promotion candidate for the core owner.
- Test helpers: never a heal, kill or clear mode inside the command; use `killmobs`, `heal`, `spawn`, or propose a separate test-helper command.
- C# 5 and the Conventions section (no `?.`, `$""`, `nameof`, expression bodies; `== null` for Unity objects; copy a `for` variable before a lambda).
- Self-check before building: `grep -nE "Terminal|Console\.|m_godMode|m_ghostMode|m_debugFly|m_noPlacementCost|m_usedCheats" src/Features/<F>/` returns nothing that writes.

## 4. Build and reload

```bash
bash <pkg>/src/build.sh <feature>      # Git Bash; core must be installed (bash src/build.sh core once, game closed)
```
- Shell safety: the feature name on the command line is letters only (`[A-Za-z]+`, the folder name); a command form from chat goes to `send-command.ps1` as one single-quoted argument with no `'`, `;`, `|`, `` ` `` or `$` in it (forms only need letters, digits, `=`, `&`, `.`, `,`, `-`, `_`).
- A compile error stops the flow with csc's message (only CS0618 is hidden). Fix and rebuild.
- Then `op=reload` (through `send-command.ps1`); the answer must name the new `HomeCheat.Cmd.<feature>.v<stamp>.dll`. A `FAILED` line means a name clash or an `OnLoad` exception: read it, fix.
- `op=help&cmd=<name>` shows the new `Usage`.
- A core change, a renamed patch target or a hang at start needs a game restart: say so plainly and wait; never close or start the game yourself.

## 5. Test

Only when Maksym says the game is running with a world loaded (default `ClaudeTest`; any other world only with his yes).
- One command at a time: `powershell -NoProfile -ExecutionPolicy Bypass -File <pkg>/send-command.ps1 "<form>"` (it picks a unique inbox name and withdraws an unanswered command). No loops in the background; anything started is confirmed stopped before the turn ends.
- `op=cheatstate` before the first and after the last test; compare, do not read `[CheatCleanSlate]` as a cheat mark.
- Evidence per test, written to the card's test log: the answer's first line, new `HomeCheat` errors in `BepInEx/LogOutput.log` since the command, probe numbers where the effect is measurable (hp, distance, counts), and Maksym's observation asked as concrete questions ("did Claude2 move, how far, any damage?").
- Enemies: `op=spawn&prefab=<name>&count=<n>`; clear with `op=killmobs`. Other-player tests on `Claude2` without asking; report the damage and push it took.
- Tuning loop: change, rebuild, reload, re-test, one parameter at a time; update `Usage`, the page and the catalog row in the same step (docs move with the code).

## 5b. No-game run

When the brief says `no game`, instead of step 5:
1. `bash <pkg>/src/build.sh selftest` (exit 0) and the feature build (exit 0). Core changes (wave 0 core brief only): `bash src/build.sh core` (the game is closed in a no-game run).
2. The card's Test plan becomes Maksym's test list: probes first, then one command per line with the expected first answer line and what to look at; `op=cheatstate` before and after; enemies by `spawn`, clean-up by `killmobs`.
3. Status stays B in the index, with the line "built YYYY-MM-DD, not run in game: test list in <card>".
4. Then `done-gate.md`, trigger *built*.

## 6. Hand over to the done gate

When Maksym is satisfied with an iteration or says it is done: `done-gate.md`. If the session ends before that, the card holds the state: status, what is built, test log, what is left.
