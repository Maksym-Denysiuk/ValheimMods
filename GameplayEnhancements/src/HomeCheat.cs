// HomeCheat v0.1 - BepInEx client plugin for Maksym's own Valheim game and world.
// Local command channel: drop <name>.cmd (form body, op=give&prefab=Wood&amount=50) into
// BepInEx/config/homecheat/inbox/; the result appears in outbox/<name>.out. No network port.
// Commands run on the Unity main thread.
// It never calls the console cheat path (Terminal/Console commands), on purpose.
// Written in C# 5 (no ?., no $"") because the only compiler on this machine is csc 4.0.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace HomeCheat
{
    [BepInPlugin("com.homeos.homecheat", "HomeCheat", "0.1.0")]
    public class HomeCheatPlugin : BaseUnityPlugin
    {
        internal static HomeCheatPlugin Inst;
        const string ScaleKey = "scale";
        string inboxDir, outboxDir;
        float nextPoll;

        // ---- command registry: commands from extra DLLs (see HomeApi.cs) --------------------
        // BepInEx/homecheat-commands/*.dll hold IHomeCommand implementations. 'op=reload' loads new files
        // (without a game restart); a file is loaded once, so every build gets a new file name.
        internal static readonly Dictionary<string, IHomeCommand> Registry = new Dictionary<string, IHomeCommand>();
        static readonly HashSet<string> LoadedFiles = new HashSet<string>();
        string commandsDir;

        string Reload()
        {
            StringBuilder sb = new StringBuilder();
            Directory.CreateDirectory(commandsDir);
            string[] files = Directory.GetFiles(commandsDir, "*.dll");
            Array.Sort(files);
            foreach (string f in files)
            {
                string key = Path.GetFileName(f);
                if (LoadedFiles.Contains(key)) continue;
                LoadedFiles.Add(key);
                try
                {
                    Assembly asm = Assembly.Load(File.ReadAllBytes(f));
                    foreach (Type t in asm.GetTypes())
                    {
                        if (t.IsAbstract || !typeof(IHomeCommand).IsAssignableFrom(t) || t.GetConstructor(Type.EmptyTypes) == null) continue;
                        IHomeCommand c = (IHomeCommand)Activator.CreateInstance(t);
                        Registry[c.Name.ToLowerInvariant()] = c;
                        sb.Append("registered ").Append(c.Name).Append(" from ").Append(key).Append('\n');
                    }
                }
                catch (Exception e)
                {
                    sb.Append("FAILED ").Append(key).Append(": ").Append(e.Message).Append('\n');
                    Logger.LogError(e);
                }
            }
            return sb.Length == 0 ? "no new command DLLs in " + commandsDir : sb.ToString();
        }

        string Commands()
        {
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, IHomeCommand> kv in Registry) sb.Append(kv.Key).Append(" - ").Append(kv.Value.Help).Append('\n');
            return sb.Length == 0 ? "no extra commands loaded (op=reload)" : sb.ToString();
        }

        // run an action on the main thread after a delay (seconds, may be below one frame: runs on the next frame)
        readonly List<KeyValuePair<float, Action>> timers = new List<KeyValuePair<float, Action>>();

        internal static void LogInfo(string msg) { Inst.Logger.LogInfo(msg); }

        internal static void Later(float seconds, Action act)
        {
            Inst.timers.Add(new KeyValuePair<float, Action>(Time.time + Mathf.Max(0f, seconds), act));
        }

        void Awake()
        {
            Inst = this;
            string root = Path.Combine(Paths.ConfigPath, "homecheat");
            inboxDir = Path.Combine(root, "inbox");
            outboxDir = Path.Combine(root, "outbox");
            commandsDir = Path.Combine(Paths.BepInExRootPath, "homecheat-commands");
            Directory.CreateDirectory(inboxDir);
            Directory.CreateDirectory(outboxDir);
            // command DLLs reference this assembly: hand them the one that is already loaded
            AppDomain.CurrentDomain.AssemblyResolve += delegate (object s, ResolveEventArgs e)
            {
                return e.Name.StartsWith("HomeCheat,") ? typeof(HomeCheatPlugin).Assembly : null;
            };
            try
            {
                new Harmony("com.homeos.homecheat").PatchAll(typeof(HomeCheatPlugin).Assembly);
            }
            catch (Exception e) { Logger.LogError("Harmony: " + e); }
            Logger.LogInfo("HomeCheat watching " + inboxDir + " (no network port). Drop <name>.cmd, read outbox/<name>.out");
            try { Logger.LogInfo(Reload()); } catch (Exception e) { Logger.LogError(e); }
        }

        // Command channel: a file <name>.cmd in inbox/ holding a form body (op=give&prefab=Wood&amount=50).
        // The result lands in outbox/<name>.out. No network listener, nothing reachable from outside the machine.
        void Update()
        {
            for (int i = timers.Count - 1; i >= 0; i--)
                if (Time.time >= timers[i].Key)
                {
                    Action act = timers[i].Value;
                    timers.RemoveAt(i);
                    try { act(); } catch (Exception e) { Logger.LogError(e); }
                }
            if (Time.unscaledTime < nextPoll) return;
            nextPoll = Time.unscaledTime + 0.5f;
            string[] files;
            try { files = Directory.GetFiles(inboxDir, "*.cmd"); }
            catch { return; }
            int n = 0;
            foreach (string f in files)
            {
                if (n++ >= 20) break;
                string body, result;
                try { body = File.ReadAllText(f, Encoding.UTF8); File.Delete(f); }
                catch { continue; }
                try { result = Run(ParseForm(body)); }
                catch (Exception e) { result = "ERROR: " + e.Message; Logger.LogError(e); }
                string name = Path.GetFileNameWithoutExtension(f);
                string tmp = Path.Combine(outboxDir, name + ".tmp");
                File.WriteAllText(tmp, result, Encoding.UTF8);
                string dst = Path.Combine(outboxDir, name + ".out");
                if (File.Exists(dst)) File.Delete(dst);
                File.Move(tmp, dst);
            }
        }

        static Dictionary<string, string> ParseForm(string body)
        {
            Dictionary<string, string> d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string pair in body.Split(new char[] { '&', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int i = pair.IndexOf('=');
                string k = i < 0 ? pair : pair.Substring(0, i);
                string v = i < 0 ? "" : pair.Substring(i + 1);
                d[Uri.UnescapeDataString(k.Replace('+', ' ')).Trim()] = Uri.UnescapeDataString(v.Replace('+', ' ')).Trim();
            }
            return d;
        }

        internal static string S(Dictionary<string, string> a, string k, string def)
        { string v; return a.TryGetValue(k, out v) && v.Length > 0 ? v : def; }
        internal static int I(Dictionary<string, string> a, string k, int def)
        { int v; return int.TryParse(S(a, k, ""), out v) ? v : def; }
        internal static float F(Dictionary<string, string> a, string k, float def)
        { float v; return float.TryParse(S(a, k, ""), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v) ? v : def; }

        string Run(Dictionary<string, string> a)
        {
            // Defaults, so a command works with only the essential parameter:
            //  * blocks (field, plant, place, cultivate): rows=1, perrow=1 -> one item exactly at the crosshair
            //  * scale=1, spacing=auto (plant: twice the sapling's grow radius; others 1.5 m), clear=auto
            //  * refill/repair: aim=true; with aim=false the radius defaults to 10 m; amount 0 = full
            //  * required (no safe default): prefab for give/spawn/field/plant/place, filter for scale
            string op = S(a, "op", "").ToLowerInvariant();
            IHomeCommand extra;
            if (Registry.TryGetValue(op, out extra)) return extra.Run(a);
            switch (op)
            {
                case "harmeffect":
                    {
                        string kind = S(a, "kind", "").ToLowerInvariant();
                        IHomeCommand he;
                        if (Registry.TryGetValue(kind, out he) && he is HarmEffect) return he.Run(a);
                        StringBuilder kinds = new StringBuilder();
                        foreach (KeyValuePair<string, IHomeCommand> kv in Registry) if (kv.Value is HarmEffect) kinds.Append(kv.Key).Append(' ');
                        return "ERROR: kind required; loaded harm effects: " + (kinds.Length == 0 ? "(none, op=reload)" : kinds.ToString());
                    }
                case "reload": return Reload();
                case "commands": return Commands();
                case "ping": return "pong";
                case "status": return Status();
                case "list": return List(S(a, "filter", ""));
                case "give": return Give(S(a, "prefab", ""), I(a, "amount", 1), I(a, "quality", 1));
                case "spawn": return Spawn(S(a, "prefab", ""), I(a, "count", 1), F(a, "dist", 4f), I(a, "level", 0));
                case "field":
                    {
                        if (S(a, "prefab", "").Length == 0 && S(a, "crops", "").Length == 0) return "ERROR: prefab (or crops) required, e.g. prefab=Pickable_Carrot";
                        return Field(S(a, "prefab", ""), S(a, "crops", ""), I(a, "rows", 1), I(a, "perrow", I(a, "cols", 1)), F(a, "spacing", 1.5f),
                            MakeAnchor(P(), a), F(a, "scale", 1f), F(a, "clear", 0.4f));
                    }
                case "cultivate":
                    return Cultivate(I(a, "rows", 1), I(a, "perrow", I(a, "cols", 1)), F(a, "spacing", 1.5f), MakeAnchor(P(), a));
                case "plant":
                    {
                        if (S(a, "prefab", "").Length == 0) return "ERROR: prefab required, e.g. prefab=Birch_Sapling (op=list&filter=sapling)";
                        // prefab = what to grow, perrow = items in a row, rows = number of rows, scale = size factor
                        int perRow = I(a, "perrow", I(a, "cols", 1));
                        return PlantBlock(S(a, "prefab", ""), I(a, "rows", 1), perRow, F(a, "spacing", 0f),
                            I(a, "aimcol", (perRow - 1) / 2), F(a, "scale", 1f), F(a, "clear", 0f));
                    }
                case "repair": return Repair(Flag(a, "aim", true), F(a, "radius", 10f), F(a, "amount", 0f));
                case "refill": return Refill(Flag(a, "aim", true), F(a, "radius", 10f), F(a, "amount", 0f));
                case "place":
                    {
                        int perRow = I(a, "perrow", I(a, "cols", 1));
                        return PlaceBlock(S(a, "prefab", ""), I(a, "rows", 1), perRow, F(a, "spacing", 1.5f), I(a, "aimcol", (perRow - 1) / 2),
                            F(a, "scale", 1f), I(a, "amount", 1), I(a, "level", 0), F(a, "lift", 0f), F(a, "clear", 0f));
                    }
                case "groundlevel": return GroundLevel(F(a, "delta", 0f), F(a, "radius", 4f), S(a, "tool", "raise"));
                case "path": return MakePath(S(a, "type", "dirt").ToLowerInvariant(), F(a, "length", 5f), F(a, "width", 2f), S(a, "tool", ""), MakeAnchor(P(), a));
                case "inspect": return Inspect(S(a, "prefab", ""));
                case "cheatstate": return CheatState();
                case "scale":
                    if (S(a, "filter", "").Length == 0) return "ERROR: filter required, e.g. filter=Oak";
                    return ScaleTrees(S(a, "filter", ""), F(a, "factor", 2f), F(a, "radius", 50f));
                case "cheatinfo": return CheatInfo();
                default: return "ERROR: unknown op '" + op + "'. ops: ping status list give spawn field cultivate plant place refill repair groundlevel path inspect scale cheatstate cheatinfo";
            }
        }

        // ---- commands ----------------------------------------------------------------

        internal static Player P()
        {
            if (Player.m_localPlayer == null) throw new Exception("no local player: load a world first");
            return Player.m_localPlayer;
        }

        string Status()
        {
            Player p = P();
            Vector3 v = p.transform.position;
            return "player=" + p.GetPlayerName() + " pos=" + v.x.ToString("F1") + "," + v.y.ToString("F1") + "," + v.z.ToString("F1")
                + " isServer=" + ZNet.instance.IsServer() + " world=" + ZNet.instance.GetWorldName()
                + " dedicated=" + ZNet.instance.IsDedicated();
        }

        string List(string filter)
        {
            StringBuilder sb = new StringBuilder();
            int n = 0;
            FieldInfo f = typeof(ZNetScene).GetField("m_namedPrefabs", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Dictionary<int, GameObject> d = f.GetValue(ZNetScene.instance) as Dictionary<int, GameObject>;
            List<string> names = new List<string>();
            foreach (KeyValuePair<int, GameObject> kv in d)
                if (kv.Value != null && kv.Value.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) names.Add(kv.Value.name);
            names.Sort();
            foreach (string s in names) { if (n++ >= 200) { sb.Append("... (more)"); break; } sb.Append(s).Append('\n'); }
            return sb.ToString();
        }

        string Give(string prefab, int amount, int quality)
        {
            Player p = P();
            if (prefab.Length == 0) return "ERROR: prefab required";
            GameObject go = ObjectDB.instance.GetItemPrefab(prefab);
            if (go == null) return "ERROR: no item prefab '" + prefab + "'";
            ItemDrop drop = go.GetComponent<ItemDrop>();
            int max = Math.Max(1, drop.m_itemData.m_shared.m_maxStackSize);
            Inventory inv = p.GetInventory();
            int left = amount, added = 0;
            while (left > 0)
            {
                int put = Math.Min(left, max);
                ItemDrop.ItemData it = drop.m_itemData.Clone();
                it.m_dropPrefab = go;
                it.m_stack = put;
                it.m_quality = quality;
                it.m_durability = it.GetMaxDurability();
                if (!inv.AddItem(it)) break;
                added += put; left -= put;
            }
            return "gave " + added + "/" + amount + " " + prefab + (left > 0 ? " (inventory full)" : "");
        }

        string Spawn(string prefab, int count, float dist, int level)
        {
            Player p = P();
            GameObject go = ZNetScene.instance.GetPrefab(prefab);
            if (go == null) return "ERROR: no prefab '" + prefab + "' (try op=list&filter=...)";
            Vector3 pos = p.transform.position + p.transform.forward * dist + Vector3.up;
            int made = 0;
            for (int i = 0; i < count; i++)
            {
                Vector3 jitter = new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 0f, UnityEngine.Random.Range(-0.5f, 0.5f));
                GameObject inst = (GameObject)UnityEngine.Object.Instantiate(go, pos + jitter, Quaternion.identity);
                Character c = inst.GetComponent<Character>();
                if (c != null && level > 0) c.SetLevel(level);
                made++;
            }
            return "spawned " + made + " " + prefab;
        }

        // where a block starts and which way it runs: row 1 at 'origin', rows go along 'fwd' (away from the player),
        // columns along 'right'. Default origin = the crosshair point; a 'dist' parameter means "that many metres
        // straight ahead of the player" instead.
        class Anchor
        {
            public Vector3 origin, fwd, right;
            public bool aimed;
        }

        static Anchor MakeAnchor(Player p, Dictionary<string, string> a)
        {
            Anchor an = new Anchor();
            if (a.ContainsKey("dist"))
            {
                an.fwd = p.transform.forward; an.fwd.y = 0f; an.fwd.Normalize();
                an.origin = p.transform.position + an.fwd * F(a, "dist", 6f);
            }
            else
            {
                an.origin = AimPoint(p, out an.aimed);
                an.fwd = an.origin - p.transform.position; an.fwd.y = 0f;
                if (an.fwd.sqrMagnitude < 0.01f) { an.fwd = p.transform.forward; an.fwd.y = 0f; }
                an.fwd.Normalize();
            }
            an.right = Vector3.Cross(Vector3.up, an.fwd);
            return an;
        }

        static Vector3 CellPos(Anchor an, int r, int c, int cols, float spacing)
        {
            Vector3 pos = an.origin + an.fwd * (r * spacing) + an.right * ((c - (cols - 1) / 2f) * spacing);
            pos.y = ZoneSystem.instance.GetGroundHeight(pos);
            return pos;
        }

        static int blockMask = -1;

        // true when something solid already stands at the spot: buildings, trees, bushes, rocks, other plants
        // (terrain and loose items are not counted). The probe sphere sits just above the ground.
        internal static bool IsBlocked(Vector3 pos, float radius)
        {
            if (blockMask == -1)
                blockMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "vehicle");
            Collider[] hits = Physics.OverlapSphere(pos + Vector3.up * (radius + 0.1f), radius, blockMask, QueryTriggerInteraction.Ignore);
            return hits.Length > 0;
        }

        internal static bool IsCultivated(Vector3 pos)
        {
            Heightmap hm = Heightmap.FindHeightmap(pos);
            return hm != null && hm.IsCultivated(pos);
        }

        // plants only on cells that already have cultivated soil (the 'ground' command makes it).
        // If no cell of the grid is cultivated, nothing is planted.
        // 'crops' = "Pickable_Carrot:2,Pickable_Onion:1,Pickable_Turnip:2": consecutive bands of rows, one crop per band,
        // so several crops can share one field. Without 'crops', 'prefab' fills all 'rows'.
        string Field(string prefab, string crops, int rows, int cols, float spacing, Anchor an, float scale, float clear)
        {
            List<string> names = new List<string>();
            List<GameObject> prefabs = new List<GameObject>();
            List<int> counts = new List<int>();
            if (crops.Length == 0) crops = prefab + ":" + rows;
            foreach (string part in crops.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] kv = part.Split(':');
                string nm = kv[0].Trim();
                int n; if (kv.Length < 2 || !int.TryParse(kv[1].Trim(), out n)) n = 1;
                GameObject go = ZNetScene.instance.GetPrefab(nm);
                if (go == null) return "ERROR: no prefab '" + nm + "' (try op=list&filter=carrot); nothing planted";
                names.Add(nm); prefabs.Add(go); counts.Add(n);
            }
            int totalRows = 0;
            foreach (int n in counts) totalRows += n;
            // the grid may be huge (100 rows) while soil exists on a few: cells are only examined, and the
            // cap applies to objects actually planted
            if (totalRows * cols > 40000) return "ERROR: grid too large (max 40000 cells examined)";
            const int MaxPlanted = 2500;
            int made = 0, skipped = 0, blocked = 0, row = 0;
            StringBuilder per = new StringBuilder();
            for (int k = 0; k < prefabs.Count; k++)
            {
                int madeK = 0;
                for (int r = 0; r < counts[k]; r++, row++)
                    for (int c = 0; c < cols; c++)
                    {
                        Vector3 pos = CellPos(an, row, c, cols, spacing);
                        if (made >= MaxPlanted) { skipped++; continue; }
                        if (!IsCultivated(pos)) { skipped++; continue; }
                        if (IsBlocked(pos, clear)) { blocked++; continue; }
                        GameObject inst = (GameObject)UnityEngine.Object.Instantiate(prefabs[k], pos, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));
                        SetScale(inst, scale);
                        made++; madeK++;
                    }
                per.Append(" ").Append(names[k]).Append("=").Append(madeK);
            }
            if (made == 0) return "nothing planted: no cultivated soil in the " + totalRows + "x" + cols + " grid (use op=cultivate first)";
            return "planted " + made + " (" + per.ToString().Trim() + "), skipped " + skipped + " cells without cultivated soil, "
                + blocked + " cells blocked by objects";
        }

        internal static void SetScale(GameObject inst, float scale)
        {
            if (Mathf.Abs(scale - 1f) <= 0.001f) return;
            inst.transform.localScale = Vector3.one * scale;
            ZNetView nv = inst.GetComponent<ZNetView>();
            if (nv != null && nv.IsValid()) nv.GetZDO().Set(ScaleKey, Vector3.one * scale);
        }

        // where the player's crosshair points (camera ray), else 8 m ahead
        internal static Vector3 AimPoint(Player p, out bool aimed)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                int mask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "vehicle");
                RaycastHit hit;
                if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, 60f, mask))
                {
                    aimed = true;
                    return hit.point;
                }
            }
            aimed = false;
            return p.transform.position + p.transform.forward * 8f;
        }

        internal static bool Flag(Dictionary<string, string> a, string k, bool def)
        {
            string v = S(a, k, "");
            if (v.Length == 0) return def;
            v = v.ToLowerInvariant();
            return v == "1" || v == "true" || v == "yes" || v == "aim";
        }

        // refill = top up the fuel of fire places (campfire, hearth, brazier, torches, sconces: wood, resin, coal...)
        // and smelting stations (kiln, smelter, furnace: wood, coal). Parameters in order:
        //   aim    true = the object under the crosshair, false = everything within 'radius'
        //   radius metres around the player, used only when aim is false
        //   amount fuel units to set, default 0 = fill to the object's own maximum
        // The fuel itself is only the stored amount; no inventory items are used.
        string Refill(bool aim, float radius, float amount)
        {
            Player p = P();
            List<Component> targets = new List<Component>();
            if (aim)
            {
                Camera cam = Camera.main;
                RaycastHit hit;
                int mask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "vehicle");
                if (cam == null || !Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, 30f, mask, QueryTriggerInteraction.Ignore))
                    return "nothing under the crosshair (30 m)";
                Component f = hit.collider.GetComponentInParent<Fireplace>();
                if (f == null) f = hit.collider.GetComponentInParent<Smelter>();
                if (f == null) return "the object under the crosshair (" + hit.collider.transform.root.name + ") has no fuel";
                targets.Add(f);
            }
            else
            {
                if (radius <= 0f) return "ERROR: radius must be > 0";
                foreach (Fireplace f in UnityEngine.Object.FindObjectsOfType<Fireplace>())
                    if (Vector3.Distance(f.transform.position, p.transform.position) <= radius) targets.Add(f);
                foreach (Smelter s in UnityEngine.Object.FindObjectsOfType<Smelter>())
                    if (Vector3.Distance(s.transform.position, p.transform.position) <= radius) targets.Add(s);
            }
            StringBuilder sb = new StringBuilder();
            int done = 0;
            foreach (Component c in targets)
            {
                ZNetView nv = c.GetComponent<ZNetView>();
                if (nv == null || !nv.IsValid()) continue;
                float max;
                Fireplace fp = c as Fireplace;
                Smelter sm = c as Smelter;
                if (fp != null) { if (fp.m_infiniteFuel) continue; max = fp.m_maxFuel; }
                else max = sm.m_maxFuel;
                float set = amount > 0f ? Mathf.Min(amount, max) : max;
                nv.ClaimOwnership();
                float before = nv.GetZDO().GetFloat("fuel", 0f);
                nv.GetZDO().Set("fuel", set);
                sb.Append(c.name.Replace("(Clone)", "")).Append(": ").Append(before.ToString("F0")).Append(" -> ").Append(set.ToString("F0"))
                  .Append(" / ").Append(max.ToString("F0")).Append('\n');
                done++;
            }
            return "refilled " + done + " of " + targets.Count + " objects (" + (aim ? "crosshair" : "radius " + radius.ToString("F0") + " m") + ")\n" + sb;
        }

        // repair = restore building pieces (walls, floors, doors, ships, ...: everything with WearNTear) to full health.
        // Same signature as refill: aim (true = crosshair, false = radius), radius (metres, when aim is false),
        // amount (HP to set, default 0 = full).
        string Repair(bool aim, float radius, float amount)
        {
            Player p = P();
            List<WearNTear> targets = new List<WearNTear>();
            if (aim)
            {
                Camera cam = Camera.main;
                RaycastHit hit;
                int mask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "vehicle");
                if (cam == null || !Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, 30f, mask, QueryTriggerInteraction.Ignore))
                    return "nothing under the crosshair (30 m)";
                WearNTear w = hit.collider.GetComponentInParent<WearNTear>();
                if (w == null) return "the object under the crosshair (" + hit.collider.transform.root.name + ") cannot be repaired";
                targets.Add(w);
            }
            else
            {
                if (radius <= 0f) return "ERROR: radius must be > 0";
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

        // any prefab (item on the ground, object, building piece, creature) at the crosshair, optionally a block.
        // Column 'aimcol' of row 1 stands on the crosshair point; further rows go away from the player.
        // amount = stack size for item prefabs, level = star level for creatures, lift = metres above ground,
        // clear > 0 skips spots with solid objects (default 0 = no check, so pieces can be placed anywhere).
        string PlaceBlock(string prefab, int rows, int cols, float spacing, int aimcol, float scale, int amount, int level, float lift, float clear)
        {
            Player p = P();
            GameObject go = ZNetScene.instance.GetPrefab(prefab);
            if (go == null) return "ERROR: no prefab '" + prefab + "' (try op=list&filter=...)";
            if (rows * cols > 400) return "ERROR: max 400 objects";
            if (scale <= 0f) return "ERROR: scale must be > 0";
            bool aimed;
            Vector3 t = AimPoint(p, out aimed);
            Vector3 fwd = t - p.transform.position; fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.01f) { fwd = p.transform.forward; fwd.y = 0f; }
            fwd.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            int made = 0, blocked = 0;
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    Vector3 pos = t + fwd * (r * spacing) + right * ((c - aimcol) * spacing);
                    pos.y = ZoneSystem.instance.GetGroundHeight(pos) + lift;
                    if (clear > 0f && IsBlocked(pos, clear)) { blocked++; continue; }
                    GameObject inst = (GameObject)UnityEngine.Object.Instantiate(go, pos, Quaternion.LookRotation(-fwd));
                    SetScale(inst, scale);
                    ItemDrop drop = inst.GetComponent<ItemDrop>();
                    if (drop != null && amount > 1) drop.SetStack(Mathf.Min(amount, Mathf.Max(1, drop.m_itemData.m_shared.m_maxStackSize)));
                    Character ch = inst.GetComponent<Character>();
                    if (ch != null && level > 0) ch.SetLevel(level);
                    made++;
                }
            return "placed " + made + " " + prefab + " x" + scale.ToString("F2") + " (" + rows + " rows x " + cols + "), " + blocked
                + " blocked; first row, item " + (aimcol + 1) + " at " + t.x.ToString("F1") + "," + t.y.ToString("F1") + "," + t.z.ToString("F1")
                + (aimed ? " (crosshair)" : " (no crosshair hit, 8 m ahead)");
        }

        // saplings in a rows x cols block. Row 1 is centred so that column 'aimcol' (0-based, 2 = the 3rd tree)
        // stands on the crosshair point; further rows go away from the player. Spacing defaults to twice the
        // sapling's own m_growRadius, the free space a Plant needs to be allowed to grow.
        string PlantBlock(string prefab, int rows, int cols, float spacing, int aimcol, float scale, float clear)
        {
            if (scale <= 0f) return "ERROR: scale must be > 0";
            Player p = P();
            GameObject go = ZNetScene.instance.GetPrefab(prefab);
            if (go == null) return "ERROR: no prefab '" + prefab + "' (try op=list&filter=sapling)";
            if (rows * cols > 400) return "ERROR: max 400 saplings";
            Plant pl = go.GetComponent<Plant>();
            float need = pl != null ? pl.m_growRadius : 1f;
            if (spacing <= 0f) spacing = need * 2f * Mathf.Max(1f, scale);
            if (clear <= 0f) clear = Mathf.Max(0.5f, need * 0.5f);   // free radius around each trunk
            int blocked = 0;
            bool aimed;
            Vector3 t = AimPoint(p, out aimed);
            Vector3 fwd = t - p.transform.position; fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.01f) { fwd = p.transform.forward; fwd.y = 0f; }
            fwd.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            int made = 0;
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    Vector3 pos = t + fwd * (r * spacing) + right * ((c - aimcol) * spacing);
                    pos.y = ZoneSystem.instance.GetGroundHeight(pos);
                    if (IsBlocked(pos, clear)) { blocked++; continue; }
                    GameObject inst = (GameObject)UnityEngine.Object.Instantiate(go, pos, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));
                    SetScale(inst, scale);   // the grown tree copies this scale when the sapling grows
                    made++;
                }
            return "planted " + made + " " + prefab + " x" + scale.ToString("F2") + " (" + rows + " rows x " + cols + "), " + blocked
                + " blocked by objects (clear radius " + clear.ToString("F2") + "), spacing " + spacing.ToString("F2")
                + " m (growRadius " + need.ToString("F2") + "), tree " + (aimcol + 1) + " of row 1 at "
                + t.x.ToString("F1") + "," + t.y.ToString("F1") + "," + t.z.ToString("F1") + (aimed ? " (crosshair)" : " (no crosshair hit, 8 m ahead)");
        }

        // cultivated soil under the same grid: one 'cultivate' piece per cell (what the Cultivator tool places)
        string Cultivate(int rows, int cols, float spacing, Anchor an)
        {
            GameObject go = ZNetScene.instance.GetPrefab("cultivate");
            if (go == null) return "ERROR: no 'cultivate' prefab";
            if (rows * cols > 2500) return "ERROR: max 2500 pieces";
            int made = 0;
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    UnityEngine.Object.Instantiate(go, CellPos(an, r, c, cols, spacing), Quaternion.identity);
                    made++;
                }
            return "cultivated " + made + " cells, block starts at " + V(an.origin) + (an.aimed ? " (crosshair)" : "");
        }

        internal static string V(Vector3 v) { return v.x.ToString("F1") + "," + v.y.ToString("F1") + "," + v.z.ToString("F1"); }

        // ---- terrain: groundlevel, path, inspect ---------------------------------------
        // Both terrain commands reuse the game's own terrain pieces (the prefabs the Hoe places). A piece carries a
        // TerrainModifier; its radii are set on the prefab for the moment of the spawn and restored right after.

        static FieldInfo TmField(string name)
        {
            return typeof(TerrainModifier).GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        static float TmGet(TerrainModifier tm, string name, float def)
        {
            FieldInfo f = TmField(name);
            if (f == null) return def;
            object v = f.GetValue(tm);
            return v is float ? (float)v : def;
        }

        static void TmSet(TerrainModifier tm, string name, float v)
        {
            FieldInfo f = TmField(name);
            if (f != null && f.FieldType == typeof(float)) f.SetValue(tm, v);
        }

        // one terrain stamp. level/smooth radius = rLevel (0 = leave as in prefab), paint radius = rPaint (0 = leave).
        static void Stamp(GameObject prefab, Vector3 pos, float rLevel, float rPaint)
        {
            TerrainModifier tm = prefab.GetComponent<TerrainModifier>();
            if (tm == null) throw new Exception(prefab.name + " has no TerrainModifier");
            float oLR = TmGet(tm, "m_levelRadius", 0f), oSR = TmGet(tm, "m_smoothRadius", 0f), oPR = TmGet(tm, "m_paintRadius", 0f);
            try
            {
                if (rLevel > 0f) { TmSet(tm, "m_levelRadius", rLevel); TmSet(tm, "m_smoothRadius", Mathf.Max(oSR, rLevel + 1f)); }
                if (rPaint > 0f) TmSet(tm, "m_paintRadius", rPaint);
                UnityEngine.Object.Instantiate(prefab, pos, Quaternion.identity);
            }
            finally
            {
                TmSet(tm, "m_levelRadius", oLR); TmSet(tm, "m_smoothRadius", oSR); TmSet(tm, "m_paintRadius", oPR);
            }
        }

        // groundlevel: level the ground around the player to the player's own ground height + delta metres.
        // delta 0 = flatten at the current level; +1 / -1 = one metre up / down from where the player stands
        // (raise like the Hoe's raise, lower like a pickaxe), then the area is levelled flat like the Hoe's level.
        string GroundLevel(float delta, float radius, string tool)
        {
            Player p = P();
            if (radius <= 0f || radius > 12f) return "ERROR: radius must be 0..12 m";
            GameObject go = ZNetScene.instance.GetPrefab(tool);
            if (go == null) return "ERROR: no terrain prefab '" + tool + "' (try op=list&filter=raise)";
            TerrainModifier tm = go.GetComponent<TerrainModifier>();
            if (tm == null) return "ERROR: '" + tool + "' has no TerrainModifier (use op=inspect&prefab=" + tool + ")";
            Vector3 c = p.transform.position;
            float baseY = ZoneSystem.instance.GetGroundHeight(c);
            float target = baseY + delta;
            // the piece levels to its own y + m_levelOffset, so place it at target - offset
            float off = TmGet(tm, "m_levelOffset", 0f);
            Stamp(go, new Vector3(c.x, target - off, c.z), radius, 0f);
            return "ground levelled around " + V(c) + " r=" + radius.ToString("F1") + " m: " + baseY.ToString("F2") + " -> " + target.ToString("F2")
                + " (delta " + delta.ToString("F1") + ", tool " + tool + ", levelOffset " + off.ToString("F2") + ")";
        }

        // path: a dirt or stone strip, 'length' m along the direction from the player to the start point and 'width' m wide,
        // starting at the crosshair (or 'dist' m ahead), centred on its axis.
        string MakePath(string type, float length, float width, string tool, Anchor an)
        {
            if (tool.Length == 0) tool = type == "stone" ? "paved_road" : "path";
            if (type != "dirt" && type != "stone") return "ERROR: type must be dirt or stone";
            if (length <= 0f || width <= 0f) return "ERROR: length and width must be > 0";
            GameObject go = ZNetScene.instance.GetPrefab(tool);
            if (go == null) return "ERROR: no terrain prefab '" + tool + "'";
            float rr = Mathf.Clamp(width / 2f, 0.5f, 4f);
            int across = Mathf.Max(1, Mathf.CeilToInt(width / (2f * rr)));
            int along = Mathf.Max(1, Mathf.CeilToInt(length / rr));
            if (across * along > 400) return "ERROR: too many stamps (" + (across * along) + " > 400); shorten the path";
            int made = 0;
            for (int i = 0; i < along; i++)
            {
                float d = along == 1 ? length / 2f : rr + i * ((length - 2f * rr) / Mathf.Max(1, along - 1));
                if (length < 2f * rr) d = length / 2f;
                for (int k = 0; k < across; k++)
                {
                    float lat = across == 1 ? 0f : -(width / 2f - rr) + k * ((width - 2f * rr) / (across - 1));
                    Vector3 pos = an.origin + an.fwd * d + an.right * lat;
                    pos.y = ZoneSystem.instance.GetGroundHeight(pos);
                    Stamp(go, pos, 0f, rr);
                    made++;
                }
            }
            return "path " + type + " " + length.ToString("F1") + " x " + width.ToString("F1") + " m from " + V(an.origin)
                + (an.aimed ? " (crosshair)" : "") + ": " + made + " stamps of r=" + rr.ToString("F1") + " (tool " + tool + ")";
        }

        // calibration helper: dumps the TerrainModifier values of a terrain prefab so the commands can be tuned
        string Inspect(string prefab)
        {
            GameObject go = ZNetScene.instance.GetPrefab(prefab);
            if (go == null) return "ERROR: no prefab '" + prefab + "'";
            StringBuilder sb = new StringBuilder(prefab + "\n");
            Component[] comps = go.GetComponents<Component>();
            foreach (Component c in comps)
            {
                if (c == null) continue;
                sb.Append("[").Append(c.GetType().Name).Append("]\n");
                if (c is TerrainModifier || c is TerrainOp)
                    foreach (FieldInfo f in c.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        object v = null;
                        try { v = f.GetValue(c); } catch { }
                        if (v == null || v is UnityEngine.Object) continue;
                        sb.Append("  ").Append(f.Name).Append(" = ").Append(v).Append('\n');
                    }
            }
            return sb.ToString();
        }

        // reads the game's real cheat state: world/profile/inventory flags and the ZDO flag on nearby spawned objects
        string CheatState()
        {
            StringBuilder sb = new StringBuilder();
            BindingFlags all = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            Type ach = typeof(Player).Assembly.GetType("Achievements");
            Type cu = typeof(Player).Assembly.GetType("CheatUtils");
            if (ach != null)
                foreach (string n in new string[] { "IsCheatedAtAll", "IsWorldCheated" })
                    foreach (MethodInfo m in ach.GetMethods(all))
                        if (m.Name == n) sb.Append("Achievements.").Append(n).Append("(").Append(Sig(m)).Append(") = ").Append(Call(m, null)).Append('\n');
            FieldInfo tc = typeof(Terminal).GetField("m_cheat", all);
            if (tc != null) sb.Append("Terminal.m_cheat = ").Append(tc.GetValue(null)).Append('\n');
            try
            {
                PlayerProfile pp = Game.instance.GetPlayerProfile();
                FieldInfo uc = typeof(PlayerProfile).GetField("m_usedCheats", all);
                if (uc != null) sb.Append("PlayerProfile.m_usedCheats = ").Append(uc.GetValue(pp)).Append('\n');
            }
            catch (Exception e) { sb.Append("profile: ").Append(e.Message).Append('\n'); }
            Player p = P();
            foreach (MethodInfo m in typeof(Inventory).GetMethods(all))
                if (m.Name == "AnyCheatedItem") sb.Append("Inventory.AnyCheatedItem(").Append(Sig(m)).Append(") = ").Append(Call(m, p.GetInventory())).Append('\n');
            // nearest Pickable_Carrot / any pickable we spawned: is its ZDO marked cheated?
            Pickable near = null; float best = 1e9f;
            foreach (Pickable pk in UnityEngine.Object.FindObjectsOfType<Pickable>())
            {
                float d = Vector3.Distance(pk.transform.position, p.transform.position);
                if (d < best) { best = d; near = pk; }
            }
            if (near != null)
            {
                ZNetView nv = near.GetComponent<ZNetView>();
                ZDO z = nv != null ? nv.GetZDO() : null;
                if (z != null)
                {
                    sb.Append("nearest pickable ").Append(near.name).Append(" at ").Append(best.ToString("F1")).Append(" m\n");
                    FieldInfo sc = typeof(Player).Assembly.GetType("ZDOVars").GetField("s_cheated", all);
                    if (sc != null)
                    {
                        int h = (int)sc.GetValue(null);
                        sb.Append("  ZDO cheated key: int=").Append(z.GetInt(h, 0)).Append(" bool=").Append(z.GetBool(h, false)).Append('\n');
                    }
                    if (cu != null)
                        foreach (MethodInfo m in cu.GetMethods(all))
                            if (m.Name == "IsCheated")
                            {
                                ParameterInfo[] ps = m.GetParameters();
                                sb.Append("  CheatUtils.IsCheated(").Append(Sig(m)).Append(")");
                                if (ps.Length == 1 && ps[0].ParameterType.IsAssignableFrom(typeof(ZDO))) sb.Append(" = ").Append(Call(m, null, z));
                                sb.Append('\n');
                            }
                }
            }
            return sb.ToString();
        }

        static string Sig(MethodInfo m)
        {
            List<string> l = new List<string>();
            foreach (ParameterInfo pi in m.GetParameters()) l.Add(pi.ParameterType.Name);
            return string.Join(", ", l.ToArray()) + (m.IsStatic ? " static" : "");
        }

        static string Call(MethodInfo m, object target, params object[] args)
        {
            try
            {
                if (!m.IsStatic && target == null) return "(needs instance)";
                if (m.GetParameters().Length != args.Length) return "(needs " + m.GetParameters().Length + " args)";
                return Convert.ToString(m.Invoke(m.IsStatic ? null : target, args));
            }
            catch (Exception e) { return "ERR " + e.Message; }
        }

        string ScaleTrees(string filter, float factor, float radius)
        {
            Player p = P();
            Vector3 c = p.transform.position;
            int n = 0;
            foreach (TreeBase t in UnityEngine.Object.FindObjectsOfType<TreeBase>())
            {
                if (t.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (Vector3.Distance(t.transform.position, c) > radius) continue;
                ZNetView nv = t.GetComponent<ZNetView>();
                if (nv == null || !nv.IsValid()) continue;
                nv.ClaimOwnership();
                nv.GetZDO().Set(ScaleKey, Vector3.one * factor);
                ApplyTreeScale(t, factor);
                n++;
            }
            return "scaled " + n + " '" + filter + "' trees within " + radius + " m to x" + factor;
        }

        static string BaseName(GameObject g) { return g.name.Replace("(Clone)", "").Trim(); }

        // size + hit points + drops, all from the prefab values times factor
        internal static void ApplyTreeScale(TreeBase t, float factor)
        {
            t.transform.localScale = Vector3.one * factor;
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(BaseName(t.gameObject)) : null;
            TreeBase baseTree = prefab != null ? prefab.GetComponent<TreeBase>() : null;
            if (baseTree == null) return;
            t.m_health = baseTree.m_health * factor;
            t.m_dropWhenDestroyed.m_dropMin = Mathf.Max(1, Mathf.RoundToInt(baseTree.m_dropWhenDestroyed.m_dropMin * factor));
            t.m_dropWhenDestroyed.m_dropMax = Mathf.Max(1, Mathf.RoundToInt(baseTree.m_dropWhenDestroyed.m_dropMax * factor));
        }

        // discovery: list every member in the game assembly whose name contains "heat" and static values
        string CheatInfo()
        {
            StringBuilder sb = new StringBuilder();
            Assembly asm = typeof(Player).Assembly;
            foreach (Type t in asm.GetTypes())
            {
                BindingFlags bf = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                foreach (MemberInfo m in t.GetMembers(bf))
                {
                    if (m.Name.IndexOf("heat", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    string val = "";
                    FieldInfo fi = m as FieldInfo;
                    if (fi != null && fi.IsStatic)
                    {
                        try { val = " = " + fi.GetValue(null); } catch { }
                    }
                    sb.Append(t.FullName).Append('.').Append(m.Name).Append(" [").Append(m.MemberType).Append(']').Append(val).Append('\n');
                }
            }
            return sb.ToString();
        }
    }

    // vanilla ignores a saved scale on trees when they load; re-apply it so big trees stay big.
    [HarmonyPatch(typeof(ZNetView), "Awake")]
    static class ZNetViewAwakePatch
    {
        static void Postfix(ZNetView __instance)
        {
            try
            {
                ZDO z = __instance.GetZDO();
                if (z == null) return;
                TreeBase t = __instance.GetComponent<TreeBase>();
                if (t == null) return;
                Vector3 s = z.GetVec3("scale", Vector3.zero);
                if (s.x > 0.01f && Mathf.Abs(s.x - 1f) > 0.001f) HomeCheatPlugin.ApplyTreeScale(t, s.x);
            }
            catch { }
        }
    }
}
