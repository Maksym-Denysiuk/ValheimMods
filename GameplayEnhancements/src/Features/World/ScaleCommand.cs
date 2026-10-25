using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.WorldCommands
{
    // scale: trees near the player change size, hit points and drops; saved with the world and re-applied on load by the core
    // aim=1 (W3-world-plant, 2026-10-25): only the tree under the crosshair; filter=* matches every tree in the radius.
    // The size is seen only by clients with HomeCheat (TreeScalePatch): vanilla does not sync a tree's scale.
    public sealed class ScaleCommand : CommandBase
    {
        const float MaxFactor = 20f;
        const float AimRange = 60f;

        public override string Name { get { return "scale"; } }
        protected override string Usage
        {
            get
            {
                return "scale filter=<tree name|*> [factor=2] [radius=50] [aim=0 (1 = only the tree under the crosshair, " + AimRange
                    + " m; filter and radius not needed)] - resize trees around you (kept on reload, seen only with HomeCheat); factor cap " + MaxFactor;
            }
        }

        protected override string Execute(Args a)
        {
            Host.RequireWorld();
            bool aim = a.Flag("aim", false);
            float factor = a.Float("factor", 2f), radius = a.Float("radius", 50f);
            if (factor <= 0f) throw new CommandException("factor must be > 0");
            Caps.Check("factor", factor, MaxFactor);
            if (aim) return AtCrosshair(factor);
            string filter = a.Required("filter", "e.g. filter=Oak, filter=* for every tree, or aim=1 for the tree under the crosshair");
            bool any = filter == "*";
            Vector3 c = Players.Local.transform.position;
            int n = 0;
            foreach (TreeBase t in UnityEngine.Object.FindObjectsOfType<TreeBase>())
            {
                if (!any && t.name.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (Vector3.Distance(t.transform.position, c) > radius) continue;
                if (Apply(t, factor)) n++;
            }
            return "scaled " + n + " '" + filter + "' trees within " + radius + " m to x" + factor;
        }

        string AtCrosshair(float factor)
        {
            Player p = Players.Local;   // throws "no local player" before the ray
            RaycastHit hit;
            if (!Aim.SolidHit(AimRange, out hit)) throw new CommandException("nothing under the crosshair (" + AimRange + " m): look at a tree");
            TreeBase t = hit.collider.GetComponentInParent<TreeBase>();
            if (t == null) throw new CommandException("the object under the crosshair (" + hit.collider.transform.root.name.Replace("(Clone)", "") + ") is not a tree");
            if (!Apply(t, factor)) throw new CommandException("the tree under the crosshair has no valid network view");
            return "scaled 1 tree (" + Prefabs.BaseName(t.gameObject) + ") under the crosshair at " + Fmt.F(Vector3.Distance(p.transform.position, t.transform.position), 1)
                + " m from you to x" + Fmt.F(factor, 2)
                + " (seen only by players with HomeCheat)";
        }

        static bool Apply(TreeBase t, float factor)
        {
            ZNetView nv = t.GetComponent<ZNetView>();
            if (nv == null || !nv.IsValid()) return false;
            nv.ClaimOwnership();
            nv.GetZDO().Set(Trees.ScaleKey, Vector3.one * factor);
            Trees.ApplyScale(t, factor);
            return true;
        }
    }
}
