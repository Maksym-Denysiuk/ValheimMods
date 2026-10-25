using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.WorldCommands
{
    // scale: trees near the player change size, hit points and drops; saved with the world and re-applied on load by the core
    // aim=1 (W3-world-plant, 2026-10-25): only the tree under the crosshair; filter=* matches every tree in the radius.
    // Tree prefabs sync their scale (m_syncInitialScale), applied when a tree loads; Apply makes the tree again so it shows at once.
    public sealed class ScaleCommand : CommandBase
    {
        const float MaxFactor = 20f;
        const float AimRange = 60f;

        public ScaleCommand()
        {
            SubFlag("probe", "probe=1 (the tree under the crosshair: does its ZNetView sync its scale to other players (m_syncInitialScale), its size now and saved; read-only)",
                delegate (Args a) { return Probe(); });
        }

        public override string Name { get { return "scale"; } }
        protected override string Usage
        {
            get
            {
                return "scale filter=<tree name|*> [factor=2] [radius=50] [aim=0 (1 = only the tree under the crosshair, " + AimRange
                    + " m; filter and radius not needed)] - resize trees around you (the tree is made again, so every player sees the new size at once; kept on reload; damage resets); factor cap " + MaxFactor;
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
                + " (made again: every player sees it)";
        }

        string Probe()
        {
            Player p = Players.Local;
            RaycastHit hit;
            if (!Aim.SolidHit(AimRange, out hit)) throw new CommandException("nothing under the crosshair (" + AimRange + " m): look at a tree");
            TreeBase t = hit.collider.GetComponentInParent<TreeBase>();
            if (t == null) throw new CommandException("the object under the crosshair (" + hit.collider.transform.root.name.Replace("(Clone)", "") + ") is not a tree");
            ZNetView nv = t.GetComponent<ZNetView>();
            object sync = nv == null ? null : Refl.Get(nv, "m_syncInitialScale");
            Vector3 saved = nv != null && nv.IsValid() ? nv.GetZDO().GetVec3(Trees.ScaleKey, Vector3.zero) : Vector3.zero;
            return "tree " + Prefabs.BaseName(t.gameObject) + ": m_syncInitialScale=" + (sync == null ? "?" : sync.ToString()) + ", size now " + Fmt.Vec(t.transform.localScale)
                + ", saved scale " + Fmt.Vec(saved) + ", owner here " + (nv != null && nv.IsValid() && nv.IsOwner());
        }

        // Tree prefabs sync their scale (ZNetView.m_syncInitialScale, probe=1), but a client applies it only when the tree loads (Awake).
        // So the tree is made again (Maksym 2026-10-25: other players, without HomeCheat, see the new size at once): the old object
        // goes, a new one of the same prefab takes its place with the scale in its ZDO before it reaches the others. Damage is reset.
        static bool Apply(TreeBase t, float factor)
        {
            ZNetView nv = t.GetComponent<ZNetView>();
            if (nv == null || !nv.IsValid()) return false;
            GameObject prefab = Prefabs.Get(Prefabs.BaseName(t.gameObject));
            if (prefab == null)
            {
                // unknown prefab name: size here, others see it when the tree loads again
                nv.ClaimOwnership();
                nv.GetZDO().Set(Trees.ScaleKey, Vector3.one * factor);
                Trees.ApplyScale(t, factor);
                return true;
            }
            Vector3 pos = t.transform.position;
            Quaternion rot = t.transform.rotation;
            nv.ClaimOwnership();
            ZNetScene.instance.Destroy(t.gameObject);
            GameObject go = (GameObject)UnityEngine.Object.Instantiate(prefab, pos, rot);
            ZNetView nn = go.GetComponent<ZNetView>();
            TreeBase nt = go.GetComponent<TreeBase>();
            if (nn == null || !nn.IsValid() || nt == null) return false;
            nn.GetZDO().Set(Trees.ScaleKey, Vector3.one * factor);
            Trees.ApplyScale(nt, factor);
            return true;
        }
    }
}
