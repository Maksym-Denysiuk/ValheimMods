// The damage a trap's blast carries: an item's damage at a quality, by the game's own rule (ItemData.GetDamage: base +
// per-level x (quality - 1)), the same rule earthquake uses for its hammers. Small copy, promotion candidate for the Kit.
using System;
using UnityEngine;

namespace HomeCheat.Features.Traps
{
    internal sealed class TrapWeapon
    {
        public HitData.DamageTypes Damage;
        public float Push;
        public string Label;   // "BowAshlands quality 4/4"

        // null when no item of that name has damage; quality "max" or a number (clamped to 1..max)
        public static TrapWeapon Pick(string item, string quality)
        {
            if (ObjectDB.instance == null) return null;
            foreach (GameObject go in ObjectDB.instance.m_items)
            {
                if (go == null || !go.name.Equals(item, StringComparison.OrdinalIgnoreCase)) continue;
                ItemDrop d = go.GetComponent<ItemDrop>();
                if (d == null || d.m_itemData == null || d.m_itemData.m_shared == null) continue;
                ItemDrop.ItemData.SharedData sh = d.m_itemData.m_shared;
                if (sh.m_damages.GetTotalDamage() <= 0f) continue;
                int maxQ = Mathf.Max(1, sh.m_maxQuality), q;
                if (!int.TryParse(quality, out q)) q = maxQ;
                q = Mathf.Clamp(q, 1, maxQ);
                TrapWeapon w = new TrapWeapon();
                w.Damage = sh.m_damages;
                w.Damage.Add(sh.m_damagesPerLevel, q - 1);
                w.Push = sh.m_attackForce;
                w.Label = go.name + " quality " + q + "/" + maxQ;
                return w;
            }
            return null;
        }
    }
}
