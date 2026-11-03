---
description: Valheim transform command (2026-10-24) - HomeCheat command that shows a crow, serpent or fish model on our own player with matching movement (flight, deep swimming), tested in our own world ClaudeTest; what each form does, parameters, how it was built and verified, limits (client-side only), and how to remove it
---
# Transform (HomeCheat command DLL)

`op=transform` for our own Valheim game and our own hosted world, run through the HomeCheat BepInEx plugin (files dropped in `BepInEx/config/homecheat/inbox/<name>.cmd`, answers in `outbox/<name>.out`). Source: [src/Features/Transform](../../src/Features/Transform/).

## Contents

<!-- folder:start -->
| Item | What it's about | Last modified |
|---|---|---|
| [body-hiding-research.md](body-hiding-research.md) | research report 2026-10-24 - how cheaters hide a Valheim player's human body from other players while a creature model is shown; the height lie of Server Devcommands' ghost_invisibility, what needs a mod on viewers, leaks, detection and hardening; from source reading, not a live two-client test | 2026-10-24 |
| [hidebody.md](hidebody.md) | what hidebody does, the installed-game checks behind it, the result verified with a second player, leaks and undo | 2026-10-24 |
| [transform.md](transform.md) | transform command for the HomeCheat plugin - crow, serpent and fish forms on our own player (model, flight, deep swimming, fist damage, sounds), parameters, toggle=1 (the same form again turns you back to human, spells 4.1.1-4.1.3), how it works, verification on 2026-10-24 in world ClaudeTest, the forms refactor and in-game test of 2026-10-25 (other players see every form, share and hide on by default), limits and undo | 2026-11-03 |
<!-- folder:end -->
