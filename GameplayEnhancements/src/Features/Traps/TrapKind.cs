// One kind of trap: its look, trigger, damage, effects and lifetime. TrapCommand places any kind on the same ground matrix
// and runs every kind through the same watcher, so a new kind (the planned electric fence) is a new row in Get, plus its
// trigger shape in TrapCommand.Triggered when it is not a point.
namespace HomeCheat.Features.Traps
{
    internal sealed class TrapKind
    {
        public string Name;
        public string[] Visuals;      // first prefab that exists wins; any networked piece (re-found after a reload by its Piece and the hc_trap mark)
        public float TriggerRadius;   // m, a character this close sets it off; 0 = the square of the visual's footprint plus TriggerMargin
        public float BlastRadius;     // m, everyone this close is hit
        public float Life;            // s after placement, then it disappears
        public float ArmDelay;        // s after placement before it can go off (time to step away)
        public float Spacing;         // m between cells of a field
        public string Weapon;         // the item whose damage the blast carries; "" = no damage
        public string StatusEffect = "";   // a status effect the blast puts on everyone it hits (tar: Tared, the slow of tar pits)
        public float Mult;            // x that damage
        public string PushItem;       // the item whose attack force is the blast's knockback (one hit, not x Mult)
        public string FxPlace, FxBoom, FxEnd;   // comma lists of fx/sfx prefabs; "none" = no effect; FxBoom also "random" or a TrapFxSet name

        public const string Names = "mine|tar";   // mine = the explosive mine (also type=explosive), the default kind
        public static readonly string[] RandomPool = new string[] { "mine", "tar" };   // type=random: one of these per trap; a new mine kind joins here

        public bool Damages { get { return Weapon.Length > 0 && Mult > 0f; } }

        public static TrapKind Random() { return Get(RandomPool[UnityEngine.Random.Range(0, RandomPool.Length)]); }

        public static TrapKind Get(string name)
        {
            // mine (Maksym's design, 2026-10-25): lives 5 min, goes off once, hits players and creatures, 2 x the Ashlands bow.
            // Effects: the vanilla trap's arming flash and click, a random blast set of TrapFxSet (Maksym 2026-10-25), Odin's vanishing smoke
            // with the sound of wood breaking.
            // Knockback: one hit of the first sledge (SledgeStagbreaker, force 150), a small push (Maksym 2026-10-25; was the bow's 25 x 2).
            if (name == "explosive") name = "mine";   // Maksym's name for it (2026-10-25)
            if (name == "mine")
            {
                TrapKind k = new TrapKind();
                k.Name = "mine";
                // the look: a 1 x 1 m wooden floor square (Maksym 2026-10-25), set off by stepping onto the square
                k.Visuals = new string[] { "wood_floor_1x1", "piece_trap_troll", "fuling_trap" };
                k.TriggerRadius = 0f;
                k.BlastRadius = 3f;
                k.Life = 300f;
                k.ArmDelay = 3f;
                k.Spacing = 2f;
                k.Weapon = "BowAshlands";
                k.Mult = 2f;
                k.FxPlace = "fx_trap_arm";
                k.FxBoom = "random";
                k.PushItem = "SledgeStagbreaker";
                k.FxEnd = "vfx_odin_despawn,sfx_wood_destroyed";   // Odin's smoke and the floor tile breaking (Maksym 2026-10-25), at the end of its life and on remove
                return k;
            }
            // tar (Maksym 2026-10-25): the mine's look and trigger, no damage and no push; everyone in the blast gets Tared (the slow
            // of tar pits) for the effect's own time; its own blast look, the tar set
            if (name == "tar")
            {
                TrapKind k = Get("mine");
                k.Name = "tar";
                k.Weapon = "";
                k.Mult = 0f;
                k.PushItem = null;
                k.StatusEffect = "Tared";
                k.FxBoom = "tar";
                return k;
            }
            return null;
        }
    }
}
