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
        bool isStorage)
    {
        if (workplaceCount < 0)
            throw new ArgumentOutOfRangeException(nameof(workplaceCount));
        if (storageCapacity < 0)
            throw new ArgumentOutOfRangeException(nameof(storageCapacity));

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
            BuildingFootprint.Cell1x1,
            occupiesCells: true,
            blocksMovement: true,
            workplaceCount: 0,
            recipe: null,
            storageCapacity: 0,
            isShelter: true,
            isStorage: false),
        new BuildingDefinition(
            BuildingTypeId.Farm,
            "farm",
            BuildingFootprint.Cell1x1,
            occupiesCells: true,
            blocksMovement: true,
            workplaceCount: 1,
            recipe: RecipeId.FarmFood,
            storageCapacity: 16,
            isShelter: false,
            isStorage: false),
        new BuildingDefinition(
            BuildingTypeId.Storage,
            "storage",
            BuildingFootprint.Cell1x1,
            occupiesCells: true,
            blocksMovement: true,
            workplaceCount: 0,
            recipe: null,
            storageCapacity: 256,
            isShelter: false,
            isStorage: true),
        new BuildingDefinition(
            BuildingTypeId.Workshop,
            "workshop",
            BuildingFootprint.Cell1x1,
            occupiesCells: true,
            blocksMovement: true,
            workplaceCount: 1,
            recipe: RecipeId.WorkshopWood,
            storageCapacity: 16,
            isShelter: false,
            isStorage: false)
    ]);
}
