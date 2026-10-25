// Material choice (card A4): build a construction in another family of pieces. Families come from the game's own pieces: a
// building piece's name before its first '_' (wood, darkwood, stone, blackmarble, woodiron, ...); the rest is its shape. A
// piece swaps when the chosen family has the same shape (wood_beam_45 -> darkwood_beam_45); a piece with no counterpart, or
// without a family (woodwall, a bush), stays as it is and is counted. "blueprint" (the default) keeps every piece.
using System;
using System.Collections.Generic;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.Construction
{
    internal static class MaterialSwap
    {
        public const string Blueprint = "blueprint";
        const int MinFamily = 4;   // a prefix with fewer building pieces is not a family

        static Dictionary<string, HashSet<string>> families;   // family -> shapes, from this world's ZNetScene
        static ZNetScene seenScene;

        static Dictionary<string, HashSet<string>> Families()
        {
            if (families != null && seenScene == ZNetScene.instance) return families;
            Dictionary<string, HashSet<string>> f = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (GameObject go in Prefabs.All())
            {
                if (go == null || go.GetComponent<Piece>() == null || go.GetComponent<WearNTear>() == null) continue;
                string fam, shape;
                if (!Split(go.name, out fam, out shape)) continue;
                HashSet<string> s;
                if (!f.TryGetValue(fam, out s)) { s = new HashSet<string>(StringComparer.OrdinalIgnoreCase); f[fam] = s; }
                s.Add(shape);
            }
            List<string> small = new List<string>();
            foreach (KeyValuePair<string, HashSet<string>> kv in f) if (kv.Value.Count < MinFamily) small.Add(kv.Key);
            foreach (string k in small) f.Remove(k);
            families = f;
            seenScene = ZNetScene.instance;
            return f;
        }

        static bool Split(string name, out string family, out string shape)
        {
            int i = name.IndexOf('_');
            family = i > 0 ? name.Substring(0, i) : "";
            shape = i > 0 ? name.Substring(i + 1) : "";
            return i > 0 && shape.Length > 0;
        }

        // the prefab name to build for a blueprint piece in the material (the same name when nothing swaps)
        public static string Map(string prefab, string material)
        {
            if (string.IsNullOrEmpty(material) || string.Equals(material, Blueprint, StringComparison.OrdinalIgnoreCase)) return prefab;
            string fam, shape;
            if (!Split(prefab, out fam, out shape) || string.Equals(fam, material, StringComparison.OrdinalIgnoreCase)) return prefab;
            HashSet<string> own, target;
            Dictionary<string, HashSet<string>> f = Families();
            if (!f.TryGetValue(fam, out own) || !own.Contains(shape) || !f.TryGetValue(material, out target) || !target.Contains(shape)) return prefab;
            return material.ToLowerInvariant() + "_" + shape;
        }

        public static bool Known(string material)
        {
            return string.Equals(material, Blueprint, StringComparison.OrdinalIgnoreCase) || Families().ContainsKey(material);
        }

        // every family that would swap at least one piece of the construction, with the count: "darkwood 140, stone 22"
        public static List<KeyValuePair<string, int>> Options(BlueprintEntry e)
        {
            List<KeyValuePair<string, int>> list = new List<KeyValuePair<string, int>>();
            foreach (string fam in Families().Keys)
            {
                int n = 0;
                foreach (Blueprint b in e.Parts)
                    foreach (BlueprintPiece pc in b.Pieces)
                        if (Map(pc.Prefab, fam) != pc.Prefab) n++;
                if (n > 0) list.Add(new KeyValuePair<string, int>(fam.ToLowerInvariant(), n));
            }
            list.Sort(delegate (KeyValuePair<string, int> l, KeyValuePair<string, int> r) { return r.Value.CompareTo(l.Value); });
            return list;
        }
    }
}
