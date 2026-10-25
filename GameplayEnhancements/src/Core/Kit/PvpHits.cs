// The PvP rule of every attack command (Maksym 2026-10-25): with pvp=1, other players are hit while the caster's PvP is on,
// whatever their own PvP setting, and spared while it is off; the caster is never hit by his own Aoe or shot.
// What the game does without help (Valheim 1.0.20, read with il-reader):
//   Aoe.ShouldHit       an owned Aoe skips targets with the owner's m_name (m_hitSame) and non-enemies (m_hitFriendly);
//   Projectile.IsValidTarget  a shot of a player with PvP on may target players;
//   Character.RPC_Damage      a player-on-player hit is dropped while the victim's PvP is off, unless HitData.m_ignorePVP;
//   Projectile.Setup / OnHit  copy only damage, blockable, dodgeable, push, backstab and status effect of the HitData, so
//                             m_ignorePVP is lost: MarkProjectile + PvpProjectilePatch put it back on the hit.
using System.Collections.Generic;

namespace HomeCheat.Kit
{
    public static class PvpHits
    {
        public const string Usage = "[pvp=0 (1 = follow your PvP switch: your PvP on = other players are hit whatever their own PvP setting, off = they are spared; never you)]";

        static readonly HashSet<int> marked = new HashSet<int>();
        static int inMarkedHit;   // > 0 while OnHit of a marked projectile runs (PvpProjectilePatch)

        // pvp=1 asked and the local player's PvP is on
        public static bool HitsPlayers(bool asked)
        {
            Player me = Players.Local;
            return asked && me != null && me.IsPVPEnabled();
        }

        // the part of a first answer line: "; other players hit (your PvP on)" or "... spared (your PvP off)", "" when not asked
        public static string Note(bool asked)
        {
            if (!asked) return "";
            return "; other players " + (HitsPlayers(true) ? "hit (your PvP on)" : "spared (your PvP off)");
        }

        // an Aoe the caster owns: hits other players (same kind, not enemies, PvP off) exactly when hitPlayers
        public static void ArmAoe(Aoe ao, bool hitPlayers)
        {
            if (ao == null) return;
            ao.m_hitOwner = false;
            ao.m_hitFriendly = hitPlayers;
            ao.m_hitSame = hitPlayers;
            ao.m_ignorePVP = hitPlayers;
        }

        public static void ArmHit(HitData h, bool hitPlayers)
        {
            if (h != null) h.m_ignorePVP = hitPlayers;
        }

        // a projectile fired with hitPlayers: its hits get m_ignorePVP back (see the header)
        public static void MarkProjectile(Projectile pr)
        {
            if (pr == null) return;
            if (marked.Count > 2000) marked.Clear();   // ids of long gone shots
            marked.Add(pr.GetInstanceID());
        }

        internal static bool IsMarked(Projectile pr) { return pr != null && marked.Contains(pr.GetInstanceID()); }
        internal static void EnterHit() { inMarkedHit++; }
        internal static void LeaveHit() { if (inMarkedHit > 0) inMarkedHit--; }
        internal static bool InMarkedHit { get { return inMarkedHit > 0; } }
        internal static void Forget() { marked.Clear(); inMarkedHit = 0; }
    }
}
