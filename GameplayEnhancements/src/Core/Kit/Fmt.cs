// Number formatting for command answers: InvariantCulture, so a decimal point never turns into a comma.
using System.Globalization;
using UnityEngine;

namespace HomeCheat.Kit
{
    public static class Fmt
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static string Vec(Vector3 v) { return v.x.ToString("F1", Inv) + "," + v.y.ToString("F1", Inv) + "," + v.z.ToString("F1", Inv); }

        public static string F(float f, int digits) { return f.ToString("F" + digits, Inv); }
    }
}
