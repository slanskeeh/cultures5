using Cultures.Core.Ids;
using Cultures.World;

namespace Cultures.Buildings;

public sealed class PlacementResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public BuildingState? Building { get; init; }

    public static PlacementResult Ok(BuildingState building) => new() { Success = true, Building = building };
    public static PlacementResult Fail(string error) => new() { Success = false, Error = error };
}

/// <summary>
/// Authoritative building placement. Presentation must not call this on Nodes.
/// </summary>
public sealed class BuildingPlacementSystem
{
    public BuildingPlacementSystem(
        LogicalWorld world,
        BuildingDirectory buildings,
        BuildingCatalog catalog,
        EntityIdFactory ids)
    {
        World = world ?? throw new ArgumentNullException(nameof(world));
        Buildings = buildings ?? throw new ArgumentNullException(nameof(buildings));
        Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        Ids = ids ?? throw new ArgumentNullException(nameof(ids));
    }

    public LogicalWorld World { get; }
    public BuildingDirectory Buildings { get; }
    public BuildingCatalog Catalog { get; }
    public EntityIdFactory Ids { get; }

    public PlacementResult TryPlace(BuildingTypeId typeId, LogicalGridCoordinate origin, bool completeImmediately = true)
    {
        var definition = Catalog.Get(typeId);
        if (!definition.Footprint.TryMaterialize(World.Topology, origin, out var cells))
            return PlacementResult.Fail("Footprint is outside the world.");

        foreach (var cell in cells)
        {
            var terrain = World.Grid.GetCell(cell);
            if (!terrain.Generated.Passable)
                return PlacementResult.Fail("Terrain is not passable.");
            if (terrain.Occupancy.IsOccupied)
                return PlacementResult.Fail("Cell is already occupied.");
        }

        if (definition.RequiresWaterAdjacent && !HasAdjacentWater(cells))
            return PlacementResult.Fail("Building must be adjacent to water.");

        if (!TryFindAccess(cells, out var access))
            return PlacementResult.Fail("No passable access cell.");

        var id = Ids.NextBuilding();
        var workplaces = new WorkplaceSlot[definition.WorkplaceCount];
        for (var i = 0; i < workplaces.Length; i++)
            workplaces[i] = new WorkplaceSlot(i, access);

        var building = new BuildingState(id, definition, origin, cells, workplaces, access);
        if (completeImmediately)
            building.Lifecycle = BuildingLifecycle.Active;
        else
            building.Lifecycle = BuildingLifecycle.Constructing;

        if (definition.OccupiesCells)
        {
            foreach (var cell in cells)
            {
                World.Grid.TrySetOccupancy(
                    cell.ToWorld(),
                    Occupancy.ForBuilding(id, definition.BlocksMovement));
            }
        }

        Buildings.Add(building);
        return PlacementResult.Ok(building);
    }

    public void Restore(BuildingState building)
    {
        ArgumentNullException.ThrowIfNull(building);
        Buildings.Add(building);
        if (!building.Definition.OccupiesCells)
            return;
        foreach (var cell in building.FootprintCells)
        {
            World.Grid.TrySetOccupancy(
                cell.ToWorld(),
                Occupancy.ForBuilding(building.Id, building.Definition.BlocksMovement));
        }
    }

    public PlacementResult TryRemove(BuildingId id)
    {
        if (!Buildings.TryGet(id, out var building))
            return PlacementResult.Fail("Building does not exist.");

        foreach (var cell in building.FootprintCells)
        {
            var occupancy = World.Grid.GetCell(cell).Occupancy;
            if (occupancy.Kind == OccupantKind.Building && occupancy.BuildingId == id)
                World.Grid.TryClearOccupancy(cell.ToWorld());
        }

        Buildings.Remove(id);
        return PlacementResult.Ok(building);
    }

    public void CompleteConstruction(BuildingState building)
    {
        ArgumentNullException.ThrowIfNull(building);
        if (building.Lifecycle is BuildingLifecycle.Planned or BuildingLifecycle.Constructing)
            building.Lifecycle = BuildingLifecycle.Active;
    }

    private bool TryFindAccess(IReadOnlyList<LogicalGridCoordinate> footprint, out LogicalGridCoordinate access)
    {
        var occupied = footprint.ToHashSet();
        foreach (var cell in footprint)
        {
            foreach (var (dx, dy) in HexGrid.NeighborOffsets(cell.Y))
            {
                var resolution = World.Topology.Resolve(cell.X + dx, cell.Y + dy);
                if (!resolution.TryGetCell(out var neighbor) || occupied.Contains(neighbor))
                    continue;

                var terrain = World.Grid.GetCell(neighbor);
                if (terrain.Generated.Passable && !terrain.Occupancy.BlocksMovement)
                {
                    access = neighbor;
                    return true;
                }
            }
        }

        access = default;
        return false;
    }

    private bool HasAdjacentWater(IReadOnlyList<LogicalGridCoordinate> footprint)
    {
        var occupied = footprint.ToHashSet();
        foreach (var cell in footprint)
        {
            foreach (var (dx, dy) in HexGrid.NeighborOffsets(cell.Y))
            {
                var resolution = World.Topology.Resolve(cell.X + dx, cell.Y + dy);
                if (!resolution.TryGetCell(out var neighbor) || occupied.Contains(neighbor))
                    continue;
                if (World.Grid.GetCell(neighbor).IsWater)
                    return true;
            }
        }

        return false;
    }
}
