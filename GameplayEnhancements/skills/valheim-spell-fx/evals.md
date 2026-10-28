# Evals: valheim-spell-fx (independent copy)

Scenarios for this copy, run on paper (read the skill, run the finder, say what the answer would be). Every scenario also fails when the answer asks for a file, folder or tool that this copy does not ship.

| # | Scenario | Prompt | Expected | Failure looks like |
|---|---|---|---|---|
| 1 | Elemental area spell | "Frost nova: a ring of ice bursts out around me and slows enemies" | runs `find-fx.js` for cast, area, linger and sound; five different combinations with ✔ effects first (`fx_fenring_icenova`, `fx_frozenking_punch_aoe`, `fx_DvergerMage_Nova_ring` are likely), `vfx_Frost` or `vfx_Freezing` as linger, a slow carrier flagged "to find", preview commands, nothing fired | prefab ids that are not in the description files; five near-identical combinations; an effect fired without asking |
| 2 | Spell with travel | "Lightning dash: I blink forward leaving a trail of lightning" | says visual prefabs do not move by themselves; proposes the `spellshot` beam (repeated strikes along a line) or a new command for travel; layers with timing; sizes only from ✔ rows | claims a prefab flies by itself; sizes for unchecked effects |
| 3 | Thin or odd request | "Make a holy heal that looks golden" | states its reading (a heal on self, gold or yellow), picks yellow and white heal or buff effects, says plainly where no gold effect exists, never uses `fx_frozenking_spikesmash` | invents `fx_holy_heal`; asks several questions before answering |
| 4 | Finder from the copy | skill installed in `<GameplayEnhancements>/.claude/skills/`; `node scripts/find-fx.js ice nova` from the skill folder | finds `<GameplayEnhancements>/docs/FX/` by itself and prints ranked rows with ✔ first | exit 2; answers from memory instead |
| 5 | FX docs missing | skill copied to `~/.claude/skills/`, started in an unrelated folder | `find-fx.js` exits 2; the answer asks for or passes `--fx-dir <GameplayEnhancements>/docs/FX` and does not answer from memory (gate 1) | a list of prefab names from memory |
| 6 | Preview only after a yes | "show me the first one in game" while the game runs | one effect at a time through `send-command.ps1` with a unique name, only after the owner's yes; looping effects get a `duration`; nothing keeps firing | fires every layer at once; a background loop |
