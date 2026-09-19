using Cultures.Civilization;
using Cultures.Core.Events;
using Cultures.Core.Ids;
using Cultures.Core.Time;
using Cultures.Population;

namespace Cultures.Military;

/// <summary>
/// Faction-owned military identity. No combat, movement, war, or automatic consequences.
/// </summary>
public sealed class MilitarySystem
{
    public MilitarySystem(
        ulong worldSeed,
        EntityIdFactory ids,
        PopulationRoster population,
        FactionDirectory factions,
        EventBus events,
        SimulationClock clock)
    {
        WorldSeed = worldSeed;
        Ids = ids ?? throw new ArgumentNullException(nameof(ids));
        Population = population ?? throw new ArgumentNullException(nameof(population));
        Factions = factions ?? throw new ArgumentNullException(nameof(factions));
        Events = events ?? throw new ArgumentNullException(nameof(events));
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
        Units = new MilitaryUnitDirectory();
    }

    public ulong WorldSeed { get; }
    public EntityIdFactory Ids { get; }
    public PopulationRoster Population { get; }
    public FactionDirectory Factions { get; }
    public EventBus Events { get; }
    public SimulationClock Clock { get; }
    public MilitaryUnitDirectory Units { get; }

    public void SeedBaseline()
    {
        foreach (var faction in Factions.All)
        {
            for (var i = 0; i < MilitaryRules.GeneratedUnitsPerFaction; i++)
            {
                if (!TryCreateUnit(faction.Id, null, out _, out var error))
                    throw new InvalidOperationException(error);
            }
        }
    }

    public bool TryCreateUnit(FactionId factionId, string? name, out MilitaryUnitState? unit, out string error)
    {
        unit = null;
        error = "Military unit creation failed.";
        if (!Factions.TryGet(factionId, out _))
        {
            error = "Faction does not exist.";
            return false;
        }

        var id = Ids.NextMilitaryUnit();
        var salt = id.Value ^ (factionId.Value * 0x9E3779B97F4A7C15UL);
        var resolvedName = string.IsNullOrWhiteSpace(name)
            ? FictionalName.Word(WorldSeed, salt ^ 0xC2B2AE3D27D4EB4FUL, 2)
            : name.Trim();
        unit = new MilitaryUnitState(id, factionId, resolvedName, salt);
        Units.Add(unit);
        Events.Publish(new MilitaryUnitCreatedEvent(Clock.Tick, id, factionId));
        return true;
    }

    public bool TryAssignUnit(CharacterId characterId, MilitaryUnitId unitId, out string error)
    {
        error = "Military affiliation failed.";
        if (!Population.TryGet(characterId, out var character) || !character.IsAlive)
        {
            error = "Character is invalid.";
            return false;
        }

        if (unitId.IsAssigned)
        {
            if (!Units.TryGet(unitId, out var unit))
            {
                error = "Military unit does not exist.";
                return false;
            }

            if (!unit.IsActive)
            {
                error = "Military unit is not active.";
                return false;
            }

            if (character.Faction != unit.Faction)
            {
                error = "Character faction does not match the military unit.";
                return false;
            }
        }

        var previous = character.MilitaryUnit;
        if (previous == unitId)
        {
            error = "Character already has that military unit.";
            return false;
        }

        character.MilitaryUnit = unitId;
        Events.Publish(new MilitaryMembershipChangedEvent(Clock.Tick, characterId, previous, unitId));
        return true;
    }

    public bool TryRemoveFromUnit(CharacterId characterId, MilitaryUnitId unitId, out string error)
    {
        error = "Military removal failed.";
        if (!unitId.IsAssigned)
        {
            error = "Military unit is required.";
            return false;
        }

        if (!Population.TryGet(characterId, out var character) || !character.IsAlive)
        {
            error = "Character is invalid.";
            return false;
        }

        if (!Units.TryGet(unitId, out _))
        {
            error = "Military unit does not exist.";
            return false;
        }

        if (character.MilitaryUnit != unitId)
        {
            error = "Character is not in that unit.";
            return false;
        }

        return TryAssignUnit(characterId, MilitaryUnitId.None, out error);
    }

    public bool TryDisband(MilitaryUnitId unitId, out string error)
    {
        error = "Military disband failed.";
        if (!Units.TryGet(unitId, out var unit))
        {
            error = "Military unit does not exist.";
            return false;
        }

        if (!unit.IsActive)
        {
            error = "Military unit is already disbanded.";
            return false;
        }

        unit.Lifecycle = MilitaryUnitLifecycle.Disbanded;
        foreach (var person in Population.All)
        {
            if (!person.IsAlive || person.MilitaryUnit != unitId)
                continue;

            var previous = person.MilitaryUnit;
            person.MilitaryUnit = MilitaryUnitId.None;
            Events.Publish(new MilitaryMembershipChangedEvent(Clock.Tick, person.Id, previous, MilitaryUnitId.None));
        }

        Events.Publish(new MilitaryUnitDisbandedEvent(Clock.Tick, unitId, unit.Faction));
        return true;
    }

    public int CountMembers(MilitaryUnitId unit) =>
        Population.All.Count(person => person.IsAlive && person.MilitaryUnit == unit);
}
