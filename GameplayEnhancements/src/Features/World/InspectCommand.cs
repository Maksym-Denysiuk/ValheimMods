using System.Reflection;
using System.Text;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.WorldCommands
{
    // inspect: calibration helper, the TerrainModifier / TerrainOp values of a terrain prefab
    public sealed class InspectCommand : CommandBase
    {
        public override string Name { get { return "inspect"; } }
        protected override string Usage { get { return "inspect prefab=<name> - components of a prefab and the values of its terrain components"; } }

        protected override string Execute(Args a)
        {
            string prefab = a.Str("prefab", "");
            GameObject go = Prefabs.Require(prefab, "");
            StringBuilder sb = new StringBuilder(prefab + "\n");
            foreach (Component c in go.GetComponents<Component>())
            {
                if (c == null) continue;
                sb.Append("[").Append(c.GetType().Name).Append("]\n");
                if (c is TerrainModifier || c is TerrainOp)
                    foreach (FieldInfo f in c.GetType().GetFields(Refl.InstanceAll))
                    {
                        object v = null;
                        try { v = f.GetValue(c); } catch { }
                        if (v == null || v is UnityEngine.Object) continue;
                        sb.Append("  ").Append(f.Name).Append(" = ").Append(v).Append('\n');
                    }
            }
            return sb.ToString();
        }
    }
}
