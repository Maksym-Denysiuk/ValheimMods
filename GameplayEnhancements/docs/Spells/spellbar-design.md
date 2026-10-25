# Spells: replacing the `G` key action (design 2026-10-24, built since)

Status: built (view v3 2026-10-24; spell file v4 2026-10-25, section 15: built, not run in game). Sections 0-13 are the original design and its answers; 14-15 are what was built. Scope: Maksym's own game and worlds, HomeCheat plugin.

## 0. Recommendation in five lines
1. Vanilla `G` opens the game's **main radial menu** (button `OpenRadial`). `T` (button `OpenEmote`) opens the emote radial. Both go through one method, `Player.CheckKeyboardRadialPressed()`.
2. Intercept with **one Harmony prefix** on that method: when spells are on, `G` no longer counts as "radial pressed" (T still does). `Shift+G` is left to vanilla, so the old radial stays one chord away.
3. A **spell bar** (8 slots, drawn with IMGUI first) opens on `G`; digits `1`-`8` pick a slot; a slot is either one spell (cast on press) or a **group** that opens a second row of up to 8. A second **prefix on `Player.UseHotbarItem`** stops digits from switching weapons while the bar is open.
4. A cast is an **in-process call** of the existing `op=...` command (`HomeApi.Exec("op=field&prefab=...")`), same code path as the inbox, no file written. Defaults come from the commands themselves plus a `spells.txt` that holds each spell's parameters.
5. No console, no `Terminal`, no god/ghost mode: the UI only reads input, draws, and calls `Run`. A before/after snapshot of the cheat state is logged on every cast. Off switch: BepInEx config `Enabled=false` (patches are removed, vanilla `G` is byte-identical), or `Shift+G`.

6. Order of work (answer 2): the **bar UI first**, then quick-cast **pins** on `F1`-`F4`/`F6` and `Shift+1`-`Shift+5` bound to spells (section 2, "Quick-cast pins").

Recommended: **Option A** below (spell bar in a hot-reloadable command DLL, IMGUI), folded into the core when it settles (Option B).

---

## 1. What the game does with `G` (verified in `assembly_valheim.dll`, copy read with Cecil)

### 1.1 Key bindings
Defaults are created in `ZInput.ResetKBMButtons` (assembly_utils). The game uses the Unity Input System; code asks for **button names**, not key codes, and the player's remaps are saved by `ZInput.Save/Load`. So spells should ask for button names too: they follow remaps automatically. Values below are the defaults (Input System `Key` enum: G=21, E=19, T=34 ...).

| Button name | Default key | Used for |
|---|---|---|
| `OpenRadial` | **G** | opens the main radial menu |
| `OpenEmote` | T | opens the emote radial (same check) |
| `Sit` | X | sit emote |
| `Use` | E | interact |
| `Hide` | R | sheathe weapon |
| `GP` | F | guardian power |
| `AutoRun` | Q | auto run |
| `ToggleWalk` | C | walk/run toggle |
| `AutoPickup` | V | toggle auto pickup |
| `Inventory` | Tab | inventory |
| `Map` | M | map |
| `Hotbar1`..`Hotbar8` | 1..8 (Digit1..Digit8), plus empty `Hotbar{n}Alt` | `Player.UseHotbarItem(n)` |
| `Attack` / `Block` / `SecondaryAttack` | LMB / RMB / MMB | combat |
| `BuildMenu` / `Remove` | RMB / MMB | build mode |
| `Jump`, `Crouch`, `Run`, `AltPlace` | Space, LCtrl, LShift, LShift | movement, placement |
| `CamZoomIn` / `CamZoomOut` | mouse wheel | **camera zoom** (so the wheel is taken) |
| `Escape` | Esc | game menu (`Menu.Update` reads Esc directly) |
| `Console` | **F5** | the console (Key.F5 = 98 **[IL]**); opening it is not a cheat flag, running cheat commands is |
Hard-coded `KeyCode` reads: `Player.Update` Z, B, K, L (debug keys next to an `IsCheatsEnabled` call, gating UNVERIFIED), `BuildUi` F, `Hud` F3 and LCtrl (HUD hide). **Do not use Z, B, K, L, F, F3, Esc.**

### 1.2 The `G` path
1. `Player.Update` calls `HandleRadialInput()` (offset 0x03b6).
2. `HandleRadialInput`:
   - returns early when `Hud.InRadial()` (radial already open);
   - sets `Hud.instance.m_radialMenu.CanOpen = JoyRadial-down || CheckGamepadRadialBuildPressed() || CheckKeyboardRadialPressed()`;
   - if `CheckKeyboardRadialPressed()` (instance method, no arguments) and not `Hud.IsPieceSelectionVisible()` it calls `m_radialMenu.Open(Hud.m_config /*OpenRadialConfig*/, null)`.
3. `CheckKeyboardRadialPressed()` = `GetButtonDown("OpenRadial") || GetButtonDown("OpenEmote")`.
4. `OpenRadialConfig.InitRadialConfig`: if the button `OpenEmote` is **held** it opens `EmoteGroupConfig`; else `TryOpenNonDefaultRadials` (item menus for a hovered object); else `MainGroupConfig`. So **T alone reaches the emote radial; `G` is the only way to the main group radial** (quick-use groups). That is the vanilla job we must not lose silently: `Shift+G` passthrough keeps it.
5. While the radial is open `PlayerController.LateUpdate` blocks look/move input via `RadialBase.IsBlockingInput`.

