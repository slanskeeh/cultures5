using Cultures.Buildings;
using Cultures.Population;

namespace Cultures.Population;

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

    public bool IsAssigned => !string.IsNullOrEmpty(Value);
    public override string ToString() => IsAssigned ? Value : "none";
}

public sealed class ProfessionDefinition
{
    public ProfessionDefinition(
        ProfessionId id,
        string displayName,
        SkillType primarySkill,
        IReadOnlyList<BuildingTypeId> workplaces,
        CharacterLifeStage minimumStage = CharacterLifeStage.Adult)
    {
        Id = id;
        DisplayName = displayName;
        PrimarySkill = primarySkill;
        Workplaces = workplaces;
        MinimumStage = minimumStage;
    }

    public ProfessionId Id { get; }
    public string DisplayName { get; }
    public SkillType PrimarySkill { get; }
    public IReadOnlyList<BuildingTypeId> Workplaces { get; }
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
    }

    public IReadOnlyList<ProfessionDefinition> All { get; }

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
        new ProfessionDefinition(ProfessionId.Hunter, "Hunter", SkillType.Hunting, [BuildingTypeId.HuntingCamp]),
        new ProfessionDefinition(ProfessionId.Fisher, "Fisher", SkillType.Fishing, [BuildingTypeId.Fishery])
    ]);
}
