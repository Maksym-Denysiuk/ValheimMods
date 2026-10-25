---
description: transform command for the HomeCheat plugin - crow, serpent and fish forms on our own player (model, flight, deep swimming, fist damage, sounds), parameters, how it works, verification on 2026-10-24 in world ClaudeTest, the forms refactor and in-game test of 2026-10-25 (other players see every form, share and hide on by default), limits and undo
---
# transform

Game: Valheim (Unity 6 build, BepInEx, HomeCheat plugin), machine `MDENYSIUK-PERS`, world `ClaudeTest` (throwaway). Date 2026-10-24.

## Goal

`transform <type> [duration]` with types `human` (restores everything), `raven`, `serpent`, `fish`: show the creature's model on our own player and give it a matching way of moving, without the game's cheat commands.

## Command

`op=transform&type=human|raven|serpent|fish[&duration=<s>][&speed=<x>][&scale=<x>][&yoff=<m>][&hover=0][&lookdive=0][&share=1][&model=<prefab>]`

Diagnostics: `type=models` (candidate prefabs that exist), `type=effects[&play=1]` (the serpent roar and crow call prefabs the forms use; `play=1` fires both), `type=components&model=<prefab>`, `type=values&model=<prefab>&comp=<type>`, `type=probe` (live state of the model on the player).

## Forms

