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
                if (m.Name == "AnyCheatedItem" || m.Name == "CheatedDamagingItemEquipped") sb.Append("Inventory.").Append(m.Name).Append("(").Append(Sig(m)).Append(") = ").Append(Call(m, p.GetInventory())).Append('\n');
            // the attacker conditions of Character.ApplyDamage: any of these marks a creature the local player hits as cheated
            foreach (string n in new string[] { "IsDebugFlying", "InGodMode", "InGhostMode" })
            {
                MethodInfo m = typeof(Character).GetMethod(n, all, null, Type.EmptyTypes, null);
                sb.Append("Player.").Append(n).Append("() = ").Append(m == null ? "?" : Call(m, p)).Append('\n');
            }
            AppendCheatedAround(sb, p, a.Float("radius", 25f));
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

        // loose items, ragdolls and creatures around the player whose ZDO carries the cheated mark (CheatUtils.IsCheated)
        static void AppendCheatedAround(StringBuilder sb, Player p, float radius)
        {
            int key = (int)Refl.GetStatic(typeof(Player).Assembly.GetType("ZDOVars"), "s_cheated");
            int items = 0, itemsCheated = 0, chars = 0, charsCheated = 0, dolls = 0, dollsCheated = 0;
            StringBuilder names = new StringBuilder();
            foreach (ZNetView nv in UnityEngine.Object.FindObjectsOfType<ZNetView>())
            {
                if (nv == null || !nv.IsValid() || Vector3.Distance(nv.transform.position, p.transform.position) > radius) continue;
                bool cheated = nv.GetZDO().GetInt(key, 0) != 0;
                ItemDrop drop = nv.GetComponent<ItemDrop>();
                // an item keeps its mark in its own data (ItemData.m_cheated), not in the ZDO key
                if (drop != null) { items++; if (cheated || (drop.m_itemData != null && drop.m_itemData.m_cheated)) { itemsCheated++; if (names.Length < 300) names.Append(' ').Append(Prefabs.BaseName(nv.gameObject)); } }
                else if (nv.GetComponent<Ragdoll>() != null) { dolls++; if (cheated) dollsCheated++; }
                else if (nv.GetComponent<Character>() != null && nv.GetComponent<Player>() == null) { chars++; if (cheated) charsCheated++; }
            }
            StringBuilder inv = new StringBuilder();
            foreach (ItemDrop.ItemData it in p.GetInventory().GetAllItems())
                if (it != null && it.m_cheated) inv.Append(' ').Append(it.m_dropPrefab != null ? it.m_dropPrefab.name : it.m_shared.m_name).Append(" x").Append(it.m_stack);
            sb.Append("inventory items marked cheated:").Append(inv.Length > 0 ? inv.ToString() : " none").Append('\n');
            sb.Append("within ").Append(radius.ToString("F0")).Append(" m: items ").Append(itemsCheated).Append(" cheated of ").Append(items)
              .Append(", ragdolls ").Append(dollsCheated).Append(" of ").Append(dolls).Append(", creatures ").Append(charsCheated).Append(" of ").Append(chars)
              .Append(names.Length > 0 ? "; cheated items:" + names : "").Append('\n');
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
