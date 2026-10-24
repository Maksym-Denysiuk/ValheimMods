// Reflection on game members that are not public. Every lookup returns null / the default when the member is missing,
// so a game update degrades a command instead of crashing it.
using System;
using System.Reflection;

namespace HomeCheat.Kit
{
    public static class Refl
    {
        public const BindingFlags InstanceAll = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        public const BindingFlags All = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public static FieldInfo Field(Type t, string name) { return t == null ? null : t.GetField(name, All); }

        public static object Get(object obj, string field)
        {
            if (obj == null) return null;
            FieldInfo f = Field(obj.GetType(), field);
            return f == null ? null : f.GetValue(obj);
        }

        public static bool Set(object obj, string field, object value)
        {
            if (obj == null) return false;
            FieldInfo f = Field(obj.GetType(), field);
            if (f == null) return false;
            f.SetValue(obj, value);
            return true;
        }

        public static float GetFloat(object obj, string field, float def)
        {
            object v = Get(obj, field);
            return v is float ? (float)v : def;
        }

        // sets a float field only when it exists and is a float
        public static bool SetFloat(object obj, string field, float value)
        {
            if (obj == null) return false;
            FieldInfo f = Field(obj.GetType(), field);
            if (f == null || f.FieldType != typeof(float)) return false;
            f.SetValue(obj, value);
            return true;
        }

        public static object GetStatic(Type t, string field)
        {
            FieldInfo f = Field(t, field);
            return f == null ? null : f.GetValue(null);
        }
    }
}
