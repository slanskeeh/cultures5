using Cultures.Core.Events;
using Cultures.Core.Ids;
using Cultures.Core.Time;
using Cultures.Military;
using Cultures.Population;

namespace Cultures.Civilization;

/// <summary>
/// Factions and cultures. Independent of geography, LOD, exploration and presentation.
/// </summary>
public sealed class CivilizationSystem
{
    public CivilizationSystem(
        ulong worldSeed,
        EntityIdFactory ids,
        PopulationRoster population,
        EventBus events,
        SimulationClock clock)
    {
        WorldSeed = worldSeed;
        Ids = ids ?? throw new ArgumentNullException(nameof(ids));
        Population = population ?? throw new ArgumentNullException(nameof(population));
        Events = events ?? throw new ArgumentNullException(nameof(events));
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
        Cultures = new CultureDirectory();
        Factions = new FactionDirectory();
        Relations = new FactionRelationDirectory();
        Diplomacy = new DiplomacySystem(Factions, Relations, Ids, Events, Clock);
        EnsureNeutralCulture();
    }

    public ulong WorldSeed { get; }
    public EntityIdFactory Ids { get; }
    public PopulationRoster Population { get; }
    public EventBus Events { get; }
    public SimulationClock Clock { get; }
    public CultureDirectory Cultures { get; }
    public FactionDirectory Factions { get; }
    public FactionRelationDirectory Relations { get; }
    public DiplomacySystem Diplomacy { get; }

    public void SeedBaseline()
    {
        EnsureNeutralCulture();
        for (var i = 0; i < CivilizationRules.GeneratedCultureCount; i++)
        {
            if (!TryCreateCulture(null, null, out _, out var error))
                throw new InvalidOperationException(error);
        }

        var generated = Cultures.All.Where(c => c.Id != CultureId.Neutral).ToList();
        if (generated.Count == 0)
            throw new InvalidOperationException("Baseline requires a generated culture.");

        for (var i = 0; i < CivilizationRules.GeneratedFactionCount; i++)
        {
            var culture = generated[i % generated.Count].Id;
            if (!TryCreateFaction(culture, null, out _, out var error))
                throw new InvalidOperationException(error);
        }
    }

    public bool TryCreateCulture(string? name, CultureTraits? traits, out CultureState? culture, out string error)
    {
        culture = null;
        error = "Culture creation failed.";
        var id = Ids.NextCulture();
        var salt = id.Value;
        var resolvedName = string.IsNullOrWhiteSpace(name)
            ? FictionalName.Word(WorldSeed, salt, 2)
            : name.Trim();
        var resolvedTraits = traits ?? FictionalName.Traits(WorldSeed, salt);
        culture = new CultureState(id, resolvedName, resolvedTraits, salt);
        Cultures.Add(culture);
        Events.Publish(new CultureCreatedEvent(Clock.Tick, id));
        return true;
    }

    public bool TryCreateFaction(CultureId cultureId, string? name, out FactionState? faction, out string error)
    {
        faction = null;
        error = "Faction creation failed.";
        if (!Cultures.TryGet(cultureId, out _))
        {
            error = "Culture does not exist.";
            return false;
        }

        var id = Ids.NextFaction();
        var resolvedName = string.IsNullOrWhiteSpace(name)
            ? FictionalName.Word(WorldSeed, id.Value ^ 0xC2B2AE3D27D4EB4FUL, 2)
            : name.Trim();
        faction = new FactionState(id, resolvedName, cultureId, Clock.Tick);
        Factions.Add(faction);
        Events.Publish(new FactionCreatedEvent(Clock.Tick, id, cultureId));
        return true;
    }

    public bool TryAssignFaction(CharacterId characterId, FactionId factionId, out string error)
    {
        error = "Membership failed.";
        if (!Population.TryGet(characterId, out var character) || !character.IsAlive)
        {
            error = "Character is invalid.";
            return false;
        }

        if (factionId.IsAssigned && !Factions.TryGet(factionId, out _))
        {
            error = "Faction does not exist.";
            return false;
        }

        var previous = character.Faction;
        if (previous == factionId)
        {
            error = "Character already has that faction.";
            return false;
        }

        var previousGroup = character.PoliticalGroup;
        var previousUnit = character.MilitaryUnit;
        character.Faction = factionId;
        if (previousGroup.IsAssigned)
        {
            character.PoliticalGroup = PoliticalGroupId.None;
            Events.Publish(new PoliticalGroupMembershipChangedEvent(
                Clock.Tick,
                characterId,
                previousGroup,
                PoliticalGroupId.None));
        }

        if (previousUnit.IsAssigned)
        {
            character.MilitaryUnit = MilitaryUnitId.None;
            Events.Publish(new MilitaryMembershipChangedEvent(
                Clock.Tick,
                characterId,
                previousUnit,
                MilitaryUnitId.None));
        }

        Events.Publish(new FactionMembershipChangedEvent(Clock.Tick, characterId, previous, factionId));
        return true;
    }

    public bool TryAssignCulture(CharacterId characterId, CultureId cultureId, out string error)
    {
        error = "Culture assignment failed.";
        if (!Population.TryGet(characterId, out var character) || !character.IsAlive)
        {
            error = "Character is invalid.";
            return false;
        }

        if (!Cultures.TryGet(cultureId, out _))
        {
            error = "Culture does not exist.";
            return false;
        }

        var previous = character.Culture;
        if (previous == cultureId)
        {
            error = "Character already has that culture.";
            return false;
        }

        character.Culture = cultureId;
        Events.Publish(new CharacterCultureChangedEvent(Clock.Tick, characterId, previous, cultureId));
        return true;
    }

    public bool TrySetRelation(FactionId a, FactionId b, FactionRelationStance stance, out string error) =>
        Diplomacy.TrySetStance(a, b, stance, out error);

    public FactionRelationStance RelationOf(FactionId a, FactionId b) => Diplomacy.StanceOf(a, b);

    public int CountMembers(FactionId faction) =>
        Population.All.Count(person => person.IsAlive && person.Faction == faction);

    public IEnumerable<CharacterState> MembersOf(FactionId faction) =>
        Population.All.Where(person => person.IsAlive && person.Faction == faction);

    private void EnsureNeutralCulture()
    {
        if (Cultures.TryGet(CultureId.Neutral, out _))
            return;
        Cultures.Add(new CultureState(
            CultureId.Neutral,
            "Unaffiliated",
            default,
            0));
    }
}
