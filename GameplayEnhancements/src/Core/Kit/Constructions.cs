// Player-built pieces near a point: walls, floors, fences, workbenches (Piece.IsPlacedByPlayer = it has a creator).
// Uses the game's Piece.GetAllPiecesInRadius: loaded pieces only, ghost (preview) pieces skipped, distance measured to the
// piece's pivot. A long wall can cross the circle while its pivot is outside, so callers pass their radius plus a margin.
// Users: groundlevel avoid=1, path avoid=1, plant avoid=1, dome.
using System.Collections.Generic;
using UnityEngine;

namespace HomeCheat.Kit
{
    public static class Constructions
    {
        // player-built pieces within 'radius' of 'pos'
        public static List<Piece> All(Vector3 pos, float radius)
        {
            List<Piece> found = new List<Piece>();
            List<Piece> any = new List<Piece>();
            Piece.GetAllPiecesInRadius(pos, radius, any);
            foreach (Piece pc in any)
                if (pc != null && pc.IsPlacedByPlayer()) found.Add(pc);
            return found;
        }

        // how many player-built pieces are within 'radius'; 'nearest' is the closest one (null when there is none)
        public static int Near(Vector3 pos, float radius, out Piece nearest)
        {
            nearest = null;
            float best = float.MaxValue;
            List<Piece> found = All(pos, radius);
            foreach (Piece pc in found)
            {
                float d = Vector3.Distance(pos, pc.transform.position);
                if (d < best) { best = d; nearest = pc; }
            }
            return found.Count;
        }

        // "<name> at <d> m" for answers such as "piece wood_wall at 3.2 m inside the radius"
        public static string Describe(Piece pc, Vector3 from)
        {
            if (pc == null) return "none";
            return Prefabs.BaseName(pc.gameObject) + " at " + Fmt.F(Vector3.Distance(from, pc.transform.position), 1) + " m";
        }
    }
}
