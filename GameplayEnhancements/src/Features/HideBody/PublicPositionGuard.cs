// Switches ZNet.m_publicReferencePosition (the map position flag) off for one call and restores it afterwards.
// Shared by the two patches below; one guard per patched method, so nested calls cannot mix their saved values.
using System;
using System.Reflection;

namespace HomeCheat.Features.HideBody
{
    internal sealed class PublicPositionGuard
    {
        static readonly FieldInfo fPub = typeof(ZNet).GetField("m_publicReferencePosition", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        readonly string label;
        bool changed, saved;

        public PublicPositionGuard(string what) { label = what; }

        public void Off(ZNet inst)
        {
            if (!HideBodyCommand.Active || fPub == null) return;
            try { saved = (bool)fPub.GetValue(inst); fPub.SetValue(inst, false); changed = true; HideBodyCommand.Current.PubOff++; }
            catch (Exception e) { HomeApi.Log("hidebody pin off" + label + ": " + e.Message); }
        }

        public void Restore(ZNet inst)
        {
            if (!changed) return;
            try { fPub.SetValue(inst, saved); } catch (Exception e) { HomeApi.Log("hidebody pin restore" + label + ": " + e.Message); }
            changed = false;
        }
    }
}
