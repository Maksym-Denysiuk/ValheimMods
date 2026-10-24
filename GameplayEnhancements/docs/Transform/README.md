---
description: Valheim transform command (2026-10-24) - HomeCheat command that shows a crow, serpent or fish model on our own player with matching movement (flight, deep swimming), tested in our own world ClaudeTest; what each form does, parameters, how it was built and verified, limits (client-side only), and how to remove it
---
# Transform (HomeCheat command DLL)

`op=transform` for our own Valheim game and our own hosted world, run through the HomeCheat BepInEx plugin (files dropped in `BepInEx/config/homecheat/inbox/<name>.cmd`, answers in `outbox/<name>.out`). Source: [Cmd_transform.cs.txt](Cmd_transform.cs.txt) (rename to `Cmd_transform.cs` to build).

## Contents

<!-- folder:start -->
| Item | What it's about | Last modified |
|---|---|---|
| [body-hiding-research.md](body-hiding-research.md) | research report 2026-10-24 - how cheaters hide a Valheim player's human body from other players while a creature model is shown; the height lie of Server Devcommands' ghost_invisibility, what needs a mod on viewers, leaks, detection and hardening; from source reading, not a live two-client test | 2026-10-24 |
| [Cmd_hidebody.cs.txt](Cmd_hidebody.cs.txt) | source of the hidebody command (Harmony patches on ZDOMan.SendZDOs and the player-list calls; default off) | 2026-10-24 |
| [Cmd_killmobs.cs.txt](Cmd_killmobs.cs.txt) | source of the small `killmobs` helper command (kills loaded creatures within a radius around the local player with ordinary damage; skips players, tamed animals and bosses unless asked); written alongside transform for testing in our own world | 2026-10-24 |
| [Cmd_transform.cs.txt](Cmd_transform.cs.txt) | source of the transform command (classes TransformCommand, TransformState, TransformLanding), C# 5, built with csc 4.0 against the game's assemblies | 2026-10-24 |
| [hidebody.md](hidebody.md) | what hidebody does, the installed-game checks behind it, the result verified with a second player, leaks and undo | 2026-10-24 |
| [transform.md](transform.md) | what transform does per form, parameters, how it works, how it was verified, limits, build and undo | 2026-10-24 |
<!-- folder:end -->
