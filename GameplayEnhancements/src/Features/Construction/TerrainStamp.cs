// Copy of World's TerrainStamp (29 lines): features never reference each other's types. A promotion candidate for the Kit
// now that two features use it (card: docs/Backlog/construct.md). One stamp of a hoe piece with its radii set for the spawn.
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.Construction
{
    internal static class TerrainStamp
    {
        // one stamp. level/smooth radius = rLevel (0 = leave as in the prefab), paint radius = rPaint (0 = leave)
        public static void At(GameObject prefab, Vector3 pos, float rLevel, float rPaint)
        {
            TerrainModifier tm = prefab.GetComponent<TerrainModifier>();
            if (tm == null) throw new CommandException(prefab.name + " has no TerrainModifier");
            float oLR = Refl.GetFloat(tm, "m_levelRadius", 0f), oSR = Refl.GetFloat(tm, "m_smoothRadius", 0f), oPR = Refl.GetFloat(tm, "m_paintRadius", 0f);
            try
            {
                if (rLevel > 0f) { Refl.SetFloat(tm, "m_levelRadius", rLevel); Refl.SetFloat(tm, "m_smoothRadius", Mathf.Max(oSR, rLevel + 1f)); }
                if (rPaint > 0f) Refl.SetFloat(tm, "m_paintRadius", rPaint);
                UnityEngine.Object.Instantiate(prefab, pos, Quaternion.identity);
            }
            finally
            {
                Refl.SetFloat(tm, "m_levelRadius", oLR); Refl.SetFloat(tm, "m_smoothRadius", oSR); Refl.SetFloat(tm, "m_paintRadius", oPR);
            }
        }
    }
}
