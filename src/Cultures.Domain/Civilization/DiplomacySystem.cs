using Cultures.Core.Events;
using Cultures.Core.Ids;
using Cultures.Core.Time;

namespace Cultures.Civilization;

/// <summary>
/// Authoritative diplomatic stances between factions. No war, trade, or territory effects.
/// </summary>
public sealed class DiplomacySystem
{
    public DiplomacySystem(
        FactionDirectory factions,
        FactionRelationDirectory relations,
        EventBus events,
        SimulationClock clock)
    {
        Factions = factions ?? throw new ArgumentNullException(nameof(factions));
        Relations = relations ?? throw new ArgumentNullException(nameof(relations));
        Events = events ?? throw new ArgumentNullException(nameof(events));
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public FactionDirectory Factions { get; }
    public FactionRelationDirectory Relations { get; }
    public EventBus Events { get; }
    public SimulationClock Clock { get; }

    public FactionRelationStance StanceOf(FactionId a, FactionId b)
    {
        if (a == b || !a.IsAssigned || !b.IsAssigned)
            return FactionRelationStance.Neutral;
        return Relations.TryGet(a, b, out var relation) ? relation.Stance : FactionRelationStance.Neutral;
    }

    public bool TrySetStance(FactionId a, FactionId b, FactionRelationStance stance, out string error)
    {
        error = "Diplomacy failed.";
        if (!DiplomacyRules.IsDefined(stance))
        {
            error = "Diplomatic stance is invalid.";
            return false;
        }

        if (a == b)
        {
            error = "A faction cannot have a relation with itself.";
            return false;
        }

        if (!Factions.TryGet(a, out _))
        {
            error = "First faction does not exist.";
            return false;
        }

        if (!Factions.TryGet(b, out _))
        {
            error = "Second faction does not exist.";
            return false;
        }

        var previous = StanceOf(a, b);
        if (previous == stance)
        {
            error = $"Stance is already {stance}.";
            return false;
        }

        if (stance == FactionRelationStance.Neutral)
            Relations.Remove(a, b);
        else
            Relations.GetOrCreate(a, b).Stance = stance;

        var (lower, higher) = FactionRelationDirectory.Normalize(a, b);
        Events.Publish(new DiplomaticStanceChangedEvent(Clock.Tick, lower, higher, previous, stance));
        return true;
    }
}

public static class DiplomacyRules
{
    public static bool IsDefined(FactionRelationStance stance) =>
        stance is FactionRelationStance.Neutral
            or FactionRelationStance.Friendly
            or FactionRelationStance.Hostile;
}
