using Cultures.Core.Events;
using Cultures.Core.Ids;
using Cultures.World;

namespace Cultures.Settlement;

public sealed record SettlementEmergedEvent(ulong Tick, SettlementId Settlement, LogicalGridCoordinate Core)
    : ISimulationEvent;

public sealed record SettlementStageChangedEvent(
    ulong Tick,
    SettlementId Settlement,
    SettlementLifecycle Previous,
    SettlementLifecycle Current) : ISimulationEvent;

public sealed record SettlementAbandonedEvent(ulong Tick, SettlementId Settlement) : ISimulationEvent;
