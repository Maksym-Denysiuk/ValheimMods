// The blast looks of a mine (Maksym 2026-10-25, picked from 10 previews in ClaudeTest): with fxboom=random (the mine default)
// every mine gets one set at random when it is laid; fxboom=<set name> fixes one. All prefabs of a set spawn in the same frame
// at the mine; scale and speed shrink and speed up the particles (the two ice sets are boss effects).
using System;
using System.Collections.Generic;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.Traps
{
    internal sealed class TrapFxSet
    {
        public string Name;
        public string[] Prefabs;
        public float Scale = 1f, Speed = 1f;

        TrapFxSet(string name, float scale, float speed, params string[] prefabs) { Name = name; Scale = scale; Speed = speed; Prefabs = prefabs; }

        public static readonly TrapFxSet[] All = new TrapFxSet[]
        {
            new TrapFxSet("dynamite", 1f, 1f, "fx_dynamite_explosion", "sfx_bombdynamite_explosion"),
            new TrapFxSet("grenade", 1f, 1f, "fx_clusterbombstaff_hit", "sfx_bomblava_rocks"),
            new TrapFxSet("lava", 1f, 1f, "fx_lavabomb_explosion", "sfx_unstablerock_explosion"),
            new TrapFxSet("siege", 1f, 1f, "fx_siegebomb_explosion", "fx_goblinbrute_groundslam", "sfx_bombdynamite_explosion"),
            new TrapFxSet("fireball", 1f, 1f, "fx_fireball_staff_explosion", "sfx_imp_fireball_explode"),
            new TrapFxSet("glacier", 0.5f, 2f, "fx_frozenking_punch_aoe", "fx_frozenking_chain_rush_impact", "fx_DvergerMage_Nova_ring", "sfx_frozenking_doubleslam_explosion"),
            new TrapFxSet("wrath", 0.5f, 2f, "sfx_dragon_coldball_explode", "fx_frozenking_spikerain_hit", "fx_shield_start_frost", "fx_frozenking_tendrilspawn", "fx_icicle_destruction"),
            new TrapFxSet("blue", 1f, 1f, "fx_fimbulvinter_meteor_hit", "sfx_bombdynamite_explosion"),
            new TrapFxSet("shock", 1f, 1f, "fx_himminafl_aoe", "fx_chainlightning_spread", "sfx_staff_lightning_fire"),
            new TrapFxSet("gas", 1f, 1f, "vfx_BombBlob_explode_poison", "sfx_oozebomb_explode", "sfx_Poison_Start"),
        };

        // the own look of a kind, never drawn by random: tar mines (black tar splash and smoke, the ooze bomb's sound)
        public static readonly TrapFxSet[] Own = new TrapFxSet[]
        {
            new TrapFxSet("tar", 1f, 1f, "vfx_BombBlob_explode_tar", "sfx_oozebomb_explode"),
        };

        public static string Names
        {
            get
            {
                List<string> n = new List<string>();
                foreach (TrapFxSet s in All) n.Add(s.Name);
                return string.Join("|", n.ToArray());
            }
        }

        public static TrapFxSet Get(string name)
        {
            foreach (TrapFxSet s in All) if (s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return s;
            foreach (TrapFxSet s in Own) if (s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return s;
            return null;
        }

        public static TrapFxSet Random() { return All[UnityEngine.Random.Range(0, All.Length)]; }

        // smaller or faster particles: they follow the root's scale (Hierarchy; many prefabs use Local, which ignores it), play speed
        // x 'speed' (local: other clients play it at normal speed), lights shrink with the scale. Same as VisualEffectCommand.Tune (two copies, promotion candidate for Kit.Fx).
        public static void Tune(GameObject go, float scale, float speed)
        {
            if (go == null) return;
            Trees.SetScale(go, scale);   // also the ZDO scale, so other players see the smaller size
            foreach (ParticleSystem ps in go.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule m = ps.main;
                if (scale != 1f) m.scalingMode = ParticleSystemScalingMode.Hierarchy;
                if (speed != 1f) m.simulationSpeed = m.simulationSpeed * speed;
            }
            if (scale != 1f) foreach (Light l in go.GetComponentsInChildren<Light>(true)) l.range = l.range * scale;
        }
    }
}
