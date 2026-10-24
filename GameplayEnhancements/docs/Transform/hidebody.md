---
description: hidebody command for the HomeCheat plugin (2026-10-24) - hides our own player's human body from other players on vanilla clients by sending a lifted height for our player object, used with transform share=1 so only the shared creature shows; mechanism, installed-game checks, verified result with a second player, leaks, side effects and undo
---
# hidebody

Our own game, our own hosted world `ClaudeTest`, our own second client `Claude2`. Date 2026-10-24. Source: [src/Features/HideBody](../../src/Features/HideBody/). Background: [body-hiding-research.md](body-hiding-research.md).

## What it does

`op=hidebody&on=1|0`, `op=hidebody&status=1`. Default off. While on, whenever this game sends our player's network object to other peers (`ZDOMan.SendZDOs`), the height in the packet header is 10000 instead of the real one (the real value is restored right after the send, with a finalizer so an error cannot leave it changed). The map pin is also switched off for the send (`ZNet.m_publicReferencePosition`, around `SendPlayerList` and `SendServerSyncPlayerData`). Other players therefore see our body 10 km up, which means nowhere in view. A separately spawned object has its own network record and is not affected, so `transform` with `share=1` (a host-spawned Crow or Serpent that follows our real position) is still drawn.

Switch behaviour: patches are installed on `on=1` and removed on `on=0`; a new build removes the old build's patches (same Harmony id); the game quitting switches it off; every change is logged; `status` prints counters, the local object id and its real position.

## Checked against the installed game (Valheim 1.0.20, IL)

`ZDOMan.SendZDOs(ZDOPeer, bool)` writes `zdo.GetPosition()` (the private field `m_position`) into the packet header per object; it is only called from `SendZDOToPeers2` and `SendAllZDOs`. Writing the field directly (not `SetPosition`) keeps the object in its real sector, so it is still sent to nearby viewers. `ZNet.SetPublicReferencePosition` only sets a field, so it must be restored after each call. Details and code reading: `plan-hidebody.md` in the run folder.

## Verified

With a second player (`Claude2`, vanilla client, 2026-10-24): with hidebody on and `transform&type=raven&share=1`, Claude2 no longer sees our human body and sees the crow; with `type=serpent&share=1` the serpent (model and tail) is what they see. Counters during the test: hundreds of lifted sends, no log errors. `op=cheatstate` was not re-run for this step.

## Leaks and side effects (expected, not measured)

Shouts and pings carry the real head position; our name stays in the player list; chat bubbles anchor to the lifted object; positional voice; other players cannot target or hit the lifted body; monsters owned by other clients may lose us; updates for a far-up object could be delayed when bandwidth is short. Detection on a host: a player object at about y = 10000 while that peer's reference position is normal (see the research note).

## Undo

`op=hidebody&on=0` (everything is restored; the real position was never changed). Leave it off when not testing.
