using Cultures.Buildings;
using Cultures.Core.Events;
using Cultures.Core.Ids;
using Cultures.Core.Time;
using Cultures.World;

namespace Cultures.Population;

/// <summary>
/// Faction-independent household. Genealogy remains on FamilyLinks.
/// </summary>
public sealed class HouseholdState
{
    public HouseholdState(HouseholdId id, ulong foundedTick)
    {
        if (!id.IsAssigned)
            throw new ArgumentException("Household id is required.", nameof(id));
        Id = id;
        FoundedTick = foundedTick;
        Home = BuildingId.None;
    }

    public HouseholdId Id { get; }
    public ulong FoundedTick { get; }
    public BuildingId Home { get; set; }

    public HouseholdSnapshot Snapshot() => new(Id, FoundedTick, Home);
}

public readonly record struct HouseholdSnapshot(HouseholdId Id, ulong FoundedTick, BuildingId Home);

public sealed class HouseholdDirectory
{
    private readonly Dictionary<ulong, HouseholdState> _byId = new();
    private readonly List<HouseholdState> _order = new();

    public int Count => _order.Count;
    public IReadOnlyList<HouseholdState> All => _order;

    public void Add(HouseholdState household)
    {
        ArgumentNullException.ThrowIfNull(household);
        if (!_byId.TryAdd(household.Id.Value, household))
            throw new InvalidOperationException($"Duplicate household {household.Id}.");
        _order.Add(household);
    }

    public bool TryGet(HouseholdId id, out HouseholdState household) =>
        _byId.TryGetValue(id.Value, out household!);

    public HouseholdState this[int index] => _order[index];
}

