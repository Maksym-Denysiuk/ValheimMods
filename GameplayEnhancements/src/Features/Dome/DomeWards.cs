// The wards of the domes, read and written through their ZDOs, so wards far from this PC (not loaded here) count too.
// A dome ward is a vanilla guard_stone with the group keys below in its ZDO (R9: membership lives on the wards' own saved
// data). The vanilla state of a ward is its ZDO: enabled (ZDOVars.s_enabled) and the permitted list (ZDOVars.s_permitted
// count, then pu_id<i> / pu_name<i>, the format of PrivateArea.SetPermittedPlayers). Vanilla clients read the same ZDO,
// so whatever is written here is what every player's game checks.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.Dome
{
    internal sealed class WardState
    {
        public bool Enabled;
        public List<KeyValuePair<long, string>> Permitted = new List<KeyValuePair<long, string>>();

        // "1|<id>,<id>" with the ids sorted: equal for the same on/off state and the same permitted players
        public string Signature()
        {
            List<long> ids = new List<long>();
            foreach (KeyValuePair<long, string> kv in Permitted) ids.Add(kv.Key);
            ids.Sort();
            StringBuilder sb = new StringBuilder(Enabled ? "1|" : "0|");
            for (int i = 0; i < ids.Count; i++) { if (i > 0) sb.Append(','); sb.Append(ids[i]); }
            return sb.ToString();
        }
    }

    internal static class DomeWards
    {
        public const string WardPrefab = "guard_stone";
        public const string KeyDome = "hc_dome";        // zone id: the ZDO mark of every dome ward
        public const string KeyCx = "hc_dome_cx";
        public const string KeyCz = "hc_dome_cz";
        public const string KeyRadius = "hc_dome_r";
        public const string KeyCount = "hc_dome_n";     // wards placed in the group
        public const string KeySig = "hc_dome_sig";     // the group state last copied to every ward (WardState.Signature)

        static MethodInfo setEnabled;

        // every guard_stone ZDO of the world (on the host: all of them, loaded or not). The game's iterator reads one
        // sector per call and returns true after the last one (a world has about 100 000 sectors).
        public static List<ZDO> AllWards()
        {
            List<ZDO> list = new List<ZDO>();
            if (ZDOMan.instance == null) return list;
            int index = 0;
            for (int guard = 0; guard < 4000000; guard++)
                if (ZDOMan.instance.GetAllZDOsWithPrefabIterative(WardPrefab, list, ref index)) break;
            return list;
        }

        // dome wards grouped by zone id
        public static Dictionary<string, List<ZDO>> Groups(List<ZDO> wards)
        {
            Dictionary<string, List<ZDO>> groups = new Dictionary<string, List<ZDO>>();
            foreach (ZDO z in wards)
            {
                string id = z.GetString(KeyDome, "");
                if (id.Length == 0) continue;
                List<ZDO> g;
                if (!groups.TryGetValue(id, out g)) { g = new List<ZDO>(); groups[id] = g; }
                g.Add(z);
            }
            return groups;
        }

        public static bool Exists(ZDO z) { return z != null && ZDOMan.instance != null && ZDOMan.instance.GetZDO(z.m_uid) != null; }

        public static long Creator(ZDO z) { return z.GetLong(ZDOVars.s_creator, 0L); }

        public static float DistanceXZ(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        public static WardState Read(ZDO z)
        {
            WardState s = new WardState();
            s.Enabled = z.GetBool(ZDOVars.s_enabled, false);
            int n = z.GetInt(ZDOVars.s_permitted, 0);
            for (int i = 0; i < n; i++)
            {
                long id = z.GetLong("pu_id" + i, 0L);
                if (id == 0L) continue;
                s.Permitted.Add(new KeyValuePair<long, string>(id, z.GetString("pu_name" + i, "")));
            }
            return s;
        }

        // this PC takes the ward's ZDO before writing: the owner's copy is the one the game keeps
        public static void Claim(ZDO z)
        {
            if (!z.IsOwner()) z.SetOwner(ZDOMan.GetSessionID());
        }

        // writes on/off and the permitted list; a ward loaded here goes through the game's own SetEnabled
        // (status, activate / deactivate effect), a ward far away only through its ZDO (its effect does not play)
        public static void Write(ZDO z, WardState s)
        {
            Claim(z);
            if (z.GetBool(ZDOVars.s_enabled, false) != s.Enabled)
            {
                PrivateArea area = Loaded(z);
                if (area != null && SetEnabledMethod != null) SetEnabledMethod.Invoke(area, new object[] { s.Enabled });
                else z.Set(ZDOVars.s_enabled, s.Enabled);
            }
            z.Set(ZDOVars.s_permitted, s.Permitted.Count);
            for (int i = 0; i < s.Permitted.Count; i++)
            {
                z.Set("pu_id" + i, s.Permitted[i].Key);
                z.Set("pu_name" + i, s.Permitted[i].Value);
            }
        }

        // the ward's PrivateArea when it is loaded on this PC, else null
        public static PrivateArea Loaded(ZDO z)
        {
            if (ZNetScene.instance == null) return null;
            ZNetView nv = ZNetScene.instance.FindInstance(z);
            return nv == null ? null : nv.GetComponent<PrivateArea>();
        }

        static MethodInfo SetEnabledMethod
        {
            get
            {
                if (setEnabled == null) setEnabled = typeof(PrivateArea).GetMethod("SetEnabled", Refl.InstanceAll, null, new Type[] { typeof(bool) }, null);
                return setEnabled;
            }
        }

        // removes a ward: through the scene when it is loaded here, else its ZDO
        public static void Remove(ZDO z)
        {
            ZNetView nv = ZNetScene.instance == null ? null : ZNetScene.instance.FindInstance(z);
            if (nv != null)
            {
                nv.ClaimOwnership();
                ZNetScene.instance.Destroy(nv.gameObject);
                return;
            }
            Claim(z);
            ZDOMan.instance.DestroyZDO(z);
        }

        public static Vector3 Centre(ZDO z) { return new Vector3(z.GetFloat(KeyCx, 0f), 0f, z.GetFloat(KeyCz, 0f)); }
    }
}
