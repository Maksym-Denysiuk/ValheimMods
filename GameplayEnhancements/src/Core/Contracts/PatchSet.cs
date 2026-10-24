// The Harmony patches of one owner under one id. Apply patches one class at a time (never a whole assembly),
// RemoveAll undoes every patch of this id, Describe lists the patched methods for status answers.
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace HomeCheat
{
    public sealed class PatchSet
    {
        readonly string id;
        Harmony harmony;

        public PatchSet(string harmonyId) { id = harmonyId; }

        public string Id { get { return id; } }

        Harmony H
        {
            get
            {
                if (harmony == null) harmony = new Harmony(id);
                return harmony;
            }
        }

        // a [HarmonyPatch] class (Prefix / Postfix / Transpiler / Finalizer methods)
        public void Apply(Type patchClass)
        {
            H.CreateClassProcessor(patchClass).Patch();
        }

        public void Patch(MethodBase target, MethodInfo prefix, MethodInfo postfix)
        {
            if (target == null) throw new ArgumentNullException("target");
            H.Patch(target, prefix == null ? null : new HarmonyMethod(prefix), postfix == null ? null : new HarmonyMethod(postfix));
        }

        public void RemoveAll()
        {
            try { Harmony.UnpatchID(id); }
            catch (Exception e) { HomeApi.LogError("unpatch " + id + ": " + e.Message); }
        }

        public List<MethodBase> PatchedMethods()
        {
            List<MethodBase> l = new List<MethodBase>();
            foreach (MethodBase m in Harmony.GetAllPatchedMethods())
            {
                Patches p = Harmony.GetPatchInfo(m);
                if (p != null && p.Owners.Contains(id)) l.Add(m);
            }
            return l;
        }

        public bool IsActive { get { return PatchedMethods().Count > 0; } }

        public string Describe()
        {
            List<string> names = new List<string>();
            foreach (MethodBase m in PatchedMethods()) names.Add(m.DeclaringType.Name + "::" + m.Name);
            return names.Count == 0 ? "(none)" : string.Join(" ", names.ToArray());
        }
    }
}
