using Cultures.Core.Events;
using Cultures.World;

namespace Cultures.World;

public sealed record DebugCursorMovedEvent(
    ulong Tick,
    LogicalGridCoordinate From,
    LogicalGridCoordinate To,
    bool CrossedHorizontalSeam) : ISimulationEvent;
