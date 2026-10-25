// How a construction repeats upward (continue=1, Maksym 2026-10-25: "continue building the ladders this blueprint provides").
// A stackable blueprint ends with a layer that is its first layer moved by a fixed offset (the spiral stairs: the two top
// floors are the two bottom floors moved by 0.45, 3.87, -0.23). The offset is found by trying every pair of same-prefab
// pieces (one in the bottom layer, one in the top layer) and keeping the shift that lands the most bottom-layer pieces on a
// top-layer piece of the same prefab. The next copy goes up by that shift in the same turn, and its bottom pieces that land
// on the top pieces of the copy below are the shared landing: they are not placed twice. Unity-free.
using System;
using System.Collections.Generic;

namespace HomeCheat.Features.Construction
{
    internal static class BlueprintRepeat
    {
        const float LayerBand = 0.05f;   // metres: pieces this close to the lowest / highest pivot form the layer
        const float Match = 0.1f;        // metres: a shifted piece this close to a same-prefab piece lands on it
        const float MinRise = 0.5f;      // a shift must go up at least this far

        static List<BlueprintPiece> All(BlueprintEntry e)
        {
            List<BlueprintPiece> all = new List<BlueprintPiece>();
            foreach (Blueprint b in e.Parts) all.AddRange(b.Pieces);
            return all;
        }

        // false when no shift lands at least one bottom piece on a top piece (the blueprint does not stack)
        public static bool Find(BlueprintEntry e, out float dx, out float dy, out float dz, out int matched)
        {
            dx = dy = dz = 0f; matched = 0;
            List<BlueprintPiece> all = All(e);
            if (all.Count == 0) return false;
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (BlueprintPiece p in all) { if (p.Y < minY) minY = p.Y; if (p.Y > maxY) maxY = p.Y; }
            if (maxY - minY < MinRise) return false;
            List<BlueprintPiece> bottom = new List<BlueprintPiece>(), top = new List<BlueprintPiece>();
            foreach (BlueprintPiece p in all)
            {
                if (p.Y <= minY + LayerBand) bottom.Add(p);
                if (p.Y >= maxY - LayerBand) top.Add(p);
            }
            int best = 0;
            float bestLen = float.MaxValue;
            foreach (BlueprintPiece b in bottom)
                foreach (BlueprintPiece t in top)
                {
                    if (b.Prefab != t.Prefab) continue;
                    float sx = t.X - b.X, sy = t.Y - b.Y, sz = t.Z - b.Z;
                    int score = Lands(bottom, top, sx, sy, sz);
                    float len = sx * sx + sy * sy + sz * sz;
                    if (score > best || (score == best && score > 0 && len < bestLen))
                    {
                        best = score; bestLen = len; dx = sx; dy = sy; dz = sz;
                    }
                }
            matched = best;
            return best > 0;
        }

        static int Lands(List<BlueprintPiece> bottom, List<BlueprintPiece> top, float sx, float sy, float sz)
        {
            int n = 0;
            foreach (BlueprintPiece b in bottom)
                if (Partner(top, b, sx, sy, sz)) n++;
            return n;
        }

        static bool Partner(List<BlueprintPiece> top, BlueprintPiece b, float sx, float sy, float sz)
        {
            foreach (BlueprintPiece t in top)
            {
                if (t.Prefab != b.Prefab) continue;
                float ex = b.X + sx - t.X, ey = b.Y + sy - t.Y, ez = b.Z + sz - t.Z;
                if (ex * ex + ey * ey + ez * ez <= Match * Match) return true;
            }
            return false;
        }

        // the pieces of the next copy that land on a piece of the copy below (placed once, by the copy below)
        public static HashSet<BlueprintPiece> Shared(BlueprintEntry e, float dx, float dy, float dz)
        {
            HashSet<BlueprintPiece> shared = new HashSet<BlueprintPiece>();
            List<BlueprintPiece> all = All(e);
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (BlueprintPiece p in all) { if (p.Y < minY) minY = p.Y; if (p.Y > maxY) maxY = p.Y; }
            List<BlueprintPiece> bottom = new List<BlueprintPiece>(), top = new List<BlueprintPiece>();
            foreach (BlueprintPiece p in all)
            {
                if (p.Y <= minY + LayerBand) bottom.Add(p);
                if (p.Y >= maxY - LayerBand) top.Add(p);
            }
            foreach (BlueprintPiece b in bottom)
                if (Partner(top, b, dx, dy, dz)) shared.Add(b);
            return shared;
        }
    }
}
