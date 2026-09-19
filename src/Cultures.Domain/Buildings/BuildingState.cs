using Cultures.Core.Ids;
using Cultures.Economy;
using Cultures.World;

namespace Cultures.Buildings;

public sealed class BuildingProductionState
{
    public RecipeId? CurrentRecipe { get; private set; }
    public int ProgressTicks { get; private set; }
    public int DurationTicks { get; private set; }

    public bool IsActive => CurrentRecipe is not null;

    public void Start(RecipeId recipe, int durationTicks)
    {
        CurrentRecipe = recipe;
        DurationTicks = Math.Max(1, durationTicks);
        ProgressTicks = 0;
    }

    public void Restore(RecipeId? recipe, int progressTicks, int durationTicks)
    {
        CurrentRecipe = recipe;
        ProgressTicks = Math.Max(0, progressTicks);
        DurationTicks = Math.Max(0, durationTicks);
    }

    public void Advance()
    {
        if (CurrentRecipe is not null)
            ProgressTicks++;
    }

    public void Clear()
    {
        CurrentRecipe = null;
        ProgressTicks = 0;
        DurationTicks = 0;
    }
}

/// <summary>
/// A specific building in the world. Identity is BuildingId, not array index or a Node.
/// </summary>
public sealed class BuildingState
{
    public BuildingState(
        BuildingId id,
        BuildingDefinition definition,
        LogicalGridCoordinate origin,
        IReadOnlyList<LogicalGridCoordinate> footprintCells,
        IReadOnlyList<WorkplaceSlot> workplaces,
        LogicalGridCoordinate accessCell)
    {
        Id = id;
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Origin = origin;
        FootprintCells = footprintCells ?? throw new ArgumentNullException(nameof(footprintCells));
        Workplaces = workplaces ?? throw new ArgumentNullException(nameof(workplaces));
        AccessCell = accessCell;
        Inventory = new Inventory(definition.StorageCapacity == 0 ? 16 : definition.StorageCapacity);
        Lifecycle = BuildingLifecycle.Planned;
        AssociatedSettlement = SettlementId.None;
    }

    public BuildingId Id { get; }
    public BuildingDefinition Definition { get; }
    public LogicalGridCoordinate Origin { get; }
    public IReadOnlyList<LogicalGridCoordinate> FootprintCells { get; }
    public IReadOnlyList<WorkplaceSlot> Workplaces { get; }
    public LogicalGridCoordinate AccessCell { get; }
    public Inventory Inventory { get; }
    public BuildingLifecycle Lifecycle { get; set; }
    public SettlementId AssociatedSettlement { get; set; }
    public BuildingProductionState Production { get; } = new();

    public BuildingTypeId TypeId => Definition.TypeId;
    public bool IsActive => Lifecycle == BuildingLifecycle.Active;

    public BuildingSnapshot Snapshot() => new(
        Id,
        TypeId.Value,
        Origin,
        Lifecycle,
        Inventory.GetQuantity(ResourceType.Food),
        Inventory.GetQuantity(ResourceType.Wood),
        Inventory.GetQuantity(ResourceType.Stone),
        Production.ProgressTicks,
        Workplaces.Count > 0 ? Workplaces[0].Worker.Value : 0,
        AssociatedSettlement.Value);
}

public readonly record struct BuildingSnapshot(
    BuildingId Id,
    string TypeId,
    LogicalGridCoordinate Origin,
    BuildingLifecycle Lifecycle,
    int Food,
    int Wood,
    int Stone,
    int ProductionProgress,
    ulong FirstWorker,
    ulong Settlement);
