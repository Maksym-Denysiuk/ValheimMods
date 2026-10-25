// TextInput.IsVisible: true while the construct panel is open, so the game frees the cursor and the player takes no input
// (the spell form does the same, SpellPatches). Applied when the panel first opens, inside a world: UI patches during the
// plugin start froze the main-scene load (architecture doc, Lifecycle).
namespace HomeCheat.Features.Construction
{
    public static class ConstructPatches
    {
        public static void TextInputPostfix(ref bool __result)
        {
            if (ConstructPanel.Current != null && ConstructPanel.Current.Visible) __result = true;
        }
    }
}
