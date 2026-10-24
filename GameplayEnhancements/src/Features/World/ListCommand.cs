using HomeCheat.Kit;

namespace HomeCheat.Features.WorldCommands
{
    // list: prefab names containing a text (max 200)
    public sealed class ListCommand : CommandBase
    {
        public override string Name { get { return "list"; } }
        protected override string Usage { get { return "list [filter=<text>] - prefab names, at most 200"; } }

        protected override string Execute(Args a)
        {
            return Prefabs.Lines(Prefabs.Matching(a.Str("filter", ""), null), 200, "... (more)");
        }
    }
}
