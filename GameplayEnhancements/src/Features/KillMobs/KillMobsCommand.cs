// op=killmobs: kills hostile and wild creatures around the local player with ordinary damage (no console).
// Skips players, tamed animals and bosses (boss=1 includes bosses). Test helper.
using System.Collections.Generic;
using System.Text;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.KillMobs
{
    public sealed class KillMobsCommand : CommandBase
    {
        public override string Name { get { return "killmobs"; } }
        protected override string Usage { get { return "killmobs [radius=30] [boss=1] [tamed=1] - kills creatures around the local player with normal damage; skips players, tamed animals and bosses unless asked"; } }

        protected override string Execute(Args a)
        {
            Player p = Players.Local;
            float radius = a.Float("radius", 30f);
            bool bosses = a.Flag("boss", false);
            bool tamed = a.Flag("tamed", false);
            Dictionary<string, int> counts = new Dictionary<string, int>();
            int n = 0;
            foreach (Character c in new List<Character>(Character.GetAllCharacters()))
            {
                if (c == null || c.IsPlayer() || c.IsDead()) continue;
                if (!tamed && c.IsTamed()) continue;
                if (!bosses && c.IsBoss()) continue;
                if (Vector3.Distance(c.transform.position, p.transform.position) > radius) continue;
                HitData hit = new HitData();
                hit.m_damage.m_damage = 100000f;
                hit.m_point = c.GetCenterPoint();
                hit.m_dir = (c.transform.position - p.transform.position).normalized;
                hit.m_attacker = p.GetZDOID();
                c.Damage(hit);
                string nm = Prefabs.BaseName(c.gameObject);
                int k; counts.TryGetValue(nm, out k); counts[nm] = k + 1;
                n++;
            }
            StringBuilder sb = new StringBuilder("hit " + n + " creatures within " + radius.ToString("F0") + " m");
            foreach (KeyValuePair<string, int> kv in counts) sb.Append("\n  ").Append(kv.Key).Append(" x").Append(kv.Value);
            return sb.ToString();
        }
    }
}
