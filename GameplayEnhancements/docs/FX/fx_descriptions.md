---
description: Valheim fx_ effect prefabs: id, readable name, what it looks like and where the game uses it, for picking effects for actions (hit, heal, explosion, thunder)
---
# FX descriptions (fx_)

All 281 `fx_` action and creature effect prefabs of Valheim (one row per prefab, sorted by id), written 2026-10-24. Each description says first what you see (colours, shapes, motion, glow), then after "Game:" what the game uses it for; sounds, camera shake and duration are in side notes. ✔ after the name = fired in game and checked by Maksym: the description starts with what he saw and the size he gave. No mark = read from a dump of our own game (particle colours, materials, child object names, lights, which prefabs spawn it), never seen by Claude, so no size is given.

| effect id | effect name | effect description | side notes |
|---|---|---|---|
| `fx_Abomination_arise` | Abomination rises | Big cloud of tan-brown dust bursting up from the ground with flying rocks, pebbles and mud. Game: the Abomination rises out of the ground. | sound: deep rumble and growl; camera shake 1.2 after 3.5 s; lasts ~15 s |
| `fx_Abomination_arise_end` | Abomination arise end | Nothing visible, camera shake only. Game: end of Morgen's wake-up. | camera shake 1 for 3 s |
| `fx_Abomination_attack1` | Abomination swing impact | Burst of grey rock chunks, pebbles and a dust ring puffing up where a heavy blow lands. Game: Abomination swing attack. | sound: sledge-hammer thump; camera shake 0.8 |
| `fx_Abomination_attack1_start` | Abomination attack 1 roar | Nothing visible, sound only. Game: Abomination attack 1 starts. | sound: roar; not placed by any prefab |
| `fx_Abomination_attack1_trailon` | Abomination swing whoosh | Nothing visible, sound only. Game: Abomination attack 1 swing. | sound: heavy swing whoosh |
| `fx_Abomination_attack2` | Abomination slam impact | Big burst of grey rocks, mud and twigs flying up with a dust ring around it. Game: Abomination slam. | sound: rock smash; camera shake 1.2 |
| `fx_Abomination_attack2_lift` | Abomination slam wind-up | Nothing visible, sound only. Game: Abomination lifts its arms before the slam. | sound: wind-up; not placed by any prefab |
| `fx_Abomination_attack2_start` | Abomination attack 2 roar | Nothing visible, sound only. Game: Abomination attack 2 starts. | sound: roar; not placed by any prefab |
| `fx_Abomination_attack3` | Abomination ground slam impact | Big burst of grey rocks, mud and twigs flying up with a dust ring, same look as attack 2. Game: Abomination ground slam. | sound: rock smash; camera shake 1.2 |
| `fx_Abomination_attack3_start` | Abomination attack 3 roar | Nothing visible, sound only. Game: Abomination attack 3 starts. | sound: ground-slam roar; not placed by any prefab |
| `fx_Abomination_attack_hit` | Abomination hit | Small puff of grey rock dust and wood chips on the target. Game: an Abomination attack hits. |  |
| `fx_Adrenaline1` | Adrenaline pop | Short orange flash with an expanding ring, flying sparks and a heat-shimmer shockwave, lit by a brief orange glow. Game: adrenaline pop on the player. | sound: ignite whoosh |
| `fx_ArmorStand_pick` | Armor stand pick | Tiny pale dust puff. Game: an item is taken from or put on an armor stand. |  |
| `fx_Bonemass_aoe_start` | Bonemass AOE start | Spray of black-green slime shooting out with wet splashes. Game: start of the Bonemass area attack. | sound: deep growl; camera shake 1 |
| `fx_BonemawSerpent_Spit_Hit` | Bonemaw spit hit | Yellow slime splat with stretchy slime strands and a puff of smoke. Game: Bonemaw Serpent spit lands. |  |
| `fx_BonemawSerpent_poisonbreath` | Bonemaw poison breath | Stream of green-black poison smoke with chunks, lit by a flickering orange glow. Game: Bonemaw Serpent poison breath. | sound: flamethrower loop; lasts ~10 s |
| `fx_BonusYield` | Bonus yield | Small red sparkle pop over the plant. Game: a plant gives extra yield at harvest. | sound: bonus pop |
| `fx_CharredStone_Destruction` | Charred stone destroyed | Dark-red embers and a red glow with dust and bone pieces (legs, arms, ribs, skull) falling apart. Game: a charred stone spawner is destroyed. | pairs with fx_CharredStone_Gibs |
| `fx_CharredStone_Gibs` | Charred stone fragments | 17 stone fragments falling apart, no particles. Game: a charred stone spawner is destroyed. | mesh gibs |
| `fx_CinderFire_Burn` | Cinder fire burn | Red-white flames with sparks and a column of black smoke. Game: Ashlands fire burns a target. | sound: fire damage tick |
| `fx_Cinder_hit` | Cinder hit | Red sparks and small flames with black smoke on the target. Game: falling cinder hits. |  |
| `fx_Cinder_storm_hit` | Cinder storm hit | Red sparks and small flames with black smoke, same look as fx_Cinder_hit. Game: cinder storm hits. | sound: Ashlands burn |
| `fx_DvergerMage_Fire_hit` | Dvergr fire hit | Medium white-orange fire burst with smoke, orange glow. Game: Dvergr fireball or cluster bomb hits. | sound: fireball impact; camera shake 1 |
| `fx_DvergerMage_Ice_hit` | Dvergr ice hit | Blue-white ice burst with cold sparks, rock chips and smoke, cyan glow. Game: Dvergr ice projectile hits. | sound: ice shatter; camera shake 1 |
| `fx_DvergerMage_MistileSpawn` | Mistile spawn | Pink-blue magic puff with sparks and dust, red-orange glow. Game: a Mistile drone appears. | sound: ring appears; camera shake 1 |
| `fx_DvergerMage_Mistile_attack` | Mistile attack blast | Blue-white shield-like shockwave rings with rising particles and a delayed second wave, red glow. Game: Mistile kamikaze blast. | sound: charge-up and electric zap; camera shake 0.5; lasts up to ~22 s |
| `fx_DvergerMage_Mistile_die` | Mistile death | Blue-white shockwave with rising particles, sparks and small flames, red glow. Game: a Mistile dies. | sound: electric zap; camera shake 0.5 |
| `fx_DvergerMage_Nova_ring` | Dvergr nova ring ✔ | Seen in game: big cyan ring on the ground (big). Dump: ring expands over the ground with flames, sparks and a cyan glow. Game: Dvergr mage nova. | sound: ice attack hit; fades after ~5 s |
| `fx_DvergerMage_Support_hit` | Dvergr support hit | White-blue shield shockwave with rising particles, blue glow. Game: Dvergr support projectile hits. | sound: electric impact; camera shake 1 |
| `fx_DvergerMage_Support_start` | Dvergr support buff | Big white-blue shockwave rings with rising particles, sparks and smoke, red glow. Game: Dvergr support buff starts. | sound: long charge-up; camera shake 1; lasts ~28 s |
| `fx_Dverger_death` | Dvergr death | Red blood spray with chunks, skin flakes and blood splats on the ground. Game: a Dvergr dies. | sound: death grunt |
| `fx_Dverger_hit` | Dvergr hit | Red blood spray with drops and chunks. Game: a Dvergr is hit. | sound: hurt grunt |
| `fx_Eat` | Eat food | Small burst of red-orange crumbs and droplets at the mouth. Game: eating food. | used by ~50 foods; colour variants fx_Eat_* |
| `fx_Eat_Black` | Eat black food | Small burst of dark red-black crumbs and droplets. Game: eating dark foods (black soup, blood pudding, smoothie). |  |
| `fx_Eat_Blue` | Eat blue food | Small burst of red-blue crumbs and droplets. Game: eating blue foods and berries. |  |
| `fx_Eat_Green` | Eat green food | Small burst of red-green crumbs and droplets. Game: eating green foods. |  |
| `fx_Eat_Orange` | Eat orange food | Small burst of orange crumbs and droplets. Game: eating carrots, cloudberry, sauces. |  |
| `fx_Eat_Red` | Eat red food | Small burst of red crumbs and droplets. Game: eating mushrooms, raspberry. |  |
| `fx_Eat_Yellow` | Eat yellow food | Small burst of red-yellow crumbs and droplets. Game: eating yellow mushroom, royal jelly. |  |
| `fx_ElakingMole_Sandcloud` | Mole sand cloud | Large orange-brown sand cloud drifting with pebbles. Game: Elaking mole sand attack. | lasts ~15 s |
| `fx_ElakingMole_Wakeup` | Mole wake-up | Large orange-brown sand cloud with pebbles. Game: Elaking mole wakes up. | lasts ~13 s |
| `fx_Fader_CorpseExplosion` | Fader corpse explosion | White-green flame burst with floating spores, magic trails and a heat-shimmer shockwave. Game: Fader's corpse blows up. | lasts ~18 s |
| `fx_Fader_Fissure_Prespawn` | Fader fissure warning | Grey bubbling patch on the ground with a green glow. Game: warning before a Fader fissure erupts. | lasts ~5 s; not placed by any prefab |
| `fx_Fader_Ragdoll` | Fader ragdoll | Fader's whole body as a ragdoll, burning up in green-white flames with spores. Game: Fader's corpse. | ragdoll physics object |
| `fx_Fader_Roar` | Fader roar ✔ | Seen in game: floating green lights over the ground, subtle (small). Dump: green spores with a heat-shimmer shockwave. Game: Fader roars, also boss summon roars. | camera shake 0.5 |
| `fx_Fader_Roar_Projectile_Hit` | Fader roar projectile hit | Green flame spikes with swirling spores and a heat-shimmer shockwave. Game: Fader roar projectile lands. | sound: charred summon impact; camera shake 0.5 |
| `fx_Fader_Spin` | Fader spin | White-green ring of flames with spores. Game: Fader spin attack. | camera shake 1 |
| `fx_Fading_Bellfragment_Beam` | Bell fragment beam | A beam of light that fades out, no particles. Game: the Charred altar bell fragment is destroyed. | mesh |
| `fx_FallenValkyrie_projectile_explosion` | Valkyrie spit explosion | Pink-white fire burst with black smoke and spores, pink glow. Game: Fallen Valkyrie spit explodes. | lasts ~17 s |
| `fx_FeastAshlandsEat` | Eat Ashlands feast | Small splatter of orange-red droplets and crumbs. Game: eating the Ashlands feast. | sound: crunch and chew |
| `fx_FeastBlackForestEat` | Eat Black Forest feast | Small splatter of droplets and crumbs. Game: eating the Black Forest feast. | sound: crunch and chew |
| `fx_FeastMeadowsEat` | Eat Meadows feast | Small splatter of brown-red droplets and crumbs. Game: eating the Meadows feast. | sound: crunch and chew |
| `fx_FeastMistlandsEat` | Eat Mistlands feast | Small splatter of droplets and crumbs. Game: eating the Mistlands feast. | sound: crunch and chew |
| `fx_FeastMountainsEat` | Eat Mountains feast | Small splatter of dark-red droplets and crumbs. Game: eating the Mountains feast. | sound: crunch and chew |
| `fx_FeastOceansEat` | Eat Ocean feast | Small splatter of dark-red droplets and crumbs. Game: eating the Ocean feast. | sound: crunch and chew |
| `fx_FeastPlainsEat` | Eat Plains feast | Small splatter of orange-green droplets and crumbs. Game: eating the Plains feast. | sound: crunch and chew |
| `fx_FeastSmall` | Feast hit (small) | Small white-red splatter. Game: a feast or food item is hit. | sound: axe hit; used by ~110 foods |
| `fx_FeastSwampsEat` | Eat Swamps feast | Small splatter of droplets and crumbs. Game: eating the Swamps feast. | sound: crunch and chew |
| `fx_FeastTemplate` | Feast destroyed | Small orange-red splatter. Game: a feast table is destroyed. | sound: axe hit |
| `fx_GP_Activation` | Guardian power activation ✔ | Seen in game: pink glowing orb on the ground (small). Dump: red-white rising embers and trails with a red glow. Game: a guardian power activates. | sound: dark whispers; camera shake 1 |
| `fx_GP_Player` | Guardian power on player | Red-green-white embers, trails and smoke swirling around the player, red glow. Game: taking a guardian power at a boss stone. | sound: dark whispers; camera shake 1.5; ~15 s |
| `fx_GP_Stone` | Guardian power on stone | Red-green embers, trails and smoke rising from the boss stone, red glow. Game: the stone gives its power. | sound: beast transform; ~15 s |
| `fx_GoblinShieldBreak` | Shaman shield break | Purple shield bubble shattering into shards with rising particles. Game: goblin shaman shield breaks. | sound: magic burst |
| `fx_GoblinShieldHit` | Shaman shield hit | Purple flicker of the shield with shards and rising particles. Game: goblin shaman shield is hit. | sound: force-field impact |
| `fx_HildirChest_Unlock` | Hildir chest unlock | Yellow sparkles with a dust puff. Game: Hildir's chest unlocks. | sound: lock, creak, chest open; not placed by any prefab |
| `fx_IceShelf_Damage` | Ice shelf destroyed | White smoke cloud. Game: an ice shelf breaks. | sound: ice shatter and iceberg crash |
| `fx_Immobilize` | Immobilized | Blue-grey smoke and dust clinging around the target. Game: target is rooted (Immobilized). | lasts ~28 s |
| `fx_JotunWitch_Death` | Jotun witch death | Cyan-purple-white blast with streak lines, rings and a shockwave. Game: the Jotun witch dies. |  |
| `fx_JotunWitch_Dodge` | Jotun witch dodge | Small cyan puff. Game: the Jotun witch dodges. |  |
| `fx_JotunWitch_Flying` | Jotun witch flying trail | Thin cyan particle trail. Game: the Jotun witch flies. | loops |
| `fx_JotunWitch_LightningBolt_Explosion` | Witch lightning bolt hit ✔ | Seen in game: small violet lightning blast (small). Dump: pink lightning with sparks and smoke. Game: witch lightning bolt lands. | sound: ice-staff shot |
| `fx_JotunWitch_LightningBolt_Trigger` | Witch lightning bolt cast | Purple lightning with sparks and smoke, white flash. Game: the witch casts a lightning bolt. | sound: ice-staff shot |
| `fx_JotunWitch_MagicBlast` | Witch magic blast | Cyan-purple-white blast with streak lines, rings and a shockwave. Game: witch magic blast. |  |
| `fx_Lightning` | Lightning ✔ | Seen in game: small blue zigzag bolt (small). Dump: cyan sparks with a cyan glow. Game: Lightning status on a character. | sound: electric hum loop; loops |
| `fx_Lightning_red` | Lightning (red) ✔ | Seen in game: red sparks, red lightning (small). Dump: white-orange lightning arcs, sparks, clouds and red splats with a red glow. Game: red Lightning status. | sound: electric hum loop; loops |
| `fx_Potion_fireresist` | Fire resistance potion | Orange sparkle swirl with embers, flakes and a soft cloud, orange glow around the player. Game: fire resistance potion. | sound: magic spell |
| `fx_Potion_frostresist` | Frost resistance potion | Cyan-white sparkle swirl with flakes and a soft cloud, pale cyan glow around the player. Game: frost resistance potion. | sound: magic spell |
| `fx_Puke` | Puke | Green vomit stream from the mouth with splats on the ground. Game: Puke status. | camera shake 0.8; loops ~14 s |
| `fx_QueenPierceGround` | Queen ground pierce | Several white spark bursts with fog and splashes along the ground. Game: Seeker Queen pierces the ground. | sound: metal hit; camera shake 1 |
| `fx_Queen_Death` | Seeker Queen death | Huge explosion of red-black flesh, flying carapace pieces and legs, blood splats on the ground, red-yellow glow. Game: Seeker Queen dies. | sound: death roar; camera shake 1.5; lasts ~60 s |
| `fx_RootAshlands` | Ashlands roots | Dark roots growing around the target with green particles and dust. Game: Ashlands root attack (rooted). |  |
| `fx_ShieldCharge_1` | Shield charge 1 | Small orange spark burst with a heat-shimmer shockwave on the shield. Game: tower shield block charge level 1. |  |
| `fx_ShieldCharge_2` | Shield charge 2 | Small orange spark burst with a heat-shimmer shockwave on the shield. Game: block charge level 2. |  |
| `fx_ShieldCharge_3` | Shield charge 3 | Orange spark burst with a heat-shimmer shockwave, slightly stronger. Game: block charge level 3. |  |
| `fx_ShieldCharge_4` | Shield charge 4 | Orange spark burst, shockwave and a glowing ring. Game: block charge level 4. |  |
| `fx_ShieldCharge_5` | Shield charge 5 | Orange spark burst, shockwave, glowing ring and a range circle on the ground. Game: block charge level 5 (full). |  |
| `fx_StaffShield_Break` | Staff shield break | White-purple shield bubble shattering into shards with smoke, red glow. Game: staff shield breaks. | sound: shield break |
| `fx_StaffShield_Hit` | Staff shield hit | White-purple flicker of the shield with shards and smoke. Game: staff shield absorbs a hit. | sound: shield impact |
| `fx_TickBloodHit` | Tick bite | Red blood splat with chunks and drops. Game: a tick bites and attaches. |  |
| `fx_TrollUndead_Gibs` | Undead troll gibs | 9 skull and bone pieces falling apart, no particles. Game: summoned undead troll dies. | mesh gibs |
| `fx_UpgradeStation_Fail` | Upgrade failed | Grey rock burst with a ground shockwave ring and lightning, red glow. Game: upgrade at the Forge of Potential fails. | sound: failure; camera shake 2 |
| `fx_UpgradeStation_Success` | Upgrade success | Rock burst with a ground shockwave ring and lightning, blue-cyan glow. Game: upgrade at the Forge of Potential succeeds. | sound: lightning crack and success; camera shake 2 |
| `fx_WaterImpact_Big` | Big water splash | White-grey water splash. Game: a big log or object falls into water. | sound: water splash; used by ~27 logs |
| `fx_altar_charred_destruction` | Charred altar shatter | Blue-cyan crystal shatter into 11 pieces with a cloud puff. Game: the charred altar breaks. | sound: crystal break |
| `fx_altar_crystal_destruction` | Altar crystal shatter | Blue-cyan crystal shatter into 13 pieces with a cloud puff. Game: black marble altar crystal breaks. | sound: crystal break |
| `fx_ashvine_destruction` | Ash vine destroyed | Grey-red smoke with falling red leaves and twigs. Game: an ash vine is destroyed. |  |
| `fx_ashvine_leaf_puff` | Ash vine leaf puff | Small puff of red leaves. Game: an ash vine is hit. |  |
| `fx_asksvin_pet` | Pet Asksvin | Small red hearts rising. Game: petting an Asksvin. | sound: Asksvin idle |
| `fx_asksvin_soothed` | Asksvin soothed | Small yellow hearts rising. Game: an Asksvin is soothed. |  |
| `fx_asksvin_tamed` | Asksvin tamed | Small yellow hearts rising. Game: an Asksvin is tamed. | sound: charm spell |
| `fx_aspect_death` | Aspect death | Big burst of blue-white tendrils, absorb swirls and blue trails sucking in, strong blue glow. Game: a boss aspect dies. | sound: spirit death; camera shake 1.3 |
| `fx_babyseeker_death` | Baby seeker death | Green-yellow slime and red gore splatter with flying legs and wings, small orange glow. Game: a Seeker brood dies. | sound: death squelch |
| `fx_babyseeker_hurt` | Baby seeker hurt | Green-yellow slime splatter with drops and ground splats. Game: a baby seeker is hurt. |  |
| `fx_backstab` | Backstab | Quick white streak with a "bam" flash. Game: backstab (sneak attack) hit. | sound: airy hit; used by ~160 creatures |
| `fx_bat_death` | Bat death | Red-black blood cloud with skin flakes and wing bits. Game: a bat dies. | sound: gib splat |
| `fx_bat_hit` | Bat hit | Small red blood spray with drops. Game: a bat is hit. | sound: small roar |
| `fx_batteringram_fire` | Battering ram fire | Grey smoke with spikes, an expanding shockwave and flame spikes. Game: battering ram punch. | sound: piston impact; camera shake 0.9 |
| `fx_bjorn_death` | Bear death | Red-brown blood puff with fur and skin flakes. Game: Bjorn (bear) dies. | sound: death roar |
| `fx_bjorn_hit` | Bear hit | Red blood spray with chunks and skin flakes. Game: Bjorn is hit. | sound: slime splat |
| `fx_blobLava_explosion` | Lava blob explosion | Fire burst with red flame spikes, sparks, hanging glow, a heat-shimmer shockwave and a lava splash. Game: lava blob explodes. | camera shake 1 |
| `fx_blobtar_tarball_hit` | Tar ball hit | Black tar splatter with chunks, small fire and blobs. Game: tar ball projectile hits. | sound: gib splat; camera shake 0.5 |
| `fx_bloodweapon_hit` | Blood weapon hit | Red-dark red blood drops, smoke and a splash. Game: hit with a blood weapon. |  |
| `fx_boar_pet` | Pet boar | Small red hearts rising. Game: petting a boar. | sound: hog breath |
| `fx_candle_addfuel` | Candle add fuel | Small white-orange flame flare. Game: refuelling a resin candle. | sound: flame burst |
| `fx_candle_off` | Candle off | Small dark smoke wisp. Game: a candle goes out. | sound: extinguish |
| `fx_candle_on` | Candle on | Small white-orange flame flare. Game: a candle is lit. | sound: light candle |
| `fx_chainlightning_hit` | Chain lightning hit ✔ | Seen in game: thin white lightning line from the sky to the point (small). Dump: lightning with a shockwave ring, rock chips and a cyan glow. Game: chain lightning hit. | sound: electric explosion; camera shake 0.25 |
| `fx_chainlightning_spread` | Chain lightning jump ✔ | Seen in game: small lightning effect on the ground (small). Dump: pale cyan lightning arcs, sparks and smoke with a cyan glow. Game: chain lightning jumps to a new target. |  |
| `fx_chainlightning_spread_red` | Chain lightning jump (red) | Red lightning arcs, sparks and smoke with a red glow (red twin of fx_chainlightning_spread). Game: red chain lightning jump. |  |
| `fx_charred_death` | Charred death | Dark-red embers and glow with bones falling apart (legs, arms, ribcage, skull). Game: a Charred warrior dies. |  |
| `fx_charred_firestaff_chargeup` | Charred staff charge-up | Thin green glowing particle trail on the staff. Game: Charred mage charges its staff. | loops |
| `fx_charred_hit` | Charred hit | Small dark-red bits with an ember glow. Game: a Charred is hit. |  |
| `fx_charred_summoned_death` | Summoned charred death | Dark-red embers with a green glow, bones falling apart. Game: a summoned Charred dies. |  |
| `fx_chicken_birth` | Egg hatch | Tiny white dust puff. Game: an egg hatches. | sound: grass step |
| `fx_chicken_death` | Chicken death | Pale feathers flying with a dust puff and red blood. Game: a chicken dies. | sound: chick hurt |
| `fx_chicken_lay_egg` | Lay egg | Tiny white dust puff. Game: a hen lays an egg. | sound: grass step |
| `fx_chicken_pet` | Pet chicken | Small red hearts rising. Game: petting a hen. | sound: chicken cluck |
| `fx_clusterbombstaff_hit` | Cluster bomb hit ✔ | Seen in game: dynamite-like blast (big). Dump: fire burst with smoke, a heat-shimmer shockwave and an orange glow. Game: cluster bomb staff projectile hits. | sound: big grenade impact; camera shake 2 |
| `fx_clusterbombstaff_splinter_hit` | Cluster bomb splinter hit | Small fire burst with smoke and orange glow, a small copy of the cluster bomb blast. Game: a cluster bomb splinter lands. | sound: small grenade impact; camera shake 0.25 |
| `fx_creature_tamed` | Creature tamed | Small yellow hearts rising. Game: a creature is tamed. | sound: charm spell |
| `fx_crit` | Critical hit | Quick red-brown streak with a "bam" flash. Game: critical hit. | sound: spike impact; used by ~160 creatures |
| `fx_crystal_destruction` | Crystal wall destroyed | White-blue crystal shards bursting out with pixel sparks. Game: a crystal wall breaks. | sound: crystal break |
| `fx_deathsquito_hit` | Deathsquito hit | Small yellow-red blood spray. Game: a Deathsquito is hit. | sound: gib splat |
| `fx_deatsquito_death` | Deathsquito death | Red-yellow blood burst with a blood cloud, skin flakes and ground splats. Game: a Deathsquito dies. | sound: gib splat |
| `fx_dragon_land` | Dragon land | Brown-grey dust ring with rocks and pebbles. Game: a dragon lands. | sound: rock smash and thump; camera shake 2 within 100 m; not placed by any prefab |
| `fx_drown` | Drown | White water rings on the surface. Game: the player drowns. | sound: underwater death |
| `fx_dynamite_explosion` | Dynamite explosion ✔ | Seen in game: orange fire burst (big). Dump: fireball with flame spikes, sparks, hanging glow and a heat-shimmer shockwave. Game: dynamite or ice bomb explodes. |  |
| `fx_egg_splash` | Egg splash | Red-yellow-orange splash with drops and ground splats, orange glow. Game: a seeker egg bursts or hatches. | sound: tick death |
| `fx_eikthyr_forwardshockwave` | Eikthyr forward shockwave ✔ | Seen in game: a series of lightnings going forward in the direction the effect faces, a line not a point (medium). Dump: shockwave plane and lightning with a cyan glow. Game: Eikthyr charge. | sound: electric explosion; camera shake 1.5 |
| `fx_eikthyr_stomp` | Eikthyr stomp ✔ | Seen in game: huge lightning shockwave with flying rocks (huge). Dump: shockwave ring, rocks, lightning, strong cyan glow. Game: Eikthyr stomp. | sound: big electric explosions; camera shake 2 |
| `fx_fader_meteor_hit` | Fader meteor hit ✔ | Seen in game: big green blast with a green smoke cloud (big). Dump: boom spikes, sparks, rocks and a heat-shimmer shockwave, green glow. Game: Fader meteor lands. | sound: meteor impact; camera shake 1.3 |
| `fx_fader_meteorsmash` | Fader meteor smash | Fired in game, no description received. Dump: a whole falling meteor object with fire, ablation sparks, an explosion and a strong green glow (40 m). Game: Fader meteor smash. | sound: long meteor arrival; camera shake 2; lasts up to ~50 s; not placed by any prefab; fired in game, Maksym gave no description; not verified |
| `fx_fallenfalkyrie_spin` | Valkyrie spin | Grey-black smoke swirl with feathers and a glow. Game: Fallen Valkyrie spin. | sound: big bird call |
| `fx_fallenvalkyrie_death` | Valkyrie death | Pink-black smoke with bones, reanimation rays and a fire burst. Game: Fallen Valkyrie dies. | sound: gib splat; lasts ~17 s |
| `fx_fallenvalkyrie_poisonbreath` | Poison breath | Stream of green-black poison smoke with chunks, orange glow. Game: poison breath of the Valkyrie or Fader. | sound: flamethrower loop |
| `fx_fenring_burning_hand` | Fenring fire claw | Orange flames and embers on the hand, orange glow. Game: Fenring fire claw. | sound: flame burst |
| `fx_fenring_burning_hand_long` | Fenring fire claw (long) | Orange flames and embers on the hand for longer, orange glow. Game: Fenring flame attack. | sound: flame burst; ~3 s |
| `fx_fenring_flames` | Fenring flames | Two orange flame jets to left and right with embers, orange glow. Game: Fenring flame attack. | sound: flamethrower loop |
| `fx_fenring_frost` | Fenring frost breath | Cyan-white frost spray to both sides with icy bits, cyan glow. Game: Fenring frost breath. | sound: frost spray |
| `fx_fenring_frost_hand` | Fenring ice claw | Pale cyan frost flames on the hand, cyan glow. Game: Fenring ice claw. | sound: frost magic |
| `fx_fenring_frost_hand_aoestart` | Fenring ice nova start | Cyan frost flames on the hand, cyan glow. Game: start of Fenring ice nova. | sound: frost magic |
| `fx_fenring_frost_hand_long` | Fenring ice claw (long) | Pale cyan frost flames on the hand for longer, cyan glow. Game: Fenring frost attack. | sound: frost magic |
| `fx_fenring_icenova` | Fenring ice nova ✔ | Seen in game: faint blue mist and sparkles, "frost waves", subtle (small). Dump: fast shockwave plane, sparks and a ground puff, cyan glow. Game: Fenring ice nova. | sound: ice attack hit |
| `fx_fimbulvinter_meteor_hit` | Fimbulvinter meteor hit ✔ | Seen in game: blue dynamite-style explosion, white-yellow flash with blue sparks (big). Dump: fire, embers and smoke, strong blue glow. Game: Fimbulvinter meteor lands; also Frozen King death burst. | sound: rock rip and explosion; camera shake 1.3 |
| `fx_fireball_staff_explosion` | Fireball explosion ✔ | Seen in game: fire with black smoke, streak (big). Dump: fire burst, chunks, sparks and a heat-shimmer shockwave, orange glow. Game: fireball staff projectile hits. | sound: fire staff impact; camera shake 1 |
| `fx_fireskeleton_nova` | Fire skeleton nova ✔ | Seen in game: small ground flames, a ring of fire patches (small). Dump: fast shockwave plane with embers and a red glow. Game: Hildir fire skeleton nova. |  |
| `fx_flametalnode_destruction` | Flametal node destroyed | Blue-brown rock burst with dust, pebbles and sparks. Game: a lava flametal node is destroyed. |  |
| `fx_float_hitwater` | Float hits water | Small white water splash with a ring. Game: fishing float lands on water. | sound: bait splash |
| `fx_float_nibble` | Fish nibble | Small water ring with drops and a ripple. Game: a fish nibbles the float. | sound: bait splash |
| `fx_frostfoundry_addfuel` | Frost foundry add fuel | Pale cyan-white flame flare with smoke and cinders. Game: refuelling the Frost Foundry. | sound: flame burst |
| `fx_frostfoundry_additem` | Frost foundry add item | White flour-like puff with small flames. Game: adding an item to the Frost Foundry. | sound: oven item add |
| `fx_frozenking_aspectspawn` | Frozen King aspect spawn | Yellow-white flame spikes with swirling spores and a heat-shimmer shockwave. Game: a Frozen King aspect appears. | camera shake 0.5; not placed by any prefab |
| `fx_frozenking_chain_fury` | Frozen King chain flurry | White-cyan flame ring with tendrils and lingering sparks, blue glow. Game: Frozen King chain flurry. | camera shake 1 |
| `fx_frozenking_chain_fury_ascending` | Frozen King chain flurry lift | Sparks lifting off with tendrils and smoke, cyan glow. Game: Frozen King lifts off for the chain flurry. | camera shake 1 |
| `fx_frozenking_chain_ground_impact_1` | Frozen King chain slam | Big white-cyan flash spikes shooting up, a shockwave and ice decals on the ground, cyan glow. Game: Frozen King chain slam. | sound: frozen whip; camera shake 3; ~20 s |
| `fx_frozenking_chain_ground_impact_2` | Frozen King chain slam (heavy) | Bigger, brighter white-cyan flash spikes, shockwave and sparks, very strong cyan glow (20 m). Game: heavy chain slam. | sound: frozen whip; camera shake 1.3; not placed by any prefab |
| `fx_frozenking_chain_rush` | Frozen King chain rush | White-black smoke burst with cyan sparks and tendrils. Game: Frozen King rush. | camera shake 3 |
| `fx_frozenking_chain_rush_impact` | Frozen King rush impact | Big white-cyan flash spikes and shockwave, very strong cyan glow (20 m). Game: end of the Frozen King rush. | camera shake 1.3 |
| `fx_frozenking_punch_aoe` | Frozen King punch ✔ | Seen in game: ice cast, tall cyan ice spikes with flying dark ice chunks (big). Dump: vertical and ground flash spikes, shockwave and ice decals. Game: Frozen King punch area attack. | sound: crystal and ice shatter; camera shake 3 |
| `fx_frozenking_spikerain_hit` | Spike rain hit ✔ | Seen in game: big ice blast (big). Dump: boom spikes, sparks, rocks and a shockwave, strong cyan glow. Game: spike rain projectile lands. | sound: spike rain explosion; camera shake 1.3 |
| `fx_frozenking_spikerain_summoning` | Spike rain summoning | White-cyan chain swirls with a flare and sparks, very strong cyan glow. Game: Frozen King calls spike rain. | camera shake 1 |
| `fx_frozenking_spikesmash` | Frozen King spike smash | Seen in game (unclear match): huge black-green stone slab with green smoke and rain that blocks the view. Dump: identical to fx_fader_meteorsmash, a falling meteor object with fire and a strong green glow. Game: none found. | camera shake 2; lasts up to ~50 s; not placed by any prefab; Maksym: DO NOT USE, a huge black-green stone slab with green smoke that blocks the view |
| `fx_frozenking_spin` | Frozen King whirl | White-cyan flame rings turning both ways with spores, small stones and sparks, strong cyan glow. Game: Frozen King chain whirl. | sound: cold vortex; camera shake 1 |
| `fx_frozenking_tendrils_summoning` | Frozen King tendrils summon | White-cyan-green flares on the hands with tendrils and sparks, strong cyan glow. Game: Frozen King summons tendrils. | camera shake 1; not placed by any prefab |
| `fx_frozenking_tendrilspawn` | Frozen King tendril spawn | Blue-cyan tendrils bursting out of the ground with blue trails and splashes. Game: a Frozen King tendril appears. | sound: tendril grow; camera shake 1.3; not placed by any prefab |
| `fx_gdking_rootspawn` | Root spawn | Brown dust and rocks with a splash. Game: the Elder spawns a root. | sound: roots grow; camera shake 1.3; not placed by any prefab |
| `fx_gjall_death` | Gjall death | Huge red-black-yellow gore explosion with splats, orange glow lighting the area (40 m). Game: the Gjall dies. | sound: death; camera shake 2 |
| `fx_gjall_egg_splat` | Gjall egg splat | Red splatter with egg-shell flakes and ground splats. Game: a Gjall egg lands. | sound: tick drop |
| `fx_gjall_taunt` | Gjall taunt | Nothing visible, sound only. Game: Gjall taunt. | sound: taunt roar |
| `fx_goblinbrute_groundslam` | Goblin brute ground slam ✔ | Seen in game: small ground boom (small). Dump: rocks, dust ring and puff. Game: goblin brute ground slam. | sound: rock smash |
| `fx_goblinking_beam_hit` | Goblin king beam hit | White-pink fire burst with smoke and embers. Game: Yagluth beam hits the ground. | sound: energy explosion; camera shake 1 |
| `fx_goblinking_death` | Goblin king death | Huge red-pink-orange gore explosion with chunks and splats, strong pink glow. Game: Yagluth dies. | sound: large explosion; camera shake 1.2 within 100 m; ~30 s |
| `fx_goblinking_hit` | Goblin king hit | Pale pink slime splat with chunks. Game: Yagluth is hit. | sound: slime splat |
| `fx_goblinking_meteor_hit` | Goblin king meteor hit ✔ | Seen in game: big pink-red fire blast cloud that covers the player (huge). Dump: fire, embers and smoke, strong pink glow. Game: Yagluth or Ashlands meteor lands. | sound: rock rip and explosion; camera shake 1.3 |
| `fx_goblinking_nova` | Goblin king nova ✔ | Seen in game: huge blue flame explosion around the player (huge). Dump: rock burst, dust rings and a shockwave, strong blue glow. Game: Yagluth nova. | sound: energy shockwave; camera shake 1.5 |
| `fx_greenroots_projectile_hit` | Green roots hit | Green flame spikes with spores and a heat-shimmer shockwave. Game: Elder staff (green roots) projectile lands. | camera shake 1 |
| `fx_guardstone_activate` | Ward activate | Blue-white glowing dome that pulses out with sparks. Game: a ward is activated. | sound: dome shield on |
| `fx_guardstone_deactivate` | Ward deactivate | White-orange dome pulse fading with sparks. Game: a ward is deactivated. | sound: dome shield |
| `fx_guardstone_permitted_add` | Ward permitted add | Green pulse with sparks. Game: a player is added to a ward. | sound: force field |
| `fx_guardstone_permitted_removed` | Ward permitted remove | Red pulse with sparks. Game: a player is removed from a ward. | sound: force field |
| `fx_hare_death` | Hare death | Red-brown blood puff with skin flakes. Game: a hare dies. |  |
| `fx_hen_death` | Hen death | White feathers flying with a dust puff and a red splat. Game: a hen dies. | sound: bird cry |
| `fx_hen_love` | Hen love | Small red hearts rising. Game: a hen shows love (breeding). | sound: rooster |
| `fx_himminafl_aoe` | Himminafl area strike ✔ | Seen in game: single lightning strike, same look as fx_Lightning or fx_chainlightning_hit (small). Dump: lightning with a shockwave ring, rock chips and a strong cyan glow (20 m). Game: Himminafl atgeir area strike. | sound: electric explosion; camera shake 0.3 |
| `fx_himminafl_hit` | Himminafl hit | Lightning with a shockwave ring and rock chips, cyan glow (same look as fx_himminafl_aoe, smaller light). Game: Himminafl atgeir hit. | sound: electric explosion; camera shake 0.25 |
| `fx_hottub_addwood` | Hot tub add wood | White flame flare with smoke and cinders. Game: adding wood to the hot tub. | sound: flame burst |
| `fx_icefloor_destruction` | Ice floor shatter | Blue ice floor breaking into 36 pieces with snow dust and pixel sparks. Game: an ice floor breaks. | sound: crystal break; mesh gibs; not placed by any prefab |
| `fx_iceshard_hit` | Ice shard hit ✔ | Seen in game: small ice blast, blue sparks and mist (small). Dump: ice burst, rock chips, smoke and a cyan glow. Game: ice shard, frost orb or Orb of Ahri projectile hits. | sound: ice staff impact |
| `fx_iceshard_launch` | Ice shard launch | White-blue cold sparks with a smoke puff. Game: an ice staff fires. | sound: ice staff shot |
| `fx_icicle_destruction` | Icicle destroyed ✔ | Seen in game: ice fall, blue ice shards and a white mist puff (medium). Game: fractured ice floor is destroyed. | sound: crystal break |
| `fx_jelly_pickup` | Royal jelly pickup | Red-yellow-orange drops and chunks with ground splats. Game: picking royal jelly. | sound: wet splash |
| `fx_jotunbane_hit` | Jotun Bane hit | Yellow-green poison drops, splash and smoke, small green glow. Game: hit with Jotun Bane. |  |
| `fx_jotunbane_swing` | Jotun Bane swing trail | Trail of yellow-red drips and poison smoke. Game: Jotun Bane swing. |  |
| `fx_lavabomb_explosion` | Lava bomb explosion ✔ | Seen in game: flame burst with sparks and smoke (big). Dump: flame spikes, hanging glow, bubbling and a heat-shimmer shockwave. Game: lava bomb explodes. |  |
| `fx_leviathanLava_leave` | Lava leviathan sinks | Dark smoke and dust with small rocks, lava splashes and sparks rising from the sea of lava. Game: the lava leviathan sinks. | sound: sinking; camera shake 1; ~60 s |
| `fx_leviathanLava_reaction` | Lava leviathan reaction | Dark smoke, small rocks, lava splashes and sparks. Game: the lava leviathan is disturbed. | sound: taunt; camera shake 1 within 100 m |
| `fx_leviathan_leave` | Leviathan leaves | Green-grey water foam, rings and spray with dust and small rocks. Game: the leviathan dives away. | sound: growl, rumble, earth crack; camera shake 1 within 100 m; ~60 s |
| `fx_leviathan_reaction` | Leviathan reaction | Green-grey water foam, rings and spray with dust and small rocks. Game: the leviathan is disturbed. | sound: growl and rumble loop; camera shake 1 within 100 m |
| `fx_lightningstaffprojectile_hit` | Lightning staff hit ✔ | Seen in game: small red lightning on the target (small). Dump: dust ring and lightning with a red glow. Game: lightning staff projectile hits. | sound: electric explosion; camera shake 0.1 |
| `fx_lightningweapon_hit` | Lightning weapon hit | Cyan sparks and small lightning with a dust ring, cyan glow. Game: hit with a lightning weapon. | sound: electro spell; camera shake 0.1 |
| `fx_lox_death` | Lox death | Red-brown blood puff with fur and skin flakes. Game: a Lox dies. | sound: death roar |
| `fx_lox_hit` | Lox hit | Red blood spray with chunks and skin flakes. Game: a Lox is hit. | sound: slime splat |
| `fx_lox_pet` | Pet Lox | Small red hearts rising. Game: petting a Lox or Moose. | sound: growl and breath |
| `fx_lox_tamed` | Lox tamed | Small yellow hearts rising. Game: a Lox or Moose is tamed. | sound: charm spell |
| `fx_loxcalf_death` | Lox calf death | Red-brown blood puff with fur and skin flakes. Game: a Lox calf dies. | sound: death roar |
| `fx_moose_birth` | Moose birth | Small red slime splash. Game: a moose calf is born. |  |
| `fx_moose_death` | Moose death | Red-orange blood burst with fur and skin flakes. Game: a Moose dies. | sound: death explode |
| `fx_moose_hit` | Moose hit | Red blood spray with chunks and skin flakes. Game: a Moose is hit. | sound: slime splat |
| `fx_morgen_death` | Morgen death | Red blood cloud with flying bones and ground splats. Game: Morgen dies. |  |
| `fx_morgen_hit` | Morgen hit | Small red blood puff. Game: Morgen is hit. | not placed by any prefab |
| `fx_morgen_roll` | Morgen roll | Red-black dust clouds with a landing splat and rocks. Game: Morgen rolls or back-slams. | camera shake 1.3 |
| `fx_morgen_swipe` | Morgen swipe | Red-black dust clouds. Game: Morgen swipe. | not placed by any prefab |
| `fx_natureweapon_hit` | Nature weapon hit | Small green glowing particles with a green light. Game: hit with a nature weapon. |  |
| `fx_oven_add_food` | Oven add food | White flour puff with small flames and dust. Game: putting food in the oven. | sound: oven item add |
| `fx_oven_add_wood` | Oven add wood | White flame flare with smoke and cinders. Game: adding wood to the oven. | sound: flame burst |
| `fx_oven_produce` | Oven produce | White flour puff with smoke and small flames. Game: the oven finishes an item. | sound: take bread; also Frost Foundry |
| `fx_perfectdodge` | Perfect dodge | Orange glowing ring flash. Game: perfect dodge. | sound: perfect dodge |
| `fx_pheromonebomb_explode` | Pheromone bomb | White-green dust puffs with a green glow. Game: pheromone bomb bursts. | not placed by any prefab |
| `fx_radiation_hit` | Radiation hit | Small green glowing particles with a green light. Game: hit by radiation. | sound: impact |
| `fx_redlightning_burst` | Red lightning burst | Dark red lightning with sparks and smoke, white flash. Game: red lightning burst. | sound: ice staff shot; not placed by any prefab |
| `fx_redlightning_launch` | Red lightning launch | Dark red lightning with sparks and smoke. Game: blood thunder staff fires. | sound: ice staff shot |
| `fx_refinery_addfuel` | Refinery add fuel | Green sap drops and splash. Game: refuelling the Eitr refinery. | sound: water splash |
| `fx_refinery_addtissue` | Refinery add tissue | White-orange dust with drops and skin flakes. Game: adding tissue to the Eitr refinery. | sound: crunch |
| `fx_refinery_destroyed` | Refinery destroyed | Grey rock and dust burst with fire, embers and 12 falling pieces, green and pink glow. Game: the Eitr refinery is destroyed. | ~30 s |
| `fx_refinery_produce` | Refinery produce | Green-white glow with small flames and embers, green light. Game: the Eitr refinery produces. | sound: steam burst |
| `fx_seeker_death` | Seeker death | Red-black-green gore with flying legs and wings, ground splats, orange glow. Game: a Seeker dies. | sound: death |
| `fx_seeker_hurt` | Seeker hurt | Green slime splatter with drops and ground splats. Game: a Seeker or the Queen is hurt. |  |
| `fx_seeker_melee_hit` | Seeker melee hit | Nothing visible, sound and camera shake only. Game: Seeker claw or slam hits. | sound: attack voice; camera shake 1 |
| `fx_seeker_spawn` | Seeker spawn | Yellow-green slime splash with skin flakes and ground splats. Game: a Seeker or brood spawns. | sound: wet splats |
| `fx_seekerbrute_death` | Seeker brute death | Big red-black-green gore burst with legs and wings, orange glow. Game: a Seeker brute dies. | sound: death; camera shake 1.5 |
| `fx_shaman_fireball_expl` | Shaman fireball hit | White-purple flame burst with smoke and a shockwave, pink glow. Game: goblin shaman fireball lands. | sound: energy grenade; camera shake 1 |
| `fx_shaman_protect` | Shaman shield cast | White-grey shield shockwave rings with rising particles and dust, blue glow. Game: goblin shaman casts its shield. | sound: black-hole loop and zap; camera shake 1 |
| `fx_shield_start` | Staff shield cast | Red-pink orb drops to the ground and bursts into a shockwave with absorb particles, red glow. Game: staff shield cast. | sound: protector shield; camera shake 1.5 |
| `fx_shield_start_frost` | Frost shield cast | White-blue orb drops to the ground and bursts into a shockwave, blue glow. Game: frost orbs staff shield cast. | sound: frost shield spawn; camera shake 1.5 |
| `fx_shieldgenerator_attack` | Shield generator attack | Brown rock burst, ground shockwave ring and lightning, strong red glow (30 m). Game: the shield generator attacks. | sound: lightning and electricity; camera shake 2 |
| `fx_shieldgenerator_domehit` | Shield dome hit | Green-white ripple and a heat-shimmer shockwave on the dome. Game: the shield dome is hit. | camera shake 0.3 |
| `fx_siegebomb_explosion` | Siege bomb explosion | Fireball with red flame spikes, sparks, hanging glow and a heat-shimmer shockwave. Game: siege bomb or battering ram blast. |  |
| `fx_skeleton_pet` | Pet skeleton | Small red hearts rising. Game: petting a friendly skeleton. | sound: skeleton pet |
| `fx_sledge_demolisher_hit` | Sledge demolisher hit | White-orange-blue ground waves and bubble wave with a cloud, rock bursts and splashes, blue glow. Game: demolisher sledge hits. | sound: sledge and demolisher impact; camera shake 1.3 |
| `fx_stone_L_destroyed` | Stone L destroyed | Blue-white smoke, dark evil smoke and small stones, cyan glow. Game: a stone is destroyed. | sound: rock smash; not placed by any prefab |
| `fx_stone_R_destroyed` | Stone R destroyed | Blue-white smoke, dark evil smoke and small stones, cyan glow. Game: a stone is destroyed. | sound: rock smash; not placed by any prefab |
| `fx_summon_skeleton` | Summon skeleton | White-pink swirl spinning on the ground with dust, pink glow. Game: skeleton staff summon. | sound: summon skeleton; camera shake 1.5 |
| `fx_summon_skeleton_spawn` | Skeleton spawns | White-pink-purple vortex swirl on the ground with dust, pebbles and rocks, pink glow. Game: a skeleton rises where the projectile lands. | sound: black-hole loop and skeleton spawn; camera shake 1 |
| `fx_summon_spirit_spawn` | Spirit spawns | White-pink-purple vortex swirl on the ground with dust, pebbles and rocks, pink glow (same as skeleton spawn). Game: a spirit appears. | sound: spirit caller summon; camera shake 1; not placed by any prefab |
| `fx_summon_start` | Summon start | White-grey shockwave rings with rising particles, falling rain and dust, green glow. Game: summon start. | sound: summon skeleton; camera shake 1.5; not placed by any prefab |
| `fx_summon_troll` | Summon troll | White-red swirl spinning on the ground with dust, red glow. Game: troll staff summon. | sound: troll staff cast; camera shake 1.5 |
| `fx_summon_twitcher_spawn` | Twitcher spawns | Green-yellow-brown swirl with dust, pebbles, rocks and spores, green glow. Game: a twitcher appears. | sound: charred mage spawn; camera shake 1; not placed by any prefab |
| `fx_sw_addflax` | Spinning wheel add flax | Green-white fluff puff. Game: adding flax to the spinning wheel. | sound: grass body fall |
| `fx_sw_produce` | Spinning wheel produce | White fluff and smoke puff. Game: the spinning wheel produces thread. | sound: pile-driver hit |
| `fx_tar_bubbles` | Tar bubbles | Black-grey tar bubbles popping with rings on the surface. Game: tar pit ambience. | sound: bubble loop; random ambient |
| `fx_tendril_death` | Tendril death | Cyan goo burst with a blood cloud, blobs and splats. Game: a Frozen King tendril dies. | sound: tendril death |
| `fx_tentaroot_death` | Tentaroot death | Red-yellow-olive gore with a blood cloud, blobs and splats. Game: a Tentaroot dies. | sound: gib splat |
| `fx_tick_death` | Tick death | Red blood puff with a blood cloud and splats. Game: a tick dies. | sound: tick death |
| `fx_tombstone_destroyed` | Tombstone removed | Grey rock chips and sparks with a yellow glow. Game: a player tombstone is removed. | sound: rock smash |
| `fx_totem_destroyed` | Totem destroyed | Pink-white shell flakes and splashes. Game: a boss totem holder breaks. | sound: crack |
| `fx_trap_arm` | Trap arm | Tiny white puff. Game: a trap is armed. | sound: trap open |
| `fx_trap_trigger` | Trap trigger | White puff with hit sparks. Game: a trap snaps shut. | sound: trap close; camera shake 1 within 20 m |
| `fx_troll_love` | Troll love | Small red hearts rising. Game: a troll shows love (pheromones). |  |
| `fx_turret_addammo` | Turret add ammo | Small white dust and sparks. Game: loading a turret. | sound: bow nock |
| `fx_turret_fire` | Turret fire | Blue-white muzzle smoke with spikes and a shockwave. Game: a turret shoots. | sound: bolt fire; camera shake 0.9 |
| `fx_turret_newtarget` | Turret new target | Nothing visible, sound only. Game: a turret locks a target. | sound: metal click |
| `fx_turret_notarget` | Turret lost target | Nothing visible, sound only. Game: a turret loses its target. | sound: metal click |
| `fx_turret_reload` | Turret reload | Tiny white dust puff. Game: a turret reloads. | sound: bow nock |
| `fx_turret_warmup` | Turret warm-up | Nothing visible, sound only. Game: a turret warms up. | sound: trap open |
| `fx_unbjorn_death` | Unbjorn death | Red-brown blood puff with flakes. Game: an Unbjorn dies. | sound: death roar; not placed by any prefab |
| `fx_unbjorn_hit` | Unbjorn hit | Red blood spray with chunks and flakes. Game: an Unbjorn is hit. | sound: slime splat; not placed by any prefab |
| `fx_unstablelavarock_explosion` | Unstable lava rock explosion | Dark-red fireball with flame spikes, hanging glow, flying rocks and a heat-shimmer shockwave. Game: an unstable lava rock explodes. |  |
| `fx_vinegreen_destruction` | Green vine destroyed | Dark grey-green smoke with falling leaves and twigs. Game: a green vine is destroyed. |  |
| `fx_vines_hit` | Vine hit | Small puff of green leaves. Game: a vine is hit. | sound: leaves |
| `fx_wolf_pet` | Pet wolf | Small red hearts rising. Game: petting a wolf. | sound: dog growl |
| `fx_writhan_explosion` | Writhan explosion | Green-white fireball with flame spikes, sparks, hanging glow, a shockwave and a splash, yellow glow. Game: Writhan explodes. | camera shake 1 |
