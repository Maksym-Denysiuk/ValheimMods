// Main-thread timers. Every timer has an owner (a command); unloading the owner cancels its pending timers, so a delegate
// from a replaced build never fires. Time base: Time.time (stops while the game is paused, like the old Later).
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HomeCheat.Services
{
    public sealed class Scheduler
    {
        sealed class Entry
        {
            public float Due;
            public Action Act;
            public object Owner;
            public bool Cancelled;
        }

        readonly List<Entry> entries = new List<Entry>();

        public void Later(object owner, float seconds, Action act)
        {
            if (act == null) return;
            entries.Add(new Entry { Due = Time.time + Mathf.Max(0f, seconds), Act = act, Owner = owner });
        }

        public int CancelOwner(object owner)
        {
            int n = 0;
            foreach (Entry e in entries)
                if (!e.Cancelled && ReferenceEquals(e.Owner, owner)) { e.Cancelled = true; n++; }
            entries.RemoveAll(delegate (Entry e) { return e.Cancelled; });
            return n;
        }

        public int Pending { get { return entries.Count; } }

        // due timers are taken out first and then run, so a callback may add or cancel timers safely
        internal void Tick()
        {
            if (entries.Count == 0) return;
            float now = Time.time;
            List<Entry> due = null;
            for (int i = entries.Count - 1; i >= 0; i--)
                if (now >= entries[i].Due)
                {
                    if (due == null) due = new List<Entry>();
                    due.Add(entries[i]);
                    entries.RemoveAt(i);
                }
            if (due == null) return;
            for (int i = due.Count - 1; i >= 0; i--)
            {
                Entry e = due[i];
                if (e.Cancelled) continue;
                try { e.Act(); }
                catch (Exception ex) { HomeApi.LogError("timer: " + ex); }
            }
        }
    }
}