### 1.3 The hotbar path
`Player.Update` loops `i = 1..8`: `GetButtonDown("Hotbar{i}") || GetButtonDown("Hotbar{i}Alt")` -> `Player.UseHotbarItem(i)`. `HotkeyBar.Update` and `HotkeyBar.ElementClicked` also call `UseHotbarItem` (gamepad and mouse clicks on the bar). One prefix on `UseHotbarItem(int)` covers all three.

### 1.4 Input gating
`Player.TakeInput()` is false when chat has focus, the console, text input, store, inventory, game menu, text viewer, minimap, free-fly camera, barber UI, build-UI search field is open, or the player is dead/in a cutscene/teleporting. The spell bar must use the same predicate: if `TakeInput()` is false, the bar closes and ignores keys (call it by reflection/`AccessTools` if it is not public).

### 1.5 UI facts for drawing
`Hud.IsUserHidden()` is true after LCtrl+F3 (HUD hidden): hide the bar then. `Character.Message(MessageHud.MessageType, text, ...)` shows the standard top-left text and is not a cheat path (used by vanilla for "autopickup on/off").

---

## 2. Slot map (updated after Maksym's answers, 2026-10-24)

Fixed by Maksym: #1 first section of the radial menu, #2 attacking, #3 protective, #4 fun, #5 healing and blessings, #6 construction, #7 field+plant+place, #8 repair+refill.

| Slot | Name | Content | Type |
|---|---|---|---|
| **1** | Radial | the vanilla radial menu, first section (see "Slot 1" below) | hand-off |
| **2** | Attack | `spellshot` arrow 1x1 / arrow 3x3 / balista / thunderbolt; `earthquake` with `damage=1`; `slime` ring | group |
| **3** | Protect | `earthquake` push-away (`noStones=1`) and stone circle; `slime` moat (gap 2 m, damaging ring around you); later: ward/shield presets | group |
| **4** | Fun | `visualeffect` (weather, time, light, shake, once the catalog has verified entries); `transform` raven / serpent / fish / human; `scale` trees x2 / x0.5; `spawn` and `give` presets | group |
| **5** | Heal and bless | NEW commands (not built yet): `heal` (HP, stamina, eitr), `cleanse` (remove negative status effects), `bless` (boss blessings: Eikthyr, Elder, Bonemass, Moder, Yagluth, Queen... and others the game has) | group |
| **6** | Build | `groundlevel` flatten / +1 / -1, `path` dirt 5 m / stone 5 m, plus a few `place` presets of building pieces | group |
| **7** | Field | `cultivate` 3x5, `field` presets (carrots 3x5, mixed crops), `plant` presets (oak grove 3x5 x3 scale), `place` presets (walls, fires, as in `place-design.md`) | group |
| **8** | Repair | `repair` aim, `repair` radius 10, `refill` aim, `refill` radius 10, `repair+refill` radius 10 (two calls). Placed pieces only (answer 7): walls, fires, stations; no inventory durability | group |

One command can sit in several groups with different parameters (it is only a line in `spells.txt`): `earthquake` is "attack" with damage and "protect" as a push-away or stone circle, `slime` is "attack" as a ring and "protect" as a moat.

Where `groundlevel`, `path`, `cultivate`, `place` go:
- `groundlevel`, `path` -> **#6**: terrain shaping, no crops.
- `cultivate` -> **#7**, next to `field`: `field` only plants on cultivated soil (its own error says "use op=cultivate first"), so the pair is two presses in one group.
- `place` -> **#7** as you named the group; building-piece presets (floors, walls) are mirrored into #6.
Maksym has not confirmed this split ("let's see it first"): it is a file edit to change.

Exposure rule (confirmed): `kill` and `clearitems` are **not** in any slot.

### Slot 1: the radial menu
Maksym's wording: slot #1 is "the first section of the radial menu". Reading used here: slot `1` is a hand-off to the vanilla radial, which is where the first section lives. Pressing `1` closes the bar and calls `Hud.instance.m_radialMenu.Open(Hud.instance.m_config, null)`, the call vanilla `HandleRadialInput` makes (code at offset 0x00ab). The vanilla radial's content is an asset (`RadialDataSO.MainGroupConfig`), so it cannot be read from the assembly, and opening one chosen section directly would need the radial's private state. Together with `Shift+G` (answer 3) the bar therefore has two ways into the radial. If the intent is to show the radial's first-section items inline in our bar, that is a separate, larger piece of work (see Option C). To be confirmed in game: what the first section contains.

