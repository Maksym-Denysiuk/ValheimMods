// The real height of a piece: its colliders' bottom and top relative to its pivot, measured on a ghost the way the hammer makes
// its placement preview (Player.SetupPlacementGhost sets ZNetView.m_forceDisableInit around Instantiate, so no ZDO is created
// and nothing is sent; IL 1.0.20). Cached per prefab and turn. Only pieces are measured; anything else counts as pivot = bottom.
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace HomeCheat.Features.Construction
{
    internal static class PieceGhost
    {
        static readonly Vector3 Probe = new Vector3(0f, 5000f, 0f);   // far above any world, removed in the same frame
        static readonly FieldInfo ForceDisableInit = typeof(ZNetView).GetField("m_forceDisableInit", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        static readonly Dictionary<string, float[]> cache = new Dictionary<string, float[]>();

        // minOff / maxOff = collider bottom / top minus the pivot height; false (both 0) when it cannot be measured
        public static bool Measure(GameObject prefab, Quaternion rot, out float minOff, out float maxOff)
        {
            minOff = maxOff = 0f;
            if (prefab == null || prefab.GetComponent<Piece>() == null || ForceDisableInit == null) return false;
            Vector3 e = rot.eulerAngles;
            string key = prefab.name + "|" + Mathf.RoundToInt(e.x) + "|" + Mathf.RoundToInt(e.y) + "|" + Mathf.RoundToInt(e.z);
            float[] v;
            if (cache.TryGetValue(key, out v)) { minOff = v[0]; maxOff = v[1]; return v[2] > 0f; }
            GameObject g = null;
            bool ok = false;
            try
            {
                ForceDisableInit.SetValue(null, true);
                try { g = (GameObject)Object.Instantiate(prefab, Probe, rot); }
                finally { ForceDisableInit.SetValue(null, false); }
                Physics.SyncTransforms();
                Bounds bb = new Bounds();
                bool any = false;
                foreach (Collider c in g.GetComponentsInChildren<Collider>())
                {
                    if (c == null || c.isTrigger || !c.enabled) continue;
                    if (!any) { bb = c.bounds; any = true; } else bb.Encapsulate(c.bounds);
                }
                if (!any)
                    foreach (Renderer r in g.GetComponentsInChildren<Renderer>())
                    {
                        if (r == null) continue;
                        if (!any) { bb = r.bounds; any = true; } else bb.Encapsulate(r.bounds);
                    }
                if (any) { minOff = bb.min.y - Probe.y; maxOff = bb.max.y - Probe.y; ok = true; }
            }
            catch (System.Exception ex) { HomeApi.Log("construct: ghost of " + prefab.name + ": " + ex.Message); }
            finally { if (g != null) Object.Destroy(g); }
            cache[key] = new float[] { minOff, maxOff, ok ? 1f : 0f };
            return ok;
        }
    }
}
