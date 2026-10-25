// Shared cap helper for counts and sizes. Every cap is a const in the command, stated in its Usage.
//   Check  refuses a value over the cap (the user asked for too much: nothing happens)
//   Clamp  lowers it to the cap and gives the note the answer's first line must carry ("count capped at 40 (asked 100)")
namespace HomeCheat.Kit
{
    public static class Caps
    {
        public static int Check(string name, int value, int max)
        {
            if (value > max) throw new CommandException(name + "=" + value + " is over the cap " + max);
            return value;
        }

        public static float Check(string name, float value, float max)
        {
            if (value > max) throw new CommandException(name + "=" + Fmt.F(value, 2) + " is over the cap " + Fmt.F(max, 2));
            return value;
        }

        // note is "" when the value was within the cap
        public static int Clamp(string name, int value, int max, out string note)
        {
            note = "";
            if (value <= max) return value;
            note = name + " capped at " + max + " (asked " + value + ")";
            return max;
        }

        public static float Clamp(string name, float value, float max, out string note)
        {
            note = "";
            if (value <= max) return value;
            note = name + " capped at " + Fmt.F(max, 2) + " (asked " + Fmt.F(value, 2) + ")";
            return max;
        }
    }
}
