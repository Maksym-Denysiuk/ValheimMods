using System.Collections.Generic;
using System.Text;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.WorldCommands
{
    // harvest (W2-world-field, 2026-10-25): picks every ripe Pickable on the cultivated patch you stand on and the cultivated soil
    // joined to it (FieldPatch.Sample, 8 neighbours). A vanilla pick: Pickable.Interact(you, false, false), the same call as pressing E,
    // so the owner drops the items at each crop and auto-pickup takes the ones within reach; the rest lie on the ground.
    // Pickables that are not ripe, already picked or off the patch are left alone; wild pickables standing on the patch are picked too.
    public sealed class HarvestCommand : CommandBase
    {
        const int MaxPicked = 2500;

        public override string Name { get { return "harvest"; } }
        protected override string Usage
        {
            get { return "harvest - picks every ripe crop on the cultivated patch you stand on (and the soil joined to it), as by hand: drops at each crop; cap " + MaxPicked + " picks, " + FieldPatch.MaxSamples + " soil samples"; }
        }

        protected override string Execute(Args a)
        {
            Host.RequireWorld();
            Player p = Players.Local;
            FieldPatch patch = FieldPatch.Sample(p.transform.position);
            if (!patch.StartSoil) return "nothing harvested: stand on cultivated soil";
            int picked = 0, notRipe = 0, capped = 0;
            Dictionary<string, int> byName = new Dictionary<string, int>();
            foreach (Pickable pk in UnityEngine.Object.FindObjectsOfType<Pickable>())
            {
                if (pk == null || !patch.Contains(pk.transform.position)) continue;
                if (!pk.CanBePicked()) { notRipe++; continue; }
                if (picked >= MaxPicked) { capped++; continue; }
                pk.Interact(p, false, false);
                picked++;
                string name = Prefabs.BaseName(pk.gameObject);
                int n; byName.TryGetValue(name, out n); byName[name] = n + 1;
            }
            StringBuilder per = new StringBuilder();
            foreach (KeyValuePair<string, int> kv in byName) per.Append(per.Length > 0 ? " " : "").Append(kv.Key).Append("=").Append(kv.Value);
            return "harvested " + picked + (picked > 0 ? " (" + per + ")" : "") + " on the patch under you (about " + Fmt.F(patch.Area, 0) + " m2); "
                + notRipe + " not ripe or already picked" + (capped > 0 ? ", " + capped + " left over the cap of " + MaxPicked : "")
                + (patch.OverCap ? "; sampling stopped at the cap of " + FieldPatch.MaxSamples + " cells, crops beyond it were left" : "")
                + (picked > 0 ? "; drops lie at each crop, auto-pickup takes the ones within reach" : "");
        }
    }
}
