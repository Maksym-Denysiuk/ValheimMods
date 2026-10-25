// Outcome of GridRoute.Find.
namespace HomeCheat
{
    public enum GridRouteStatus
    {
        Found,      // path holds the cells from start to end
        NoRoute,    // every way is blocked
        OverCap,    // width x height is larger than GridRoute.MaxCells; nothing was searched
        Invalid     // start or end outside the grid, or an empty grid
    }
}
