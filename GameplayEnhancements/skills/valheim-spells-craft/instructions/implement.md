# Implement mode

One card: code, build, test, docs. Each phase 4 session runs this for its own cards.

## 1. Card and questions

1. Start-of-run check (SKILL.md). Read the docs listed in SKILL.md, the nearest command's page and its `src/Features/<F>/`.
2. Use the given card (`<pkg>/docs/Backlog/<name>.md`, updated in place), or write a draft (`spec-card.md`) to `LocalOutputs/<ts>/<name>-spec.md` and move it to `<pkg>/docs/Backlog/<name>.md` on Maksym's go (where a card lives: `spec-card.md`).
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
- Shape: `public sealed class <Name>Command : CommandBase`; a command that damages or pushes characters derives from `AttackCommand` (or `<Name>Effect : HarmEffect` for an effect centred on a player, which is an `AttackCommand`) and takes the shared PvP rule: `PvpUsage` in `Usage`, `HitsPlayers(a)`, `PvpNote(a)` in the first answer line, `PvpHits.ArmAoe` on every `Aoe` it spawns (a prefab can hold several: arm them all), `PvpHits.MarkProjectile` on shots; never its own PvP check; `Name` lower case; `Usage` with every parameter, default and cap; read-only helpers as `SubAction`/`SubFlag` in the constructor; constructors without side effects; patches, owned objects and files in `OnLoad`; state for Harmony patches through `static Current`.
- Parameters and answers: the architecture doc's Parameter conventions (opt-in for anything new, `Required` for no safe default, first answer line a summary with counts and any cap that applied, `CommandException` for user errors, `Fmt` for numbers).
- Targets: the Target model. Other players only through an opt-in parameter (`pvp=1` for attacks), and only after `Host.RequireServer()` (never a command's own `IsServer` check; older features keep theirs until they are next changed, Known defects).
- Safety guards (the architecture doc's table, last column): `Host.RequireWorld()` (or `RequireServer()`) first, then `Players.Local`; a `const` cap per count or size, stated in `Usage`, applied with `Caps.Check` (refuse) or `Caps.Clamp` (lower, note in the first answer line); `Later`/`Own`/`OwnInWorld`/`Patches` only, never a raw coroutine or static timer; world objects that should go when the player leaves the world are `OwnInWorld`, UI that lives across worlds is `Own`, own state is reset in `OnWorldExit()`; a ZDO mark on every object that persists, so a `clear=1` finds it later; `confirm=1` and a default radius for destructive parts (still per command).
- Shared core pieces before writing your own: two stored points `Coords.Require(Coords.Start)` / `Coords.End` (never a second point store), keep-away from buildings `Constructions.Near`, rectangles `GridMath.RectCells` (+ `RectSize` to report a cap), routes `GridRoute.Find` (answer `NoRoute` and `OverCap` as user errors). Kit by need lists them all.
- Choices the player makes (prefab, count, area): plain parameters on the command; the choosing is `ask=` flags on its `spells.txt` line (one form screen, `SpellForm`), never menu code in the command's feature. The exception is a choice `SpellForm` cannot hold: rows that depend on another answer, several picks at once, or a stateful flow with its state shown (preview, then start or remove). That gets a feature-owned window (Extension points; models `DomeMenu`, `ConstructPanel`): one spell line opens it, and every button sends the same form a typed command does, so each click can be tested from the inbox. A safety flag the menu would always send (Flatten's `avoid=1`) is a question in the form with the safe answer first, not a constant. A menu level or panel fits one ring of the game's radial (8 to 12 entries, `MaxElementsRange`): more entries go into sub-groups, not a spiral.
- Messages: only failures reach the screen (throw `CommandException`; `CommandBase.Run` shows it through `Kit.Notify`); success goes into the answer and the log; never a toast of the command's own.
- A vanilla piece (workbench, portal, ship) the player gets from a command is built as the hammer builds it (the `Player.PlacePiece` steps, as `dome` and `spawn` do: `SetCreator`, `WearNTear.OnPlaced`, place effect, a ship on the water surface) and never put into an inventory.
- What other players see: anything they should see is networked from the host (a ZDO value the game syncs, a networked stand-in); a look that only this PC shows is not done. A test aid drawn on this PC only (dome `step=show`) is the one exception: opt-in, its help starts `TEST AID, this PC only`, and the page says why it exists.
- Terrain: any terrain change that must survive a reload (a height, ground paint) goes through saved `TerrainOp`s from a registered template (architecture doc, Extension points "terrain changed exactly" and Kit by need: `TerrainLevel` in feature Construction, `TerrainOps` in feature World), never hoe pieces placed with a changed radius (their `TerrainModifier` comes back at the prefab radius after a reload). The approach exists twice already: a third feature that needs it asks for the Kit move (core change, gate 5) instead of a third copy; inside World or Construction use the copy there. Old hoe stamps in the area are removed first, and the card's Measure line reads only what the command changed (a paint check reads its own paint channel).
- Measure before you fix: when an effect on the world can fail silently or in part (terrain heights, pieces standing, ground covered), build the read-only step that measures it first (construct `step=flatness`, `step=support`), measure, then change the fix. Where the card allows, gate the world change on the measurement (construct stops at more than 5 % off).
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
- After every test read the new lines of `BepInEx/LogOutput.log`, not only the answer: success reports go only there, and some game failures show nowhere else (construct's cancelled terrain operations: `Failed to deserialize TerrainOp settings`).
- Evidence per test, written to the card's test log: the answer's first line, new `HomeCheat` errors and game warnings in `BepInEx/LogOutput.log` since the command, probe numbers where the effect is measurable (hp, distance, counts), and Maksym's observation asked as concrete questions ("did Claude2 move, how far, any damage?").
- Enemies: `op=spawn&prefab=<name>&count=<n>` (`tamed=1` for tamed-animal hits); clear with `op=killmobs`. Other-player tests on `Claude2` without asking; report the damage and push it took. Claude2 has no mod: it is also the check that other players see a look (a size, a form), before V.
- Sessions in parallel in one submodule checkout: your spell lines go through the session that owns `spells.txt`; commit only your own paths (`done-gate.md` step 3.1).
- Tuning loop: change, rebuild, reload, re-test, one parameter at a time; update `Usage`, the page and the catalog row in the same step (docs move with the code). When Maksym asks for a run of behaviour changes during a test, commit once per state he accepts, or when the loop pauses, with a subject that lists the changes. Do not commit once per rebuild: the construct loop left 12 small commits. Gate 10 still applies: nothing unfit to publish stays uncommitted in the submodule.

## 5b. No-game run

When the brief says `no game`, instead of step 5:
1. `bash <pkg>/src/build.sh selftest` (exit 0) and the feature build (exit 0). Selftest covers only `src/Core/Pure/`: Unity-free maths inside your feature is tested in a scratch console build (session scratchpad, never the repo), named in the handoff as outside selftest. Core changes (wave 0 core brief only): `bash src/build.sh core` (the game is closed in a no-game run).
2. The card's Test plan becomes Maksym's test list: probes first, then one command per line with the expected first answer line and what to look at; `op=cheatstate` before and after; enemies by `spawn`, clean-up by `killmobs`.
3. Status stays B in the index, with the line "built YYYY-MM-DD, not run in game: test list in <card>".
4. Then `done-gate.md`, trigger *built*.

## 6. Hand over to the done gate

When Maksym is satisfied with an iteration or says it is done: `done-gate.md`. If the session ends before that, the card holds the state: status, what is built, test log, what is left.
