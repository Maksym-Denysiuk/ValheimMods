// Where cultivate and field lay their grid (Maksym 2026-10-25): from the player, not the crosshair. Row 1 is the player's own
// cell (or 'dist' metres in front), centred on the look direction; rows run away from the player that way.
// Both commands use this, so the crops land exactly on the cultivated cells. A feature helper (not in the core) so it reloads.
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.WorldCommands
{
    internal static class FieldAnchor
    {
        public static Anchor AheadOfPlayer(Player p, Args a, float spacing)
        {
            Anchor an = new Anchor();
            an.Fwd = p.GetLookDir(); an.Fwd.y = 0f;
            if (an.Fwd.sqrMagnitude < 0.01f) { an.Fwd = p.transform.forward; an.Fwd.y = 0f; }
            an.Fwd.Normalize();
            an.Origin = p.transform.position + an.Fwd * a.Float("dist", 0f);
            an.Right = Vector3.Cross(Vector3.up, an.Fwd);
            return an;
        }
    }
}
