// Where the wards of a dome go, without Unity: the zone from two points and the ward centres that cover it.
// Each vanilla client checks a ward by horizontal distance with the ward's own radius (PrivateArea.IsInside uses
// Utils.DistanceXZ), so a zone wider than one ward needs several wards whose circles leave no gap.
// Covering: a hexagonal lattice with spacing sqrt(3) * Fill * wardRadius. Every point of the plane is then within
// Fill * wardRadius of a lattice point (the hexagon's circumradius), so keeping every lattice point within
// zoneRadius + Fill * wardRadius of the centre covers the whole zone. 1 - Fill of the radius is the slack a ward may be
// moved by (Nudge) when its exact spot is blocked, without opening a gap.
using System;
using System.Collections.Generic;

namespace HomeCheat.Features.Dome
{
    internal struct DomePoint
    {
        public float X, Z;
        public DomePoint(float x, float z) { X = x; Z = z; }
    }

    internal static class DomeLayout
    {
        public const float Fill = 0.95f;   // Maksym 2026-10-25: overlaps as small as possible; 5 % of the radius left for nudges
        public const float Margin = 1.1f;   // the zone covered is the marked circle plus 10 % (Maksym 2026-10-25)

        // the zone from two points on opposite edges: centre = midpoint, radius = half the distance (R1); callers add Margin
        public static void Zone(float ax, float az, float bx, float bz, out float cx, out float cz, out float radius)
        {
            cx = (ax + bx) * 0.5f;
            cz = (az + bz) * 0.5f;
            float dx = bx - ax, dz = bz - az;
            radius = (float)Math.Sqrt(dx * dx + dz * dz) * 0.5f;
        }

        // how far a ward may move from its lattice point and still close the gap it covers
        public static float Nudge(float wardRadius) { return (1f - Fill) * wardRadius; }

        // ward centres covering the disc (cx, cz, zoneRadius); one ward at the centre while the zone fits inside one ward.
        // One ward: the centre. 3 to 6 wards: a ring (no ward at the centre). More: the centre first, then the lattice in rings outwards.
        public static List<DomePoint> Wards(float cx, float cz, float zoneRadius, float wardRadius)
        {
            List<DomePoint> pts = new List<DomePoint>();
            if (wardRadius <= 0f) return pts;
            if (zoneRadius <= wardRadius) { pts.Add(new DomePoint(cx, cz)); return pts; }

            // a zone a little wider than one ward: the fewest wards that cover a disc (Maksym 2026-10-25: overlaps minimal), n = 3..6
            // on a ring at distance d = min(R cos(pi/n), r), no ward at the centre. The disc is covered when the edge point midway
            // between two ring wards is within reach: R^2 + d^2 - 2 R d cos(pi/n) <= r^2 (n = 3: d = R/2, up to 2r/sqrt(3);
            // n = 4: up to r sqrt(2); 5: about 1.62 r; 6: about 1.73 r). Checked at Fill * r, so each ward may still move by
            // Nudge(r) without a gap, as in the lattice (checked offline with random nudges).
            float rf = Fill * wardRadius;
            for (int n = 3; n <= 6; n++)
            {
                double c = Math.Cos(Math.PI / n), dr = Math.Min(zoneRadius * c, rf);
                if (zoneRadius * zoneRadius + dr * dr - 2.0 * zoneRadius * dr * c > rf * rf + 1e-6) continue;
                for (int i = 0; i < n; i++)
                {
                    double ang = i * 2.0 * Math.PI / n;
                    pts.Add(new DomePoint(cx + (float)(dr * Math.Cos(ang)), cz + (float)(dr * Math.Sin(ang))));
                }
                return pts;
            }
            pts.Add(new DomePoint(cx, cz));

            float cell = Fill * wardRadius;                     // hexagon circumradius
            float d = (float)Math.Sqrt(3.0) * cell;             // lattice spacing
            float keep = zoneRadius + cell;
            float rowH = d * (float)Math.Sqrt(3.0) * 0.5f;       // row height of a triangular lattice
            int rows = (int)Math.Ceiling(keep / rowH) + 1;
            int cols = (int)Math.Ceiling(keep / d) + 1;
            List<KeyValuePair<float, DomePoint>> ring = new List<KeyValuePair<float, DomePoint>>();
            for (int r = -rows; r <= rows; r++)
            {
                float oz = r * rowH;
                float shift = (r & 1) == 0 ? 0f : d * 0.5f;
                for (int c = -cols; c <= cols; c++)
                {
                    float ox = c * d + shift;
                    if (r == 0 && c == 0) continue;
                    float dist = (float)Math.Sqrt(ox * ox + oz * oz);
                    if (dist > keep) continue;
                    // a lattice point whose hexagon lies wholly outside the zone adds nothing
                    if (dist - cell > zoneRadius) continue;
                    ring.Add(new KeyValuePair<float, DomePoint>(dist, new DomePoint(cx + ox, cz + oz)));
                }
            }
            ring.Sort(delegate (KeyValuePair<float, DomePoint> x, KeyValuePair<float, DomePoint> y) { return x.Key.CompareTo(y.Key); });
            foreach (KeyValuePair<float, DomePoint> kv in ring) pts.Add(kv.Value);
            return pts;
        }

        // true when the lattice point's own cell (its hexagon, circumradius Fill * wardRadius) already lies inside the existing
        // wards: its centre and 12 points on the cell's circumcircle are each within wardRadius of one of them. A new ward there
        // would only overlap (Maksym 2026-10-25: overlaps minimal), so it is left out.
        public static bool Covered(DomePoint p, float wardRadius, List<DomePoint> existing)
        {
            if (existing.Count == 0) return false;
            float cell = Fill * wardRadius, r2 = wardRadius * wardRadius;
            for (int i = -1; i < 12; i++)
            {
                float x = p.X, z = p.Z;
                if (i >= 0) { double a = i * Math.PI / 6.0; x += cell * (float)Math.Cos(a); z += cell * (float)Math.Sin(a); }
                bool hit = false;
                foreach (DomePoint w in existing)
                {
                    float dx = x - w.X, dz = z - w.Z;
                    if (dx * dx + dz * dz <= r2) { hit = true; break; }
                }
                if (!hit) return false;
            }
            return true;
        }

        // true when every point of the zone is within wardRadius of some ward (checked on a sample grid: tests and preview)
        public static bool Covers(List<DomePoint> wards, float cx, float cz, float zoneRadius, float wardRadius, float step, out float worstX, out float worstZ)
        {
            worstX = cx; worstZ = cz;
            float r2 = wardRadius * wardRadius;
            for (float x = -zoneRadius; x <= zoneRadius; x += step)
                for (float z = -zoneRadius; z <= zoneRadius; z += step)
                {
                    if (x * x + z * z > zoneRadius * zoneRadius) continue;
                    bool hit = false;
                    foreach (DomePoint w in wards)
                    {
                        float dx = cx + x - w.X, dz = cz + z - w.Z;
                        if (dx * dx + dz * dz <= r2) { hit = true; break; }
                    }
                    if (!hit) { worstX = cx + x; worstZ = cz + z; return false; }
                }
            return true;
        }
    }
}
