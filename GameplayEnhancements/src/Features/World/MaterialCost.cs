// cost= on plant, field and spawn (card materials-nearby, Maksym 2026-11-03: "Our commands only", "Safe chests, menu"):
//   0 (default) = built free, as typed commands always were;
//   nearby = Kit.Materials: per piece your inventory first, then safe chests within radius= nearest first, the rest free;
//   check = what nearby would take; nothing is taken, planted or built.
// The spell lines (7.2 field, 7.4 plant, 8 spawn) pass cost=nearby. Only HomeCheat commands take materials this way; the
// vanilla hammer and cultivator stay vanilla.
// The recipe of a sapling or a construction is its Piece's m_resources; a crop (Pickable_*) has none of its own: it is the
// seed piece that grows it (GrowRules.PlantOf), e.g. Pickable_Carrot -> sapling_carrot (CarrotSeeds 1).
using System.Collections.Generic;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.WorldCommands
{
    internal sealed class MaterialCost
    {
        public const string None = "0", Nearby = "nearby", Check = "check";

        public readonly string Mode;
        public readonly float Radius;
        public bool Off { get { return Mode == None; } }
        public bool CheckOnly { get { return Mode == Check; } }

        MaterialCost(string mode, float radius) { Mode = mode; Radius = radius; }

        // cost=0|nearby|check, radius=50 (cap 100: Caps.Clamp, noted in the answer)
        public static MaterialCost From(Args a, out string note)
        {
            note = "";
            string v = a.Str("cost", None).Trim().ToLowerInvariant();
            if (v == "" || v == "0" || v == "false" || v == "off" || v == "no") v = None;
            if (v != None && v != Nearby && v != Check)
                throw new CommandException("cost must be 0, nearby or check (nearby = materials from you, then safe chests within radius=, the rest free)");
            float r = a.Float("radius", Materials.DefaultRadius);
            if (r < 0f) throw new CommandException("radius must be >= 0");
            if (v != None && r > Materials.MaxRadius) note = " (radius " + Fmt.F(r, 0) + " lowered to the cap " + Fmt.F(Materials.MaxRadius, 0) + " m)";
            return new MaterialCost(v, Mathf.Min(r, Materials.MaxRadius));
        }

        // the requirements a sapling, crop or construction costs: its own Piece, or the seed piece that grows the crop
        public static Piece.Requirement[] Recipe(GameObject prefab)
        {
            if (prefab == null) return null;
            Piece own = prefab.GetComponent<Piece>();
            if (own != null) return own.m_resources;
            Plant seed = GrowRules.PlantOf(prefab);
            Piece sp = seed != null ? seed.GetComponent<Piece>() : null;
            return sp != null ? sp.m_resources : null;
        }

        // the recipe times n (one entry per item): n pieces in one take, same split as n takes of one piece (you first, then
        // the chests nearest first, the rest free), without counting every chest n times
        public static Piece.Requirement[] Times(Piece.Requirement[] reqs, int n)
        {
            List<Piece.Requirement> l = new List<Piece.Requirement>();
            if (reqs == null || n <= 0) return l.ToArray();
            foreach (Piece.Requirement r in reqs)
            {
                if (r == null || r.m_resItem == null || r.m_amount <= 0) continue;
                Piece.Requirement x = new Piece.Requirement();
                x.m_resItem = r.m_resItem;
                x.m_amount = r.m_amount * n;
                l.Add(x);
            }
            return l.ToArray();
        }

        // a run for this cast (null when cost=0)
        public MaterialsRun Begin(Player p)
        {
            return Off ? null : Materials.Begin(p, Radius, CheckOnly);
        }

        // "; materials 10 from you, 50 from 1 chest(s), 40 free (Wood 10/50/40)" or "; materials (check, nothing taken): ..."
        public string Line(MaterialsRun run, string note)
        {
            if (run == null) return "";
            return "; materials " + (CheckOnly ? "that would be taken (cost=check, nothing taken): " : "(radius " + Fmt.F(Radius, 0) + " m): ")
                + run.Report.Summary() + note + (CheckOnly ? "" : ", no undo");
        }
    }
}
