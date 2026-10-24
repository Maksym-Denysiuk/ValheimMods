using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.Diag
{
    // cheatstate: reads the game's real cheat state - world/profile/inventory flags and the ZDO flag on the nearest pickable
    public sealed class CheatStateCommand : CommandBase
    {
        public override string Name { get { return "cheatstate"; } }
        protected override string Usage { get { return "cheatstate - read-only: Achievements cheat checks, Terminal.m_cheat, profile used-cheats, inventory and nearest pickable cheat flags"; } }

        protected override string Execute(Args a)
        {
            StringBuilder sb = new StringBuilder();
            BindingFlags all = Refl.All;
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
            Player p = Players.Local;
            foreach (MethodInfo m in typeof(Inventory).GetMethods(all))
                if (m.Name == "AnyCheatedItem") sb.Append("Inventory.AnyCheatedItem(").Append(Sig(m)).Append(") = ").Append(Call(m, p.GetInventory())).Append('\n');
            // nearest pickable: is its ZDO marked cheated?
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
                    Type zv = typeof(Player).Assembly.GetType("ZDOVars");
                    FieldInfo sc = zv == null ? null : zv.GetField("s_cheated", all);
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
    }
}
