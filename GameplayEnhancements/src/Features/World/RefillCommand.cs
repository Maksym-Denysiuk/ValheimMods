using System.Collections.Generic;
using System.Text;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.WorldCommands
{
    // refill: tops up the fuel of fire places (campfire, hearth, brazier, torches, sconces) and smelting stations
    // (kiln, smelter, furnace). Same parameters as repair; amount = fuel units, default 0 = the object's own maximum.
    // Only the stored amount changes; no inventory items are used.
    public sealed class RefillCommand : CommandBase
    {
        public override string Name { get { return "refill"; } }
        protected override string Usage { get { return "refill [aim=1] [radius=10 (with aim=0)] [amount=0 (= full)] - fuel of fireplaces and smelters"; } }

        protected override string Execute(Args a)
        {
            bool aim = a.Flag("aim", true);
            float radius = a.Float("radius", 10f), amount = a.Float("amount", 0f);
            Player p = Players.Local;
            List<Component> targets = new List<Component>();
            if (aim)
            {
                RaycastHit hit;
                if (!Aim.SolidHit(30f, out hit)) return "nothing under the crosshair (30 m)";
                Component f = hit.collider.GetComponentInParent<Fireplace>();
                if (f == null) f = hit.collider.GetComponentInParent<Smelter>();
                if (f == null) return "the object under the crosshair (" + hit.collider.transform.root.name + ") has no fuel";
                targets.Add(f);
            }
            else
            {
                if (radius <= 0f) throw new CommandException("radius must be > 0");
                foreach (Fireplace f in UnityEngine.Object.FindObjectsOfType<Fireplace>())
                    if (Vector3.Distance(f.transform.position, p.transform.position) <= radius) targets.Add(f);
                foreach (Smelter s in UnityEngine.Object.FindObjectsOfType<Smelter>())
                    if (Vector3.Distance(s.transform.position, p.transform.position) <= radius) targets.Add(s);
            }
            StringBuilder sb = new StringBuilder();
            int done = 0;
            foreach (Component c in targets)
            {
                ZNetView nv = c.GetComponent<ZNetView>();
                if (nv == null || !nv.IsValid()) continue;
                float max;
                Fireplace fp = c as Fireplace;
                Smelter sm = c as Smelter;
                if (fp != null) { if (fp.m_infiniteFuel) continue; max = fp.m_maxFuel; }
                else max = sm.m_maxFuel;
                float set = amount > 0f ? Mathf.Min(amount, max) : max;
                nv.ClaimOwnership();
                float before = nv.GetZDO().GetFloat("fuel", 0f);
                nv.GetZDO().Set("fuel", set);
                sb.Append(c.name.Replace("(Clone)", "")).Append(": ").Append(before.ToString("F0")).Append(" -> ").Append(set.ToString("F0"))
                  .Append(" / ").Append(max.ToString("F0")).Append('\n');
                done++;
            }
            return "refilled " + done + " of " + targets.Count + " objects (" + (aim ? "crosshair" : "radius " + radius.ToString("F0") + " m") + ")\n" + sb;
        }
    }
}
