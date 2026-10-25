// Named points per world (CoordStart / CoordEnd and any other name), kept in BepInEx/config/homecheat/coords.state.
// In the core so the points survive feature reloads; the file survives game restarts. Only this PC knows them.
// Format and limits: Pure/PointStore.cs. Users: coord, path from=coords, cultivate / field / plant area=coords, dome.
using System;
using System.IO;
using BepInEx;
using UnityEngine;

namespace HomeCheat.Kit
{
    public static class Coords
    {
        public const string Start = "start";
        public const string End = "end";

        static PointStore store;
        static string file;

        public static string FilePath
        {
            get
            {
                if (file == null) file = Path.Combine(Path.Combine(Paths.ConfigPath, "homecheat"), "coords.state");
                return file;
            }
        }

        // the loaded world's name: the key the points are kept under (throws when no world is loaded)
        public static string WorldKey { get { return Host.RequireWorld(); } }

        public static bool Get(string name, out Vector3 pos)
        {
            float x, y, z;
            bool ok = Store.TryGet(WorldKey, name, out x, out y, out z);
            pos = new Vector3(x, y, z);
            return ok;
        }

        // the point, or a CommandException that says how to set it
        public static Vector3 Require(string name)
        {
            Vector3 pos;
            if (!Get(name, out pos)) throw new CommandException("coord '" + name + "' is not set in world " + WorldKey + ": op=coord&set=" + name);
            return pos;
        }

        public static void Set(string name, Vector3 pos)
        {
            Store.Set(WorldKey, name, pos.x, pos.y, pos.z);
            Save();
        }

        public static bool Clear(string name)
        {
            bool had = Store.Clear(WorldKey, name);
            if (had) Save();
            return had;
        }

        // every point of the loaded world; returns how many were removed
        public static int ClearAll()
        {
            int n = Store.ClearWorld(WorldKey);
            if (n > 0) Save();
            return n;
        }

        public static System.Collections.Generic.List<string> Names() { return Store.Names(WorldKey); }

        static PointStore Store
        {
            get
            {
                if (store != null) return store;
                int skipped = 0;
                try { store = File.Exists(FilePath) ? PointStore.Parse(File.ReadAllText(FilePath), out skipped) : new PointStore(); }
                catch (Exception e) { HomeApi.LogError("coords: read " + FilePath + ": " + e.Message); store = new PointStore(); }
                if (skipped > 0) HomeApi.Log("coords: skipped " + skipped + " bad line(s) in " + FilePath);
                return store;
            }
        }

        // written as .tmp, then moved, so a crash never leaves half a file
        static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, Store.Format());
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(tmp, FilePath);
            }
            catch (Exception e) { throw new CommandException("coords: could not write " + FilePath + ": " + e.Message); }
        }
    }
}
