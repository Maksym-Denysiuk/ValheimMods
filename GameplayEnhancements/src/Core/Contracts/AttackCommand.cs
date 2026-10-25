// Base of every command that damages or pushes characters (spellshot, trap, and through HarmEffect slime and earthquake).
// It carries the PvP rule (Kit.PvpHits): an opt-in pvp=1 parameter, stated in Usage with PvpUsage, read with HitsPlayers,
// and reported in the first answer line with PvpNote; Aoe and projectile helpers in PvpHits apply it.
// A new attack command derives from AttackCommand (or HarmEffect), takes pvp=, and its spell lines carry pvp=1.
using HomeCheat.Kit;

namespace HomeCheat
{
    public abstract class AttackCommand : CommandBase
    {
        protected const string PvpUsage = PvpHits.Usage;

        // pvp=1 given
        protected static bool PvpAsked(Args a) { return a.Flag("pvp", false); }

        // pvp=1 and your PvP is on: other players are hit
        protected static bool HitsPlayers(Args a) { return PvpHits.HitsPlayers(PvpAsked(a)); }

        protected static string PvpNote(Args a) { return PvpHits.Note(PvpAsked(a)); }
    }
}