| Form | Prefab | What it does |
|---|---|---|
| raven | `Crow` (`Raven` and `Hugin` do not exist in this world) | walks as a sitting crow with a hop animation until Space is pressed; Space takes off (sustained 0.8 s climb); in the air Space climbs, Ctrl dives, look pitch also climbs and dives; flight uses `Character.m_flying` (the game's own flight code), with the Crow's own numbers (speed 15, flap 2 s, sail 1.5 s), momentum, banking into turns, pitch with climb; no fall damage while a raven (walking off a ledge in the sitting form included); a dive builds speed (x1.5), a climb costs it (x0.7); nothing touched = sink at 1.4 m/s (`hover=0`: keeps gliding forward at stall speed); block = the Crow's own call; camera 4 m further back and wider view with speed; landing after standing still on the ground for 0.5 s |
| serpent | `Serpent` | swims with the Serpent prefab's own numbers (speed 10, turn 100, acceleration 0.2, depth 1.16); Space/Ctrl (or look pitch) dive and surface by changing `m_swimDepth` (3 m/s, never closer than 1.5 m to the floor); fists hit like `Serpent_attack` (70 slash) x1.5 (1 star) divided by the average unarmed-skill roll; bite animation on attack, taunt animation and `sfx_serpent_taunt` on block; on land lifted 1.3 m, tail bones raised over what is under them, 20 % walk speed |
| fish | `Fish1` | model only, swims x2, dives with Space/Ctrl |
| all | | stamina is refilled every physics step, so running, jumping, swimming and attacking never use it |

## How it works

- The prefab is cloned under an inactive holder so no `Awake` runs (no `ZNetView`, no `Character` in the world list, no AI), then stripped to visuals. The serpent's `Tail` script is kept: the tail wave is a script, not an animator clip.
- Human renderers are hidden every `LateUpdate` (equipment can re-create them) and restored on revert.
- All state is one `TransformState` component on the player; `type=human` destroys it from any DLL build. Speeds, swim depth, fist damage, camera values are restored in `OnDestroy`. A flight that ends in mid-air leaves a `TransformLanding` component that keeps the fall-damage reference at the current height until landing.
- Raven, no fall damage (2026-10-25, Maksym: "remove fall damage"): `Character.UpdateGroundContact` turns the drop below `m_maxAirAltitude` into damage (`Clamp01((drop - 4) / 16) * 100`, players only) and passes it through `SEMan.ModifyFallDamage(baseDamage, ref damage)`, the hook status effects use (`SE_Stats.m_fallDamageModifier`); `Damage()` follows only when the result is above 0. `TransformFallPatch` (postfix, applied in `OnLoad`, active while `TransformCommand.Current` is set) sets it to 0 when the status-effect manager is the local player's and that player's `TransformState` holds a `RavenForm`; every other form, the human and other players are untouched, and the landing itself (effects, ground reset) still runs. Log line `transform: raven fall damage <n> -> 0`. Flight itself never took fall damage: `Character.UpdateFlying` resets `m_maxAirAltitude` every step, so the case this covers is the sitting crow walking or hopping off a ledge. After the form ends in mid-air `TransformLanding` still keeps the fall harmless until landing (unchanged).
- Space and Ctrl are read through `ZInput.GetButton("Jump")` and `("Crouch")` (the game's own bindings), ignored while a menu or chat has the input.

## Verification

Tested in game with Maksym: model and revert for all three forms, serpent tail animation, serpent swimming, dive and surface, roar on block, head and tail on land, raven takeoff, flight, sink speed, walk animation. Not measured: real fist damage per hit; behaviour with other clients.

`op=cheatstate` after the tests: `Achievements.IsCheatedAtAll` True, `Terminal.m_cheat` True, `PlayerProfile.m_usedCheats` True; `IsWorldCheated`, inventory and spawned objects False. The command never calls the console, devcommands or debug fly. No baseline was taken before testing, so the origin of the flags is not attributed to this command.

## Limits

- By default client-side only. The model, animations and sounds exist only on the PC that runs the command; other players see a human who flies or swims fast. The command refuses another player's nickname. Other clients draw players from synced player data, so a player's mesh cannot be replaced for vanilla viewers.
- Another player as a raven (`target=<nickname>`, host only, raven only; verified 2026-10-24 with Claude2): the host spawns a networked `Crow` (no AI) that follows that player's real position (sitting model on the ground, flapping model in the air) and hides their body on the host screen only. The target keeps walking (no flight) and still sees their own body, because their own client is unmodified; the crow is small and its size cannot be synced. `type=human&target=<nickname>` removes it. The component lives on the host's copy of their player and goes away when they leave.
- `share=1` for the serpent (host only, verified with Claude2 on 2026-10-24: serpent and moving tail visible, human body still visible): a real networked `Serpent` is spawned, its `MonsterAI`, `CharacterDrop` and `Humanoid` are removed on the host so nothing simulates it, its body is kept kinematic, and it follows the local model; `inWater` and the `attack`/`taunt` triggers are mirrored through `ZSyncAnimation`. It cannot be hurt; it is a persistent world object removed by `type=human`.
- `share=1` (raven, host only): a real networked `Crow` is spawned by the host game, its wandering script removed, hidden on the host screen, and placed on the local model every frame; the standing/flying model state (`s_landed`) and the flapping animation are written to it. Verified 2026-10-24 with a second player (Claude2) on a vanilla client: while the host stood still only the human was seen (the small sitting crow sat inside the body); once flying, both the crow and the human body were seen. The human body cannot be hidden for them. The crow is a persistent world object (`persistent=True`): `type=human` removes it, a crash can leave it in the save. If it is destroyed it is respawned.
- Hiding the human body from vanilla viewers works with the separate `hidebody` command (see [hidebody.md](hidebody.md), verified with a second player together with `share=1`); research behind it: [body-hiding-research.md](body-hiding-research.md). Before that was found, the game code was checked (2026-10-24) and showed no way to do it through the object's own data: the remote player's visibility is `ZDO.HasOwner()` (`Character.CustomFixedUpdate` -> `SetVisible`), the Player prefab has `ZSyncTransform.m_syncScale = False` and `ZNetView.m_syncInitialScale = False` (also False on the Crow), `Player.m_ghostMode` is local only, and the appearance data (`VisEquipment`) only selects clothing and one of two body models. Routes that remain: a small viewer plugin on the other player's client that hides the renderers of players carrying a custom ZDO key (needs the plugin there), or a shared model big enough to enclose the body (a different, larger prefab). Releasing the player's ZDO owner would hide the body but also stops our own control, so it was rejected.
- Diagnostics: `type=probe` prints the shared crow's owner and persistence and, for every connected peer, whether the host believes that peer has received it (`ZDOMan.m_peers[].m_zdos`).
- A 35 m serpent still clips rocks and buildings in places on land.
- The unarmed-skill compensation makes the expected hit match, not each hit.

## Build and undo

```bash
bash src/build.sh transform
```

`src/build.sh` (extra references in `src/Features/Transform/refs.txt`) stages `HomeCheat.Cmd.transform.v<time>.dll` in `BepInEx/homecheat-commands/`; `op=reload` loads it without a restart. Undo: `op=transform&type=human`, then delete the `HomeCheat.Cmd.transform.*.dll` files and restart the game.

## In-game test 2026-10-25

Maksym's test pass of the phase 4 commands, world `ClaudeTest` hosted on this PC, Claude2 (no mod) as the second player.

- Raven and serpent with `share=1&hide=1`: Claude2 sees the creature, not the human. V ("changes worked").
- Forms without a timer: a form lasts until Human (4.1.4); the 300 s Shift versions were removed.
- Fish: no swim stamina drain while a fish (restored after).
- **Forms refactor** (Maksym: the fish was seen as a human, "same mistake"; what other players see must be inherited): `TransformForm` (abstract: prefabs, flies or swims, stand-in preparation, begin, step, pose) with `RavenForm`, `SerpentForm`, `FishForm`, a new instance per cast. `TransformState` keeps what every form shares: the hidden human, a networked stand-in for every form (was raven and serpent only), hiding the body, stamina, movement modes, revert. `share` and `hide` default on (`share=0`, `hide=0` opt out). Fish stand-in: a networked `Fish1` with `Fish`, `ItemDrop`, `Destructible` stripped on the host; the fish jumps out of the water on block and flops on land.
- Spells: 4.1.1 Raven, 4.1.2 Serpent, 4.1.3 Fish, 4.1.4 Human.

Still untested (B): Claude2 sees the fish; jump and splash; the flop on land; raven and serpent after the refactor.
