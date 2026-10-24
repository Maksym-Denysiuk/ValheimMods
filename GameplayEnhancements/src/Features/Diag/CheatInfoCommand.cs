using System;
using System.Reflection;
using System.Text;

namespace HomeCheat.Features.Diag
{
    // cheatinfo: discovery - every member of the game assembly whose name contains "heat", with static values
    public sealed class CheatInfoCommand : CommandBase
    {
        public override string Name { get { return "cheatinfo"; } }
        protected override string Usage { get { return "cheatinfo - read-only: game members containing \"heat\" and their static values"; } }

        protected override string Execute(Args a)
        {
            StringBuilder sb = new StringBuilder();
            Assembly asm = typeof(Player).Assembly;
            const BindingFlags bf = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            foreach (Type t in asm.GetTypes())
                foreach (MemberInfo m in t.GetMembers(bf))
                {
                    if (m.Name.IndexOf("heat", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    string val = "";
                    FieldInfo fi = m as FieldInfo;
                    if (fi != null && fi.IsStatic)
                    {
                        try { val = " = " + fi.GetValue(null); } catch { }
                    }
                    sb.Append(t.FullName).Append('.').Append(m.Name).Append(" [").Append(m.MemberType).Append(']').Append(val).Append('\n');
                }
            return sb.ToString();
        }
    }
}
