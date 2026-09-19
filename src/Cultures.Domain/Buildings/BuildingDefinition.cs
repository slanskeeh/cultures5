using Cultures.Economy;
using Cultures.Population;

namespace Cultures.Buildings;

/// <summary>
/// Static building type. Location is never stored here.
/// </summary>
public sealed class BuildingDefinition
{
    public BuildingDefinition(
        BuildingTypeId typeId,
        string nameKey,
        BuildingFootprint footprint,
        bool occupiesCells,
        bool blocksMovement,
        int workplaceCount,
        RecipeId? recipe,
        int storageCapacity,
        bool isShelter,
        bool isStorage,
        ProfessionId? requiredProfession = null,
        bool requiresWaterAdjacent = false,
        IReadOnlyList<ResourceStack>? constructionCost = null,
        int constructionTicks = 24)
    {
        if (workplaceCount < 0)
            throw new ArgumentOutOfRangeException(nameof(workplaceCount));
        if (storageCapacity < 0)
            throw new ArgumentOutOfRangeException(nameof(storageCapacity));
        if (constructionTicks <= 0)
            throw new ArgumentOutOfRangeException(nameof(constructionTicks));

        TypeId = typeId;
        NameKey = nameKey;
        Footprint = footprint ?? throw new ArgumentNullException(nameof(footprint));
        OccupiesCells = occupiesCells;
        BlocksMovement = blocksMovement;
        WorkplaceCount = workplaceCount;
        Recipe = recipe;
        StorageCapacity = storageCapacity;
        IsShelter = isShelter;
        IsStorage = isStorage;
        RequiredProfession = requiredProfession;
        RequiresWaterAdjacent = requiresWaterAdjacent;
        ConstructionCost = constructionCost ?? [];
        ConstructionTicks = constructionTicks;
    }

    public BuildingTypeId TypeId { get; }
    public string NameKey { get; }
    public BuildingFootprint Footprint { get; }
    public bool OccupiesCells { get; }
    public bool BlocksMovement { get; }
    public int WorkplaceCount { get; }
    public RecipeId? Recipe { get; }
    public int StorageCapacity { get; }
    public bool IsShelter { get; }
    public bool IsStorage { get; }
    public ProfessionId? RequiredProfession { get; }
    public bool RequiresWaterAdjacent { get; }
    public IReadOnlyList<ResourceStack> ConstructionCost { get; }
    public int ConstructionTicks { get; }
}

public sealed class BuildingCatalog
{
    /// <summary>Tiny Phase 4 development set. Not a settlement catalogue.</summary>
    public static BuildingCatalog Development { get; } = CreateDevelopment();

    private readonly Dictionary<string, BuildingDefinition> _byId;

    public BuildingCatalog(IEnumerable<BuildingDefinition> definitions)
    {
        _byId = definitions.ToDictionary(d => d.TypeId.Value, StringComparer.Ordinal);
        All = _byId.Values.ToArray();
    }

    public IReadOnlyList<BuildingDefinition> All { get; }

    public BuildingDefinition Get(BuildingTypeId typeId)
    {
        if (!_byId.TryGetValue(typeId.Value, out var definition))
            throw new KeyNotFoundException($"Unknown building type '{typeId.Value}'.");
        return definition;
    }

    public bool TryGet(BuildingTypeId typeId, out BuildingDefinition definition) =>
        _byId.TryGetValue(typeId.Value, out definition!);

    private static BuildingCatalog CreateDevelopment() => new(
    [
        new BuildingDefinition(
            BuildingTypeId.Shelter,
            "shelter",
            BuildingFootprint.Rect(3, 2),
            occupiesCells: true,
            blocksMovement: true,
            workplaceCount: 0,
            recipe: null,
            storageCapacity: 0,
            isShelter: true,
            isStorage: false,
            constructionCost: [new ResourceStack(ResourceType.Wood, 3)],
            constructionTicks: 24),
        new BuildingDefinition(
            BuildingTypeId.Farm,
            "farm",
            BuildingFootprint.Disk(1),
            occupiesCells: true,
            blocksMovement: true,
            workplaceCount: 1,
            recipe: RecipeId.FarmFood,
            storageCapacity: 16,
            isShelter: false,
            isStorage: false,
            requiredProfession: ProfessionId.Farmer,
            constructionCost: [new ResourceStack(ResourceType.Wood, 2)],
            constructionTicks: 24),
        new BuildingDefinition(
            BuildingTypeId.Storage,
            "storage",
            BuildingFootprint.Rect(2, 2),
            occupiesCells: true,
            blocksMovement: true,
            workplaceCount: 0,
            recipe: null,
            storageCapacity: 256,
            isShelter: false,
            isStorage: true,
            constructionCost:
            [
                new ResourceStack(ResourceType.Wood, 4),
                new ResourceStack(ResourceType.Stone, 1)
            ],
            constructionTicks: 30),
        new BuildingDefinition(
            BuildingTypeId.Workshop,
            "workshop",
            BuildingFootprint.FromCubeOffsets(
                (0, 0), (1, 0), (2, 0),
                (0, 1), (0, 2),
                (1, -1)),
            occupiesCells: true,
            blocksMovement: true,
            workplaceCount: 1,
            recipe: RecipeId.WorkshopWood,
            storageCapacity: 16,
            isShelter: false,
            isStorage: false,
            constructionCost:
            [
                new ResourceStack(ResourceType.Wood, 6),
                new ResourceStack(ResourceType.Stone, 2)
            ],
            constructionTicks: 40),
        new BuildingDefinition(
            BuildingTypeId.HuntingCamp,
            "hunting_camp",
            BuildingFootprint.Rect(2, 2),
            occupiesCells: true,
            blocksMovement: true,
            workplaceCount: 1,
            recipe: RecipeId.HuntFood,
            storageCapacity: 16,
            isShelter: false,
            isStorage: false,
            requiredProfession: ProfessionId.Hunter,
            constructionCost: [new ResourceStack(ResourceType.Wood, 2)],
            constructionTicks: 20),
        new BuildingDefinition(
            BuildingTypeId.Fishery,
            "fishery",
            BuildingFootprint.FromCubeOffsets((0, 0), (1, 0), (2, 0)),
            occupiesCells: true,
            blocksMovement: true,
            workplaceCount: 1,
            recipe: RecipeId.FisheryFood,
            storageCapacity: 16,
            isShelter: false,
            isStorage: false,
            requiredProfession: ProfessionId.Fisher,
            requiresWaterAdjacent: true,
            constructionCost: [new ResourceStack(ResourceType.Wood, 3)],
            constructionTicks: 24)
    ]);
}