public sealed class SocialLifeSystem
{
    public SocialLifeSystem(
        EntityIdFactory ids,
        PopulationRoster population,
        HouseholdDirectory households,
        BuildingDirectory buildings,
        ProfessionCatalog professions,
        EventBus events,
        SimulationClock clock)
    {
        Ids = ids ?? throw new ArgumentNullException(nameof(ids));
        Population = population ?? throw new ArgumentNullException(nameof(population));
        Households = households ?? throw new ArgumentNullException(nameof(households));
        Buildings = buildings ?? throw new ArgumentNullException(nameof(buildings));
        Professions = professions ?? throw new ArgumentNullException(nameof(professions));
        Events = events ?? throw new ArgumentNullException(nameof(events));
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public EntityIdFactory Ids { get; }
    public PopulationRoster Population { get; }
    public HouseholdDirectory Households { get; }
    public BuildingDirectory Buildings { get; }
    public ProfessionCatalog Professions { get; }
    public EventBus Events { get; }
    public SimulationClock Clock { get; }

    public IEnumerable<CharacterState> MembersOf(HouseholdId household) =>
        Population.Alive.Where(p => p.Household == household);

    public bool TryAssignProfession(CharacterId characterId, ProfessionId profession, out string error)
    {
        error = "Profession assignment failed.";
        if (!Population.TryGet(characterId, out var character) || !character.IsAlive)
        {
            error = "Character is invalid.";
            return false;
        }

        ProfessionDefinition? definition = null;
        if (profession.IsAssigned && !Professions.TryGet(profession, out definition))
        {
            error = "Profession does not exist.";
            return false;
        }

        if (profession.IsAssigned && character.LifeStage < CharacterLifeStage.Adult)
        {
            error = "Character is too young for a profession.";
            return false;
        }

        if (profession.IsAssigned && definition is not null && definition.MinimumStage > character.LifeStage)
        {
            error = "Character stage is not eligible.";
            return false;
        }

        if (character.Profession == profession)
        {
            error = "Character already has that profession.";
            return false;
        }

        var previous = character.Profession;
        character.Profession = profession;
        Events.Publish(new ProfessionChangedEvent(Clock.Tick, characterId, previous, profession));
        return true;
    }

    public bool TryMatchProfession(CharacterId characterId, out string error)
    {
        error = "Profession matching failed.";
        if (!Population.TryGet(characterId, out var character) || !character.IsAlive)
        {
            error = "Character is invalid.";
            return false;
        }

        ProfessionDefinition? best = null;
        var bestSkill = -1;
        foreach (var definition in Professions.All)
        {
            var skill = character.Skills.GetLevel(definition.PrimarySkill);
            if (skill < bestSkill)
                continue;
            if (skill == bestSkill && best is not null && string.CompareOrdinal(definition.Id.Value, best.Id.Value) >= 0)
                continue;
            best = definition;
            bestSkill = skill;
        }

        if (best is null)
        {
            error = "No profession available.";
            return false;
        }

        return TryAssignProfession(characterId, best.Id, out error);
    }

    public bool TryFormHousehold(CharacterId founderId, CharacterId? partnerId, out HouseholdState? household, out string error)
    {
        household = null;
        error = "Household formation failed.";
        if (!Population.TryGet(founderId, out var founder) || !founder.IsAlive)
        {
            error = "Founder is invalid.";
            return false;
        }

        CharacterState? partner = null;
        if (partnerId is { IsAssigned: true } other)
        {
            if (!Population.TryGet(other, out partner) || !partner.IsAlive)
            {
                error = "Partner is invalid.";
                return false;
            }
        }

        if (founder.Household.IsAssigned && partner is not null && partner.Household == founder.Household)
        {
            error = "Characters already share a household.";
            return false;
        }

        var id = Ids.NextHousehold();
        household = new HouseholdState(id, Clock.Tick);
        Households.Add(household);
        AssignHousehold(founder, id);
        if (partner is not null)
            AssignHousehold(partner, id);
        Events.Publish(new HouseholdFormedEvent(Clock.Tick, id, founderId));
        return true;
    }

    public bool TrySetHome(HouseholdId householdId, BuildingId buildingId, out string error)
    {
        error = "Home assignment failed.";
        if (!Households.TryGet(householdId, out var household))
        {
            error = "Household does not exist.";
            return false;
        }

        if (!Buildings.TryGet(buildingId, out var building) || !building.Definition.IsShelter || !building.IsActive)
        {
            error = "Home must be an active shelter.";
            return false;
        }

        if (household.Home == buildingId)
        {
            error = "Household already uses that home.";
            return false;
        }

        household.Home = buildingId;
        Events.Publish(new HouseholdHomeChangedEvent(Clock.Tick, householdId, buildingId));
        return true;
    }

    public bool TryFormPartnership(CharacterId leftId, CharacterId rightId, out string error)
    {
        error = "Partnership failed.";
        if (leftId == rightId)
        {
            error = "A character cannot partner with themselves.";
            return false;
        }

        if (!Population.TryGet(leftId, out var left) || !left.IsAlive
            || !Population.TryGet(rightId, out var right) || !right.IsAlive)
        {
            error = "Character is invalid.";
            return false;
        }

        if (!SkillRules.CanTeach(left) || !SkillRules.CanTeach(right))
        {
            error = "Both partners must be adults.";
            return false;
        }

        if (left.Partner.IsAssigned || right.Partner.IsAssigned)
        {
            error = "A partner is already pledged.";
            return false;
        }

        left.Partner = rightId;
        right.Partner = leftId;
        if (!left.Household.IsAssigned || left.Household != right.Household)
        {
            if (!TryFormHousehold(leftId, rightId, out _, out error))
                return false;
        }

        Events.Publish(new PartnershipFormedEvent(Clock.Tick, leftId, rightId));
        return true;
    }

    public bool TryLeaveHousehold(CharacterId characterId, out string error)
    {
        error = "Leave household failed.";
        if (!Population.TryGet(characterId, out var character) || !character.IsAlive)
        {
            error = "Character is invalid.";
            return false;
        }

        if (!character.Household.IsAssigned)
        {
            error = "Character has no household.";
            return false;
        }

        character.Household = HouseholdId.None;
        return true;
    }

    public BuildingState? HomeOf(CharacterState character)
    {
        if (!character.Household.IsAssigned)
            return null;
        if (!Households.TryGet(character.Household, out var household) || !household.Home.IsAssigned)
            return null;
        return Buildings.TryGet(household.Home, out var building) && building.IsActive ? building : null;
    }

    private static void AssignHousehold(CharacterState character, HouseholdId household) =>
        character.Household = household;
}
