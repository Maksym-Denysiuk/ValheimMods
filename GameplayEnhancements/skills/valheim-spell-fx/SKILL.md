---
name: valheim-spell-fx
description: Takes a spell description for our own Valheim game and proposes five effect combinations built from real fx_, vfx_ and sfx_ prefabs (cast, travel, impact, area, linger, sound), each with timing, placement, HomeCheat preview commands and what is still needed to make the spell complete. Use when Maksym describes a spell, ability or magic effect ("a frost nova that roots enemies", "make a lightning dash look cool") and asks which effects to use, how to make it look and sound right, or for spell combinations.
---

# Valheim spell FX composer

Turns one spell description into five complete effect combinations. Every prefab comes from the three description files of our own game, never from memory. Our own game and our own world only.

## Where it runs

The skill and its sources live in the GameplayEnhancements package (repository ValheimMods). `<pkg>` below is that package folder:

| Where | `<pkg>` | the skill |
|---|---|---|
| HomeOS | `references/ValheimCustomMods/GameplayEnhancements` | `.claude/skills/valheim-spell-fx/` (the original) |
| the package on its own | the unpacked `GameplayEnhancements` folder | `<pkg>/skills/valheim-spell-fx/` (a copy written by the HomeOS build; edit the original) |

`scripts/find-fx.js` finds `<pkg>/docs/FX/` by itself from either place; a copy moved anywhere else needs `--fx-dir <pkg>/docs/FX` or `VALHEIM_FX_DIR`.

## Sources (read in this order, only what is needed)

| Source | What it gives | How to read |
|---|---|---|
| `scripts/find-fx.js` | ranked candidates from all three files | run it, do not read it |
| `<pkg>/docs/FX/README.md` | the ~27 effects checked in game with size, the skip list, how to fire one | read the "Tested effects" and "Skip list" parts |
| `<pkg>/docs/FX/{fx,vfx,sfx}_descriptions.md` | every prefab: look, game use, side notes (sound, shake, duration, loops) | grep a prefab id when you need its full row; never load a whole file |
| `<pkg>/docs/Spells/` | spell commands that already exist (`spellshot`, `slime`, `earthquake`) and what carries damage | read the README table; open a command's `.md` only when a combination reuses it |

Description format in the fx_ and vfx_ files: first what you see, then `Game:` what the game uses it for. Side notes hold sound, camera shake, duration, loops and "not placed by any prefab". ✔ after the name = fired in game and checked by Maksym; everything else is inferred from a dump and was never seen.

## Gates (never skipped)

1. **No invented ids.** Every prefab in the answer appears in `find-fx.js` output or in a grepped row. If no effect fits a layer, say so and leave the layer empty.
2. **Nothing fires without a yes.** The answer may contain preview commands, but Claude sends one only after Maksym says to, one effect at a time, through `<pkg>/send-command.ps1` (unique inbox name, withdrawn when the game does not answer). Never a background loop that keeps firing.
3. **Skip list stays out.** No effect marked "DO NOT USE" (today `fx_frozenking_spikesmash`).
4. **Say what is unverified.** Mark each prefab ✔ (checked in game) or `inferred`. Sizes come only from ✔ rows; for the rest write "size unknown".
5. **Own game, own world.** Test in `ClaudeTest`; no servers we do not own.

## Steps

1. **Read the spell.** Pull out: element (fire, ice, lightning, poison, nature, blood, spirit, shadow, earth, water, wind), shape (projectile, beam, nova/ring, strike from the sky, aura on self, area on the ground, summon, buff, heal, shield, teleport), scale (personal, room-size, boss-size), duration (instant, seconds, lingering) and mood (subtle, brutal, holy, creepy). If something is missing, pick the most likely reading and state it in one line; do not ask.
2. **Search.** Run the finder from this skill's folder (in HomeOS `.claude/skills/valheim-spell-fx/`, in the package `skills/valheim-spell-fx/`), three to six times with different word sets:
   ```bash
   node scripts/find-fx.js ice nova
   node scripts/find-fx.js --prefix sfx ice explosion
   node scripts/find-fx.js --prefix vfx --limit 25 ice hit
   ```
   Exit 2 means the description files were not found: pass `--fx-dir`, never answer from memory.
   Words are OR-ed and expanded with synonyms (run with no words to list the groups). One search per layer works best: cast words (`cast charge`), impact words (`hit explosion`), area words (`nova ring`), linger words (the element plus `status` or `loop`), sound words (`--prefix sfx` plus the element).
3. **Fill the layers.** A complete spell has up to six layers; use the ones the spell needs:
   - **Cast** on the caster (hand flare, charge-up, swirl on the ground).
   - **Travel** between caster and target (trail, breath, beam, projectile). Visual prefabs do not move by themselves; travel needs a command that moves or repeats them (see step 5).
   - **Impact** at the target point (burst, blast, splash).
   - **Area** around the impact or the caster (nova ring, shockwave, ground flames).
   - **Linger** after the hit (status look on a character such as `vfx_Burning`, `vfx_Frost`, `vfx_Poison`; ground fog; looping effects need a `duration`).
   - **Sound** for each beat. Many fx_ prefabs already carry a sound (see side notes); add an sfx_ only where a beat is silent or a stronger sound is wanted. sfx_ prefabs are sound only.
