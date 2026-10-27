// The smoke of blink at a quarter size (Maksym 2026-10-27: "2 times smaller", then smaller again at both ends; teleport's small puff stays full size).
// Same as TrapFxSets.Tune and VisualEffectCommand.Tune without speed: a third copy, so it is a Kit.Fx candidate (core change, asked first).
using System.Reflection;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.TeleportCommands
{
    static class TeleportFx
    {
        internal const float Scale = 0.25f;
        internal const float FallGuardTime = 3f;   // Maksym 2026-10-27: no fall damage for 3 s after you appear
        const float FallGuardStep = 0.1f;

        static readonly FieldInfo fMaxAir = typeof(Character).GetField("m_maxAirAltitude", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        // fall damage is the drop from m_maxAirAltitude when you touch ground (Character.UpdateGroundContact), so holding it at your
        // height every 0.1 s for 'seconds' forgives any fall in that time; a fall still going on after it counts from where you were then
        internal static void FallGuard(CommandBase owner, Player p, float seconds)
        {
            if (p == null || fMaxAir == null) return;
            fMaxAir.SetValue(p, p.transform.position.y);
            if (seconds <= 0f) return;
            owner.Later(FallGuardStep, delegate { FallGuard(owner, p, seconds - FallGuardStep); });
        }

        // particles follow the root's scale (Hierarchy; many prefabs use Local, which ignores it), the ZDO scale for other players, lights shrink too
        internal static void Shrink(GameObject go, float scale)
        {
            if (go == null || scale == 1f) return;
            Trees.SetScale(go, scale);
            foreach (ParticleSystem ps in go.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule m = ps.main;
                m.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }
            foreach (Light l in go.GetComponentsInChildren<Light>(true)) l.range = l.range * scale;
        }
    }
}
