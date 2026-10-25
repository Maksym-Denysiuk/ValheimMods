// One kind of trap: its look, trigger, damage, effects and lifetime. TrapCommand places any kind on the same ground matrix
// and runs every kind through the same watcher, so a new kind (the planned electric fence) is a new row in Get, plus its
// trigger shape in TrapCommand.Triggered when it is not a point.
namespace HomeCheat.Features.Traps
{
    internal sealed class TrapKind
    {
        public string Name;
        public string[] Visuals;      // first prefab that exists wins; it must carry a vanilla Trap (re-found after a reload by that)
        public float TriggerRadius;   // m, a character this close sets it off
        public float BlastRadius;     // m, everyone this close is hit
        public float Life;            // s after placement, then it disappears
        public float ArmDelay;        // s after placement before it can go off (time to step away)
        public float Spacing;         // m between cells of a field
        public string Weapon;         // the item whose damage the blast carries
        public float Mult;            // x that damage
        public string FxPlace, FxBoom, FxEnd;   // comma lists of fx/sfx prefabs; "none" = no effect

        public const string Names = "mine";

        public static TrapKind Get(string name)
        {
            // mine (Maksym's design, 2026-10-25): lives 5 min, goes off once, hits players and creatures, 2 x the Ashlands bow.
            // Effects: the vanilla trap's arming flash and click, the dynamite blast and its sound, Odin's vanishing smoke.
            if (name == "mine")
            {
                TrapKind k = new TrapKind();
                k.Name = "mine";
                k.Visuals = new string[] { "piece_trap_troll", "fuling_trap" };
                k.TriggerRadius = 1.5f;
                k.BlastRadius = 3f;
                k.Life = 300f;
                k.ArmDelay = 3f;
                k.Spacing = 2f;
                k.Weapon = "BowAshlands";
                k.Mult = 2f;
                k.FxPlace = "fx_trap_arm";
                k.FxBoom = "fx_dynamite_explosion,sfx_bombdynamite_explosion";
                k.FxEnd = "vfx_odin_despawn";
                return k;
            }
            return null;
        }
    }
}
