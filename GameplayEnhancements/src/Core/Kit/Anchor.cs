// Where a block of cells starts and which way it runs: row 1 at Origin, rows go along Fwd (away from the player),
// columns along Right. Default origin = the crosshair point; a 'dist' parameter means "that many metres straight
// ahead of the player" instead.
using UnityEngine;

namespace HomeCheat.Kit
{
    public sealed class Anchor
    {
        public Vector3 Origin, Fwd, Right;
        public bool Aimed;

        // the crosshair point, direction = player -> point (flat)
        public static Anchor AtAim(Player p)
        {
            Anchor an = new Anchor();
            an.Origin = Aim.Point(p, out an.Aimed);
            an.Fwd = an.Origin - p.transform.position; an.Fwd.y = 0f;
            if (an.Fwd.sqrMagnitude < 0.01f) { an.Fwd = p.transform.forward; an.Fwd.y = 0f; }
            an.Fwd.Normalize();
            an.Right = Vector3.Cross(Vector3.up, an.Fwd);
            return an;
        }

        // 'dist' metres straight ahead when the parameter is given (default 6), else the crosshair
        public static Anchor FromArgs(Player p, Args a)
        {
            if (!a.Has("dist")) return AtAim(p);
            Anchor an = new Anchor();
            an.Fwd = p.transform.forward; an.Fwd.y = 0f; an.Fwd.Normalize();
            an.Origin = p.transform.position + an.Fwd * a.Float("dist", 6f);
            an.Right = Vector3.Cross(Vector3.up, an.Fwd);
            return an;
        }

        // one cell on the ground; 'centre' = the column on the anchor line (GridMath.CentreColumn(cols) or an aim column)
        public Vector3 Cell(int row, int col, float centre, float spacing)
        {
            float along, across;
            GridMath.Offset(row, col, centre, spacing, out along, out across);
            Vector3 pos = Origin + Fwd * along + Right * across;
            pos.y = Ground.Height(pos);
            return pos;
        }
    }
}