### Slot 5: heal and bless (what exists in the game, **[IL]**)
- `Character.Heal(float hp, bool showText)`, `Character.SetHealth`, `Player.AddStamina(float)`, `Player.AddEitr(float)`: plain methods, no console, no cheat flag (flags come only from `ConsoleCommand.RunAction` and god/ghost mode, see the review).
- Status effects: `Character.GetSEMan()` -> `SEMan.AddStatusEffect(int nameHash, bool resetTime, int itemLevel, float skillLevel, short variant)` and `RemoveStatusEffect(int nameHash, bool quiet)`, `GetStatusEffects()`.
- Guardian powers: `Player.SetGuardianPower(string)` (which power is equipped), `Player.StartGuardianPower()` / `ActivateGuardianPower()` (use it, with the game's cooldown), `Player.m_guardianSE`.
- **Not verifiable offline:** the status effect names (`GP_Eikthyr`, `GP_TheElder`, `GP_Bonemass`, `GP_Moder`, `GP_Yagluth`, `GP_Queen`, ...) live in the `ObjectDB.m_StatusEffects` asset list. The new `bless` command prints them with `bless&list=1` (like `find=` in the other commands) and only then are they put into `spells.txt`. Two ways to give a blessing: (a) `SEMan.AddStatusEffect(hash)` directly, ignores the guardian cooldown, (b) `SetGuardianPower(name)` + `StartGuardianPower()`, honest to the game and cooldown-limited. Default: (a) with a `duration` parameter; (b) as a second spell line.
- Defaults: `heal` = full HP, stamina and eitr of the local player; `cleanse` = all negative effects (wet excluded); `bless&name=bonemass&duration=300`.
- These are three new commands for the core or command-DLL authors (session owner decides); the bar only needs their `op=` names.

### Quick-cast pins (answer 2)
After the bar works, ten spells can be **pinned** to direct keys so they fire without opening the bar:

| Pin | Key | Note |
|---|---|---|
| 1-5 | `F1`, `F2`, `F3`, `F4`, `F6` | `F5` is the game's **console** key (`Console` = Key.F5 **[IL]**); `F3` is only taken with LCtrl (HUD hide) so plain `F3` is free |
| 6-10 | `Shift+1` ... `Shift+5` | the game most likely ignores Shift on `Hotbar1..5` (a plain key path, UNVERIFIED) and would also switch the weapon, so the `UseHotbarItem` prefix swallows digits while Shift is held and pins are on; Shift is also Run |
Maksym asked for F1-F5; default proposal is F1-F4 + F6, and `F5` can be added in the config at the cost of also opening the console. A pin is a flag in `spells.txt` (`pin=F1`, `pin=S3`) or set live with `op=spells&pin=F1&spell=7.2`. Keys are read with `ZInput.GetKeyDown(KeyCode.F1, false)`, same `TakeInput()` gate as the bar. The bar shows pinned spells as a small second row with the key label above the 8 slots.

---

## 3. Interaction model

```
          G (not Shift)                  digit n                       digit m
 closed  ---------------->  bar open  ------------>  slot n is spell: cast, bar closes (or stays: sticky)
                                |                       slot n is group: second row opens
                                |                                      ------------>  cast spell m, close
                                |   G / same digit again / 6 s idle / TakeInput()==false / Hud hidden  ->  close one level
                                '-- Shift+G  -> not intercepted: vanilla radial opens
```

- **Open/close:** `G` toggles. Idle timeout 6 s per level. `Esc` is not used (the game menu owns it).
- **Select:** keys `1`-`8` (the same keys as the vanilla hotbar, which the bar suspends while open). Mouse wheel is **not** used: it is bound to camera zoom (`CamZoomIn/Out`). An optional `[` / `]` step-and-confirm is a later add.
- **Cast on press:** pressing the final digit casts at once, no second confirmation. Aim = crosshair (core `AimPoint`). This keeps the loop `G, 7, 2` (open, field group, third entry).
- **Sticky spells:** spells flagged `sticky=1` (repair, refill) keep the bar open on their level so `G, 8, 1, 1, 1` repairs three things in a row.
- **Modifiers while pressing the digit:** `Shift` = "variant B" of the spell (each spell line may carry a `shift=` override string, e.g. `radius=30`), `Ctrl` is reserved (Crouch). No other modifiers in v1.
- **Display:** an 8-slot bar at the bottom centre above the vanilla hotbar area: number, short name, a group marker, a cooldown shade. A one-line result under the bar for 3 s (first line of the command's answer). Errors in red. The same text goes to `LogOutput.log`.
- **Blocking:** while the bar is open: digits do not reach `UseHotbarItem`; `G` does not reach the radial; attack is not blocked (a swing during the bar is allowed), movement is not blocked.

### Rendering choices
- **IMGUI** (`OnGUI` on a `DontDestroyOnLoad` object): no assets, 100-150 lines, flat look, scaled with `GUI.matrix` from `Screen.height / 1080`. Good enough to prove input and flow.
- **uGUI built in code** (Canvas under `Hud.instance`, TextMeshPro already in the game): follows the game's UI scale and HUD hiding. Later upgrade.
- Reusing the vanilla radial: see Option C.

---

## 4. Parameters and defaults

Three layers, last wins:
1. **Command defaults** (already in the core, see `Run` comment): aim = crosshair (camera ray 60 m, else 8 m ahead), `rows=1`, `perrow=1`, `scale=1`, `spacing` auto, `clear` auto, `repair/refill aim=true`, `amount 0 = full`, `groundlevel radius 4`, `path 5 x 2 dirt`, slime `gap 2`, earthquake `stages 5`, spellshot `perrow=1 rows=1 dist=1`.
2. **Spell line** in `BepInEx/config/homecheat/spells.txt`: only what differs from the defaults.
3. **Modifier** (`Shift` -> the `shift=` string).

File format (one spell per line, `#` comments, same grammar as inbox bodies and the `place-design.md` presets):
```
# slot.entry | label | command | flags
7.1 | Cultivate 3x5   | op=cultivate&rows=3&perrow=5
7.2 | Carrots 3x5     | op=field&prefab=Pickable_Carrot&rows=3&perrow=5
7.3 | Oak grove       | op=plant&prefab=Oak_Sapling&rows=3&perrow=5&scale=3
8.1 | Repair (aim)    | op=repair                       | sticky
8.2 | Repair r10      | op=repair&aim=0&radius=10       | sticky
8.3 | Refill r10      | op=refill&aim=0&radius=10       | sticky
8.4 | Repair+Refill   | op=repair&aim=0&radius=10 ;; op=refill&aim=0&radius=10
3.1 | Quake           | op=earthquake                   | shift=mode=strong
3.2 | Slime           | op=slime
2.1 | Arrow           | op=spellshot&type=arrow         | shift=perrow=3&rows=3
5.1 | Raven 30 s      | op=transform&type=raven&duration=30
5.4 | Human           | op=transform&type=human
```
- `;;` chains calls in one press (run sequentially in the same frame).
- The file is read on bar open if its timestamp changed, and by `op=spells&reload=1`; a bad line is skipped and shown once in the toast. If the file is missing, the bar starts with a built-in list written out as the default file so it can be edited.
- Slot `1` is not read from the file in v1.

---

## 5. Calling the file-based commands without a human typing a file

The inbox poll in `Update` calls `Run(ParseForm(body))` on the main thread. The spell bar also runs on the main thread (our `Update`/`OnGUI`), so it can call the same function directly:

```
string HomeApi.Exec(string form)   // "op=field&prefab=Pickable_Carrot&rows=3&perrow=5"
   -> HomeCheatPlugin.Run(ParseForm(form)), exceptions caught, returns the answer text
```
Why not write a file: a 0.5 s poll adds up to half a second of delay per press, files race with the reader (code review finding 10) and spam `outbox/`. The inbox stays as it is for Claude and scripts. Both paths end in the same `Run`, so behaviour is identical and every command DLL works unchanged.

Result handling: first non-empty line -> toast under the bar; full text -> `LogInfo`; if the text starts with `ERROR` the slot flashes red. Re-entrancy: ignore a cast while the previous `Exec` is still on the stack (not possible today, cheap guard). Per-spell cooldown (default 0.4 s, spellshot already has its own).

---

## 6. How the UI stays clear of every cheat flag

Facts (code review, last section): `Terminal.m_cheat` is set only by `devcommands`; `PlayerProfile.m_usedCheats` only by `ConsoleCommand.RunAction`; the per-object ZDO mark comes from damage/destroy/place under god or ghost mode, a flagged item, or an item with more than 10000 damage.

Design rules:
1. The bar never references `Terminal`, `Console`, `Player.m_godMode`, `m_ghostMode`, `m_debugFly`, `m_noPlacementCost`, `PlayerProfile`. A grep over the new source for those names is part of review.
2. Only two Harmony patches, both on non-cheat methods (`CheckKeyboardRadialPressed`, `UseHotbarItem`). They read `ZInput` and our own state only.
3. **Snapshot guard:** at startup and around every cast, read `Terminal.m_cheat`, `PlayerProfile.m_usedCheats`, `Player.m_godMode`, `m_ghostMode`, `m_debugFly`, `m_noPlacementCost` (the `cheatstate` code already knows how). If a value went from false to true during a cast, log `FLAG CHANGED by <spell>` and disable the spell bar until the next restart. On ClaudeTest the baseline is already true, so the guard only reports changes; on a clean world it protects the flag itself.
4. Damage spells are only flag-safe with ghost/god mode off and no cheated item equipped: the toast prints `ghost mode ON: damage will mark creatures as cheated` when `m_ghostMode` is set and a damaging spell is cast.
5. `give` presets stay below `m_maxQuality`.
6. The world guard of the code review (finding 1) applies to `Exec`. Maksym's answer 6 is "all worlds"; read as all worlds he hosts or plays alone (ClaudeTest, VanilaPVP and any new one), so the guard checks **who hosts** (`ZNet.instance.IsServer()`: single player or a world this PC hosts), not a list of names. The bar stays inert on a server joined as a client. If "all worlds" was meant to include other people's servers, that is outside this project's scope (own game and own world only) and is not designed here.

---

## 7. Switching it off

| Level | How | Effect |
|---|---|---|
| Per press | `Shift+G` | vanilla radial opens, bar not touched |
| Runtime | `op=spells&enabled=0` (or BepInEx config `Spells.Enabled=false`) | bar closes, both Harmony patches are **removed** (`UnpatchSelf` on its own Harmony id) so vanilla `G` and the hotbar are byte-identical, `Update` stops |
| Per session | delete/rename `spells.txt` + `enabled=0` | nothing to cast |
| Permanent | remove `HomeCheat.Cmd.spells.*.dll` from `homecheat-commands/` (Option A) or the setting (Option B) | gone after restart |
Re-enable with `op=spells&enabled=1` without restart. `op=spells&status=1` prints enabled, patches applied, number of spells, last cast, snapshot state.

---

## 8. Options

### Option A (recommended): spell bar as a hot-reloadable command DLL, IMGUI
`Cmd_spells.cs` -> `HomeCheat.Cmd.spells.v<stamp>.dll`, class `SpellsCommand : IHomeCommand` (`op=spells`) that creates one `SpellBar` MonoBehaviour and its own `Harmony("com.homeos.homecheat.spells")`.
- Pros: iterate without restarting the shared game; the five sibling commands keep working untouched; easy to turn off by unloading.
- Cons: a reload must tear the previous instance down first (needs the lifecycle hook below), or two bars and two patch sets would run; Harmony patches from an in-memory assembly need `UnpatchSelf` on unload.
- Work: about 400 lines C# 5 (bar 150, input/state machine 100, spells file parser 80, snapshot/guard 40, status/enable 30).

### Option B: the same code inside the core `HomeCheat.dll`
- Pros: simplest lifecycle (Awake/OnDestroy of the plugin), patches via the existing `PatchAll`; no unload problem; fastest at runtime.
- Cons: every change needs a game restart, which the shared game session forbids today; a bug in the bar can break the inbox channel.
- Use it as the **second step**: once Option A is stable, move the file into the core.

### Option C: reuse the vanilla radial menu (`Valheim.UI.RadialBase`)
Implement `IRadialConfig` (`InitRadialConfig`, `LocalizedName`, `Sprite`) so `G` opens a radial with our elements, nested groups via `RadialMenuElement.TryOpenSubRadial`, back element built in, gamepad and mouse selection for free, same look as the game.
- Pros: native look and input blocking (`IsBlockingInput`), 8 elements fit a pie naturally.
- Cons: elements are `RadialMenuElement` components with `Image`/`CanvasGroup` that `RadialBase.ConstructRadial` builds from a prefab (`m_elementInfo`) and a `RadialDataSO` ScriptableObject; `RadialBase` has about 60 private fields and animation managers. No stable API, high chance of version breaks, hard to unit-test without the game. Selection is by mouse direction, not by digits, which is not the "slot 1-8" model you described.
- Not recommended unless you prefer a pie menu to a bar.

---

## 9. Core changes needed (the core is owned by the session that started this one; nothing edited here)

Required for Option A:
1. **`HomeApi.Exec(string form)`**: public, in-process, runs `Run(ParseForm(form))` with the same exception handling as `Update`.
2. **Command lifecycle:** optional `interface IHomeCommandLifecycle { void OnLoad(); void OnUnload(); void OnWorldExit(); }`. `Reload` calls `OnUnload()` on the previous instance of the same `Name` before replacing it; a Harmony postfix on `ZNetScene.OnDestroy` (or `Game.Logout`) clears `timers` and calls `OnWorldExit()` on every registered command (this also fixes code review finding 3).
3. **World guard:** `Guard()`/`RequireWorld()` at the top of `Run` (code review findings 1 and 8). `Exec` goes through it.
4. **Toast:** `HomeApi.Toast(string)` -> `Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, text)` (no-op without a player), so commands and the bar show text the same way.
5. **Input helpers:** `HomeApi.TakeInput()` wrapping `Player.TakeInput()`, `HomeApi.HudHidden()` wrapping `Hud.IsUserHidden()`.
6. **Config:** BepInEx `Config.Bind` entries for `Spells.Enabled`, `Spells.PinKeys` (default F1,F2,F3,F4,F6 + Shift+1..5); the host check needs no name list (answer 6). Expose `HomeApi.Config`.
6b. **New commands for slot 5:** `heal`, `cleanse`, `bless` (+ `bless&list=1` to list the status effects of this game), each following the existing rules (local player, no console path, `op=cheatstate` after). The command owner decides where they live (new `Cmd_*.cs` files in this folder, like the other five).
7. **Cheat snapshot:** move the reading part of `CheatState()` into `HomeApi.CheatSnapshot()` returning a small struct, so the bar can compare before/after (and `op=cheatstate` uses it too).
8. **Shared helpers** (reduce the duplication list in the review): `AllPrefabs(filter)`, `FirstPrefab(params)`, `Field(obj,name)`.
9. **Optional metadata:** `IHomeCommand` stays as is; `spells.txt` carries the slot mapping, so commands need no new members.
10. `build-cmd.sh`: keep only the newest staged DLL per command (finding 6) so a restart does not load 40 builds and resurrect removed classes.

Not required: nothing in the existing five command DLLs has to change for the bar to call them; two small fixes there (spellshot origin, `kill`/`clearitems` confirm) are in the review.

## 10. Implementation order (after approval)
1. Core items 1-3 (Exec, lifecycle, guard) and the `build-cmd.sh` cleanup, with `op=ping/reload/commands` re-checked.
2. **Bar UI first** (answer 2): `Cmd_spells.cs` with the state machine and IMGUI bar for the 8 slots and their group rows, built-in list, no Harmony yet; open with a debug op (`op=spells&open=1`) to test drawing.
3. Add the two Harmony patches; test `G`, `Shift+G`, `T`, digits with the bar closed and open; slot 1 hand-off to the radial.
4. `spells.txt` parser and the slot map of section 2.
5. **Pins:** `F1`-`F4`/`F6` and `Shift+1`-`Shift+5` bound to spells, second row on the bar, `pin=` flag and `op=spells&pin=`.
6. Snapshot guard, status, enable/disable, world-exit cleanup.
7. In parallel, owned elsewhere: `heal`, `cleanse`, `bless` commands for slot 5.

## 11. Test plan (when you say the game is running with ClaudeTest loaded)
| Test | Expected |
|---|---|
| `G` with spells on | bar appears, vanilla radial does not |
| `Shift+G` | vanilla radial opens |
| `T` | emote radial opens as before |
| digits with bar closed | hotbar item switches as before |
| digits with bar open | no weapon switch, spell cast / group opens |
| chat open, inventory open, map open, game menu | `G` and digits ignored, bar closed |
| LCtrl+F3 (HUD hidden) | bar hidden, `G` does nothing |
| `op=spells&enabled=0` | `G` opens the vanilla radial again; `status` shows patches removed |
| reload `Cmd_spells` twice | one bar, one patch set (`HarmonyLib.Harmony.GetPatchInfo` shows one prefix per method) |
| quit to menu and back | no bar in the menu, no timers firing in the new world |
| every spell once | `op=cheatstate` identical before and after (baseline True in ClaudeTest) |
| `#8` repair+refill on a damaged, empty campfire | both fixed, no message |

## 12. Open risks
- `CheckKeyboardRadialPressed` is called from two places in `HandleRadialInput`; the prefix must return the same value both times within a frame (it reads `GetButtonDown`, which is stable within a frame) - fine, but verify in game.
- `T`-held `OpenEmote` logic in `InitRadialConfig` is unchanged by the patch, but `G` while T is held would behave as T: acceptable.
- A key remapped away from `G` still works because we read `OpenRadial`.
- IMGUI is drawn over everything including the game menu unless we hide it: the `TakeInput()`/`Menu.IsVisible()` check handles it.
- Gamepad is out of scope (the `JoyRadial` path is untouched).

## 13. Maksym's answers (2026-10-24) and what is still open

| # | Question | Answer | Effect on the design |
|---|---|---|---|
| 1 | Slot #1 custom hotbar | "the first section of radial menu" | Slot 1 = hand-off to the vanilla radial (section 2, "Slot 1"); still to confirm in game what the first section is |
| 2 | Cast on press? | yes; bar UI first, then keys F1-F5 and Shift+1..5 bound to bar slots | Cast on press; bar before pins; pins F1-F4 + F6 by default because F5 is the console key |
| 3 | Vanilla radial on Shift+G | yes | As designed |
| 4 | Grouping | "let's see it first" | Slot map in section 2 stays a proposal, editable in spells.txt |
| 5 | kill / clearitems off the bar | yes | As designed |
| 6 | Worlds | in all worlds | Guard by hosting, not by name (section 6, rule 6) |
| 7 | Repair inventory items | no, placed items only | Repair group = pieces, fires, stations |

New requests, in section 2: #2 attack, #3 protective, #4 fun, #5 heal and bless (Bonemass and others).

Still open:
1. **F5** is the game console key: use F6 instead (default), or allow F5 and also open the console?
2. **Slot 1:** is "open the vanilla radial" what you meant, or should the radial's first-section items show inline in our bar?
3. **Blessings:** which ones beyond the boss powers (Eikthyr, Elder, Bonemass, Moder, Yagluth, Queen, ...), and should they use the game's guardian power with its cooldown, or just apply the effect for N seconds (default 300)?
4. **"All worlds":** all worlds hosted by this PC or played alone? (Servers joined as a client are not covered.)

## 14. Implementation status (2026-10-24, night)

Staged and **loaded in the running game** (reload done, `op=spells` shows ON / PLACEHOLDER, 10 pins, both patches applied; the radial and the bar were not looked at on screen by me):

| File | DLL | Contains |
|---|---|---|
| `Cmd_heal.cs` | `HomeCheat.Cmd.heal.v162951.dll` | `heal`, `cleanse`, `bless` (slot 5). Not used while placeholder mode is on |
| `Cmd_spells.cs` | `HomeCheat.Cmd.spells.v220937.dll` | `spells`: radial picker, 10-cell action bar with icons, placing by Shift+click (view v3), spell files, placeholder mode, cheat-state snapshot per cast |

Build line: `M=/d/steam/steamapps/common/Valheim/valheim_Data/Managed; bash build-cmd.sh spells -r:$M/UnityEngine.IMGUIModule.dll -r:$M/UnityEngine.TextRenderingModule.dll -r:$M/UnityEngine.UI.dll -r:$M/Unity.TextMeshPro.dll -r:$M/assembly_utils.dll`. After each build delete the older `HomeCheat.Cmd.spells.v*.dll` here and in `homecheat-commands/`. No core file was edited.

### View v2: the game's own radial menu + a classic two-row action bar
**Radial (G).** The game's `Valheim.UI` radial classes are public, so the spell menu is built on them instead of a custom drawing:
- `RadialBase` is the game's menu; we implement its extension point `IRadialConfig` with three classes: `SpellRootConfig` (8 sections), `SpellGroupConfig` (one group, its spells as sections) and `SpellLeafConfig` (name and icon of a leaf). Elements are the game's own `GroupElement`, `EmptyElement` prefabs from `RadialData.SO`; a spell leaf is a `GroupElement` whose `Interact` and `CloseOnInteract` are replaced, so selecting it calls our cast code and `CloseOnInteract=false` (sticky) keeps the radial open.
- One Harmony prefix on `RadialBase.Open(config, backConfig)`: when the vanilla main radial (`ValheimRadialConfig`) is about to open and Shift is not held, the config is swapped for ours. Opening on G, the animation, input blocking, Back, release-to-use, closing: all vanilla. `T` (emotes), hover menus and `Shift+G` (vanilla main radial) are untouched.
- Section 1 is the game's own **Hotbar** group, built exactly like the vanilla main radial builds it (answer to "slot #1 = first section of the radial menu"). Sections 2-8 are the groups of the spell file; a missing group is an `EmptyElement`. Icons are the game's existing ones: weapons/tools (2, 8), shield (3), emote (4), food bowl (5, 7), bag (6).
- If building the radial throws, the spell system switches itself off and the vanilla radial is opened instead (log line `spells: radial build failed`).

**Action bar, view v3 (always on screen while spells are enabled).** One row of 10 cells, bottom centre (answer to the 2026-10-24 21:40 feedback):
- cells 1-5 fire on **Shift+1..5**, cells F1-F5 on **F1..F5**; a wider gap separates the two groups of five. The old "Shift +" caption is gone: a cell label is just the digit or the F key;
- a cell shows the spell **icon** (the sprite of its group in the game radial; spells have none of their own yet) with the spell **name under it**; gold flash on fire, shade during the 0.3 s cooldown, dimmed while input is blocked, hidden with the HUD; an empty cell is a dark card with its key;
- cells 1-5 no longer add the spell's `shift=` variant (Shift is part of their key); F cells still do when Shift is held;
- toast above the row for 3.5 s.
`F5` is also the game console key: a pin on F5 opens the console too.

**Hotbar section (radial slot 1) and placing (view v3.1, 2026-10-24 22:05).** Slot 1 of the radial is no longer the game's item hotbar: it is our **Hotbar** with the same 10 cells as the bar (Shift+1..5, F1..F5). Each section shows the cell's spell (icon, name, key in the subtitle) or "(empty)".
- Click on a section casts its spell (like the key). **Shift+RMB** on a section empties that cell and reopens the Hotbar, where the cell now shows as an empty section (name "(empty)", no icon); plain RMB stays the radial's Back. While Shift is held the radial's Back is suppressed (wrapper on `RadialBase.GetBack`). Detected in `Update` from `RadialSecondaryInteract` / mouse 1 and the radial's `Selected` element. Subtitle of a filled cell: "Shift+RMB: empty this cell".
- **Placing:** in any group, **Shift+click** on a spell picks it up and switches the radial to the Hotbar section (`RadialBase.QueuedOpen`); the subtitles then read "click: place <spell> here". A click on a section puts the spell there (a spell sits in one cell, it moves) and closes the radial. A banner above the bar says what is pending; closing the radial without choosing (~0.6 s) cancels.
- The earlier click-on-the-bar mode (free cursor, input blocking) was removed; the bar on screen only displays.
- Placements are saved to `<spell file>.pins` (`spells-ui-test.txt.pins`, `spells.txt.pins`), one `S1=2.1` / `F3=5.1` line per cell. If that file exists it replaces the `pin=` flags of the spell file; `op=spells&pins=reset` deletes it.
- Reload safety: a radial still holding a config of a replaced build opens the vanilla radial instead of throwing (log `stale radial config`), without touching `enabled`.

### Placeholder mode (default ON)
- The bar reads `BepInEx/config/homecheat/spells-ui-test.txt` instead of `spells.txt`. A cast, from the radial or a pin, only shows `placeholder: <label> (<slot.entry>)` (+ ` +shift=<text>` with Shift on a spell that has one) and logs it. It never calls a command, takes no cheat snapshot.
- A line with `@dummy` does the same with placeholder mode off.
- `op=spells&placeholder=1|0` switches (saved in `spells.state`); `reset=1` rewrites the active file from the built-in defaults (old one saved as `.bak`: the first v2 load found the v1 file with `pin=F6`, `reset=1` fixed it).
- Test file: groups 2-8 with 5-8 entries, sticky entries (`3.5`, `4.7`, `4.8`, 6.1-6.3, all of 8), `shift=` variants, pins F1-F5 (2.1, 3.1, 5.1, 8.2, 8.4) and Shift+1..5 (7.1, 3.2, 5.3, 4.1, 4.4). Slot 1 is not read from the file.

### How to test
1. Already loaded and ON. If needed: `op=reload`, `op=spells&enabled=1`.
2. Press `G`: the radial opens with our sections (Hotbar on top, then Attack, Protect, Fun, Heal, Build, Field, Repair); pick a group, pick a spell: toast `placeholder: ...`; sticky spells keep the radial open, others close it; Back works; the Hotbar section shows your real hotbar items.
3. With the radial closed: `F1`-`F5` and `Shift+1`..`Shift+5` flash the matching slot and show a toast; plain `1`-`5` still switch the hotbar; Shift+digit does not switch the weapon.
4. `Shift+G` = vanilla radial, `T` = emotes. Chat/inventory/map open: pins ignored, bar dimmed.
5. `op=spells&enabled=0` removes the patch and hides the bar. `op=cheatstate` after: unchanged (cheat=True used=True ghost=True are the earlier baseline).
6. Real spells later: `op=spells&placeholder=0` (needs `spells.txt`; `op=bless&list=1` for names).

View v3 unverified in game: the click-to-place flow (cursor really freed, no camera drift, the order of the leaf Interact and CloseOnInteract calls, mouse position under the 1080-scaled GUI matrix) and the sprite drawing in IMGUI (atlas sprites). Fallback if the cursor stays locked: place by pressing the cell key.

Known limits: not yet seen on screen (the radial elements are the game's prefabs, so how a leaf looks inside a group is unverified); leaves use their group's icon; gamepad radial not covered; `spells.txt` pins are read at load (live `pin=` last one session); the core fixes of `code-review.md` are not applied.

## 15. Spell file v4 and the menu tree (2026-10-25, phase 4 wave 0, built, not run in game)

Built by session S0-spells of the Valheim work plan (HomeOS `LocalOutputs/2026-10-25--01-59/backlog-plan.md` section 2), unattended and without the game. Format and use: [spellbar.md](spellbar.md).

| Change | How | Evidence |
|---|---|---|
| ids of any depth, any number of entries per level | `SpellParse` (no Unity) builds a tree of `SpellNode`s; `SpellGroupConfig` takes a node id and opens sub-sections as `GroupElement`s, so Back is vanilla at every depth | offline parser check: both spell files 0 bad lines |
| no 8-entry limit | a level passes all its elements to `RadialBase.ConstructRadial`, which calls `SetElementsPerLayer(count)` (thresholds `RadialDataSO.MaxElementsRange`) and lays the rest out in layers, the spiral of the emote wheel (`EmoteGroupConfig` passes 25) | IL of `ConstructRadial`, `SetElementsPerLayer`, `EmoteGroupConfig.InitRadialConfig` |
| title lines at any depth, sticky titles | `id \| label` or `id \| label \| \| sticky`; sticky passes to every spell below | parser check |
| answer chains | `ask=` flags; `SpellAskConfig` is one level per question; a click stores the answer and `QueuedOpen`s the next level with the current one on the Back stack; the last answer casts with `{key}` replaced (values escaped) | not run in game |
| dynamic answers | `SpellAskSources`: `@players` (others only when hosting), `@crops`, `@trees`, `@prefabs` (categories: Creatures, Items by type); more than 40 answers split into alphabetical blocks | not run in game |
| `status=1` | also prints `RadialData.SO.MaxElementsRange` | not run in game |
| menu tree | plan section 2.2 in `spells.txt`, `spells-ui-test.txt` (all `@dummy`) and `SpellFile.DefaultText`; commands not built yet are `@dummy` "(soon)" lines with their real form in a comment above | `commands-check.js` exit 0 |

Assumptions taken in the unattended run (Maksym reviews):
- **A18**: existing ids keep their meaning; Repair moves from 8 to 6.5 with its pin; 6.1 (Flatten) becomes a sub-section title; the old 6.2 Raise 1 m / 6.3 Lower 1 m are dropped (plan 2.2).
- **A21**: overflow is the game's same-level spiral. Fallback if `GroupElement`s look wrong in layers: a "More..." group.
- **ASSUMPTION (S0-spells)**: a title is a line whose command field is empty, so a sticky title is written `id | label | | sticky` (the plan did not fix a syntax).
- **ASSUMPTION (S0-spells)**: a section or sub-section with nothing below it (8 Construct) opens one note "(nothing here yet)" instead of an empty level.
- **ASSUMPTION (S0-spells)**: a pinned spell with questions (F-key or Shift+digit) opens the radial on its first question; from the Hotbar section it opens the questions with the Hotbar as Back.
- **ASSUMPTION (S0-spells)**: fixed answers keep the order of the flag; dynamic answers are sorted by label (by category first for `@prefabs`); an alphabetical block is named "first - last" label.
- **ASSUMPTION (S0-spells)**: Flatten 6.1.x waits for `avoid=1` (W1-world-build): without it the presets would flatten into buildings, so they are `@dummy` until then, and the working Flatten / Raise / Lower of v3 is not in the menu meanwhile (`op=groundlevel` still works from the inbox).

Probes for Maksym's test: `op=spells&status=1` (the `MaxElementsRange` values), `op=bless&list=1` (`GP_Fader` for 5.9).
