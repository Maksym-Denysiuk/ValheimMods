using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.WorldCommands
{
    // scale: trees near the player change size, hit points and drops; saved with the world and re-applied on load by the core
    public sealed class ScaleCommand : CommandBase
    {
        public override string Name { get { return "scale"; } }
        protected override string Usage { get { return "scale filter=<tree name> [factor=2] [radius=50] - resize trees around you (kept on reload)"; } }

        protected override string Execute(Args a)
        {
            string filter = a.Required("filter", "e.g. filter=Oak");
            float factor = a.Float("factor", 2f), radius = a.Float("radius", 50f);
            Vector3 c = Players.Local.transform.position;
            int n = 0;
            foreach (TreeBase t in UnityEngine.Object.FindObjectsOfType<TreeBase>())
            {
                if (t.name.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (Vector3.Distance(t.transform.position, c) > radius) continue;
                ZNetView nv = t.GetComponent<ZNetView>();
                if (nv == null || !nv.IsValid()) continue;
                nv.ClaimOwnership();
                nv.GetZDO().Set(Trees.ScaleKey, Vector3.one * factor);
                Trees.ApplyScale(t, factor);
                n++;
            }
            return "scaled " + n + " '" + filter + "' trees within " + radius + " m to x" + factor;
        }
    }
}
