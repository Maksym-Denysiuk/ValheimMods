// Terrain work that stays as it was made after the area reloads (2026-10-25; the approach of Construction's TerrainLevel,
// copied here: a Kit promotion candidate now that two features carry it).
// The hoe pieces (raise, path, paved_road) carry the old TerrainModifier: it stays in the world as an object that the
// terrain re-applies from its own fields, and after the area reloads it comes back from the prefab with the prefab's radius
// (1.5 m level for raise, 2 m paint for path), so a stamp with a changed radius does not keep it (IL 1.0.20: no radius in its
// ZDO). A TerrainOp is applied once into the zone's TerrainComp, which saves heights and paint, and destroys itself.
// The operation travels as its prefab's name hash: the terrain owner reads the settings back from the TerrainOp registered
// under that hash in ObjectDB.m_terrainOpsByHash (an unregistered one is cancelled with "Failed to deserialize TerrainOp
// settings"), so every shape used here is one registered template:
// - level: a 0.3 m circle that sets one vertex. TerrainComp.LevelTerrain adds target - current height to every vertex it
//   covers and rounds the radius up to whole vertices (only a circle checks the distance), so wider or overlapping level
//   operations stack; one per vertex (whole world metres) sets each vertex exactly once.
// - paint: a hoe piece's paint and smoothing settings with the paint radius asked for; paint does not stack.
// This PC must own the zone's TerrainComp, or the operation is dropped without a log line (TerrainComp.RPC_ApplyOperation
// returns unless IsOwner, IL 1.0.20): Claim takes the zones another peer owns before every batch (Construction's
// TerrainLevel.Claim, portal hub 2026-10-25: 3 of 4 zones owned by another peer, every operation there dropped).
using System.Collections.Generic;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.WorldCommands
{
    internal static class TerrainOps
    {
        const string LevelName = "hc_world_level";
        public const float Tolerance = 0.05f;   // a vertex this close to its target counts as level
        public const float Limit = 8f;          // the game keeps terrain within this many metres of the world's own ground
        static readonly Dictionary<string, GameObject> templates = new Dictionary<string, GameObject>();

        // a template (inactive, across worlds) under this name
        static GameObject Template(string name, TerrainOp.Settings s)
        {
            GameObject t;
            if (templates.TryGetValue(name, out t) && !(t == null)) return t;
            t = new GameObject(name);
            t.SetActive(false);
            Object.DontDestroyOnLoad(t);
            TerrainOp op = t.AddComponent<TerrainOp>();
            op.m_settings = s;
            op.m_onPlacedEffect = new EffectList();
            templates[name] = t;
            return t;
        }

        // every template registered in this world's ObjectDB (Settings.Deserialize -> ObjectDB.TryGetTerrainOp)
        static void Register()
        {
            if (ZNetScene.instance == null) throw new CommandException("no world loaded");
            if (ObjectDB.instance == null) throw new CommandException("no ObjectDB: load a world first");
            System.Collections.IDictionary ops = Refl.Get(ObjectDB.instance, "m_terrainOpsByHash") as System.Collections.IDictionary;
            if (ops == null) throw new CommandException("ObjectDB.m_terrainOpsByHash not found: cannot register the terrain operations");
            foreach (KeyValuePair<string, GameObject> kv in templates)
                if (!(kv.Value == null)) ops[kv.Key.GetStableHashCode()] = kv.Value.GetComponent<TerrainOp>();
        }

        static void Spawn(GameObject template, Vector3 pos)
        {
            GameObject go = (GameObject)Object.Instantiate(template, pos, Quaternion.identity);
            go.name = template.name;
            go.SetActive(true);
        }

        // call once before a batch of Vertex / Paint: creates the level template and registers every template
        public static void Prepare()
        {
            TerrainOp.Settings s = new TerrainOp.Settings();
            s.m_level = true;
            s.m_levelOffset = 0f;
            s.m_levelRadius = 0.3f;
            s.m_square = false;
            s.m_raise = false;
            s.m_smooth = false;
            s.m_paintCleared = false;
            Template(LevelName, s);
            Register();
        }

        // one terrain vertex (whole world metres) set to height y
        public static void Vertex(float x, float z, float y)
        {
            Spawn(templates[LevelName], new Vector3(x, y, z));
        }

        // the paint (and with smooth the smoothing) of a hoe piece's TerrainModifier with paint radius r (0 = the piece's own);
        // level left out: path levels nothing, and a level of more than one vertex would stack. null when nothing is left to do
        public static string PaintTemplate(GameObject prefab, float r, bool smooth)
        {
            TerrainModifier tm = prefab.GetComponent<TerrainModifier>();
            if (tm == null) throw new CommandException(prefab.name + " has no TerrainModifier");
            smooth = smooth && tm.m_smooth;
            if (!tm.m_paintCleared && !smooth) return null;
            if (r <= 0f) r = tm.m_paintRadius;
            string name = "hc_world_paint_" + Prefabs.BaseName(prefab) + "_" + Mathf.RoundToInt(r * 100f) + (smooth ? "_s" : "");
            TerrainOp.Settings s = new TerrainOp.Settings();
            s.m_level = false;
            s.m_raise = false;
            s.m_levelOffset = tm.m_levelOffset;
            s.m_square = tm.m_square;
            s.m_smooth = smooth;
            s.m_smoothRadius = tm.m_smoothRadius;
            s.m_smoothPower = tm.m_smoothPower;
            s.m_paintCleared = tm.m_paintCleared;
            s.m_paintHeightCheck = tm.m_paintHeightCheck;
            s.m_paintType = tm.m_paintType;
            s.m_paintRadius = r;
            s.m_paintStrength = tm.m_paintStrength;
            Template(name, s);
            Register();
            return name;
        }

        public static void Paint(string template, Vector3 pos)
        {
            Spawn(templates[template], pos);
        }

        // the box around these vertices and points (whole world metres); false when both are empty
        public static bool Bounds(List<Vector2> verts, List<Vector3> pts, out float x0, out float z0, out float x1, out float z1)
        {
            x0 = z0 = float.MaxValue; x1 = z1 = float.MinValue;
            if (verts != null)
                foreach (Vector2 v in verts) { x0 = Mathf.Min(x0, v.x); x1 = Mathf.Max(x1, v.x); z0 = Mathf.Min(z0, v.y); z1 = Mathf.Max(z1, v.y); }
            if (pts != null)
                foreach (Vector3 q in pts) { x0 = Mathf.Min(x0, q.x); x1 = Mathf.Max(x1, q.x); z0 = Mathf.Min(z0, q.z); z1 = Mathf.Max(z1, q.z); }
            return x0 <= x1;
        }

        // takes over the TerrainComps another peer owns under the box grown by 'reach' m (an operation reaches every heightmap
        // within its radius, TerrainOp.Awake). The other peer takes a zone back within seconds, so call it before every batch.
        // The commands are host only. Returns the zones taken
        public static int Claim(float x0, float z0, float x1, float z1, float reach)
        {
            int zones, foreign;
            Zones(x0, z0, x1, z1, reach, true, out zones, out foreign);
            return foreign;
        }

        // report=1, read-only: the terrain zones under the box grown by 'reach' m and how many another peer owns. A zone without
        // a TerrainComp yet counts as this PC's (the first operation creates it here)
        public static string OwnersLine(float x0, float z0, float x1, float z1, float reach)
        {
            int zones, foreign;
            Zones(x0, z0, x1, z1, reach, false, out zones, out foreign);
            return "terrain zones " + zones + ": " + (zones - foreign) + " owned by this PC, " + foreign + " by another peer"
                + (foreign > 0 ? " (a change there is dropped unless this PC takes the zone first, as groundlevel and path do)" : "");
        }

        static void Zones(float x0, float z0, float x1, float z1, float reach, bool claim, out int zones, out int foreign)
        {
            zones = 0; foreign = 0;
            x0 -= reach; z0 -= reach; x1 += reach; z1 += reach;
            List<Heightmap> maps = new List<Heightmap>();
            Heightmap.FindHeightmap(new Vector3((x0 + x1) * 0.5f, 0f, (z0 + z1) * 0.5f), 0.5f * Mathf.Sqrt((x1 - x0) * (x1 - x0) + (z1 - z0) * (z1 - z0)), maps);
            foreach (Heightmap hm in maps)
            {
                if (hm == null) continue;
                zones++;
                // FindTerrainCompiler, not GetAndCreateTerrainCompiler: the report creates nothing
                TerrainComp tc = TerrainComp.FindTerrainCompiler(hm.transform.position);
                ZNetView nv = tc != null ? tc.GetComponent<ZNetView>() : null;
                if (nv == null || !nv.IsValid() || nv.IsOwner()) continue;
                foreign++;
                if (claim) nv.ClaimOwnership();
            }
        }

        // the old hoe stamps (TerrainModifier objects) whose level or smoothing reaches into the box: removed, so they stop
        // overriding the saved heights. With avoid, a stamp that also reaches within 'keep' m of a player-built piece stays
        // (it may hold the ground under a building). kept = the stamps left for that reason
        public static int RemoveLegacy(float ax, float az, float bx, float bz, bool avoid, float keep, out int kept)
        {
            int n = 0;
            kept = 0;
            foreach (TerrainModifier tm in TerrainModifier.GetAllInstances().ToArray())
            {
                if (tm == null || !(tm.m_level || tm.m_smooth)) continue;
                Vector3 p = tm.transform.position;
                float reach = Mathf.Max(tm.m_level ? tm.m_levelRadius : 0f, tm.m_smooth ? tm.m_smoothRadius : 0f);
                float dx = Mathf.Max(0f, Mathf.Max(ax - p.x, p.x - bx)), dz = Mathf.Max(0f, Mathf.Max(az - p.z, p.z - bz));
                if (dx * dx + dz * dz > reach * reach) continue;
                Piece nearest;
                if (avoid && Constructions.Near(p, reach + keep, out nearest) > 0) { kept++; continue; }
                ZNetView nv = tm.GetComponent<ZNetView>();
                if (nv != null && nv.IsValid()) { nv.ClaimOwnership(); ZNetScene.instance.Destroy(tm.gameObject); }
                else Object.Destroy(tm.gameObject);
                n++;
            }
            return n;
        }

        // the vertices (whole world metres) of a circle around c, or of the box a-b
        public static List<Vector2> Disc(Vector3 c, float r)
        {
            List<Vector2> v = new List<Vector2>();
            int x0 = Mathf.CeilToInt(c.x - r), x1 = Mathf.FloorToInt(c.x + r), z0 = Mathf.CeilToInt(c.z - r), z1 = Mathf.FloorToInt(c.z + r);
            for (int x = x0; x <= x1; x++)
                for (int z = z0; z <= z1; z++)
                    if ((x - c.x) * (x - c.x) + (z - c.z) * (z - c.z) <= r * r) v.Add(new Vector2(x, z));
            return v;
        }

        // the box rounded out to whole metres, so a thin rectangle still holds vertices
        public static List<Vector2> Box(float ax, float az, float bx, float bz)
        {
            List<Vector2> v = new List<Vector2>();
            for (int x = Mathf.FloorToInt(Mathf.Min(ax, bx)); x <= Mathf.CeilToInt(Mathf.Max(ax, bx)); x++)
                for (int z = Mathf.FloorToInt(Mathf.Min(az, bz)); z <= Mathf.CeilToInt(Mathf.Max(az, bz)); z++)
                    v.Add(new Vector2(x, z));
            return v;
        }

        // how level the vertices are against target (NaN = against their median height): within Tolerance, too high, too low,
        // the worst miss and where, the height range; vertices past the game's 8 m limit counted apart. Read-only
        public static string Flatness(List<Vector2> verts, float target)
        {
            if (verts.Count == 0) return "no terrain vertices in the area";
            float[] h = new float[verts.Count];
            float lo = float.MaxValue, hi = float.MinValue;
            for (int i = 0; i < verts.Count; i++)
            {
                h[i] = Ground.Height(new Vector3(verts[i].x, 0f, verts[i].y));
                lo = Mathf.Min(lo, h[i]); hi = Mathf.Max(hi, h[i]);
            }
            string against = "the target";
            if (float.IsNaN(target))
            {
                float[] s = (float[])h.Clone();
                System.Array.Sort(s);
                target = s[s.Length / 2];
                against = "their median";
            }
            int ok = 0, high = 0, low = 0, beyond = 0;
            float worst = 0f;
            Vector2 at = Vector2.zero;
            for (int i = 0; i < verts.Count; i++)
            {
                float d = h[i] - target;
                if (Mathf.Abs(d) <= Tolerance) ok++; else if (d > 0f) high++; else low++;
                if (Mathf.Abs(d) > Tolerance && WorldGenerator.instance != null
                    && Mathf.Abs(target - WorldGenerator.instance.GetHeight(verts[i].x, verts[i].y)) > Limit - 0.1f) beyond++;
                if (Mathf.Abs(d) > Mathf.Abs(worst)) { worst = d; at = verts[i]; }
            }
            return ok + " of " + verts.Count + " vertices within " + Fmt.F(Tolerance * 100f, 0) + " cm of " + against + " " + Fmt.F(target, 2)
                + " (" + Fmt.F(100f * ok / verts.Count, 1) + " %), " + high + " too high, " + low + " too low"
                + (beyond > 0 ? " (" + beyond + " of them more than " + Fmt.F(Limit, 0) + " m from the world's own ground, the game's limit)" : "")
                + (Mathf.Abs(worst) > Tolerance ? ", worst " + (worst > 0f ? "+" : "") + Fmt.F(worst, 2) + " m at " + Fmt.F(at.x, 0) + "," + Fmt.F(at.y, 0) : "")
                + "; heights " + Fmt.F(lo, 2) + ".." + Fmt.F(hi, 2);
        }

        // is the ground at pos painted with this hoe piece's paint type: the paint mask pixel (read as Heightmap.IsCleared does:
        // half a cell back, WorldToVertexMask) is over 0.5 in the channel of Heightmap.m_paintMask<Dirt|Paved|Cultivated>
        public static bool PaintedLike(Vector3 pos, GameObject tool)
        {
            TerrainModifier tm = tool.GetComponent<TerrainModifier>();
            Heightmap hm = Heightmap.FindHeightmap(pos);
            if (tm == null || hm == null) return false;
            string type = tm.m_paintType.ToString();
            object want = Refl.GetStatic(typeof(Heightmap), "m_paintMask" + (type == "Cultivate" ? "Cultivated" : type));
            Texture2D mask = Refl.Get(hm, "m_paintMask") as Texture2D;
            System.Reflection.MethodInfo w2v = typeof(Heightmap).GetMethod("WorldToVertexMask",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (!(want is Color) || mask == null || w2v == null) return false;
            object[] args = { new Vector3(pos.x - 0.5f, pos.y, pos.z - 0.5f), 0, 0 };
            w2v.Invoke(hm, args);
            // dirt (1,0,0), cultivated (0,1,0), paved (0,0,1): paint over other paint mixes the channels, so only the type's own
            // channel is read
            Color c = mask.GetPixel((int)args[1], (int)args[2]), t = (Color)want;
            return (t.r > 0.5f && c.r > 0.5f) || (t.g > 0.5f && c.g > 0.5f) || (t.b > 0.5f && c.b > 0.5f);
        }
    }
}
