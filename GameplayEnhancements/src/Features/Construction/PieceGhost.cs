// The real size of a piece: its colliders' box relative to its pivot, measured on a ghost the way the hammer makes its
// placement preview (Player.SetupPlacementGhost sets ZNetView.m_forceDisableInit around Instantiate, so no ZDO is created
// and nothing is sent; IL 1.0.20). Cached per prefab and turn. Only pieces are measured; anything else counts as a point.
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
            Vector3 lo, hi;
            bool ok = Box(prefab, rot, out lo, out hi);
            minOff = lo.y; maxOff = hi.y;
            return ok;
        }

        // the collider box (world axes, the piece turned by rot) minus the pivot; false (zero box) when it cannot be measured
        public static bool Box(GameObject prefab, Quaternion rot, out Vector3 lo, out Vector3 hi)
        {
            lo = hi = Vector3.zero;
            if (prefab == null || prefab.GetComponent<Piece>() == null || ForceDisableInit == null) return false;
            Vector3 e = rot.eulerAngles;
            string key = prefab.name + "|" + Mathf.RoundToInt(e.x) + "|" + Mathf.RoundToInt(e.y) + "|" + Mathf.RoundToInt(e.z);
            float[] v;
            if (!cache.TryGetValue(key, out v))
            {
                v = Measure(prefab, rot);
                cache[key] = v;
            }
            lo = new Vector3(v[0], v[1], v[2]); hi = new Vector3(v[3], v[4], v[5]);
            return v[6] > 0f;
        }

        static float[] Measure(GameObject prefab, Quaternion rot)
        {
            float[] v = new float[7];
            GameObject g = null;
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
                if (any)
                {
                    Vector3 lo = bb.min - Probe, hi = bb.max - Probe;
                    v[0] = lo.x; v[1] = lo.y; v[2] = lo.z; v[3] = hi.x; v[4] = hi.y; v[5] = hi.z; v[6] = 1f;
                }
            }
            catch (System.Exception ex) { HomeApi.Log("construct: ghost of " + prefab.name + ": " + ex.Message); }
            finally { if (g != null) Object.Destroy(g); }
            return v;
        }
    }
}
