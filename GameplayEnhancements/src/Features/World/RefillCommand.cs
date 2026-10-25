using System.Collections.Generic;
using System.Text;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.WorldCommands
{
    // refill: tops up the fuel of fire places (campfire, hearth, brazier, torches, sconces) and smelting stations
    // (kiln, smelter, furnace). Same parameters as repair; amount = fuel units, default 0 = the object's own maximum.
    // Only the stored amount changes; no inventory items are used.
    // fuel=<item name part> (W3-world-plant, 2026-10-25): only objects whose own fuel item (m_fuelItem prefab name: Wood, Coal,
    // Resin, ...) contains it; the others are counted as skipped.
    public sealed class RefillCommand : CommandBase
    {
        public override string Name { get { return "refill"; } }
        protected override string Usage
        {
            get { return "refill [aim=1] [radius=10 (with aim=0)] [amount=0 (= full)] [fuel=<item name part, e.g. Wood, Coal, Resin; default any>] - fuel of fireplaces and smelters"; }
        }

        protected override string Execute(Args a)
        {
            Host.RequireWorld();
            bool aim = a.Flag("aim", true);
            float radius = a.Float("radius", 10f), amount = a.Float("amount", 0f);
            string fuel = a.Str("fuel", "");
            Player p = Players.Local;
            List<Component> targets = new List<Component>();
            if (aim)
            {
                RaycastHit hit;
                if (!Aim.SolidHit(30f, out hit)) return "nothing under the crosshair (30 m)";
                Component f = hit.collider.GetComponentInParent<Fireplace>();
                if (f == null) f = hit.collider.GetComponentInParent<Smelter>();
                if (f == null) return "the object under the crosshair (" + hit.collider.transform.root.name + ") has no fuel";
                if (fuel.Length > 0 && !FuelMatches(f, fuel))
                    return "the object under the crosshair (" + f.name.Replace("(Clone)", "") + ") burns " + FuelName(f) + ", not '" + fuel + "'";
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
            int done = 0, full = 0, other = 0;
            foreach (Component c in targets)
            {
                if (fuel.Length > 0 && !FuelMatches(c, fuel)) { other++; continue; }
                ZNetView nv = c.GetComponent<ZNetView>();
                if (nv == null || !nv.IsValid()) continue;
                float max;
                Fireplace fp = c as Fireplace;
                Smelter sm = c as Smelter;
                if (fp != null) { if (fp.m_infiniteFuel) continue; max = fp.m_maxFuel; }
                else max = sm.m_maxFuel;
                float set = amount > 0f ? Mathf.Min(amount, max) : max;
                float before = nv.GetZDO().GetFloat("fuel", 0f);
                if (before >= set - 0.01f) { full++; continue; }
                nv.ClaimOwnership();
                nv.GetZDO().Set("fuel", set);
                sb.Append(c.name.Replace("(Clone)", "")).Append(": ").Append(before.ToString("F0")).Append(" -> ").Append(set.ToString("F0"))
                  .Append(" / ").Append(max.ToString("F0")).Append('\n');
                done++;
            }
            string filter = fuel.Length > 0 ? ", fuel '" + fuel + "': " + other + " with other fuel skipped" : "";
            return "refilled " + done + " of " + targets.Count + " objects (" + full + " already full, " + (aim ? "crosshair" : "radius " + radius.ToString("F0") + " m") + filter + ")\n" + sb;
        }

        // the prefab name of the object's own fuel item ("none" when it has none, e.g. a kiln)
        static string FuelName(Component c)
        {
            Fireplace fp = c as Fireplace;
            Smelter sm = c as Smelter;
            ItemDrop item = fp != null ? fp.m_fuelItem : (sm != null ? sm.m_fuelItem : null);
            return item == null ? "none" : item.gameObject.name;
        }

        static bool FuelMatches(Component c, string fuel)
        {
            string name = FuelName(c);
            return name != "none" && name.IndexOf(fuel, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
