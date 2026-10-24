// Command DLL file names: HomeCheat.Cmd.<feature>.v<stamp>.dll. The stamp is compared as a number, so the
// 14-digit yyyyMMddHHmmss stamps of build.sh always win over the old 6-digit hhmmss ones and never reset at midnight.
// No Unity reference: also compiled into HomeCheat.SelfTest.exe.
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace HomeCheat
{
    public static class BuildStamp
    {
        static readonly Regex Pattern = new Regex(@"^HomeCheat\.Cmd\.([A-Za-z0-9_\-]+)\.v(\d{1,18})\.dll$", RegexOptions.IgnoreCase);

        public static bool TryParse(string fileName, out string feature, out long stamp)
        {
            feature = null; stamp = 0;
            if (fileName == null) return false;
            Match m = Pattern.Match(fileName);
            if (!m.Success) return false;
            if (!long.TryParse(m.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out stamp)) return false;
            feature = m.Groups[1].Value.ToLowerInvariant();
            return true;
        }

        public static string FileName(string feature, DateTime when)
        {
            return "HomeCheat.Cmd." + feature.ToLowerInvariant() + ".v" + when.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) + ".dll";
        }
    }
}
