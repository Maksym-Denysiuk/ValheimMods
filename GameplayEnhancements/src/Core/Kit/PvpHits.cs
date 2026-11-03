// The PvP rule of every attack command (Maksym 2026-10-25): with pvp=1, other players are hit while the caster's PvP is on,
// whatever their own PvP setting, and spared while it is off; the caster is never hit by his own Aoe or shot.
// What the game does without help (Valheim 1.0.20, read with il-reader):
//   Aoe.ShouldHit       an owned Aoe skips targets with the owner's m_name (m_hitSame) and non-enemies (m_hitFriendly);
//   Projectile.IsValidTarget  a shot of a player with PvP on may target players;
//   Character.RPC_Damage      a player-on-player hit is dropped while the victim's PvP is off, unless HitData.m_ignorePVP;
//   Projectile.Setup / OnHit  copy only damage, blockable, dodgeable, push, backstab and status effect of the HitData, so
//                             m_ignorePVP is lost: MarkProjectile + PvpProjectilePatch put it back on the hit.
// Re-read 2026-11-03 after the 2026-10-28 game update: the three methods above are unchanged.
// Tamed creatures (card pvp-tamed, Maksym 2026-11-03: "Not mine = hit"): with pvp=1 and the caster's PvP on, tamed creatures
// that are not the caster's are hit and the caster's own are spared; with his PvP off every tamed creature is spared.
//   BaseAI.IsEnemy      a tamed creature is never a player's enemy, so an Aoe with m_hitFriendly off spares every tamed one
//                       and an armed Aoe (m_hitFriendly on) hits every tamed one, the caster's own too;
//   Projectile.IsValidTarget  a shot of a player with PvP on may target any character, tamed ones included;
//   Character.RPC_Damage      has no tamed check of its own.
//   So PvpTamedPatch drops the hit of a marked Aoe or shot on a tamed creature when HitsTamed says no.
//   "Own" (IsOwnTamed): it follows the caster (ZDO "follow" = his player name, set by Tameable.RPC_Command, or the
//   owner's MonsterAI.m_follow), or the caster renamed it (ZDO "TamedNameAuthor" = his platform user id, Tameable.SetText).
using System.Collections.Generic;
using UnityEngine;

namespace HomeCheat.Kit
{
    public static class PvpHits
    {
        public const string Usage = "[pvp=0 (1 = follow your PvP switch: your PvP on = other players are hit whatever their own PvP setting, off = they are spared; never you)]";

        static readonly HashSet<int> marked = new HashSet<int>();
        static readonly HashSet<int> markedAoe = new HashSet<int>();
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
            if (markedAoe.Count > 2000) markedAoe.Clear();   // ids of long gone Aoes
            markedAoe.Add(ao.GetInstanceID());   // PvpTamedPatch: the tamed rule (only spares, never adds a hit)
        }

        // the tamed rule for a hit of a marked attack: false = spare c. Not tamed: true (the game decides as before).
        // Tamed: hit only while the local player's PvP is on and it is not his own.
        public static bool HitsTamed(Character c)
        {
            if (c == null || !c.IsTamed()) return true;
            Player me = Player.m_localPlayer;
            if (me == null || !me.IsPVPEnabled()) return false;
            return !IsOwnTamed(c, me);
        }

        // a tamed creature that follows 'me' or that 'me' renamed
        public static bool IsOwnTamed(Character c, Player me)
        {
            if (c == null || me == null || !c.IsTamed()) return false;
            MonsterAI ai = c.GetBaseAI() as MonsterAI;
            if (ai != null && ai.GetFollowTarget() == me.gameObject) return true;
            ZNetView nv = c.GetComponent<ZNetView>();
            ZDO z = nv != null && nv.IsValid() ? nv.GetZDO() : null;
            if (z == null) return false;
            string follow = z.GetString(ZDOVars.s_follow, "");
            if (follow.Length > 0 && follow == me.GetPlayerName()) return true;
            string author = z.GetString(ZDOVars.s_tamedNameAuthor, "");
            if (author.Length == 0) return false;
            if (author == "host") return ZNet.instance != null && ZNet.instance.IsServer();   // renamed by the host before sign-in
            return author == MyUserId();
        }

        static string myUserId;

        // Splatform.PlatformManager.DistributionPlatform.LocalUser.PlatformUserID, as Tameable.SetText sends it; read by
        // reflection because the core does not reference Splatform.dll
        static string MyUserId()
        {
            if (myUserId != null) return myUserId;
            try
            {
                foreach (System.Reflection.Assembly asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    System.Type pm = asm.GetType("Splatform.PlatformManager");
                    if (pm == null) continue;
                    object platform = pm.GetProperty("DistributionPlatform").GetValue(null, null);
                    object user = platform.GetType().GetProperty("LocalUser").GetValue(platform, null);
                    object id = user.GetType().GetProperty("PlatformUserID").GetValue(user, null);
                    if (id != null) myUserId = id.ToString();
                    break;
                }
            }
            catch (System.Exception e) { HomeApi.LogError("pvp tamed rule: no platform user id: " + e.Message); }
            return myUserId ?? "";
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
        internal static bool IsMarked(Aoe ao) { return ao != null && markedAoe.Contains(ao.GetInstanceID()); }
        internal static void EnterHit() { inMarkedHit++; }
        internal static void LeaveHit() { if (inMarkedHit > 0) inMarkedHit--; }
        internal static bool InMarkedHit { get { return inMarkedHit > 0; } }
        internal static void Forget() { marked.Clear(); markedAoe.Clear(); inMarkedHit = 0; }
    }
}
