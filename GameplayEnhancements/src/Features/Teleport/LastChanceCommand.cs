// op=lastchance: a hit that would bring your health to 0 is cancelled (no damage, no healing: health stays as it was) and you
// blink as spell 9.3 does (10 m, width 2, push 150, shove and tar on the line, the pvp=1 rule) in a random flat direction 0-360
// degrees. Then a cooldown (60 s, cooldown= 10-3600) of world time, during which a killing hit kills as normal. A refused blink
// (the world edge) lets the hit through and starts no cooldown. On by default while HomeCheat is loaded (Maksym 2026-11-03).
// The guard is LastChancePatch, a prefix on Character.ApplyDamage (the damage path in this game version: RPC_Damage applies armour
// and resistances, then ApplyDamage scales a player's damage by Game.m_localDamgeTakenRate, drops under 0.1 and sets the health).
// No god mode or cheat field is written. State per world in BepInEx/config/homecheat/teleport.state (this PC only), saved on a
// change and once per save (at most once per cooldown). Card: docs/Spells/teleport.md, Cards.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using BepInEx;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.TeleportCommands
{
    public sealed class LastChanceCommand : CommandBase
    {
        const float DefaultCooldown = 60f;
        const float MinCooldown = 10f;
        const float MaxCooldown = 3600f;
        const float NoDamage = 0.1f;   // ApplyDamage drops a hit below this

        static readonly FieldInfo fTakenRate = typeof(Game).GetField("m_localDamgeTakenRate", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        static readonly MethodInfo mApplyDamage = typeof(Character).GetMethod("ApplyDamage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        // for LastChancePatch: set while this build is loaded
        internal static LastChanceCommand Current;

        public LastChanceCommand()
        {
            SubFlag("status", "status=1 (read-only: on or off, the cooldown and how much of it is left in this world)",
                delegate (Args a) { Host.RequireWorld(); return Status(); });
            SubFlag("probe", "probe=1 (read-only: the damage method the guard patches, the damage-taken rate, whether blink is loaded)",
                delegate (Args a) { return Probe(); });
        }

        public override string Name { get { return "lastchance"; } }
        protected override string Usage
        {
            get
            {
                return "lastchance [on=1|0 (on by default)] [cooldown=60 (seconds, 10-3600)]"
                    + " - a hit that would kill you is cancelled (no damage, no healing) and you blink 10 m in a random direction as spell 9.3 does"
                    + " (shove and tar on the line; your PvP on = other players on it too, others' tamed creatures too, yours spared);"
                    + " then the cooldown, during which a killing hit kills; the world edge refuses the blink and the hit goes through";
            }
        }

        protected override void OnLoad()
        {
            Current = this;
            Patches.Apply(typeof(LastChancePatch));
        }

        protected override void OnUnload() { if (Current == this) Current = null; }

        protected override string Execute(Args a)
        {
            Host.RequireWorld();
            State st = Get();
            bool changed = false;
            string onRaw = a.Has("on") ? a.Str("on", "") : null;
            if (onRaw != null)
            {
                if (onRaw != "0" && onRaw != "1") throw new CommandException("on=" + onRaw + ": write on=1 or on=0");
                st.On = onRaw == "1";
                changed = true;
            }
            if (a.Has("cooldown"))
            {
                float cd = Caps.Check("cooldown", a.Float("cooldown", DefaultCooldown), MaxCooldown);
                if (cd < MinCooldown) throw new CommandException("cooldown must be at least " + Fmt.F(MinCooldown, 0) + " s");
                st.Cooldown = cd;
                changed = true;
            }
            if (!changed) return Status();
            Save();
            HomeApi.Log("lastchance: " + (st.On ? "on" : "off") + ", cooldown " + Fmt.F(st.Cooldown, 0) + " s (" + World + ")");
            return "last chance " + (st.On ? "on" : "off") + ", cooldown " + Fmt.F(st.Cooldown, 0) + " s" + Left(st);
        }

        string Status()
        {
            State st = Get();
            return "last chance " + (st.On ? "on" : "off") + ", cooldown " + Fmt.F(st.Cooldown, 0) + " s" + Left(st)
                + (BlinkCommand.Current == null ? "; blink is not loaded: a killing hit goes through" : "");
        }

        static string Left(State st)
        {
            float left = (float)(st.ReadyAt - Now());
            return left > 0f ? ", " + Fmt.F(left, 0) + " s of it left" : ", ready";
        }

        // ---- the guard (LastChancePatch) ------------------------------------------------------------

        // true = the hit is cancelled: called on the local player's own ApplyDamage, before the game applies it
        internal bool Save(Player p, HitData hit)
        {
            if (p.IsDead() || p.IsTeleporting() || p.InGodMode() || p.InGhostMode() || p.IsDebugFlying()) return false;   // the game itself does not kill there
            float rate = fTakenRate != null ? Convert.ToSingle(fTakenRate.GetValue(null)) : 1f;
            float damage = hit.GetTotalDamage() * rate;
            float health = p.GetHealth();
            if (damage < NoDamage || health - damage > 0f) return false;

            State st = Get();
            string by = Attacker(hit);
            if (!st.On) return false;
            double now = Now();
            if (st.ReadyAt > now)
            {
                HomeApi.Log("lastchance: killing hit of " + Fmt.F(damage, 1) + " (health " + Fmt.F(health, 1) + ") by " + by
                    + " in the cooldown (" + Fmt.F((float)(st.ReadyAt - now), 0) + " s left): it goes through");
                return false;
            }
            BlinkCommand blink = BlinkCommand.Current;
            if (blink == null) { HomeApi.Log("lastchance: blink is not loaded: the killing hit goes through"); return false; }

            float ang = UnityEngine.Random.Range(0f, 360f);
            Vector3 dir = new Vector3(Mathf.Sin(ang * Mathf.Deg2Rad), 0f, Mathf.Cos(ang * Mathf.Deg2Rad));
            string line;
            try
            {
                line = blink.Dash(p, dir, BlinkCommand.SpellDist, BlinkCommand.SpellWidth, BlinkCommand.SpellPush, false, false, PvpHits.HitsPlayers(true), true);
            }
            catch (CommandException e)
            {
                HomeApi.Log("lastchance: " + e.Message + ": the killing hit goes through");
                return false;
            }
            st.ReadyAt = now + st.Cooldown;
            Save();
            HomeApi.Log("lastchance: killing hit of " + Fmt.F(damage, 1) + " (health " + Fmt.F(health, 1) + ") by " + by
                + " cancelled; " + line + " at " + Fmt.F(ang, 0) + " deg" + PvpHits.Note(true) + "; cooldown " + Fmt.F(st.Cooldown, 0) + " s");
            return true;
        }

        static string Attacker(HitData hit)
        {
            Character c = hit.GetAttacker();
            if (c == null) return "no attacker";
            Player pl = c as Player;
            return pl != null ? "player" : Prefabs.BaseName(c.gameObject);
        }

        string Probe()
        {
            StringBuilder sb = new StringBuilder("damage method ");
            if (mApplyDamage == null) sb.Append("Character.ApplyDamage NOT found (game update?)");
            else
            {
                sb.Append("Character.ApplyDamage(");
                ParameterInfo[] ps = mApplyDamage.GetParameters();
                for (int i = 0; i < ps.Length; i++) sb.Append(i > 0 ? ", " : "").Append(ps[i].ParameterType.Name).Append(' ').Append(ps[i].Name);
                sb.Append(")");
            }
            sb.Append("; patched ").Append(Patches.IsActive);
            sb.Append("; Game.m_localDamgeTakenRate ").Append(fTakenRate != null ? Fmt.F(Convert.ToSingle(fTakenRate.GetValue(null)), 2) : "NOT found");
            sb.Append("; blink ").Append(BlinkCommand.Current != null ? "loaded" : "NOT loaded");
            Player p = Player.m_localPlayer;
            if (p != null) sb.Append("; your health ").Append(Fmt.F(p.GetHealth(), 1)).Append(" of ").Append(Fmt.F(p.GetMaxHealth(), 1));
            return sb.ToString();
        }

        // ---- state per world: BepInEx/config/homecheat/teleport.state, one line per world: world <tab> on <tab> cooldown <tab> readyAt

        sealed class State
        {
            public bool On = true;
            public float Cooldown = DefaultCooldown;
            public double ReadyAt;   // world time (ZNet.GetTimeSeconds) when a killing hit is saved again
        }

        static readonly Dictionary<string, State> states = new Dictionary<string, State>();
        static bool loaded;

        static string FilePath { get { return Path.Combine(Path.Combine(Paths.ConfigPath, "homecheat"), "teleport.state"); } }
        static string World { get { return ZNet.instance == null ? "" : ZNet.instance.GetWorldName(); } }
        static double Now() { return ZNet.instance == null ? 0.0 : ZNet.instance.GetTimeSeconds(); }

        static State Get()
        {
            Load();
            State st;
            if (!states.TryGetValue(World, out st)) { st = new State(); states[World] = st; }
            return st;
        }

        static void Load()
        {
            if (loaded) return;
            loaded = true;
            try
            {
                if (!File.Exists(FilePath)) return;
                System.Globalization.CultureInfo inv = System.Globalization.CultureInfo.InvariantCulture;
                foreach (string line in File.ReadAllLines(FilePath))
                {
                    string[] f = line.Split('\t');
                    float cd;
                    double ready;
                    if (f.Length != 4 || !float.TryParse(f[2], System.Globalization.NumberStyles.Float, inv, out cd)
                        || !double.TryParse(f[3], System.Globalization.NumberStyles.Float, inv, out ready)) continue;
                    State st = new State();
                    st.On = f[1] != "0";
                    st.Cooldown = Mathf.Clamp(cd, MinCooldown, MaxCooldown);
                    st.ReadyAt = ready;
                    states[f[0]] = st;
                }
            }
            catch (Exception e) { HomeApi.LogError("lastchance: reading " + FilePath + ": " + e.Message); }
        }

        static void Save()
        {
            try
            {
                System.Globalization.CultureInfo inv = System.Globalization.CultureInfo.InvariantCulture;
                StringBuilder sb = new StringBuilder();
                foreach (KeyValuePair<string, State> kv in states)
                    sb.Append(kv.Key).Append('\t').Append(kv.Value.On ? "1" : "0").Append('\t')
                        .Append(kv.Value.Cooldown.ToString("F0", inv)).Append('\t').Append(kv.Value.ReadyAt.ToString("F1", inv)).Append('\n');
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.WriteAllText(FilePath, sb.ToString(), new UTF8Encoding(false));
            }
            catch (Exception e) { HomeApi.LogError("lastchance: writing " + FilePath + ": " + e.Message); }
        }
    }
}
