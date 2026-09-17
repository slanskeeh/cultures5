using Cultures.Core.Events;
using Cultures.Core.Ids;
using Cultures.Core.Time;
using Cultures.Population;

namespace Cultures.Civilization;

/// <summary>
/// Faction-local internal politics. No autonomous AI, diplomacy, war, or territory effects.
/// </summary>
public sealed class InternalPoliticsSystem
{
    public InternalPoliticsSystem(
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
        Groups = new PoliticalGroupDirectory();
        Stability = new InternalPoliticsDirectory();
    }

    public ulong WorldSeed { get; }
    public EntityIdFactory Ids { get; }
    public PopulationRoster Population { get; }
    public FactionDirectory Factions { get; }
    public EventBus Events { get; }
    public SimulationClock Clock { get; }
    public PoliticalGroupDirectory Groups { get; }
    public InternalPoliticsDirectory Stability { get; }

    public void SeedBaseline()
    {
        foreach (var faction in Factions.All)
        {
            for (var i = 0; i < PoliticsRules.GeneratedGroupsPerFaction; i++)
            {
                if (!TryCreateGroup(faction.Id, null, out _, out var error))
                    throw new InvalidOperationException(error);
            }
        }
    }

    public bool TryCreateGroup(FactionId factionId, string? name, out PoliticalGroupState? group, out string error)
    {
        group = null;
        error = "Political group creation failed.";
        if (!Factions.TryGet(factionId, out _))
        {
            error = "Faction does not exist.";
            return false;
        }

        var id = Ids.NextPoliticalGroup();
        var salt = id.Value ^ (factionId.Value * 0x9E3779B97F4A7C15UL);
        var resolvedName = string.IsNullOrWhiteSpace(name)
            ? FictionalName.Word(WorldSeed, salt ^ 0x85EBCA77C2B2AE63UL, 2)
            : name.Trim();
        group = new PoliticalGroupState(
            id,
            factionId,
            resolvedName,
            FictionalName.PoliticalTraits(WorldSeed, salt),
            salt);
        Groups.Add(group);
        Events.Publish(new PoliticalGroupCreatedEvent(Clock.Tick, id, factionId));
        return true;
    }

    public bool TryAssignGroup(CharacterId characterId, PoliticalGroupId groupId, out string error)
    {
        error = "Political affiliation failed.";
        if (!Population.TryGet(characterId, out var character) || !character.IsAlive)
        {
            error = "Character is invalid.";
            return false;
        }

        if (groupId.IsAssigned)
        {
            if (!Groups.TryGet(groupId, out var group))
            {
                error = "Political group does not exist.";
                return false;
            }

            if (character.Faction != group.Faction)
            {
                error = "Character faction does not match the political group.";
                return false;
            }
        }

        var previous = character.PoliticalGroup;
        if (previous == groupId)
        {
            error = "Character already has that political group.";
            return false;
        }

        character.PoliticalGroup = groupId;
        Events.Publish(new PoliticalGroupMembershipChangedEvent(Clock.Tick, characterId, previous, groupId));
        return true;
    }

    public bool TrySetInfluence(PoliticalGroupId groupId, int influence, out string error)
    {
        error = "Influence failed.";
        if (!Groups.TryGet(groupId, out var group))
        {
            error = "Political group does not exist.";
            return false;
        }

        if (influence is < PoliticsRules.InfluenceMin or > PoliticsRules.InfluenceMax)
        {
            error = "Influence is out of range.";
            return false;
        }

        if (group.Influence == influence)
        {
            error = "Influence is already set to that value.";
            return false;
        }

        var previous = group.Influence;
        group.Influence = influence;
        Events.Publish(new PoliticalGroupInfluenceChangedEvent(Clock.Tick, groupId, previous, influence));
        return true;
    }

    public bool TrySetStability(FactionId factionId, int value, out string error)
    {
        error = "Stability failed.";
        if (!Factions.TryGet(factionId, out _))
        {
            error = "Faction does not exist.";
            return false;
        }

        if (!InternalStability.IsInRange(value))
        {
            error = "Stability is out of range.";
            return false;
        }

        var previous = Stability.Of(factionId);
        if (previous.Value == value)
        {
            error = "Stability is already set to that value.";
            return false;
        }

        Stability.Set(factionId, new InternalStability(value));
        Events.Publish(new InternalStabilityChangedEvent(Clock.Tick, factionId, previous.Value, value));
        return true;
    }

    public int CountMembers(PoliticalGroupId group) =>
        Population.All.Count(person => person.IsAlive && person.PoliticalGroup == group);
}
