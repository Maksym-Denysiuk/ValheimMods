---
description: HomeCheat plugin commands that act like spells or area effects in Valheim (earthquake and others) - what each command does, its parameters, how it was built and verified, and how to remove it
---
# Spells (HomeCheat command DLLs)

Effect commands for our own Valheim game and our own hosted world, run through the HomeCheat BepInEx plugin (files dropped in `BepInEx/config/homecheat/inbox/<name>.cmd`, answers in `outbox/<name>.out`). One file per command.

## Contents

<!-- folder:start -->
| Item | What it's about | Last modified |
|---|---|---|
| [earthquake.md](earthquake.md) | earthquake command of the HomeCheat Valheim plugin (2026-10-24) - expanding rings of knockback around the player plus a tilted stone circle that breaks outer-first; parameters, how it works, test results on goblins and trolls, weapon push ranking, files, limits and undo | 2026-10-24 |
| [slime.md](slime.md) | slime command of the HomeCheat Valheim plugin (2026-10-24) - two even rings of Blob poison clouds around the player with a free 2 m gap, Blob attack damage x3 so it kills goblins, the caster immune to the clouds; prefabs and numbers found, parameters, tests (goblins, standing and running), files, limits and undo | 2026-10-24 |
| [spellbar-design.md](spellbar-design.md) | Status: design for approval. | 2026-10-24 |
| [spellbar.md](spellbar.md) | spell bar of the HomeCheat Valheim plugin (2026-10-24) - G opens the game's own radial menu with our spell groups, slot 1 is a Hotbar of 10 cells (Shift+1..5, F1..F5) shown as an on-screen bar, Shift+click places a spell, Shift+RMB empties a cell; spell files, placeholder mode, patches, tests, limits and undo | 2026-10-24 |
| [spellshot.md](spellshot.md) | spellshot command of the HomeCheat Valheim plugin (2026-10-24) - a grid of arrows, ballista bolts or lightning beams fired along the crosshair; prefabs found, how a Projectile is launched, the thunderbolt beam (lightning strikes with hidden damage carriers), parameters, tests including the push-back measurements, files, limits and undo | 2026-10-24 |
<!-- folder:end -->
