using Cultures.Core.Events;
using Cultures.Core.Ids;
using Cultures.World;

namespace Cultures.Population;

public sealed record CharacterDiedEvent(ulong Tick, CharacterId Character, LogicalGridCoordinate Position)
    : ISimulationEvent;