4. **Make five different combinations.** Not five copies with one prefab swapped. Cover these angles (rename them to fit the spell):
   1. **Faithful**: closest match to the description, ✔ effects first.
   2. **Epic**: boss-scale version (big ✔ blasts, camera shake, strong light), and say it may block the view.
   3. **Subtle**: small and cheap, good for spamming; no camera shake.
   4. **Twist**: same mechanics, a different element or colour that still fits the mood.
   5. **Layered**: the richest build, four to six layers with exact timing.
5. **Make it complete.** For each combination say what turns the look into a working spell:
   - **Effect carrier**: what does the damage, slow, heal or push. Visual prefabs do nothing by themselves. Reuse what exists in `Spells/` (`spellshot` beam with hidden `lightningAOE` strikes, `slime` Blob clouds, `earthquake` knockback rings) or name the kind of game object needed (an `Aoe`, a `Projectile`, a status effect `SE_*`), flagged as "to find".
   - **Placement and timing**: `self`, `aim`, a ring (`pattern=outward`), or along a line; delays between layers in seconds.
   - **Cleanup**: looping or long effects (side notes "loops", "lasts ~N s") need `duration`.
   - **Who sees it**: most prefabs have a `ZNetView` and show for every player when spawned on the host.
   - **Risks**: camera shake strength, effects that cover the player, prefabs "not placed by any prefab" (never used by the game, more likely to look broken).
   - **Next command work**: if the spell needs more than firing prefabs one by one (movement, repeats, damage), say which existing command to extend or that a new command is needed (a class in `<pkg>/src/Features/<Feature>/`), and hand the chosen combination to `/valheim-spells-craft`, which builds the mechanics, tests and documents them.
6. **Write the answer** in the format below. English in files, chat in Maksym's language.

## Output format

For each combination:

```
### 1. <name>: <one-line idea>
| Beat | t (s) | Prefab | Where | Looks like / sounds like | Status |
|---|---|---|---|---|---|
| Cast | 0.0 | `fx_...` | self | ... | ✔ small / inferred |
| Impact | 0.6 | `fx_...` | aim | ... | ✔ big |
| Area | 0.6 | `vfx_...` | ring 3 x 6 m | ... | inferred, size unknown |
| Linger | 0.8 | `vfx_...` | on target, duration 5 | ... | inferred |
| Sound | 0.6 | `sfx_...` | aim | ... | inferred (clip name) |
Complete it: carrier ..., cleanup ..., risks ...
Preview (fire one at a time, only after a yes):
op=visualeffect&name=_probe&prefab=<id>&at=aim&duration=8
```

Then one short table comparing the five (look, scale, cost, how much is ✔), a recommendation with the reason, and the test order: which unverified prefabs to fire first.

## Preview commands

Needs GameplayEnhancements installed (the `visualeffect` command) and the game running with a world loaded. Send one effect at a time; the script finds the game through Steam, writes `BepInEx/config/homecheat/inbox/<unique-name>.cmd` and prints the answer:

```
powershell -NoProfile -ExecutionPolicy Bypass -File <pkg>/send-command.ps1 "op=visualeffect&name=_probe&prefab=<id>&at=aim&duration=8"
```

- one effect: `op=visualeffect&name=_probe&prefab=<id>&at=aim&duration=8` (`at=self` or `at=x,y,z`; `scale=` for networked prefabs; `lift=` raises it)
- a ring of strikes needs a catalog name of the `visualeffect` command (`lightning`, `lightningred`, `explosion`, `fireball`, `nova`, `thunderstomp`): `op=visualeffect&name=nova&pattern=outward&rings=3&step=6&itemsize=10&delay=3`
- the probe has no delay between layers; to see a combination in order, fire the layers one by one in timing order

## Why v1 exists

v1 (2026-10-24): built on the three description files made that day from a dump of our own game (`fx_` 281, `vfx_` 487, `sfx_` 791 prefabs, 27 checked in game), so spell ideas reuse real prefabs instead of names from memory.

v2 (2026-10-24): the Valheim docs moved into the GameplayEnhancements package (submodule ValheimMods). The skill now runs from HomeOS or from its copy in the package (`<pkg>/skills/`, written by `scripts/valheim/packages/build.js`); the finder locates the FX docs itself and stops with exit 2 instead of silently finding nothing; previews go through `send-command.ps1` instead of a hard-coded game path.

v3 (2026-10-25): after the HomeCheat restructure, command code lives in `src/Features/<Feature>/` (no more `Cmd_<name>.cs`); step 5 hands the mechanics to the new `valheim-spells-craft` skill.
