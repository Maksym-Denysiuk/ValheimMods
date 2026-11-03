// Allocation maths of Kit.Materials: one requirement of 'need' items split over sources in order (source 0 = your inventory,
// then chests nearest first), each giving what it has; the rest is free. Never all or nothing: a short source gives its part.
// Ledger keeps what a check-only run would have taken, so several pieces checked in a row do not count the same items twice.
// No Unity reference: also compiled into HomeCheat.SelfTest.exe.
using System;
using System.Collections.Generic;

namespace HomeCheat
{
    public static class MaterialSplit
    {
        // taken[i] from source i (available[i] items there, negatives count as 0); free = what no source had
        public static int[] Split(int need, int[] available, out int free)
        {
            int n = available == null ? 0 : available.Length;
            int[] taken = new int[n];
            int left = Math.Max(0, need);
            for (int i = 0; i < n && left > 0; i++)
            {
                int t = Math.Min(left, Math.Max(0, available[i]));
                taken[i] = t;
                left -= t;
            }
            free = left;
            return taken;
        }

        // the same item named twice in one recipe counts once with the sum; order of first appearance kept
        public static List<KeyValuePair<string, int>> Merge(IList<KeyValuePair<string, int>> needs)
        {
            List<KeyValuePair<string, int>> res = new List<KeyValuePair<string, int>>();
            Dictionary<string, int> at = new Dictionary<string, int>();
            if (needs == null) return res;
            foreach (KeyValuePair<string, int> kv in needs)
            {
                if (string.IsNullOrEmpty(kv.Key) || kv.Value <= 0) continue;
                int i;
                if (at.TryGetValue(kv.Key, out i)) res[i] = new KeyValuePair<string, int>(kv.Key, res[i].Value + kv.Value);
                else { at[kv.Key] = res.Count; res.Add(kv); }
            }
            return res;
        }
    }

    // items a check-only run has already counted per (source, item); a real run reads the live counts instead
    public sealed class MaterialLedger
    {
        readonly Dictionary<string, int> used = new Dictionary<string, int>();

        static string Key(int source, string item) { return source + "|" + item; }

        public int Used(int source, string item)
        {
            int v;
            return used.TryGetValue(Key(source, item), out v) ? v : 0;
        }

        public void Add(int source, string item, int count)
        {
            if (count <= 0) return;
            used[Key(source, item)] = Used(source, item) + count;
        }

        // what is still there for a check-only run: live count minus what earlier pieces of the run counted
        public int Left(int source, string item, int live) { return Math.Max(0, live - Used(source, item)); }
    }
}
