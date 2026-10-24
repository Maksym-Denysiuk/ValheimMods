using System.Collections.Generic;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.WorldCommands
{
    // repair: building pieces (walls, floors, doors, ships, ...: everything with WearNTear) back to full health.
    //   aim    true = the object under the crosshair, false = everything within 'radius'
    //   radius metres around the player, used only when aim is false
    //   amount HP to set, default 0 = full
    public sealed class RepairCommand : CommandBase
    {
        public RepairCommand()
        {
            SubFlag("report", "report=1 [radius=10] (health of every piece around you, changes nothing)", delegate (Args a) { return Report(a.Float("radius", 10f)); });
        }

        // health / max of the pieces within 'radius' m, read from the ZDO like the repair itself
        static string Report(float radius)
        {
            Player p = Players.Local;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            int n = 0;
            foreach (WearNTear w in UnityEngine.Object.FindObjectsOfType<WearNTear>())
            {
                float d = Vector3.Distance(w.transform.position, p.transform.position);
                if (d > radius) continue;
                ZNetView nv = w.GetComponent<ZNetView>();
                if (nv == null || !nv.IsValid()) continue;
                sb.Append(Prefabs.BaseName(w.gameObject)).Append(" at ").Append(d.ToString("F1")).Append(" m: ")
                  .Append(nv.GetZDO().GetFloat("health", w.m_health).ToString("F1")).Append(" / ").Append(w.m_health.ToString("F0")).Append('\n');
                n++;
            }
            return n == 0 ? "no pieces within " + radius.ToString("F0") + " m" : sb.ToString();
        }

        public override string Name { get { return "repair"; } }
        protected override string Usage { get { return "repair [aim=1] [radius=10 (with aim=0)] [amount=0 (= full)] - building pieces back to full health"; } }

        protected override string Execute(Args a)
        {
            bool aim = a.Flag("aim", true);
            float radius = a.Float("radius", 10f), amount = a.Float("amount", 0f);
            Player p = Players.Local;
            List<WearNTear> targets = new List<WearNTear>();
            if (aim)
            {
                RaycastHit hit;
                if (!Aim.SolidHit(30f, out hit)) return "nothing under the crosshair (30 m)";
                WearNTear w = hit.collider.GetComponentInParent<WearNTear>();
                if (w == null) return "the object under the crosshair (" + hit.collider.transform.root.name + ") cannot be repaired";
                targets.Add(w);
            }
            else
            {
                if (radius <= 0f) throw new CommandException("radius must be > 0");
                foreach (WearNTear w in UnityEngine.Object.FindObjectsOfType<WearNTear>())
                    if (Vector3.Distance(w.transform.position, p.transform.position) <= radius) targets.Add(w);
            }
            int fixedCount = 0, full = 0;
            foreach (WearNTear w in targets)
            {
                ZNetView nv = w.GetComponent<ZNetView>();
                if (nv == null || !nv.IsValid()) continue;
                float cur = nv.GetZDO().GetFloat("health", w.m_health);
                float want = amount > 0f ? Mathf.Min(amount, w.m_health) : w.m_health;
                if (cur >= want - 0.01f) { full++; continue; }
                nv.ClaimOwnership();
                nv.GetZDO().Set("health", want);
                nv.InvokeRPC(ZNetView.Everybody, "RPC_HealthChanged", want);
                fixedCount++;
            }
            return "repaired " + fixedCount + " of " + targets.Count + " objects (" + full + " already at full health), "
                + (aim ? "crosshair" : "radius " + radius.ToString("F0") + " m");
        }
    }
}
