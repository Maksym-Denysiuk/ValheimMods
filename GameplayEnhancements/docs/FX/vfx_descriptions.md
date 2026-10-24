---
description: Valheim vfx_ effect prefabs: id, readable name, what it looks like and where the game uses it, for picking effects for actions (hit, heal, explosion, thunder)
---
# VFX descriptions (vfx_)

All 487 `vfx_` visual effect prefabs of Valheim (one row per prefab, sorted by id), written 2026-10-24. Each description says first what you see (colours, shapes, motion, glow), then after "Game:" what the game uses it for; sounds, camera shake and duration are in side notes. ✔ after the name = fired in game and checked by Maksym: the description starts with what he saw and the size he gave. No mark = read from a dump of our own game (particle colours, materials, child object names, lights, which prefabs spawn it), never seen by Claude, so no size is given.

| effect id | effect name | effect description | side notes |
|---|---|---|---|
| `vfx_ carrion_destroyed` | Carrion destroyed | Pale dust, bones and rock chips. Game: an Asksvin carrion or skeleton piece is destroyed. | prefab name has a space after the underscore |
| `vfx_ArcheryTarget_hit` | Archery Target hit | Orange-grey fibres and dust puff. Game: an arrow hits the archery target. |  |
| `vfx_AshPieceDestroyed` | Ash Piece Destroyed | Black-white rock chips and dust. Game: an Ashlands stone building piece is destroyed. | used by ~45 Ashlands pieces |
| `vfx_Ash_Arch2_Broken1_destroyed` | Ash Arch2 Broken1 destroyed | Rock chips plus 1 stone gib. Game: a broken Ashlands arch (type 1) is destroyed. | mesh gibs |
| `vfx_Ash_Arch2_Broken2_destroyed` | Ash Arch2 Broken2 destroyed | Rock chips plus 1 stone gib. Game: a broken Ashlands arch (type 2) is destroyed. | mesh gibs |
| `vfx_Ashpiece_roof_destroyed` | Ashpiece roof destroyed | Rock chips and 16 roof fragments that fall apart. Game: a damaged Ashlands arch roof is destroyed. | mesh gibs |
| `vfx_Ashruin_steepstair_destroyed` | Ashruin steepstair destroyed | Rock chips and 4 stone fragments. Game: the Ashlands steep stair is destroyed. | mesh gibs |
| `vfx_Ashruin_wall3_destroyed` | Ashruin wall3 destroyed | Rock chips and 6 wall fragments. Game: a broken Ashlands ruin wall (3) is destroyed. | mesh gibs |
| `vfx_Ashruin_wall4_destroyed` | Ashruin wall4 destroyed | Rock chips and 6 wall fragments. Game: a broken Ashlands ruin wall (4) is destroyed. | mesh gibs |
| `vfx_Ashruin_wall5_destroyed` | Ashruin wall5 destroyed | Rock chips and 7 wall fragments. Game: a broken Ashlands ruin wall (5) is destroyed. | mesh gibs |
| `vfx_Ashruin_window2_destroyed` | Ashruin window2 destroyed | Rock chips and 9 wall fragments. Game: a broken Ashlands ruin window wall (2) is destroyed. | mesh gibs |
| `vfx_Ashruin_window3_destroyed` | Ashruin window3 destroyed | Rock chips and 8 wall fragments. Game: a broken Ashlands ruin window wall (3) is destroyed. | mesh gibs |
| `vfx_Ashruin_window4_destroyed` | Ashruin window4 destroyed | Rock chips and 5 wall fragments. Game: a broken Ashlands ruin window wall (4) is destroyed. | mesh gibs |
| `vfx_Ashruin_window5_destroyed` | Ashruin window5 destroyed | Rock chips and 6 wall fragments. Game: a broken Ashlands ruin window wall (5) is destroyed. | mesh gibs |
| `vfx_Ashruin_window6_destroyed` | Ashruin window6 destroyed | Rock chips and 6 wall fragments. Game: a broken Ashlands ruin window wall (6) is destroyed. | mesh gibs |
| `vfx_BigBlob_destroyed` | Big Blob destroyed | Falls apart into 96 small mesh pieces (no particles). Game: a big Mork blob is destroyed. | mesh gibs |
| `vfx_BloodDeath` | Blood Death | Red-brown blood burst with skin flakes and decals. Game: generic creature death blood. | not placed by any prefab |
| `vfx_BloodHit` | Blood Hit | Red blood splatter with drops and chunks. Game: generic creature hit blood. | used by the troll trap |
| `vfx_BombBlob_explode_frost` | Bomb Blob explode frost | White-cyan frost splash, smoke and bits. Game: a frost bomb blob bursts. |  |
| `vfx_BombBlob_explode_lava` | Bomb Blob explode lava | Red lava splash, flame spikes, sparks and smoke. Game: a lava bomb blob bursts. |  |
| `vfx_BombBlob_explode_morkhalla` | Bomb Blob explode morkhalla | Cyan splash and smoke. Game: a Morkhalla bomb blob bursts. |  |
| `vfx_BombBlob_explode_poison` | Bomb Blob explode poison | Green poison splash and smoke. Game: a poison bomb blob bursts. |  |
| `vfx_BombBlob_explode_poisonelite` | Bomb Blob explode poisonelite | Red splash and smoke. Game: an elite poison bomb blob bursts. |  |
| `vfx_BombBlob_explode_tar` | Bomb Blob explode tar | Black tar splash and smoke. Game: a tar bomb blob bursts. |  |
| `vfx_BonemassDeath` | Bonemass Death | Brown-olive-green slime, bones, skulls and chunks flying out. Game: Bonemass or a hive dies. |  |
| `vfx_BonemassHit` | Bonemass Hit | Brown-orange-green slime splash with bones. Game: Bonemass or a hive is hit. |  |
| `vfx_BugRepellent` | Bug Repellent | Yellow-blue swirl with a yellow light. Game: while the bug repellent potion is active. |  |
| `vfx_Burning` | Burning | Red-orange flames licking up the body with sparks and a flickering orange glow. Game: a character is on fire (Burning status). | sound: ignite and burn loop; loops |
| `vfx_Burning_blue` | Burning blue | Cyan-white flames on the body with sparks and cyan glow. Game: blue variant of Burning. | sound: burn loop; loops |
| `vfx_Burning_green` | Burning green | Green-white flames on the body with sparks and green-cyan glow. Game: green variant of Burning. | sound: burn loop; loops |
| `vfx_CharredBanner1_destroyed` | Charred Banner1 destroyed | Brown sawdust and 18 cloth pieces. Game: the charred banner (1) is destroyed. | mesh gibs |
| `vfx_CharredBanner2_destroyed` | Charred Banner2 destroyed | Brown sawdust and 10 cloth pieces. Game: the charred banner (2) is destroyed. | mesh gibs |
| `vfx_CharredBanner3_destroyed` | Charred Banner3 destroyed | Brown sawdust and 9 cloth pieces. Game: a charred banner (3) or Morkhalla banner is hit or destroyed. | mesh gibs |
| `vfx_CoalDestroyed` | Coal Destroyed | Black coal dust. Game: a coal pile or flametal stand is destroyed. |  |
| `vfx_CoalHit` | Coal Hit | Black-white coal and rock chips. Game: coal or an Ashlands cliff is hit. |  |
| `vfx_Cold` | Cold | Red-white frost breath and blood-like puff. Game: while the Cold status is on a character. | loops |
| `vfx_ColdBall_Hit` | Cold Ball Hit | White-blue ice burst with a cyan light. Game: a cold ball (hatchling cold spit) lands. |  |
| `vfx_ColdBall_launch` | Cold Ball launch | White-blue frost puff and smoke at the mouth. Game: a cold ball is spat. |  |
| `vfx_Damaged_Karve` | Damaged Karve | Small white fog puff. Game: a Karve boat is damaged. |  |
| `vfx_Damaged_Raft` | Damaged Raft | Small white fog puff. Game: a raft is damaged. |  |
| `vfx_Damaged_VikingShip` | Damaged Viking Ship | Small white fog puff. Game: a longship or trader ship is damaged. |  |
| `vfx_Destroyed_AshlandsShip` | Destroyed Ashlands Ship | White fog and 4 hull pieces (with physics). Game: the Ashlands ship is destroyed. | mesh gibs with rigidbodies |
| `vfx_Destroyed_Karve` | Destroyed Karve | White fog and 6 hull pieces (with physics). Game: a Karve is destroyed. | mesh gibs with rigidbodies |
| `vfx_Destroyed_Raft` | Destroyed Raft | White fog and 9 log pieces (with physics). Game: a raft is destroyed. | mesh gibs with rigidbodies |
| `vfx_Destroyed_VikingShip` | Destroyed Viking Ship | White fog and 8 hull pieces (with physics). Game: a longship is destroyed. | mesh gibs with rigidbodies |
| `vfx_Destroyed_VikingShip_frozen` | Destroyed Viking Ship frozen | White fog and 8 hull pieces (with physics). Game: a frozen ship wreck is destroyed. | mesh gibs with rigidbodies |
| `vfx_DraugrSpawn` | Draugr Spawn | Brown dust and slime splash. Game: a draugr appears. | not placed by any prefab |
| `vfx_FallenWarrior_death` | Fallen Warrior death | White-blue ethereal smoke with a blue light. Game: a fallen warrior dies. |  |
| `vfx_FallenWarrior_hit` | Fallen Warrior hit | White-blue ethereal smoke puff. Game: a fallen warrior is hit. |  |
| `vfx_FernAshlands_destroyed` | Fern Ashlands destroyed | Red-yellow leaves and a smoke puff. Game: an Ashlands fern is cut. |  |
| `vfx_FernAshlands_puff` | Fern Ashlands puff | Small red leaf puff. Game: an Ashlands fern is hit. |  |
| `vfx_FireAddFuel` | Fire Add Fuel | White flame puff with smoke. Game: wood is added to a fire pit. | used by ~9 fire pits |
| `vfx_FireWork_BlackCore` | Fire Work Black Core | Rocket climbs and bursts into a purple-white firework with sparks and smoke, lighting up the sky (white light, 100 m). Game: black-core fireworks from a fire. | sound: rocket launch and magic burst; lasts ~30 s |
| `vfx_FireWork_SurtlingCore` | Fire Work Surtling Core | Rocket climbs and bursts into an orange-white firework with sparks and smoke, lighting up the sky (100 m). Game: surtling-core fireworks from a fire. | sound: rocket launch and spell shot; lasts ~30 s |
| `vfx_FireWork_ThunderStone` | Fire Work Thunder Stone | Rocket climbs and bursts into a blue firework with sparks, lighting up the sky (100 m). Game: thunderstone fireworks from a fire. | sound: rocket launch and electric burst; lasts ~30 s |
| `vfx_FireballHit` | Fireball Hit | White fire and smoke. Game: an imp fireball lands. |  |
| `vfx_Firework_Rocket` | Firework Rocket | White-yellow rocket with spark boost, a big glow and a very strong white light (200 m). Game: a white firework rocket. | lasts ~20 s |
| `vfx_Firework_Rocket_Blue` | Firework Rocket Blue | Blue rocket firework with spark boost and a blue light. | lasts ~20 s |
| `vfx_Firework_Rocket_Cyan` | Firework Rocket Cyan | Cyan rocket firework with spark boost and a cyan light. | lasts ~20 s |
| `vfx_Firework_Rocket_Green` | Firework Rocket Green | Green rocket firework with spark boost and a green light. | lasts ~20 s |
| `vfx_Firework_Rocket_Purple` | Firework Rocket Purple | Purple rocket firework with spark boost and a purple light. | lasts ~20 s |
| `vfx_Firework_Rocket_Red` | Firework Rocket Red | Red rocket firework with spark boost and a red light. | lasts ~20 s |
| `vfx_Firework_Rocket_Yellow` | Firework Rocket Yellow | Yellow rocket firework with spark boost and an orange light. | lasts ~20 s |
| `vfx_FlameBall_launch` | Flame Ball launch | White-orange glowing smoke at the mouth. Game: the Fallen Valkyrie spits a flame ball. |  |
| `vfx_ForgeAddFuel` | Forge Add Fuel | White flame puff with smoke. Game: fuel is added to a forge. | not placed by any prefab |
| `vfx_Freezing` | Freezing | Red-white frost breath and snow flakes. Game: while the Freezing status is on a character. | loops |
| `vfx_Frost` | Frost | Red-white frost flakes and mist. Game: while the Frost status is on a character. | loops |
| `vfx_FrostOrbs` | Frost Orbs | Glassy shield sphere around the player with a soft blue glow. Game: while the frost orbs staff shield is active. | sound: shield hum loop; loops |
| `vfx_GiantMetal_destroyed` | Giant Metal destroyed | Grey rust chips. Game: a giant metal helmet or sword is destroyed. |  |
| `vfx_GoblinShield` | Goblin Shield | Purple shield sphere with a blue light on a goblin shaman. Game: while the shield is active. | loops |
| `vfx_GodExplosion` | God Explosion | White-green burst with rising trails, a forcefield bubble and smoke, green glow. Game: deer god explosion. | sound: large explosion; camera shake 3 within 50 m; not placed by any prefab |
| `vfx_GraustenDestroyed` | Grausten Destroyed | Grey-white rock chips. Game: Ashlands grausten cliff rock is destroyed. |  |
| `vfx_GraustenDestroyed_large` | Grausten Destroyed large | Olive-grey large rocks and pebbles. Game: large Ashlands grausten rock or an arch is destroyed. |  |
| `vfx_GuckSackDestroyed` | Guck Sack Destroyed | Green-red slime splatter and rock chips. Game: a guck sack is destroyed. |  |
| `vfx_GuckSackHit` | Guck Sack Hit | Green-brown slime splatter. Game: a guck sack or Writhan is hit. |  |
| `vfx_GuckSackSmall_Destroyed` | Guck Sack Small Destroyed | Green-red slime splatter. Game: a small guck sack is destroyed. |  |
| `vfx_Harpooned` | Harpooned | A line renderer rope between harpooner and target. Game: while a target is harpooned. | no particles |
| `vfx_HealthUpgrade` | Health Upgrade | White-red-green embers and trails swirling up then exploding outward with smoke, red glow. Game: a health upgrade. | sound: magic burst and dark-magic impact; camera shake 1.5; not placed by any prefab |
| `vfx_HearthAddFuel` | Hearth Add Fuel | White-red flame puff with smoke. Game: wood is added to the hearth. |  |
| `vfx_HitSparks` | Hit Sparks | Small white sparks. Game: a metal or stone object is hit. | used as hit effect by ~310 prefabs and destroy effect by ~30 |
| `vfx_HoleSpawner_destruction` | Hole Spawner destruction | Brown rock chips and 9 pieces with an orange light. Game: a hole spawner is destroyed. | mesh gibs |
| `vfx_HoleSpawner_double_destruction` | Hole Spawner double destruction | Brown rock chips and 18 pieces with orange lights. Game: a double hole spawner is destroyed. | mesh gibs |
| `vfx_ImpDeath` | Imp Death | White fire and smoke puff. Game: a Surtling or Frysling dies. |  |
| `vfx_LastBossGate_destroyed` | Last Boss Gate destroyed | Green-blue ice rocks, pebbles, sparks and smoke with a blue light. Game: the last boss gate breaks. | not placed by any prefab |
| `vfx_LightFoot` | Light Foot | Green-white swirl with a green light. Game: while the light-foot (jump) potion is active. |  |
| `vfx_MarbleDestroyed` | Marble Destroyed | Black-white rock chips. Game: a black marble piece is destroyed. | used by ~50 pieces |
| `vfx_MarbleHit` | Marble Hit | Black-white rock chips. Game: a black marble piece is hit. | used by ~45 pieces |
| `vfx_MeadBzerker` | Mead Bzerker | Red swirl with a red light. Game: while the berserker mead is active. |  |
| `vfx_MeadHasty` | Mead Hasty | White-yellow swirl with a yellow light. Game: while the hasty mead is active. | loops |
| `vfx_MeadSplash` | Mead Splash | Orange mead splash. Game: a mead or potion bottle is drunk or breaks. | used by ~14 potions |
| `vfx_MeadStrength` | Mead Strength | Orange swirl with an orange light. Game: while the strength mead is active. |  |
| `vfx_MeadSwimmer` | Mead Swimmer | Cyan-red swirl with a cyan light. Game: while the swimmer mead is active. |  |
| `vfx_MeadTamer` | Mead Tamer | Pink-red hearts swirl with a pink light. Game: while the tamer mead is active. |  |
| `vfx_MorkhallaStatueDestroyed` | Morkhalla Statue Destroyed | Grey rock chips. Game: a Morkhalla statue piece is destroyed. |  |
| `vfx_MudDestroyed` | Mud Destroyed | Brown-yellow mud splash and rock chips. Game: a mud pile is destroyed or Bonemass punches. |  |
| `vfx_MudHit` | Mud Hit | Brown-green mud splash. Game: a mud pile is hit. |  |
| `vfx_Place_HildirClothesRack` | Place Hildir Clothes Rack | Small white dust puff. Game: a Hildir clothes rack is placed. | sound: poof; not placed by any prefab |
| `vfx_Place_HildirFabricRoll` | Place Hildir Fabric Roll | Small white dust puff. Game: a Hildir fabric roll is placed. | sound: poof; not placed by any prefab |
| `vfx_Place_HildirTableClothes` | Place Hildir Table Clothes | Small white dust puff. Game: Hildir table clothes are placed. | sound: poof; not placed by any prefab |
| `vfx_Place_Karve` | Place Karve | Small white fog puff. Game: a Karve boat is placed. |  |
| `vfx_Place_Raft` | Place Raft | Small white fog puff. Game: a raft is placed. |  |
| `vfx_Place_VikingShip` | Place Viking Ship | Small white fog puff. Game: a longship or trader ship is placed. |  |
| `vfx_Place_bed` | Place bed | Small white fog puff. Game: a bed is placed. |  |
| `vfx_Place_beehive` | Place beehive | Small white fog puff. Game: a beehive, bird nest or sap collector is placed. |  |
| `vfx_Place_brazierceiling01` | Place brazierceiling01 | Small white fog puff. Game: a brazier is placed. |  |
| `vfx_Place_cart` | Place cart | Small white fog puff. Game: a cart, sled, catapult or battering ram is placed. |  |
| `vfx_Place_cauldron` | Place cauldron | Small white fog puff. Game: a cauldron or prep table is placed. |  |
| `vfx_Place_charcoalkiln` | Place charcoalkiln | Small white fog puff. Game: a kiln or shield generator is placed. |  |
| `vfx_Place_chest` | Place chest | Small white fog puff. Game: a chest is placed. | used by ~55 chests |
| `vfx_Place_cookingstation_iron` | Place cookingstation iron | Small white fog puff. Game: an iron cooking station is placed. | not placed by any prefab |
| `vfx_Place_darkwood_gate` | Place darkwood gate | Small white fog puff. Game: a darkwood gate is placed. | not placed by any prefab |
| `vfx_Place_digg` | Place digg | Olive-grey rocks and pebbles. Game: ground is dug with the cultivator or hoe (dig). |  |
| `vfx_Place_forge` | Place forge | Small white fog puff. Game: a forge is placed. |  |
| `vfx_Place_mud_road` | Place mud road | Olive-brown rocks, pebbles and a small shake. Game: a road or path is made or ground cultivated. | camera shake 1 within 30 m |
| `vfx_Place_oven` | Place oven | Small white fog puff. Game: an oven or frost foundry is placed, destroyed or its stage switches. |  |
| `vfx_Place_portal` | Place portal | Small white fog puff. Game: a portal is placed or destroyed. |  |
| `vfx_Place_raise` | Place raise | Olive-brown rocks, pebbles and a small shake. Game: ground is raised. | camera shake 1 within 30 m |
| `vfx_Place_refinery` | Place refinery | Small white fog puff. Game: an Eitr refinery is placed. |  |
| `vfx_Place_replant` | Place replant | Olive-green pebbles. Game: a plant is replanted. |  |
| `vfx_Place_smallitem` | Place smallitem | Small white fog puff. Game: a sapling or a small item is planted. | used by ~12 saplings |
| `vfx_Place_smelter` | Place smelter | Small white fog puff. Game: a blast furnace, fermenter or ward is placed. |  |
| `vfx_Place_spinningwheel` | Place spinningwheel | Small white fog puff. Game: the spinning wheel is placed or destroyed. |  |
| `vfx_Place_stone_floor` | Place stone floor | Small white fog puff. Game: a stone floor is placed. |  |
| `vfx_Place_stone_floor_2x2` | Place stone floor 2x2 | Small white fog puff. Game: a 2x2 stone floor is placed. |  |
| `vfx_Place_stone_wall_2x1` | Place stone wall 2x1 | Small white fog puff. Game: a stone wall piece is placed. | used by ~80 stone pieces |
| `vfx_Place_stone_wall_4x2` | Place stone wall 4x2 | Small white fog puff. Game: a large stone piece (grausten) is placed. |  |
| `vfx_Place_throne02` | Place throne02 | Small white fog puff. Game: a throne is placed, destroyed or switches. |  |
| `vfx_Place_turret` | Place turret | Small white fog puff. Game: a turret is placed. |  |
| `vfx_Place_windmill` | Place windmill | Small white fog puff. Game: a windmill is placed or destroyed. |  |
| `vfx_Place_wood_beam` | Place wood beam | Small white fog puff. Game: a wooden beam or arch is placed. | used by ~30 pieces |
| `vfx_Place_wood_floor` | Place wood floor | Small white fog puff. Game: a wooden floor is placed (also a turret target-set effect). |  |
| `vfx_Place_wood_pole` | Place wood pole | Small white fog puff. Game: a wood pole or most small wooden items are placed. | used by ~235 pieces; the common placement puff |
| `vfx_Place_wood_roof` | Place wood roof | Small white fog puff. Game: a roof piece is placed. | used by ~35 roofs |
| `vfx_Place_wood_stair` | Place wood stair | Small white fog puff. Game: a stair is placed. |  |
| `vfx_Place_wood_wall` | Place wood wall | Small white fog puff. Game: a wooden wall, door or window is placed. | used by ~105 pieces |
| `vfx_Place_wood_wall_half` | Place wood wall half | Small white fog puff. Game: a half or quarter wooden wall is placed. |  |
| `vfx_Place_wood_wall_roof` | Place wood wall roof | Small white fog puff. Game: a wooden wall roof is placed. | not placed by any prefab |
| `vfx_Place_workbench` | Place workbench | Small white fog puff. Game: a workbench or workstation extension is placed. |  |
| `vfx_Poison` | Poison | Red-green drops and smoke. Game: while the Poison status is on a character. | loops |
| `vfx_Potion_eitr_minor` | Potion eitr minor | Purple-pink swirl with a pink light. Game: while an Eitr potion is active. |  |
| `vfx_Potion_health_medium` | Potion health medium | Red swirl with a red light. Game: while a health potion is active. | used by all health potions |
| `vfx_Potion_stamina_medium` | Potion stamina medium | Yellow swirl with a yellow light. Game: while a stamina potion is active. | used by all stamina potions |
| `vfx_ProjectileHit` | Projectile Hit | Brown-olive-green slime splash. Game: a Bonemass thrown projectile lands. |  |
| `vfx_RockDestroyed` | Rock Destroyed | Grey-white rock chips and dust. Game: a rock or stone piece is destroyed. | used by ~65 pieces |
| `vfx_RockDestroyed_Obsidian` | Rock Destroyed Obsidian | Grey-white rock chips with dark obsidian shards. Game: obsidian is mined out. |  |
| `vfx_RockDestroyed_large` | Rock Destroyed large | Olive-grey large rocks and pebbles. Game: a big rock or ore vein is destroyed. | used by ~28 prefabs |
| `vfx_RockDestroyed_marble` | Rock Destroyed marble | Large rocks and pebbles. Game: giant marble ribs or skull are destroyed. |  |
| `vfx_RockHit` | Rock Hit | Grey-white rock chips and dust. Game: stone is hit. | used as hit effect by ~220 prefabs; also spawned inside many other effects |
| `vfx_RockHit_Marble` | Rock Hit Marble | Grey-white marble chips. Game: giant ribs or skull are hit. |  |
| `vfx_RockHit_Obsidian` | Rock Hit Obsidian | Grey-white rock chips with obsidian shards. Game: obsidian is hit. |  |
| `vfx_SawDust` | Saw Dust | Yellow sawdust puff. Game: wood is hit or destroyed. | the most common wood hit effect, used by ~530 prefabs |
| `vfx_SawDust_Ashlands` | Saw Dust Ashlands | Black sawdust puff. Game: an Ashlands tree is hit. |  |
| `vfx_SawDust_abomination` | Saw Dust abomination | Yellow sawdust puff. | not placed by any prefab |
| `vfx_ShadowPerson_death` | Shadow Person death | White-grey ethereal smoke with a grey light. Game: a shadow person dies. |  |
| `vfx_ShadowPerson_hit` | Shadow Person hit | White-grey ethereal smoke puff. Game: a shadow person is hit. |  |
| `vfx_Slimed` | Slimed | White slime splash overlay. Game: while the Slimed status is on a character. | loops |
| `vfx_Smoked` | Smoked | Red-brown small smoke. Game: while the Smoked status is on a character. | loops |
| `vfx_StaffShield` | Staff Shield | Red shield sphere with a red light around a player. Game: while the staff shield is active. | loops |
| `vfx_StaminaUpgrade` | Stamina Upgrade | White-yellow-green embers and trails swirling up then exploding outward with smoke, yellow glow. Game: a stamina upgrade. | sound: magic burst and dark-magic impact; camera shake 1.5; not placed by any prefab |
| `vfx_Tared` | Tared | Red-black tar drips and splats. Game: while the Tared status is on a character. | loops |
| `vfx_TrainingDummy_destruction` | Training Dummy destruction | Brown wood chips and smoke. Game: the training dummy is destroyed. |  |
| `vfx_TrainingDummy_hit` | Training Dummy hit | Brown wood chips and smoke. Game: the training dummy is hit. |  |
| `vfx_TrollFrost_Death` | Troll Frost Death | Grey smoke and pebbles. Game: a frost troll dies. |  |
| `vfx_TrollPheromones` | Troll Pheromones | Cyan-blue-green swirl with hearts and a cyan light. Game: while the troll pheromones potion is active. | loops |
| `vfx_UndeadBurn` | Undead Burn | Red-cyan-white flames on the body with rising trails, green glow. Game: while the Spirit (undead burn) status is on a character. | sound: fire loop; loops |
| `vfx_WaterImpact_Karve` | Water Impact Karve | White water splash and a strong camera shake. Game: a Karve hits the water. | camera shake 3 within 15 m |
| `vfx_WaterImpact_Raft` | Water Impact Raft | White water splash and a strong camera shake. Game: a raft hits the water. | camera shake 3 within 15 m |
| `vfx_WaterImpact_VikingShip` | Water Impact Viking Ship | White water splash and a strong camera shake. Game: a longship hits the water. | camera shake 3 within 15 m |
| `vfx_Wet` | Wet | White water drops. Game: while the Wet status is on a character. | loops |
| `vfx_WishbonePing` | Wishbone Ping | Red-cyan-white trails and flames with a green light. Game: the wishbone's ping to treasure. |  |
| `vfx_arbalest_fire` | Arbalest fire | White-grey muzzle smoke, spikes and shockwave. Game: a crossbow is fired. |  |
| `vfx_archerytarget_bullseye` | Archerytarget bullseye | Orange-white sparks, a ring and a shockwave distortion on a bullseye arrow hit on the archery target. |  |
| `vfx_archerytarget_bullseye_double` | Archerytarget bullseye double | Same burst as bullseye for a double bullseye. |  |
| `vfx_archerytarget_bullseye_quint` | Archerytarget bullseye quint | Longer orange-white-pink burst, a ring and a shockwave for a full (quintuple) bullseye. | lasts ~10 s |
| `vfx_arrowhit` | Arrowhit | Small pale puff of dust. Game: an arrow or bolt hits. | used by ~75 projectiles |
| `vfx_ashland_gate_destruction` | Ashland gate destruction | Falls apart into 18 mesh pieces (no particles). Game: the Ashlands fortress gate is destroyed. | mesh gibs |
| `vfx_ashland_ruin_wall_windows_broken_4x6_destruction` | Ashland ruin wall windows broken 4x6 destruction | Rock chips and 22 wall fragments. Game: a broken Ashlands ruin window wall is destroyed. | mesh gibs |
| `vfx_ashland_wall_2x2_corner_destruction` | Ashland wall 2x2 corner destruction | Falls apart into 11 mesh pieces. Game: an Ashlands corner wall is destroyed. | mesh gibs |
| `vfx_ashland_wall_2x2_destruction` | Ashland wall 2x2 destruction | Falls apart into 8 mesh pieces. Game: an Ashlands fortress wall or pillar is destroyed. | mesh gibs |
| `vfx_ashlandsbow_blood_fire` | Ashlandsbow blood fire | Dark-red muzzle smoke and spikes. Game: the Ashlands blood bow is shot. |  |
| `vfx_ashlandsbow_lightning_fire` | Ashlandsbow lightning fire | Grey-cyan muzzle smoke and spikes. Game: the Ashlands storm bow is shot. |  |
| `vfx_ashlandsbow_nature_fire` | Ashlandsbow nature fire | Green-white muzzle smoke and spikes. Game: the Ashlands root bow is shot. |  |
| `vfx_ashlandslogdestroyed` | Ashlandslogdestroyed | Black sawdust and smoke puff. Game: an Ashlands log is destroyed. |  |
| `vfx_ashlandslogdestroyed_half` | Ashlandslogdestroyed half | Black sawdust and smoke puff. Game: a half Ashlands log is destroyed. |  |
| `vfx_ashlandstreecut` | Ashlandstreecut | White-black-orange sparks, smoke and twigs. Game: an Ashlands tree is cut down. |  |
| `vfx_aspect_summoned_prespawn` | Aspect summoned prespawn | White-blue-cyan fire vortex spinning up from the ground with smoke, embers and splashes, cyan glow. Game: a Frozen King aspect projectile lands and summons. | sound: spirit summon; camera shake 2 within 50 m |
| `vfx_auto_pickup` | Auto pickup | Small white sparkle puff. Game: an item is auto-picked up. |  |
| `vfx_bar_ancientmetal_stack_destroyed` | Bar ancientmetal stack destroyed | Pale dust and 16 bar pieces fall apart. Game: an ancient metal bar stack is destroyed. | mesh gibs |
| `vfx_bar_blackmetal_stack_destroyed` | Bar blackmetal stack destroyed | Pale dust and 16 bar pieces fall apart. Game: a black metal bar stack is destroyed. | mesh gibs |
| `vfx_bar_bronze_stack_destroyed` | Bar bronze stack destroyed | Pale dust and 16 bar pieces fall apart. Game: a bronze bar stack is destroyed. | mesh gibs |
| `vfx_bar_copper_stack_destroyed` | Bar copper stack destroyed | Pale dust and 16 bar pieces fall apart. Game: a copper bar stack is destroyed. | mesh gibs |
| `vfx_bar_flametal_stack_destroyed` | Bar flametal stack destroyed | Pale dust and 16 bar pieces fall apart. Game: a flametal bar stack is destroyed. | mesh gibs |
| `vfx_bar_gold_stack_destroyed` | Bar gold stack destroyed | Pale dust and 16 bar pieces fall apart. Game: a gold bar stack is destroyed. | mesh gibs |
| `vfx_bar_iron_stack_destroyed` | Bar iron stack destroyed | Pale dust and 16 bar pieces fall apart. Game: an iron bar stack is destroyed. | mesh gibs |
| `vfx_bar_silver_stack_destroyed` | Bar silver stack destroyed | Pale dust and 16 bar pieces fall apart. Game: a silver bar stack is destroyed. | mesh gibs |
| `vfx_bar_tin_stack_destroyed` | Bar tin stack destroyed | Pale dust and 16 bar pieces fall apart. Game: a tin bar stack is destroyed. | mesh gibs |
| `vfx_barley_destroyed` | Barley destroyed | Small pale yellow dust puff. Game: barley or flax is picked or cut. |  |
| `vfx_barnacle_destroyed` | Barnacle destroyed | Grey-yellow barnacle pieces, flakes and slime. Game: a leviathan barnacle is destroyed. |  |
| `vfx_barnacle_hit` | Barnacle hit | Yellow-pale barnacle flakes. Game: a leviathan barnacle is hit. |  |
| `vfx_barrle_destroyed` | Barrle destroyed | Brown-grey sawdust and 7 plank pieces. Game: a barrel is destroyed. | mesh gibs; the prefab name has a typo (barrle) |
| `vfx_beech_cut` | Beech cut | White-yellow leaves, twigs and smoke puffs. Game: a beech tree is cut down. |  |
| `vfx_beech_small1_destroy` | Beech small1 destroy | White-olive leaves and twigs. Game: a small beech (1) is destroyed. |  |
| `vfx_beech_small2_destroy` | Beech small2 destroy | White-olive leaves and twigs. Game: a small beech (2) is destroyed. |  |
| `vfx_beechlog_destroyed` | Beechlog destroyed | Yellow-olive sawdust and smoke. Game: a beech log is destroyed. |  |
| `vfx_beechlog_half_destroyed` | Beechlog half destroyed | Yellow-olive sawdust and smoke. Game: a half beech log is destroyed. | not placed by any prefab |
| `vfx_beehive_destroyed` | Beehive destroyed | Grey-yellow hive bits, honey and a smoke puff. Game: a beehive is destroyed. |  |
| `vfx_beehive_hit` | Beehive hit | Grey-yellow hive bits, honey and a smoke puff. Game: a beehive is hit. |  |
| `vfx_birch1_aut_cut` | Birch1 aut cut | White-yellow leaves and twigs. Game: an autumn birch (1) is cut down. |  |
| `vfx_birch1_cut` | Birch1 cut | White-yellow leaves and twigs. Game: a birch (1) is cut down. |  |
| `vfx_birch2_aut_cut` | Birch2 aut cut | White-yellow leaves and twigs. Game: an autumn birch (2) is cut down. |  |
| `vfx_birch2_cut` | Birch2 cut | White-yellow leaves and twigs. Game: a birch (2) is cut down. |  |
| `vfx_bjorn_groundslam` | Bjorn groundslam | Grey-brown rocks, dust and a ring. Game: Bjorn's (bear) ground slam attack. |  |
| `vfx_blackice_destroyed` | Blackice destroyed | Green-blue ice sparks, smoke and sparkles with a blue light. Game: black ice is destroyed. |  |
| `vfx_blastfurance_addfuel` | Blastfurance addfuel | White flame puff, smoke and cinders. Game: fuel is added to the blast furnace. | prefab name has a typo (furance) |
| `vfx_blastfurnace_addore` | Blastfurnace addore | White flame puff, smoke and cinders. Game: ore is added to the blast furnace. |  |
| `vfx_blastfurnace_produce` | Blastfurnace produce | White flame puff, smoke and embers. Game: the blast furnace produces a bar. |  |
| `vfx_blob_attack` | Blob attack | Black-green flies, slime splash and smoke. Game: blob area attack. |  |
| `vfx_blob_death` | Blob death | Brown-green slime chunks, bones and decals. Game: a blob dies. |  |
| `vfx_blob_frost_attack` | Blob frost attack | Pale cyan slime splash and bits. Game: frost blob attack. |  |
| `vfx_blob_frost_death` | Blob frost death | Cyan slime chunks and decals. Game: a frost blob dies. |  |
| `vfx_blob_frost_hit` | Blob frost hit | Cyan slime splash. Game: a frost blob is hit. |  |
| `vfx_blob_hit` | Blob hit | Brown-green slime chunks and decals. Game: a blob is hit. |  |
| `vfx_blobelite_attack` | Blobelite attack | Black-green flies, slime splash and smoke. Game: elite blob area attack. |  |
| `vfx_blobmork_attack` | Blobmork attack | Black-cyan flies, slime splash and smoke. Game: Mork blob area attack. |  |
| `vfx_blobmork_death` | Blobmork death | Cyan-white slime chunks, bones and decals. Game: a Mork blob dies. |  |
| `vfx_blobmork_hit` | Blobmork hit | Cyan-white slime chunks. Game: a Mork blob is hit. |  |
| `vfx_blobtar_death` | Blobtar death | Black tar chunks, fire and decals. Game: a tar blob dies. |  |
| `vfx_blobtar_hit` | Blobtar hit | Black tar splash and decal. Game: a tar blob is hit. |  |
| `vfx_blocked` | Blocked | Pale yellow-white dust and wave. Game: a hit is blocked with a weapon or shield. | used by ~270 weapons |
| `vfx_boar_birth` | Boar birth | Small red slime splash. Game: a young animal is born. | used by Asksvin, boar, lox, wolf |
| `vfx_boar_death` | Boar death | Red-brown blood puff with skin flakes. Game: a boar dies. |  |
| `vfx_boar_hit` | Boar hit | Red blood splatter with drops and chunks. Game: a boar, piglet or chicken is hit. |  |
| `vfx_boar_love` | Boar love | Red hearts. Game: a boar or wolf shows love (breeding). |  |
| `vfx_bone_stack_destroyed` | Bone stack destroyed | Falls apart into 32 bone mesh pieces (no particles). Game: a bone stack is destroyed. | mesh gibs |
| `vfx_bonemawserpent_death` | Bonemawserpent death | Yellow-orange-red flesh chunks, meat lumps and blood. Game: a Bonemaw Serpent dies. |  |
| `vfx_bonepile_destroyed` | Bonepile destroyed | Pale dust, bones and a skull. Game: a bone pile spawner is destroyed. |  |
| `vfx_bones_pick` | Bones pick | Grey-pale dust puff. Game: crypt remains (bones) are picked up. |  |
| `vfx_bonfire_AddFuel` | Bonfire Add Fuel | White flame puff with smoke. Game: wood is added to a bonfire. |  |
| `vfx_bonfire_destroyed` | Bonfire destroyed | White smoke and cinders. Game: a bonfire is destroyed. |  |
| `vfx_bow_fire` | Bow fire | Blue-white muzzle puff and smoke spikes. Game: a bow is shot. | used by ~17 bows |
| `vfx_bowl_AddItem` | Bowl Add Item | White-pink-red flame puff with trails. Game: an item is put in a bowl. | not placed by any prefab |
| `vfx_bush2_e_hit` | Bush2 e hit | White-yellow leaves and dust. Game: a Bush02 is hit. |  |
| `vfx_bush2_en_destroyed` | Bush2 en destroyed | White-yellow leaves, twigs and a smoke puff. Game: a Bush02 is destroyed. |  |
| `vfx_bush_destroyed` | Bush destroyed | Green-yellow leaves and a smoke puff. Game: a bush (blueberry, cloudberry etc.) is destroyed. |  |
| `vfx_bush_destroyed_heath` | Bush destroyed heath | Green-yellow leaves and a smoke puff. Game: a heath bush is destroyed. |  |
| `vfx_bush_leaf_puff` | Bush leaf puff | Small green leaf puff. Game: a bush is hit. |  |
| `vfx_bush_leaf_puff_heath` | Bush leaf puff heath | Small green leaf puff. Game: a heath bush is hit. |  |
| `vfx_cartograpertable_write` | Cartograpertable write | Small white-grey cinders and smoke over the table. Game: the cartography table records the map. | sound: record discoveries; prefab name has a typo (cartograper) |
| `vfx_catapult_legdown` | Catapult legdown | Grey smoke puff. Game: the catapult leg goes down. |  |
| `vfx_catapult_load` | Catapult load | Grey smoke puff. Game: the catapult is loaded. |  |
| `vfx_catapult_release` | Catapult release | Grey-black smoke and dust. Game: the catapult is released. |  |
| `vfx_charredbanner_destroyed` | Charredbanner destroyed | Falls apart into 8 cloth pieces (no particles). Game: a charred banner is destroyed. | mesh gibs; not placed by any prefab |
| `vfx_charredcross_spawner_destroyed` | Charredcross spawner destroyed | Black coal dust and 6 pieces. Game: the charred cross spawner is destroyed. | mesh gibs |
| `vfx_cloth_hanging_destroyed` | Cloth hanging destroyed | Falls apart into 8 cloth pieces. Game: hanging cloth is destroyed. | mesh gibs |
| `vfx_clubhit` | Clubhit | Small pale dust puff on a blunt hit. | used as hit effect by ~150 weapons |
| `vfx_coin_pile_destroyed` | Coin pile destroyed | White glitter and coins. Game: a coin pile is picked or destroyed. | mesh gibs |
| `vfx_coin_stack_destroyed` | Coin stack destroyed | Coins scatter. Game: a treasure stack or chest is destroyed or hit. | mesh gibs |
| `vfx_cooking_station_transform` | Cooking station transform | Small pale dust puff. Game: a cooking station finishes, burns or an item is added or picked. |  |
| `vfx_corpse_destruction_large` | Corpse destruction large | Big white-red-pink spark burst with trails, smoke and a shockwave ring, strong pink glow (30 m). Game: a boss corpse vanishes. | sound: despawn; camera shake 2 within 60 m |
| `vfx_corpse_destruction_medium` | Corpse destruction medium | White-red-pink spark burst with trails, smoke and a shockwave ring, pink glow. Game: a medium corpse vanishes. | sound: despawn; camera shake 1.5 |
| `vfx_corpse_destruction_small` | Corpse destruction small | Small white-red spark burst with trails and smoke, faint pink glow. Game: a small corpse vanishes. | sound: despawn; camera shake 1; used by ~30 ragdolls |
| `vfx_crate_destroyed` | Crate destroyed | Brown-grey sawdust and 13 plank pieces. Game: a crate is destroyed. | mesh gibs; not placed by any prefab |
| `vfx_creature_soothed` | Creature soothed | Yellow hearts. Game: a creature is soothed (calmed). |  |
| `vfx_creep_hangingdetroyed` | Creep hangingdetroyed | Green slime sprites and 14 pieces. Game: hanging creep (growth) is destroyed. | mesh gibs; prefab name has a typo (detroyed) |
| `vfx_crossbow_blood_fire` | Crossbow blood fire | Dark-red muzzle smoke, spikes and a wave. Game: a blood crossbow is fired. |  |
| `vfx_crossbow_lightning_fire` | Crossbow lightning fire | Grey-cyan muzzle smoke, spikes and a wave. Game: a lightning crossbow is fired. |  |
| `vfx_crossbow_nature_fire` | Crossbow nature fire | Green-white muzzle smoke, spikes and a wave. Game: a nature crossbow is fired. |  |
| `vfx_crow_death` | Crow death | Black feathers, pale puff and a drop of blood. Game: a crow or fish dies. | used by crow and ~13 fish |
| `vfx_crypt_skeleton_chest_destroyed` | Crypt skeleton chest destroyed | Brown sawdust and stone chest pieces. Game: a crypt chest is destroyed. | mesh gibs |
| `vfx_damaged_cart` | Damaged cart | White-yellow fog and sawdust. Game: a cart, sled or catapult is damaged or destroyed. |  |
| `vfx_darkland_groundfog` | Darkland groundfog | Dark grey low swamp mist on the ground. | loops, no owner |
| `vfx_deer_death` | Deer death | Red-brown blood puff with skin flakes. Game: a deer or seal dies. |  |
| `vfx_deer_hit` | Deer hit | Red blood splatter. Game: a deer, seal or Eikthyr is hit. |  |
| `vfx_dragon_coldbreath` | Dragon coldbreath | Cyan-white cold breath cloud with spray, a cyan light and flakes. Game: dragon (Moder) cold breath trail. | lasts up to ~18 s |
| `vfx_dragon_death` | Dragon death | Big red-blue-white gore burst with cold spray, smoke and sparks, cyan glow. Game: the dragon Moder dies. | sound: flame burst; camera shake 2 within 60 m |
| `vfx_dragon_hurt` | Dragon hurt | Red blood splatter. Game: the dragon is hurt. |  |
| `vfx_dragon_ice_hit` | Dragon ice hit | White-blue ice burst with a cyan light. Game: a dragon ice projectile lands. |  |
| `vfx_dragonegg_destroy` | Dragonegg destroy | Red-pink-cyan shell flakes and drops. Game: a dragon egg cup is destroyed. |  |
| `vfx_draugr_death` | Draugr death | Brown-green chunks and decals. Game: a draugr dies. |  |
| `vfx_draugr_hit` | Draugr hit | Brown-green chunks and decals. Game: a draugr is hit. |  |
| `vfx_draugrpile_destroyed` | Draugrpile destroyed | Green-red slime and rock chips. Game: a draugr pile is destroyed. |  |
| `vfx_draugrpile_hit` | Draugrpile hit | Brown-green-red chunks. Game: a draugr pile is hit. |  |
| `vfx_dvergpost_destroyed` | Dvergpost destroyed | Brown sawdust and 20 stone pieces. Game: a black marble post is destroyed. | mesh gibs |
| `vfx_dvergr_beam_destroyed` | Dvergr beam destroyed | Brown sawdust and 2 pole pieces. Game: a Dvergr beam is destroyed. | mesh gibs; not placed by any prefab |
| `vfx_dvergr_curtain_banner_destroyed` | Dvergr curtain banner destroyed | Falls apart into 8 cloth pieces. Game: a Dvergr banner or curtain is destroyed. | mesh gibs |
| `vfx_dvergr_curtain_banner_destroyed_horisontal` | Dvergr curtain banner destroyed horisontal | Falls apart into 4 cloth pieces. Game: a horizontal Dvergr curtain is destroyed. | mesh gibs |
| `vfx_dvergr_pole_destroyed` | Dvergr pole destroyed | Brown sawdust and 6 pole pieces. Game: a Dvergr pole is destroyed. | mesh gibs; not placed by any prefab |
| `vfx_dvergr_stake_destroyed` | Dvergr stake destroyed | Brown sawdust and 2 stake pieces. Game: a Dvergr stake is destroyed. | mesh gibs; not placed by any prefab |
| `vfx_dvergr_stakewall_destroyed` | Dvergr stakewall destroyed | Brown sawdust and 5 stakes. Game: a Dvergr stake wall is destroyed. | mesh gibs; not placed by any prefab |
| `vfx_dvergr_wood_wall02_destroyed` | Dvergr wood wall02 destroyed | Falls apart into 8 wood pieces. Game: a Dvergr wall (02) is destroyed. | mesh gibs; not placed by any prefab |
| `vfx_dvergr_wood_wall03_destroyed` | Dvergr wood wall03 destroyed | Falls apart into 30 wood pieces. Game: a Dvergr wall (03) is destroyed. | mesh gibs; not placed by any prefab |
| `vfx_dvergr_wood_wall04_destroyed` | Dvergr wood wall04 destroyed | Falls apart into 20 wood pieces and nails. Game: a Dvergr town wall is destroyed. | mesh gibs |
| `vfx_dvergrchair_destroyed` | Dvergrchair destroyed | Brown sawdust and 7 chair pieces. Game: a Dvergr chair is destroyed. | mesh gibs; not placed by any prefab |
| `vfx_dvergrcreep_beam_destroyed` | Dvergrcreep beam destroyed | Brown sawdust and 2 pole pieces. Game: a Dvergr creep beam is destroyed. | mesh gibs |
| `vfx_dvergrcreep_pole_destroyed` | Dvergrcreep pole destroyed | Brown sawdust and 6 pole pieces. Game: a Dvergr creep pole is destroyed. | mesh gibs |
| `vfx_dvergrcreep_support_destroyed` | Dvergrcreep support destroyed | Brown sawdust and 10 pieces. Game: a Dvergr creep support is destroyed. | mesh gibs |
| `vfx_dvergrcreep_wood_wall02_destroyed` | Dvergrcreep wood wall02 destroyed | Falls apart into 6 wood pieces. Game: a Dvergr creep wall (02) is destroyed. | mesh gibs |
| `vfx_dvergrcreep_wood_wall03_destroyed` | Dvergrcreep wood wall03 destroyed | Falls apart into 24 wood pieces. Game: a Dvergr creep wall (01) is destroyed. | mesh gibs |
| `vfx_dvergrstool_destroyed` | Dvergrstool destroyed | Brown sawdust and 5 pieces. Game: a Dvergr stool is destroyed. | mesh gibs; not placed by any prefab |
| `vfx_dvergrtable_destroyed` | Dvergrtable destroyed | Brown sawdust and 5 pieces. Game: a Dvergr table is destroyed. | mesh gibs; not placed by any prefab |
| `vfx_edge_clouds` | Edge clouds | Dark grey distant fog at the world edge. | loops; not placed by any prefab |
| `vfx_eikthyr_death` | Eikthyr death | Red-brown blood, skin flakes and head splatter. Game: Eikthyr dies. |  |
| `vfx_fenring_cultist_death` | Fenring cultist death | Red blood with hair flakes and a burst of fire and embers, orange glow. Game: a Fenring cultist dies. | sound: flame loop |
| `vfx_fenring_cultist_hildir_death` | Fenring cultist hildir death | Red blood with hair flakes and a burst of fire and embers, blue glow. Game: a Hildir Fenring cultist dies. | sound: flame loop |
| `vfx_fenring_death` | Fenring death | Red-grey blood and hair puff. Game: a Fenring dies. |  |
| `vfx_fenring_hurt` | Fenring hurt | Red blood splatter. Game: a Fenring or cultist is hurt. |  |
| `vfx_fenrirhide_hanging_destroyed` | Fenrirhide hanging destroyed | Falls apart into 9 hide pieces. Game: a hanging Fenrir hide is destroyed. | mesh gibs |
| `vfx_fermenter_add` | Fermenter add | Yellow-orange mead splash. Game: an ingredient is added to the fermenter. |  |
| `vfx_fermenter_tap` | Fermenter tap | Orange mead splash. Game: the fermenter is tapped. |  |
| `vfx_fir_oldlog` | Fir oldlog | Yellow-olive sawdust. Game: an old fir log or Ashlands branch is destroyed. |  |
| `vfx_firetree_regrow` | Firetree regrow | Small yellow dust puff. Game: an Ashlands tree regrows. |  |
| `vfx_firetreecut` | Firetreecut | White-yellow branches, leaves and smoke puffs. Game: a fir or pine tree is cut down. |  |
| `vfx_firetreecut_dead` | Firetreecut dead | White-yellow dead branches and smoke. Game: a dead small fir is cut. |  |
| `vfx_firetreecut_dead_abomination` | Firetreecut dead abomination | White-yellow-brown dead branches and mud with a rock smash and a strong shake. Game: the Abomination dies. | camera shake 2.5 within 70 m |
| `vfx_firetreecut_dead_snow` | Firetreecut dead snow | White-yellow branches and smoke. Game: a snowy dead fir is cut. | not placed by any prefab |
| `vfx_firetreecut_snow` | Firetreecut snow | White-yellow snowy branches and smoke. Game: a snow fir tree is cut down. |  |
| `vfx_firetreecut_snow_FrostTrollDeath` | Firetreecut snow Frost Troll Death | Snowy branches and smoke. Game: a frost troll dies. |  |
| `vfx_firetreecut_snow_FrostTrollThrow` | Firetreecut snow Frost Troll Throw | Snowy branches and smoke. Game: the frost troll throws something. |  |
| `vfx_firetreecut_snow_small` | Firetreecut snow small | Snowy branches and smoke. Game: a small snow fir is cut down. |  |
| `vfx_firlogdestroyed` | Firlogdestroyed | Yellow-olive sawdust and smoke. Game: a log is destroyed. | used by ~8 logs |
| `vfx_firlogdestroyed_half` | Firlogdestroyed half | Yellow-olive sawdust and smoke. Game: a half log is destroyed. | used by ~8 half logs |
| `vfx_flintpile_destroyed` | Flintpile destroyed | Pale yellow dust and 18 flint pieces. Game: a flint pile is destroyed. | mesh gibs |
| `vfx_foresttroll_hit` | Foresttroll hit | Blue-red-brown splatter. Game: a troll is hit. |  |
| `vfx_frostarrow_hit` | Frostarrow hit | Pale-white-blue ice burst with a cyan light. Game: a frost arrow lands. |  |
| `vfx_frostcore_pick` | Frostcore pick | Grey-pale cyan crystal dust. Game: a frost core is picked. |  |
| `vfx_frostfoundry_transform` | Frostfoundry transform | Blue glow flash. Game: the Frost Foundry finishes an item. |  |
| `vfx_frostkiln_addore` | Frostkiln addore | White flame puff with smoke. Game: ore is added to the frost kiln. |  |
| `vfx_frostkiln_produce` | Frostkiln produce | Cyan smoke puff. Game: the frost kiln produces. |  |
| `vfx_frosttroll_hit` | Frosttroll hit | Blue-red splatter. Game: a frost troll is hit. | not placed by any prefab |
| `vfx_frozengd_destroyed` | Frozengd destroyed | Pale cyan-white ice smoke, stones and flakes. Game: a frozen elder or frozen skeleton breaks. |  |
| `vfx_frozenking_blackice_destroyed` | Frozenking blackice destroyed | Green-blue-white black ice rocks, sparks and smoke with a blue light. Game: the Frozen King phase 2 dies. |  |
| `vfx_frozenking_death` | Frozenking death | White-red-blue vortex swirls with sparks being sucked in, absorb trails and a flare, blue glow. Game: the Frozen King dies. | sound: storm loop; camera shake 2; not placed by any prefab |
| `vfx_frozenking_final_death` | Frozenking final death | Huge swirl of white-red-green-blue flares rising up, vortex swirls, sparks and smoke, cyan glow. Game: the Frozen King's final death. | sound: storm loop; camera shake 2; not placed by any prefab |
| `vfx_frozenking_final_death_ground` | Frozenking final death ground | White-blue-cyan ground smoke ring, flash spikes and ice cracks with a strong blue light. Game: the Frozen King's final death on the ground. | not placed by any prefab |
| `vfx_gdking_stomp` | Gdking stomp | Brown-grey dust and rock shockwave with a strong shake. Game: the Elder (great tree king) stomps. | camera shake 2 within 40 m |
| `vfx_ghost_death` | Ghost death | Green-black wraith smoke and sparks with a green light. Game: a ghost dies. | short camera shake 0.25 |
| `vfx_ghost_hit` | Ghost hit | Black wraith smoke puff. Game: a ghost is hit. |  |
| `vfx_ghost_spawn` | Ghost spawn | Black-green wraith smoke. Game: a ghost spawns. |  |
| `vfx_gjall_spit` | Gjall spit | Green-olive slime spit with a splash and smoke, orange glow. Game: the Gjall spits. | sound: spit |
| `vfx_goblin_death` | Goblin death | Red blood spray with chunks and flakes. Game: a goblin dies. |  |
| `vfx_goblin_dn_death` | Goblin dn death | Red blood spray with chunks. Game: a deep north goblin dies. |  |
| `vfx_goblin_dn_hit` | Goblin dn hit | Red blood spray. Game: a deep north goblin is hit. |  |
| `vfx_goblin_hit` | Goblin hit | Red blood spray. Game: a goblin or Elaking is hit. |  |
| `vfx_goblin_woodwall_destroyed` | Goblin woodwall destroyed | White-yellow sawdust and fog. Game: a goblin wooden wall piece is destroyed. | used by ~30 pieces |
| `vfx_goblinbrute_death` | Goblinbrute death | Red blood spray with chunks. Game: a goblin brute dies. |  |
| `vfx_goblinbrute_hildir_death` | Goblinbrute hildir death | Red blood spray with chunks. Game: a Hildir goblin brute dies. |  |
| `vfx_goblinbrute_hit` | Goblinbrute hit | Red blood spray. Game: a goblin brute is hit. |  |
| `vfx_goblinking_beam_OLD` | Goblinking beam OLD | Pink-white beam cloud and spray with a pink light. Game: the old Yagluth beam. | old, not placed by any prefab |
| `vfx_grausten_stair_destroyed` | Grausten stair destroyed | Grey-white rock chips. Game: a grausten stair is destroyed. |  |
| `vfx_greydwarf_death` | Greydwarf death | Red-yellow-olive gore with blobs. Game: a greydwarf dies. |  |
| `vfx_greydwarf_elite_death` | Greydwarf elite death | Red-yellow-olive gore with blobs. Game: an elite greydwarf dies. |  |
| `vfx_greydwarf_hit` | Greydwarf hit | Yellow-olive splatter. Game: a greydwarf, root or evil heart is hit. |  |
| `vfx_greydwarf_root_destroyed` | Greydwarf root destroyed | Yellow-olive-white splatter and twigs. Game: a greydwarf root is destroyed. |  |
| `vfx_greydwarf_shaman_pray` | Greydwarf shaman pray | Yellow-grey spray with a green light. Game: the greydwarf shaman prays (heals allies) or breathes. |  |
| `vfx_greydwarfnest_destroyed` | Greydwarfnest destroyed | Red-yellow-olive gore and 55 physical pieces. Game: a greydwarf nest is destroyed. | mesh gibs with rigidbodies |
| `vfx_greydwarfnest_hit` | Greydwarfnest hit | Yellow-olive splatter and twigs. Game: a greydwarf nest or root is hit. |  |
| `vfx_groundtorch_addFuel` | Groundtorch add Fuel | White flame puff with smoke. Game: fuel is added to a ground torch. |  |
| `vfx_hatchling_death` | Hatchling death | Red-white-blue gore burst with a yellow light and a shake. Game: a hatchling dies. | camera shake 1 within 30 m |
| `vfx_hatchling_hurt` | Hatchling hurt | Red blood splatter. Game: a hatchling, Gjall or hive is hurt. |  |
| `vfx_hjall_spit_hit` | Hjall spit hit | Splash of white-blue cold sparks and yellow-green slime with smoke and embers, cyan and orange glow. Game: a Gjall spit lands. | sound: spit impact; prefab name has a typo (hjall) |
| `vfx_ice_destroyed` | Ice destroyed | White ice rocks, stones and pebbles. Game: ice (rock, stalactite, ice ship) is destroyed. | used by ~14 ice pieces |
| `vfx_ice_destroyed_shelf` | Ice destroyed shelf | White ice rocks and pebbles. Game: an ice shelf piece or ice shore shard is destroyed. |  |
| `vfx_ice_hit` | Ice hit | Small white ice chips. Game: ice or black ice is hit. | used as hit effect by ~35 prefabs |
| `vfx_iceblocker_destroyed` | Iceblocker destroyed | White-pale blue ice spikes, rocks and pebbles. Game: an ice blocker is destroyed. |  |
| `vfx_icecube_destroyed` | Icecube destroyed | White ice rocks and pebbles. Game: an ice cube piece is destroyed. |  |
| `vfx_kiln_addore` | Kiln addore | White flame puff with smoke. Game: ore is added to the kiln or battering ram. |  |
| `vfx_kiln_produce` | Kiln produce | White smoke puff. Game: the kiln produces charcoal. |  |
| `vfx_leech_death` | Leech death | Red blood puff with skin flakes. Game: a leech dies. |  |
| `vfx_leech_hit` | Leech hit | Red blood splatter. Game: a leech is hit. |  |
| `vfx_lever` | Lever | Cyan sparks flying from the lever, cyan glow. Game: the incinerator lever is pulled. | sound: distant thunder, lever, electricity; camera shake 1.5 within 40 m |
| `vfx_lightningstaff_fire` | Lightningstaff fire | Red muzzle smoke and a wave. Game: the lightning staff is fired. |  |
| `vfx_lootspawn` | Lootspawn | Small pale dust sparkle. Game: loot spawns. |  |
| `vfx_lox_groundslam` | Lox groundslam | Grey-brown rocks, dust and a ring. Game: Lox's stomp attack. |  |
| `vfx_lox_love` | Lox love | Red hearts. Game: a Lox or Moose shows love (breeding). |  |
| `vfx_lox_soothed` | Lox soothed | Yellow hearts. Game: a Lox or Moose is soothed. |  |
| `vfx_mill_add` | Mill add | White dust puff. Game: grain is added to the windmill. |  |
| `vfx_mill_produce` | Mill produce | White dust and smoke. Game: the windmill produces flour. |  |
| `vfx_mistlands_mist` | Mistlands mist | Blue heavy mist layer typical of the Mistlands. | loops, ambient; not placed by any prefab |
| `vfx_morgenhole_pile_destroyed` | Morgenhole pile destroyed | Orange-black tar chunks and a blob death. Game: a Morgen hole pile is destroyed. |  |
| `vfx_morkhalla_bench_destroyed` | Morkhalla bench destroyed | Falls apart into mesh pieces (no particles). Game: a Morkhalla bench is destroyed. | mesh gibs; not placed by any prefab |
| `vfx_morkhalla_firepit_destroyed` | Morkhalla firepit destroyed | Falls apart into 12 mesh pieces. Game: a Morkhalla fire pit is destroyed. | mesh gibs |
| `vfx_morkhalla_gatedoor02_destroyed` | Morkhalla gatedoor02 destroyed | Brown sawdust and 18 pieces. Game: Morkhalla gate door 02 is destroyed. | mesh gibs |
| `vfx_morkhalla_gatedoor03_destroyed` | Morkhalla gatedoor03 destroyed | Brown sawdust and 20 pieces. Game: Morkhalla gate door 03 is destroyed. | mesh gibs |
| `vfx_morkhalla_gatedoor_destroyed` | Morkhalla gatedoor destroyed | Brown sawdust and 24 pieces. Game: the Morkhalla gate door is destroyed. | mesh gibs |
| `vfx_morkhalla_stool_destroyed` | Morkhalla stool destroyed | Falls apart into mesh pieces. Game: a Morkhalla stool is destroyed. | mesh gibs; not placed by any prefab |
| `vfx_morkhalla_table_destroyed` | Morkhalla table destroyed | Falls apart into mesh pieces. Game: a Morkhalla table is destroyed. | mesh gibs; not placed by any prefab |
| `vfx_morkhalla_trainingdummy1_destroyed` | Morkhalla trainingdummy1 destroyed | Falls apart into mesh pieces. Game: Morkhalla training dummy 1 is destroyed. | mesh gibs; not placed by any prefab |
| `vfx_morkhalla_trainingdummy2_destroyed` | Morkhalla trainingdummy2 destroyed | Falls apart into mesh pieces. Game: Morkhalla training dummy 2 is destroyed. | mesh gibs; not placed by any prefab |
| `vfx_morkhalla_weaponstand_destroyed` | Morkhalla weaponstand destroyed | Falls apart into 8 pieces. Game: a Morkhalla weapon stand is destroyed. | mesh gibs |
| `vfx_morkhalla_web_destroyed` | Morkhalla web destroyed | White web strands. Game: a Morkhalla web is destroyed. |  |
| `vfx_mountainkit_chair_destroyed` | Mountainkit chair destroyed | Falls apart into 12 chair pieces. Game: a mountain-kit chair is destroyed. | mesh gibs |
| `vfx_mountainkit_table_destroyed` | Mountainkit table destroyed | Falls apart into 12 table pieces. Game: a mountain-kit table is destroyed. | mesh gibs |
| `vfx_neck_death` | Neck death | White-green gore with blobs. Game: a Neck dies. |  |
| `vfx_neck_hit` | Neck hit | Green splatter and drips. Game: a Neck (or Asksvin) is hit. |  |
| `vfx_oak_cut` | Oak cut | White-yellow leaves, twigs and smoke puffs. Game: an oak is cut down. |  |
| `vfx_oaklogdestroyed` | Oaklogdestroyed | Yellow-olive sawdust and smoke. Game: an oak log is destroyed. |  |
| `vfx_oaklogdestroyed_half` | Oaklogdestroyed half | Yellow-olive sawdust and smoke. Game: a half oak or beech log is destroyed. |  |
| `vfx_obsidian_destroyed` | Obsidian destroyed | Pale dust and 18 obsidian pieces. Game: an obsidian pile is destroyed. | mesh gibs |
| `vfx_ocean_clouds` | Ocean clouds | White swamp-mist cloud layer. | loops, ambient; not placed by any prefab |
| `vfx_odin_despawn` | Odin despawn | Red-black smoke with blood fog. Game: Odin (the visitor) despawns. |  |
| `vfx_offering` | Offering | White-pink sparkles with a pink light. Game: an offering is placed on the boss altar. |  |
| `vfx_perfectblock` | Perfectblock | Yellow-red dust and a wave. Game: the player blocks at exactly the right moment (parry). |  |
| `vfx_pick_wisp` | Pick wisp | Small pale blue puff. Game: a wisp or Fader ember is picked. |  |
| `vfx_pickable_pick` | Pickable pick | Small pale dust puff. Game: a plant, berry bush or most pickables are picked. | used by ~100 pickables, boss stones and armor stands |
| `vfx_pinelogdestroyed` | Pinelogdestroyed | Yellow sawdust and smoke. Game: an old pine log is destroyed. |  |
| `vfx_pinelogdestroyed_half` | Pinelogdestroyed half | Yellow sawdust and smoke. Game: a half old pine log is destroyed. |  |
| `vfx_pinetree_regrow` | Pinetree regrow | Pale yellow dust puff. Game: a pine tree regrows. |  |
| `vfx_pinetreecut` | Pinetreecut | Green-white pine needles, twigs and smoke. Game: a pine tree is cut down. |  |
| `vfx_pinetreecut_dead` | Pinetreecut dead | White-pale branches and smoke. Game: a dead snow pine is cut down. |  |
| `vfx_pinetreecut_snow` | Pinetreecut snow | Green-white needles and twigs. Game: a snow pine is cut down. |  |
| `vfx_player_death` | Player death | Red-orange blood, flakes and decals. Game: the player dies. |  |
| `vfx_player_hit` | Player hit | White-red-orange blood drops, chunks and decals. Game: the player is hit. |  |
| `vfx_poisonarrow_hit` | Poisonarrow hit | Red-green poison burst with a green light. Game: a poison arrow hits. |  |
| `vfx_prespawn` | Prespawn | White falling embers before the Frozen King boss spawns at the altar. | lasts ~20 s |
| `vfx_prespawn_fader` | Prespawn fader | Green falling embers before Fader spawns. | lasts ~25 s; not placed by any prefab |
| `vfx_seagull_death` | Seagull death | Pale feathers and a red puff. Game: a seagull dies. |  |
| `vfx_seal_hit` | Seal hit | Red blood splatter. Game: a seal is hit. |  |
| `vfx_seekerbrute_groundslam` | Seekerbrute groundslam | Brown-white-grey dust, ring and pebbles with a camera shake. Game: the Seeker brute's ground slam. | camera shake 1.5 within 30 m |
| `vfx_serpent_attack_trigger` | Serpent attack trigger | White-cyan water splash and ring. Game: the sea serpent bites or rams. |  |
| `vfx_serpent_death` | Serpent death | Red-cyan blood, flakes and meat lumps. Game: a sea serpent dies. |  |
| `vfx_serpent_hurt` | Serpent hurt | Red blood splatter. Game: a sea serpent is hurt. |  |
| `vfx_shieldgenerator_refuel` | Shieldgenerator refuel | Cyan-green energy particles and a wave. Game: the shield generator is refuelled. |  |
| `vfx_shieldgenerator_startup` | Shieldgenerator startup | Cyan energy particles and a wave. Game: the shield generator starts. |  |
| `vfx_shrub_2_destroyed` | Shrub 2 destroyed | White-yellow leaves, twigs and a smoke puff. Game: a shrub is destroyed. |  |
| `vfx_shrub_2_heath_destroyed` | Shrub 2 heath destroyed | White-yellow heath leaves and twigs. Game: a heath shrub is destroyed. |  |
| `vfx_shrub_2_hit` | Shrub 2 hit | Small white leaf puff. Game: a shrub is hit. |  |
| `vfx_shrub_heath_hit` | Shrub heath hit | Small white heath leaf puff. Game: a heath shrub is hit. |  |
| `vfx_silvermace_hit` | Silvermace hit | White snow flakes and flying lumps. Game: the silver mace hits. |  |
| `vfx_skeleton_big_death` | Skeleton big death | Green-white fire, rocks, bones and a skull. Game: a poison skeleton dies. |  |
| `vfx_skeleton_death` | Skeleton death | Pale yellow-grey dust, bones and a skull. Game: a skeleton dies. | used by ~12 skeletons |
| `vfx_skeleton_fire_death` | Skeleton fire death | Black-white dust, burned bones and a skull. Game: a fire skeleton dies. |  |
| `vfx_skeleton_hit` | Skeleton hit | Grey-pale dust puff. Game: a skeleton or a bone pile is hit. | used by ~18 prefabs |
| `vfx_skeleton_mace_hit` | Skeleton mace hit | Green fire splatter and chunks. Game: the skeleton mace hits. |  |
| `vfx_skullpile_destroyed` | Skullpile destroyed | Pale yellow dust and 25 skull pieces. Game: a skull pile is destroyed. | mesh gibs |
| `vfx_sledge_hit` | Sledge hit | White-brown shockwave, cloud and rock chips. Game: a wood or stagbreaker sledge hammer hits. |  |
| `vfx_sledge_iron_hit` | Sledge iron hit | White-grey-blue shockwave, cloud and rocks with a blue light. Game: an iron sledge hits. |  |
| `vfx_smelter_addfuel` | Smelter addfuel | White flame puff, smoke and cinders. Game: fuel is added to a smelter. |  |
| `vfx_smelter_addore` | Smelter addore | White flame puff, smoke and cinders. Game: ore is added to a smelter. |  |
| `vfx_smelter_produce` | Smelter produce | White flame puff and smoke. Game: a smelter produces a bar. |  |
| `vfx_spawn` | Spawn | White-pink sparkles with a pink light. Game: a boss or Black Ice core spawn puff. |  |
| `vfx_spawn_large` | Spawn large | White-pink sparkles with a pink light. Game: a large spawn puff. | not placed by any prefab |
| `vfx_spawn_small` | Spawn small | White-red-black sparks with trails and smoke, faint pink glow. Game: a spawner releases a creature (bone piles, evil hearts and others). | sound: flame burst; used by ~15 spawners |
| `vfx_spikey_beam_destruction` | Spikey beam destruction | Falls apart into 20 mesh pieces. Game: an Ashlands fortress spike beam is destroyed. | mesh gibs |
| `vfx_standing_brazier_destroyed` | Standing brazier destroyed | Falls apart into 4 brazier pieces. Game: a standing brazier is destroyed. | mesh gibs |
| `vfx_stone_floor_2x2_destroyed` | Stone floor 2x2 destroyed | Grey-white rock chips. Game: a 2x2 stone floor is destroyed. |  |
| `vfx_stone_floor_destroyed` | Stone floor destroyed | Grey-white rock chips. Game: a stone floor is destroyed. |  |
| `vfx_stone_stair_destroyed` | Stone stair destroyed | Grey-white rock chips. Game: a stone stair is destroyed. |  |
| `vfx_stone_wall_4x2_destroyed` | Stone wall 4x2 destroyed | Grey-white rock chips. Game: a 4x2 stone wall is destroyed. |  |
| `vfx_stone_wall_ruin_2_destroyed` | Stone wall ruin 2 destroyed | Falls apart into 8 stone pieces. Game: a ruined stone wall (2) is destroyed. | mesh gibs |
| `vfx_stone_wall_ruin_destroyed` | Stone wall ruin destroyed | Falls apart into 6 stone pieces. Game: a ruined stone wall is destroyed. | mesh gibs |
| `vfx_stonegolem_attack_hit` | Stonegolem attack hit | Grey-white rock chips, dust and sparks. Game: a stone golem's attack lands. |  |
| `vfx_stonegolem_death` | Stonegolem death | Grey-yellow rock burst, dust, sparks and 14 physical pieces with a yellow light. Game: a stone golem dies. | mesh gibs with rigidbodies |
| `vfx_stonegolem_hurt` | Stonegolem hurt | Grey-yellow rock chips and sparks. Game: a stone golem is hurt. |  |
| `vfx_stonegolem_wakeup` | Stonegolem wakeup | White-yellow dust and magic sparks. Game: a stone golem wakes up. |  |
| `vfx_stubbe` | Stubbe | Olive sawdust puff. Game: a tree stump is destroyed. |  |
| `vfx_swamp_mist` | Swamp mist | Grey low swamp mist layer. | loops, ambient; not placed by any prefab |
| `vfx_swamptree_cut` | Swamptree cut | White-yellow twigs and smoke. Game: a swamp tree is cut down. |  |
| `vfx_torch_hit` | Torch hit | White flame and smoke puff. Game: a burning torch or lantern hits something. |  |
| `vfx_tree_fall_hit` | Tree fall hit | Pale yellow dust puff. Game: a falling tree or log hits something. | used by ~29 logs |
| `vfx_troll_attack_hit` | Troll attack hit | White-brown dust and sparks. Game: a troll attack (log swing) lands. |  |
| `vfx_troll_death` | Troll death | Red-blue gore, decals and chunks. Game: a troll dies. |  |
| `vfx_troll_groundslam` | Troll groundslam | Grey-white-brown rocks, dust and a ring. Game: a troll (or golem, Abomination) slams the ground. | used as hit-ground effect by ~12 attacks |
| `vfx_troll_log_hitground` | Troll log hitground | Grey-white-brown rocks and a ring. Game: the troll's log hits the ground. |  |
| `vfx_troll_rock_destroyed` | Troll rock destroyed | Grey-white rock chips. Game: a thrown troll rock hits or a big creature jumps. |  |
| `vfx_troll_summoned_death` | Troll summoned death | White-black-red flame, smoke and magic swirls. Game: a summoned troll dies. |  |
| `vfx_troll_summoned_prespawn` | Troll summoned prespawn | White-red-black fire vortex with a flare, flying rocks, an explosion and a heat-shimmer shockwave, red glow. Game: the summoned troll rises. | sound: troll staff meteor; camera shake 2 within 50 m; not placed by any prefab |
| `vfx_trollsnow_attack_hit` | Trollsnow attack hit | White-grey dust and sparks. Game: a snow troll attack lands. |  |
| `vfx_trollsnow_groundslam` | Trollsnow groundslam | Grey-white snowy rocks and a ring. Game: a snow troll slams the ground. |  |
| `vfx_trollsnow_log_destroyed` | Trollsnow log destroyed | Grey-black-pale cyan rocks, splinters and smoke. Game: the snow troll's thrown log or punch hits. |  |
| `vfx_turnip_grow` | Turnip grow | Small pale dust puff. Game: a sapling grows into a plant. | used by ~16 saplings |
| `vfx_ulv_death` | Ulv death | Red-grey blood puff with skin flakes. Game: an Ulv dies. |  |
| `vfx_vines_destroyed` | Vines destroyed | Green-white leaves, twigs and smoke. Game: vines are destroyed. |  |
| `vfx_wagon_destroyed` | Wagon destroyed | Brown-grey sawdust and 29 wagon pieces. Game: the trader wagon is destroyed. | mesh gibs |
| `vfx_walltorch_addFuel` | Walltorch add Fuel | White flame puff with smoke. Game: fuel is added to a wall torch. |  |
| `vfx_wolf_death` | Wolf death | Red-grey blood puff with skin flakes. Game: a wolf dies. |  |
| `vfx_wolf_hit` | Wolf hit | Red blood splatter. Game: a wolf or Ulv is hit. |  |
| `vfx_wood_black_stack_destroyed` | Wood black stack destroyed | Falls apart into 16 log pieces. Game: a black wood stack is destroyed. | mesh gibs |
| `vfx_wood_core_stack_destroyed` | Wood core stack destroyed | Falls apart into 9 core wood pieces. Game: a core wood stack is destroyed. | mesh gibs |
| `vfx_wood_fine_stack_destroyed` | Wood fine stack destroyed | Falls apart into 20 fine wood pieces. Game: a fine wood stack is destroyed. | mesh gibs |
| `vfx_wood_frost_stack_destroyed` | Wood frost stack destroyed | Falls apart into 21 pieces. Game: a frost wood stack is destroyed. | mesh gibs |
| `vfx_wood_stack_destroyed` | Wood stack destroyed | Falls apart into 12 split-wood pieces. Game: a wood stack is destroyed. | mesh gibs |
| `vfx_wood_yggdrasil_stack_destroyed` | Wood yggdrasil stack destroyed | Falls apart into 30 shoot bundle pieces. Game: a Yggdrasil wood stack is destroyed. | mesh gibs |
| `vfx_wooden_path_destroyed` | Wooden path destroyed | Falls apart into 12 plank and log pieces. Game: a wooden path is destroyed. | mesh gibs |
| `vfx_wraith_death` | Wraith death | Red-black-white gore and wraith smoke with a yellow light and a shake. Game: a wraith dies. | camera shake 1 within 30 m |
| `vfx_wraith_hit` | Wraith hit | Black wraith smoke puff. Game: a wraith is hit. |  |
| `vfx_yggashoot_cut` | Yggashoot cut | White-yellow leaves, twigs and smoke. Game: a Yggdrasil shoot is cut down. |  |
| `vfx_yggashoot_small1_destroy` | Yggashoot small1 destroy | White-olive leaves and twigs. Game: a small Yggdrasil shoot is destroyed. |  |
