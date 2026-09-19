using Cultures.Core.Events;
using Cultures.Core.Ids;
using Cultures.Core.Time;

namespace Cultures.Civilization;

/// <summary>
/// Authoritative diplomatic stances and explicit pacts. Hostile is not war.
/// </summary>
public sealed class DiplomacySystem
{
    public DiplomacySystem(
        FactionDirectory factions,
        FactionRelationDirectory relations,
        EntityIdFactory ids,
        EventBus events,
        SimulationClock clock)
    {
        Factions = factions ?? throw new ArgumentNullException(nameof(factions));
        Relations = relations ?? throw new ArgumentNullException(nameof(relations));
        Ids = ids ?? throw new ArgumentNullException(nameof(ids));
        Events = events ?? throw new ArgumentNullException(nameof(events));
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
        Pacts = new DiplomaticPactDirectory();
    }

    public FactionDirectory Factions { get; }
    public FactionRelationDirectory Relations { get; }
    public EntityIdFactory Ids { get; }
    public EventBus Events { get; }
    public SimulationClock Clock { get; }
    public DiplomaticPactDirectory Pacts { get; }

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

    public bool TryFormPact(FactionId a, FactionId b, DiplomaticPactKind kind, out DiplomaticPact? pact, out string error)
    {
        pact = null;
        error = "Pact failed.";
        if (kind is not (DiplomaticPactKind.Trade or DiplomaticPactKind.NonAggression or DiplomaticPactKind.Alliance))
        {
            error = "Pact kind is invalid.";
            return false;
        }

        if (a == b)
        {
            error = "A faction cannot pact with itself.";
            return false;
        }

        if (!Factions.TryGet(a, out _) || !Factions.TryGet(b, out _))
        {
            error = "Faction does not exist.";
            return false;
        }

        if (Pacts.Has(a, b, kind))
        {
            error = "That pact already exists.";
            return false;
        }

        var stance = StanceOf(a, b);
        if (kind == DiplomaticPactKind.Trade && stance == FactionRelationStance.Hostile)
        {
            error = "Hostile factions cannot form a trade pact.";
            return false;
        }

        if (kind == DiplomaticPactKind.Alliance && stance != FactionRelationStance.Friendly)
        {
            error = "Alliance requires a Friendly stance.";
            return false;
        }

        var (lower, higher) = FactionRelationDirectory.Normalize(a, b);
        pact = new DiplomaticPact(Ids.NextDiplomaticPact(), lower, higher, kind, Clock.Tick);
        Pacts.Add(pact);
        Events.Publish(new DiplomaticPactFormedEvent(Clock.Tick, pact.Id, lower, higher, kind));
        return true;
    }

    public bool TryBreakPact(DiplomaticPactId id, out string error)
    {
        error = "Pact break failed.";
        if (!Pacts.TryGet(id, out var pact))
        {
            error = "Pact does not exist.";
            return false;
        }

        Pacts.Remove(id);
        Events.Publish(new DiplomaticPactBrokenEvent(Clock.Tick, pact.Id, pact.Lower, pact.Higher, pact.Kind));
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
