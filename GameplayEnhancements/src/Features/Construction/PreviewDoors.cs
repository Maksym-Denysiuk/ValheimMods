// The doors of a planned construction, shown in its preview (Maksym 2026-10-25: "if there are any doors in the planned
// construction I want to see them in a preview. Place pillars under them to not spawn them mid-air"). Every piece with a Door
// component goes at its exact place and turn from Layout, with no creator (the keep-away check and the space clearing ignore
// it) and no OnPlaced. Where its collider bottom hangs more than the gap tolerance over the surface, core-wood poles are
// stacked from that bottom down to the surface under both ends of its width. Doors and poles carry the preview mark, so
// RemovePillars takes them down with the columns (start, remove, rotate, a new preview).
using System.Collections.Generic;
using System.Text;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.Construction
{
    internal static class PreviewDoors
    {
        const int MaxDoors = 40;
        const int MaxPolesPerEnd = 10;     // 40 m of poles
        const float EndInset = 0.25f;      // the poles stand this far inside the ends of the door's width

        internal sealed class Spot
        {
            public GameObject Prefab;
            public Vector3 Pos;
            public Quaternion Rot;
        }

        internal sealed class Result
        {
            public int Doors, Poles, Hanging, NoSurface;
            public float WorstGap;
            public string Note = "";
            public readonly Dictionary<string, int> ByPrefab = new Dictionary<string, int>();
            public readonly List<ZNetView> Placed = new List<ZNetView>();
            public readonly List<Spot> PlacedSpots = new List<Spot>();   // the spot of each Placed door, same order

            public string Text()
            {
                if (Doors == 0) return "no doors in the plan";
                StringBuilder sb = new StringBuilder();
                foreach (KeyValuePair<string, int> kv in ByPrefab) sb.Append(sb.Length == 0 ? "" : ", ").Append(kv.Key).Append(" ").Append(kv.Value);
                return Doors + " door(s) shown at their place (" + sb + ")"
                    + (Hanging > 0 ? ", " + Hanging + " hanging up to " + Fmt.F(WorstGap, 2) + " m on " + Poles + " pole(s)" : ", all on the surface")
                    + (NoSurface > 0 ? ", " + NoSurface + " door end(s) with no surface within 64 m" : "")
                    + (Note.Length > 0 ? ", " + Note : "");
            }
        }

        // the first surface below from, skipping the objects of an older preview: a new preview, a rotate or a restore places
        // its objects in the frame where the old ones are still being destroyed, and a plain ray stood the new ones on the old
        // (2026-10-25: a re-preview of the gumdrop at the same spot left 74 door ends up to 5.5 m in the air, no poles)
        public static bool SurfaceBelow(Vector3 from, float dist, string mark, out RaycastHit hit)
        {
            hit = new RaycastHit();
            RaycastHit[] hits = Physics.RaycastAll(from, Vector3.down, dist, Masks.SolidAndTerrain, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            foreach (RaycastHit h in hits)
            {
                if (h.distance >= best) continue;
                ZNetView nv = h.collider.GetComponentInParent<ZNetView>();
                if (nv != null)
                {
                    if (!nv.IsValid()) continue;   // destroyed this frame (ZNetScene.Destroy drops its ZDO at once), gone next frame
                    string m = nv.GetZDO().GetString(ConstructCommand.KeyPillar, "");
                    if (m.Length > 0 && m != mark) continue;
                }
                best = h.distance;
                hit = h;
            }
            return best < float.MaxValue;
        }

        // a piece with a Door component: doors, gates, grates; not the window shutters (wood_window also has one), which would
        // put pole columns all over a facade (ASSUMPTION 2026-10-25, unattended: Maksym asked for doors)
        public static bool IsDoor(GameObject prefab)
        {
            return prefab != null && prefab.GetComponent<Door>() != null && prefab.name.ToLowerInvariant().IndexOf("window") < 0;
        }

        // pole: the core-wood pole, poleLo its collider bottom under the pivot, poleLen its height
        public static Result Place(List<Spot> spots, string mark, GameObject pole, float poleLo, float poleLen, float gapTolerance)
        {
            Result r = new Result();
            int count = Caps.Clamp("doors", spots.Count, MaxDoors, out r.Note);
            for (int i = 0; i < count; i++)
            {
                Spot s = spots[i];
                Vector3 lo, hi;
                float bottom = PieceGhost.Box(s.Prefab, s.Rot, out lo, out hi) ? s.Pos.y + lo.y : s.Pos.y;
                bool hung = false;
                foreach (Vector3 end in Ends(s))
                {
                    if (Ground.Height(end) >= bottom - gapTolerance) continue;   // the terrain reaches the door here
                    RaycastHit h;
                    if (!SurfaceBelow(new Vector3(end.x, bottom + 0.05f, end.z), 64f, mark, out h)) { r.NoSurface++; continue; }
                    float gap = bottom - h.point.y;
                    if (gap <= gapTolerance) continue;
                    hung = true;
                    if (gap > r.WorstGap) r.WorstGap = gap;
                    for (int k = 0; k < MaxPolesPerEnd; k++)
                    {
                        float poleBottom = bottom - (k + 1) * poleLen;
                        if (Spawn(pole, new Vector3(end.x, poleBottom - poleLo, end.z), Quaternion.identity, mark) != null) r.Poles++;
                        if (poleBottom <= h.point.y) break;
                    }
                }
                if (hung) r.Hanging++;
                // the door after its poles, so it rests on them from the first frame
                ZNetView door = Spawn(s.Prefab, s.Pos, s.Rot, mark);
                if (door == null) continue;
                r.Placed.Add(door);
                r.PlacedSpots.Add(s);
                r.Doors++;
                int c;
                r.ByPrefab.TryGetValue(s.Prefab.name, out c);
                r.ByPrefab[s.Prefab.name] = c + 1;
            }
            return r;
        }

        // measure (lessons 2026-10-25: measure before you fix): how many doors stand, and under both ends of each standing one,
        // from its live collider bottom, what it rests on (a preview pole, the terrain, anything else) and the largest gap
        public static string Check(Result r, float gapTolerance)
        {
            int standing = 0, onPole = 0, onGround = 0, onOther = 0, hanging = 0;
            float worst = 0f;
            List<string> where = new List<string>();
            for (int i = 0; i < r.Placed.Count; i++)
            {
                ZNetView nv = r.Placed[i];
                if (nv == null || !nv.IsValid()) continue;
                standing++;
                float bottom = float.MaxValue;
                foreach (Collider c in nv.GetComponentsInChildren<Collider>())
                    if (c != null && !c.isTrigger && c.enabled) bottom = Mathf.Min(bottom, c.bounds.min.y);
                if (bottom == float.MaxValue) bottom = nv.transform.position.y;
                foreach (Vector3 end in Ends(r.PlacedSpots[i]))
                {
                    if (Ground.Height(end) >= bottom - gapTolerance) { onGround++; continue; }   // the terrain reaches it (a ray from inside finds nothing)
                    RaycastHit h;
                    // a ray that starts inside the door's own collider does not hit it
                    if (!Physics.Raycast(new Vector3(end.x, bottom + 0.02f, end.z), Vector3.down, out h, 64f, Masks.SolidAndTerrain, QueryTriggerInteraction.Ignore)) { hanging++; continue; }
                    float gap = bottom - h.point.y;
                    if (gap > worst) worst = gap;
                    if (gap > gapTolerance)
                    {
                        hanging++;
                        if (where.Count < 4) where.Add(Prefabs.BaseName(nv.gameObject) + " at " + Fmt.F(end.x, 1) + "," + Fmt.F(end.z, 1) + " " + Fmt.F(gap, 2) + " m over " + Prefabs.BaseName(h.collider.transform.root.gameObject));
                        continue;
                    }
                    ZNetView under = h.collider.GetComponentInParent<ZNetView>();
                    if (under != null && under.IsValid() && under.GetZDO().GetString(ConstructCommand.KeyPillar, "").Length > 0 && Prefabs.BaseName(under.gameObject) != Prefabs.BaseName(nv.gameObject)) onPole++;
                    else if (h.collider.GetComponent<Heightmap>() != null || LayerMask.LayerToName(h.collider.gameObject.layer) == "terrain") onGround++;
                    else onOther++;
                }
            }
            return standing + " of " + r.Placed.Count + " door(s) standing; door ends: " + onPole + " on a preview pole, " + onGround + " on the ground, " + onOther + " on other objects, "
                + hanging + " more than " + Fmt.F(gapTolerance, 2) + " m over a surface (largest gap " + Fmt.F(worst, 2) + " m" + (where.Count > 0 ? ": " + string.Join("; ", where.ToArray()) : "") + ")";
        }

        // both ends of the door's width (its longer side, measured unturned), EndInset inside, at the door's turn; the pivot when
        // it cannot be measured or is too narrow for two
        static List<Vector3> Ends(Spot s)
        {
            List<Vector3> ends = new List<Vector3>();
            Vector3 lo, hi;
            if (!PieceGhost.Box(s.Prefab, Quaternion.identity, out lo, out hi)) { ends.Add(s.Pos); return ends; }
            bool alongX = hi.x - lo.x >= hi.z - lo.z;
            float a0 = alongX ? lo.x : lo.z, a1 = alongX ? hi.x : hi.z;
            float mid = alongX ? (lo.z + hi.z) * 0.5f : (lo.x + hi.x) * 0.5f;
            if (a1 - a0 < 2f * EndInset + 0.1f) { a0 = a1 = (a0 + a1) * 0.5f; }
            else { a0 += EndInset; a1 -= EndInset; }
            float[] along = a0 == a1 ? new float[] { a0 } : new float[] { a0, a1 };
            foreach (float a in along)
                ends.Add(s.Pos + s.Rot * (alongX ? new Vector3(a, 0f, mid) : new Vector3(mid, 0f, a)));
            return ends;
        }

        static ZNetView Spawn(GameObject prefab, Vector3 pos, Quaternion rot, string mark)
        {
            GameObject go = (GameObject)Object.Instantiate(prefab, pos, rot);
            ZNetView nv = go.GetComponent<ZNetView>();
            if (nv == null || !nv.IsValid()) { Object.Destroy(go); return null; }
            nv.GetZDO().Set(ConstructCommand.KeyPillar, mark);
            return nv;
        }
    }
}
