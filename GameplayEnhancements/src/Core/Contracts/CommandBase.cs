// Base of every command. Template method: Run builds Args, tries the sub-actions in registration order, else Execute.
// A CommandException becomes "ERROR: <message>"; any other exception is logged with its stack trace and answered the same way.
// What a command owns (Harmony patches, GameObjects, timers) is released in Unload, after OnUnload, even when OnUnload throws.
// Leaving the world (the core's WorldExitPatch) cancels its timers and destroys what it owns with OwnInWorld, after OnWorldExit;
// patches and Own(...) objects stay until the command is unloaded (the spell bar lives across worlds).
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HomeCheat
{
    public abstract class CommandBase : IHomeCommand, ICommandLifecycle
    {
        sealed class SubActionEntry
        {
            public string Key, Help;
            public bool IsFlag;
            public Func<Args, string> Run;
        }

        readonly List<SubActionEntry> subActions = new List<SubActionEntry>();
        readonly List<GameObject> owned = new List<GameObject>();
        readonly List<GameObject> ownedInWorld = new List<GameObject>();
        PatchSet patches;

        public abstract string Name { get; }

        // the main parameters; sub-action help is appended from the registrations
        protected abstract string Usage { get; }

        public string Help
        {
            get
            {
                StringBuilder sb = new StringBuilder(Usage);
                foreach (SubActionEntry s in subActions) sb.Append(" | ").Append(s.Help);
                return sb.ToString();
            }
        }

        protected abstract string Execute(Args a);

        // runs when key=<non-empty value> is present; help is the full text, e.g. "find=<text> (prefabs with an Aoe)"
        protected void SubAction(string key, string help, Func<Args, string> run)
        {
            subActions.Add(new SubActionEntry { Key = key, Help = help, IsFlag = false, Run = run });
        }

        // runs when key=1|true|yes is present
        protected void SubFlag(string key, string help, Func<Args, string> run)
        {
            subActions.Add(new SubActionEntry { Key = key, Help = help, IsFlag = true, Run = run });
        }

        public string Run(Dictionary<string, string> raw)
        {
            Args a = new Args(raw);
            try
            {
                foreach (SubActionEntry s in subActions)
                {
                    bool hit = s.IsFlag ? a.Flag(s.Key, false) : a.Str(s.Key, "").Length > 0;
                    if (hit) return s.Run(a);
                }
                return Execute(a);
            }
            catch (CommandException e) { return "ERROR: " + e.Message; }
            catch (Exception e)
            {
                HomeApi.LogError(Name + ": " + e);
                return "ERROR: " + e.Message;
            }
        }

        // ---- lifecycle ------------------------------------------------------------------------------

        protected virtual void OnLoad() { }
        protected virtual void OnUnload() { }

        // the player left the world (logout, disconnect, quit); runs before the timers and world objects are cleared
        protected virtual void OnWorldExit() { }

        void ICommandLifecycle.Load() { OnLoad(); }

        void ICommandLifecycle.Unload()
        {
            try { OnUnload(); }
            catch (Exception e) { HomeApi.LogError(Name + ": OnUnload: " + e); }
            finally
            {
                HomeApi.Scheduler.CancelOwner(this);
                if (patches != null) patches.RemoveAll();
                foreach (GameObject go in owned) if (go != null) UnityEngine.Object.Destroy(go);
                owned.Clear();
                foreach (GameObject go in ownedInWorld) if (go != null) Kit.Fx.Destroy(go);
                ownedInWorld.Clear();
            }
        }

        // called by the registry when the world's net scene is destroyed; returns timers cancelled + objects removed.
        // The net scene is going away, so world objects are destroyed locally: a networked object's ZDO stays in the save
        // (mark it in its ZDO so a clear=1 finds it in the next session).
        internal int WorldExit()
        {
            int n = 0;
            try { OnWorldExit(); }
            catch (Exception e) { HomeApi.LogError(Name + ": OnWorldExit: " + e); }
            finally
            {
                n += HomeApi.Scheduler.CancelOwner(this);
                foreach (GameObject go in ownedInWorld) if (go != null) { UnityEngine.Object.Destroy(go); n++; }
                ownedInWorld.Clear();
            }
            return n;
        }

        // ---- owned resources ------------------------------------------------------------------------

        // Harmony patches of this command, id com.homeos.homecheat.<name>; removed on unload
        protected PatchSet Patches
        {
            get
            {
                if (patches == null) patches = new PatchSet("com.homeos.homecheat." + Name.ToLowerInvariant());
                return patches;
            }
        }

        // destroyed on unload
        protected GameObject Own(GameObject go)
        {
            if (go != null) owned.Add(go);
            return go;
        }

        // destroyed when the player leaves the world, or on unload, whichever comes first
        protected GameObject OwnInWorld(GameObject go)
        {
            if (go != null) ownedInWorld.Add(go);
            return go;
        }

        // runs on the main thread after 'seconds' (0 = next frame); cancelled when this command is unloaded or the world is left
        public void Later(float seconds, Action act)
        {
            HomeApi.Scheduler.Later(this, seconds, act);
        }
    }
}
