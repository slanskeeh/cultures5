using Cultures.Buildings;
using Cultures.Economy;

namespace Cultures.Population;

public enum LaborKind : byte
{
    None = 0,
    Workplace = 1,
    Gather = 2,
    Hunt = 3,
    Fish = 4,
    Build = 5,
    Scout = 6,
    Haul = 7
}

/// <summary>
/// Data-driven vocation. Distinct from skill.
/// </summary>
public readonly record struct ProfessionId(string Value)
{
    public static ProfessionId None => new("");
    public static ProfessionId Farmer => new("farmer");
    public static ProfessionId Woodcutter => new("woodcutter");
    public static ProfessionId Mason => new("mason");
    public static ProfessionId Crafter => new("crafter");
    public static ProfessionId Hunter => new("hunter");
    public static ProfessionId Fisher => new("fisher");
    public static ProfessionId WoodGatherer => new("wood_gatherer");
    public static ProfessionId StoneGatherer => new("stone_gatherer");
    public static ProfessionId ClayGatherer => new("clay_gatherer");
    public static ProfessionId MushroomGatherer => new("mushroom_gatherer");
    public static ProfessionId Builder => new("builder");
    public static ProfessionId Scout => new("scout");
    public static ProfessionId Porter => new("porter");

    public bool IsAssigned => !string.IsNullOrEmpty(Value);
    public override string ToString() => IsAssigned ? Value : "none";
}

public sealed class ProfessionDefinition
{
    public ProfessionDefinition(
        ProfessionId id,
        string displayName,
        SkillType? primarySkill,
        IReadOnlyList<BuildingTypeId> workplaces,
        LaborKind labor = LaborKind.Workplace,
        ResourceType? fieldResource = null,
        bool requiresTraining = true,
        CharacterLifeStage minimumStage = CharacterLifeStage.Adult)
    {
        Id = id;
        DisplayName = displayName;
        PrimarySkill = primarySkill;
        Workplaces = workplaces;
        Labor = labor;
        FieldResource = fieldResource;
        RequiresTraining = requiresTraining;
        MinimumStage = minimumStage;
    }

    public ProfessionId Id { get; }
    public string DisplayName { get; }
    public SkillType? PrimarySkill { get; }
    public IReadOnlyList<BuildingTypeId> Workplaces { get; }
    public LaborKind Labor { get; }
    public ResourceType? FieldResource { get; }
    public bool RequiresTraining { get; }
    public CharacterLifeStage MinimumStage { get; }
}

public sealed class ProfessionCatalog
{
    public static ProfessionCatalog Development { get; } = CreateDevelopment();

    private readonly Dictionary<string, ProfessionDefinition> _byId;

    public ProfessionCatalog(IEnumerable<ProfessionDefinition> definitions)
    {
        _byId = definitions.ToDictionary(d => d.Id.Value, StringComparer.Ordinal);
        All = _byId.Values.ToArray();
        Starting = All.Where(p => !p.RequiresTraining).ToArray();
    }

    public IReadOnlyList<ProfessionDefinition> All { get; }
    public IReadOnlyList<ProfessionDefinition> Starting { get; }

    public bool TryGet(ProfessionId id, out ProfessionDefinition definition)
    {
        if (!id.IsAssigned)
        {
            definition = null!;
            return false;
        }

        return _byId.TryGetValue(id.Value, out definition!);
    }

    public ProfessionDefinition? ForWorkplace(BuildingTypeId type) =>
        All.FirstOrDefault(p => p.Workplaces.Any(w => w == type));

    private static ProfessionCatalog CreateDevelopment() => new(
    [
        new ProfessionDefinition(ProfessionId.Farmer, "Farmer", SkillType.Farming, [BuildingTypeId.Farm]),
        new ProfessionDefinition(ProfessionId.Woodcutter, "Woodcutter", SkillType.Woodworking, [BuildingTypeId.Workshop]),
        new ProfessionDefinition(ProfessionId.Mason, "Mason", SkillType.Stoneworking, [BuildingTypeId.Workshop]),
        new ProfessionDefinition(ProfessionId.Crafter, "Crafter", SkillType.Crafting, [BuildingTypeId.Workshop]),
        new ProfessionDefinition(
            ProfessionId.WoodGatherer, "Wood gatherer", null, [],
            LaborKind.Gather, ResourceType.Wood, requiresTraining: false),
        new ProfessionDefinition(
            ProfessionId.StoneGatherer, "Stone gatherer", null, [],
            LaborKind.Gather, ResourceType.Stone, requiresTraining: false),
        new ProfessionDefinition(
            ProfessionId.ClayGatherer, "Clay gatherer", null, [],
            LaborKind.Gather, ResourceType.Clay, requiresTraining: false),
        new ProfessionDefinition(
            ProfessionId.MushroomGatherer, "Mushroom gatherer", null, [],
            LaborKind.Gather, ResourceType.Mushrooms, requiresTraining: false),
        new ProfessionDefinition(
            ProfessionId.Hunter, "Hunter", SkillType.Hunting, [BuildingTypeId.HuntingCamp],
            LaborKind.Hunt, ResourceType.Meat, requiresTraining: false),
        new ProfessionDefinition(
            ProfessionId.Fisher, "Fisher", SkillType.Fishing, [BuildingTypeId.Fishery],
            LaborKind.Fish, ResourceType.Fish, requiresTraining: false),
        new ProfessionDefinition(
            ProfessionId.Builder, "Builder", null, [],
            LaborKind.Build, requiresTraining: false),
        new ProfessionDefinition(
            ProfessionId.Scout, "Scout", null, [],
            LaborKind.Scout, requiresTraining: false),
        new ProfessionDefinition(
            ProfessionId.Porter, "Porter", null, [],
            LaborKind.Haul, requiresTraining: false)
    ]);
}
