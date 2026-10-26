---
description: HomeCheat plugin commands that act like spells or area effects in Valheim (earthquake and others) - what each command does, its parameters, how it was built and verified, and how to remove it
---
# Spells (HomeCheat command DLLs)

Effect commands for our own Valheim game and our own hosted world, run through the HomeCheat BepInEx plugin (files dropped in `BepInEx/config/homecheat/inbox/<name>.cmd`, answers in `outbox/<name>.out`). One file per command.

## Contents

<!-- folder:start -->
| Item | What it's about | Last modified |
|---|---|---|
| [earthquake.md](earthquake.md) | earthquake command of the HomeCheat Valheim plugin (2026-10-24) - expanding rings of knockback around the player plus a tilted stone circle that breaks outer-first; parameters, how it works, test results on goblins and trolls, weapon push ranking, files, limits and undo | 2026-10-25 |
| [slime.md](slime.md) | slime command of the HomeCheat Valheim plugin (2026-10-24) - two even rings of Blob poison clouds around the player with a free 2 m gap, Blob attack damage x3 so it kills goblins, the caster immune to the clouds; prefabs and numbers found, parameters, tests (goblins, standing and running), files, limits and undo | 2026-10-25 |
| [spellbar-design.md](spellbar-design.md) | spell bar design and build record - how the G key, the game radial and the hotbar work (IL), the slot map, view v2/v3 as built (2026-10-24) and section 15: spell file v4, spiral, sub-sections, answer chains and the menu tree (2026-10-25, built, not run in game) with the assumptions taken | 2026-10-25 |
| [spellbar.md](spellbar.md) | spell bar of the HomeCheat Valheim plugin (2026-10-24, spell file v4 and in-game test 2026-10-25) - G opens the game's own radial with sections and sub-sections, slot 1 Hotbar = Panel 1-5 and Panel 6-0 (20 cells, a two-row bar F1-F10 and Shift+1..0), Shift versions on Shift+F, one form screen for a spell with questions, failures-only messages; spell file v4 format and form flags, patches, tests, limits and undo | 2026-10-25 |
| [spellshot.md](spellshot.md) | spellshot command of the HomeCheat Valheim plugin (2026-10-24, harpoon 2026-10-25) - a grid of arrows, ballista bolts, lightning beams or pulling harpoons fired along the crosshair; prefabs found, how a Projectile is launched, the thunderbolt beam (lightning strikes with hidden damage carriers), the harpoon type (vanilla SE_Harpooned through the hit's status effect, built but not yet run in game, with its test list), parameters, tests including the push-back measurements, files, limits and undo | 2026-10-25 |
| [teleport.md](teleport.md) | teleport and blink commands of the HomeCheat Valheim plugin (2026-10-26, built, not run in game) - teleport to your last death point, to a spot you left-click on the large map, or next to another player (the game's own portal path, a smoke puff and a little pop where you appear); blink, a 5 m dash along your flat look direction through walls and creatures that shoves everyone on the line aside with tar and no damage, Hugin the raven poofing at both ends; spell group 9 Teleport, Blink on F6; cards, how it works, probes and the test list, limits, undo | 2026-10-26 |
| [traps.md](traps.md) | trap command of the HomeCheat Valheim plugin (2026-10-25, reworked in game the same day) - mines on a floor-tile look laid at the crosshair, in front of you as a scattered matrix, or as a minefield of rings around you; kinds explosive and tar (random per mine), a random blast look per mine, hammer-like push, detonate all, the shared PvP rule taken when laid; host only, cap 6 000; card, first design, how it works, the in-game test (passed: "Mines are good") with what is still untested, limits, undo | 2026-10-25 |
<!-- folder:end -->
