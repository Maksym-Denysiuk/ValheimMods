// op=bowcharge: holding a vanilla bow past its full draw adds spell arrows to the shot (Maksym 2026-11-03: "bound to bow charge,
// not to bindings"). Once the game's full draw is reached a timer starts; every whole second after that adds 1 extra arrow, up to
// 25 (a 5x5 matrix). On release the normal bow shot fires as the game fires it, plus the extra arrows as spellshot type=arrow
// (spell damage, no ammo used): a centred grid (ceil(sqrt n) per row, rows filled top to bottom, each row centred), from the
// crosshair centre along the crosshair direction, the spellshot way (eye point + 1 m along the camera's forward, 0.6 m apart).
// On by default while loaded; on=0|1; follows your PvP switch by default (Maksym 2026-11-03: the PvP rule of AttackCommand, read at
// each release: your PvP on = other players and their tamed creatures hit, yours spared; off = all spared; pvp=0 opts out). Crossbows have no hold
// (their m_bowDraw is false): not affected. The hook is BowChargePatch on Player.UpdateAttackBowDraw (probe 2026-11-03: the
// draw time grows there while the attack is held, and the release calls StartAttack and zeroes it). Page: docs/Spells/bowcharge.md.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.Spellshot
{
    public sealed class BowChargeCommand : AttackCommand
    {
        internal const int MaxArrows = 25;   // a 5x5 matrix
        const float Dist = 1f;               // spellshot's defaults: metres ahead of the eye, spacing, arrow speed
        const float Spacing = 0.6f;

        internal static readonly FieldInfo fDrawTime = typeof(Humanoid).GetField("m_attackDrawTime", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        internal static readonly FieldInfo fHold = typeof(Character).GetField("m_attackHold", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        internal static readonly FieldInfo fCurrentAttack = typeof(Humanoid).GetField("m_currentAttack", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        static readonly MethodInfo mBowDraw = typeof(Player).GetMethod("UpdateAttackBowDraw", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        // for BowChargePatch: set while this build is loaded
        internal static BowChargeCommand Current;

        bool on = true;          // on by default while loaded
        bool pvpAsked = true;    // follows your PvP switch from the start; pvp=0 opts out
        internal float FullAt = -1f;   // Time.time when the held draw reached full, -1 = not held at full

        public BowChargeCommand()
        {
            SubFlag("status", "status=1 (read-only: on or off, the pvp setting, how long the current draw has been held past full)",
                delegate (Args a) { return Status(); });
            SubFlag("probe", "probe=1 (read-only: the bow draw method and fields this game version has, your weapon and its draw now)",
                delegate (Args a) { return Probe(); });
        }

        public override string Name { get { return "bowcharge"; } }
        protected override string Usage
        {
            get
            {
                return "bowcharge [on=1|0 (on by default while loaded)] [pvp=1 (default: follow your PvP switch: your PvP on = other players and their tamed creatures are hit, yours spared; off = all spared; never you; 0 = unmarked arrows, as a vanilla bow)]"
                    + " - hold a vanilla bow past its full draw: every whole second adds 1 spell arrow to the shot (cap " + MaxArrows
                    + ", a 5x5 grid), fired on release with the normal arrow, from the crosshair centre along the crosshair (spellshot type=arrow:"
                    + " spell damage, no ammo); the setting lasts until the game restarts; crossbows are not affected";
            }
        }

        protected override void OnLoad()
        {
            Current = this;
            Patches.Apply(typeof(BowChargePatch));
        }

        protected override void OnUnload() { if (Current == this) Current = null; }
        protected override void OnWorldExit() { FullAt = -1f; }

        protected override string Execute(Args a)
        {
            Host.RequireWorld();
            bool changed = false;
            if (a.Has("on"))
            {
                string raw = a.Str("on", "");
                if (raw != "0" && raw != "1") throw new CommandException("on=" + raw + ": write on=1 or on=0");
                on = raw == "1";
                changed = true;
            }
            if (a.Has("pvp")) { pvpAsked = PvpAsked(a); changed = true; }
            if (!changed) return Status();
            if (!on) FullAt = -1f;
            HomeApi.Log("bowcharge: " + (on ? "on" : "off") + ", pvp=" + (pvpAsked ? "1" : "0"));
            return Status();
        }

        string Status()
        {
            string held = "";
            if (FullAt >= 0f) held = ", drawn at full for " + Fmt.F(Time.time - FullAt, 1) + " s = " + Extra(Time.time - FullAt) + " extra";
            return "bow charge " + (on ? "on" : "off") + " (1 spell arrow per whole second past full draw, cap " + MaxArrows + ")" + held + PvpHits.Note(pvpAsked);
        }

        static int Extra(float heldPastFull) { return Mathf.Max(0, Mathf.FloorToInt(heldPastFull)); }

        string Probe()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("release method ").Append(mBowDraw != null ? "Player.UpdateAttackBowDraw found" : "Player.UpdateAttackBowDraw NOT found (game update?)");
            sb.Append("; fields m_attackDrawTime ").Append(fDrawTime != null ? "ok" : "NOT found").Append(", m_attackHold ").Append(fHold != null ? "ok" : "NOT found").Append(", m_currentAttack ").Append(fCurrentAttack != null ? "ok" : "NOT found");
            sb.Append("; patched ").Append(Patches.IsActive);
            Player p = Player.m_localPlayer;
            if (p != null)
            {
                ItemDrop.ItemData w = p.GetCurrentWeapon();
                if (w == null || w.m_shared == null) sb.Append("; weapon none");
                else
                {
                    Attack at = w.m_shared.m_attack;
                    sb.Append("; weapon ").Append(w.m_shared.m_name).Append(at != null && at.m_bowDraw ? " (draws: drawDurationMin " + Fmt.F(at.m_drawDurationMin, 2) + " s)" : " (no draw: not affected)");
                    sb.Append("; draw now ").Append(Fmt.F(p.GetAttackDrawPercentage() * 100f, 0)).Append(" %");
                    if (fDrawTime != null) sb.Append(", draw time ").Append(Fmt.F(Convert.ToSingle(fDrawTime.GetValue(p)), 2)).Append(" s");
                }
            }
            return sb.ToString();
        }

        // ---- the release (BowChargePatch) --------------------------------------------------------

        // called after the game's release started the normal shot; heldPastFull = seconds the draw stayed at full
        internal void Release(Player p, float heldPastFull)
        {
            if (!on) return;
            int asked = Extra(heldPastFull);
            if (asked <= 0) return;
            string capNote;
            int n = Caps.Clamp("arrows", asked, MaxArrows, out capNote);
            Camera cam = Camera.main;
            if (cam == null) { HomeApi.Log("bowcharge: no camera: no extra arrows"); return; }

            GameObject projPrefab, ammoPrefab;
            string used = SpellShotCommand.Resolve(new List<string>(SpellShotCommand.Candidates("arrow")), out projPrefab, out ammoPrefab);
            MethodInfo setup = SpellShotCommand.FindSetup();
            if (used == null || projPrefab == null || projPrefab.GetComponent<Projectile>() == null || setup == null)
            {
                HomeApi.LogError("bowcharge: no arrow projectile or Projectile.Setup in this game version: no extra arrows");
                return;
            }
            bool hitPlayers = PvpHits.HitsPlayers(pvpAsked);

            Vector3 origin = p.GetEyePoint();
            Vector3 D = cam.transform.forward.normalized;
            Vector3 Rt = Vector3.Cross(Vector3.up, D);
            if (Rt.sqrMagnitude < 0.0001f) Rt = cam.transform.right;
            Rt.Normalize();
            Vector3 Up = Vector3.Cross(D, Rt).normalized;
            Vector3 C = origin + D * Dist;

            float[] off = GridOffsets(n);
            int ok = 0;
            string firstErr = null;
            for (int k = 0; k < n; k++)
            {
                try
                {
                    Vector3 pos = C + Rt * (off[2 * k] * Spacing) + Up * (off[2 * k + 1] * Spacing);
                    SpellShotCommand.Fire(p, setup, projPrefab, ammoPrefab, pos, D, SpellShotCommand.DefaultSpeed("arrow"), 1f, 0, hitPlayers);
                    ok++;
                }
                catch (Exception e)
                {
                    if (firstErr == null) firstErr = e.GetType().Name + ": " + (e.InnerException != null ? e.InnerException.Message : e.Message);
                }
            }
            HomeApi.Log("bowcharge: held " + Fmt.F(heldPastFull, 1) + " s past full draw = " + ok + " extra arrows (prefab " + projPrefab.name
                + (capNote.Length > 0 ? ", " + capNote : "") + ") from " + Fmt.Vec(C) + " along " + Fmt.Vec(D) + PvpHits.Note(pvpAsked)
                + (firstErr != null ? " ; first error: " + firstErr : ""));
        }

        // n arrows as a centred grid in spacing units: ceil(sqrt n) per row, rows filled top to bottom, each row centred on its own
        // count (a short last row stays centred). Returns x0,y0,x1,y1,... (x to the right, y up). Unity-free.
        internal static float[] GridOffsets(int n)
        {
            if (n <= 0) return new float[0];
            int perRow = (int)Math.Ceiling(Math.Sqrt(n));
            int rows = (n + perRow - 1) / perRow;
            float[] r = new float[2 * n];
            for (int k = 0; k < n; k++)
            {
                int row = k / perRow;
                int col = k % perRow;
                int inRow = Math.Min(perRow, n - row * perRow);
                r[2 * k] = col - (inRow - 1) / 2f;
                r[2 * k + 1] = (rows - 1) / 2f - row;
            }
            return r;
        }
    }
}
