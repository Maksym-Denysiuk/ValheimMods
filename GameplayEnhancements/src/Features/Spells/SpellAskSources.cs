using System;
using System.Collections.Generic;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.Spells
{
    // The answers of a question: the fixed options of its ask= flag, or a dynamic source read from the game when the level opens.
    //   @players  players online (only you when this PC does not host the world)
    //   @crops    Pickable_* crops that grow from a Plant (value = the Pickable_ prefab, as field prefab= takes it)
    //   @trees    *_Sapling prefabs with a Plant (value = the sapling, as plant prefab= takes it)
    //   @prefabs  creatures (non-player Character) and items by ItemDrop item type, as categories (Group)
    // Read only: nothing in the world is changed.
    internal static class SpellAskSources
    {
        public static List<SpellAskOption> Resolve(SpellAsk a)
        {
            if (a.Source.Length == 0) return a.Options;
            List<SpellAskOption> r = new List<SpellAskOption>();
            try
            {
                switch (a.Source)
                {
                    case "@players": Players(r); break;
                    case "@crops": Crops(r); break;
                    case "@trees": Trees(r); break;
                    case "@prefabs": AllPrefabs(r); break;
                }
            }
            catch (Exception e) { HomeApi.Log("spells: answers of " + a.Source + " failed: " + e.Message); }
            r.Sort(delegate (SpellAskOption x, SpellAskOption y)
            {
                int g = string.Compare(x.Group, y.Group, StringComparison.OrdinalIgnoreCase);
                return g != 0 ? g : string.Compare(x.Label, y.Label, StringComparison.OrdinalIgnoreCase);
            });
            return r;
        }

        static void Players(List<SpellAskOption> r)
        {
            Player me = Player.m_localPlayer;
            bool host = ZNet.instance != null && ZNet.instance.IsServer();
            foreach (Player p in Player.GetAllPlayers())
            {
                if (p == null || (!host && p != me)) continue;
                string n = p.GetPlayerName();
                if (!string.IsNullOrEmpty(n)) r.Add(new SpellAskOption(n, n, ""));
            }
        }

        static void Crops(List<SpellAskOption> r)
        {
            if (ZNetScene.instance == null) return;
            HashSet<string> seen = new HashSet<string>();
            foreach (GameObject g in Prefabs.All())
            {
                Plant pl = g.GetComponent<Plant>();
                if (pl == null || pl.m_grownPrefabs == null) continue;
                foreach (GameObject grown in pl.m_grownPrefabs)
                {
                    if (grown == null || !grown.name.StartsWith("Pickable_") || !seen.Add(grown.name)) continue;
                    r.Add(new SpellAskOption(grown.name.Substring(9), grown.name, ""));
                }
            }
        }

        static void Trees(List<SpellAskOption> r)
        {
            if (ZNetScene.instance == null) return;
            foreach (GameObject g in Prefabs.All())
            {
                if (!g.name.EndsWith("_Sapling", StringComparison.OrdinalIgnoreCase) || g.GetComponent<Plant>() == null) continue;
                r.Add(new SpellAskOption(g.name.Substring(0, g.name.Length - 8), g.name, ""));
            }
        }

        static void AllPrefabs(List<SpellAskOption> r)
        {
            if (ZNetScene.instance != null)
                foreach (GameObject g in Prefabs.All())
                    if (g.GetComponent<Character>() != null && g.GetComponent<Player>() == null) r.Add(new SpellAskOption(g.name, g.name, "Creatures"));
            if (ObjectDB.instance != null)
                foreach (GameObject g in ObjectDB.instance.m_items)
                {
                    if (g == null) continue;
                    ItemDrop d = g.GetComponent<ItemDrop>();
                    if (d == null || d.m_itemData == null || d.m_itemData.m_shared == null) continue;
                    r.Add(new SpellAskOption(g.name, g.name, "Items: " + d.m_itemData.m_shared.m_itemType));
                }
        }
    }
}
