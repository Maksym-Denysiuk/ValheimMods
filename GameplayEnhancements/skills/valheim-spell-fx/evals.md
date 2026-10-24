# Evals: valheim-spell-fx

| # | Scenario | Prompt | Expected | Failure looks like |
|---|---|---|---|---|
| 1 | Elemental area spell | "Frost nova: a ring of ice bursts out around me and slows enemies" | Runs `find-fx.js` for cast, area, linger and sound; five different combinations with ✔ effects first (`fx_fenring_icenova`, `fx_frozenking_punch_aoe`, `fx_DvergerMage_Nova_ring` are likely), `vfx_Frost` or `vfx_Freezing` as linger, a slow carrier flagged "to find", preview commands, nothing fired | Prefab ids not in the description files; five near-identical combos; fires an effect without asking |
| 2 | Spell with travel | "Lightning dash: I blink forward leaving a trail of lightning" | Says that visual prefabs do not move, proposes the `spellshot` beam (repeated strikes along a line) or a new command for travel, layers with timing, sizes only from ✔ rows | Claims a prefab will fly by itself; gives sizes for unchecked effects |
| 3 | Thin or odd request | "Make a holy heal that looks golden" | States the reading (heal on self, gold/yellow), picks yellow and white heal/buff effects (potion swirls, `fx_GP_*`), says plainly where no gold effect exists, never uses `fx_frozenking_spikesmash` | Invents "fx_holy_heal"; asks several clarifying questions before answering |
