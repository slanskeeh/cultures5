using Cultures.Core.Events;
using Cultures.Core.Time;
using Cultures.Core.Ids;
using Cultures.Economy;
using Cultures.Population;
using Cultures.World;

namespace Cultures.Buildings;

public sealed class ProductionSystem
{
    public ProductionSystem(
        LogicalWorld world,
        BuildingDirectory buildings,
        ProductionResolver resolver,
        EventBus events,
        SimulationClock clock)
    {
        World = world ?? throw new ArgumentNullException(nameof(world));
        Buildings = buildings ?? throw new ArgumentNullException(nameof(buildings));
        Resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        Events = events ?? throw new ArgumentNullException(nameof(events));
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public LogicalWorld World { get; }
    public BuildingDirectory Buildings { get; }
    public ProductionResolver Resolver { get; }
    public EventBus Events { get; }
    public SimulationClock Clock { get; }

    public bool TryEvaluate(BuildingState building, CharacterState? worker, out ProductionEvaluation evaluation)
    {
        var terrain = World.Grid.GetCell(building.Origin);
        return Resolver.TryEvaluate(building, terrain, worker, out evaluation);
    }

    public void BeginWork(BuildingState building, CharacterState worker)
    {
        if (!building.IsActive)
            return;
        if (!TryEvaluate(building, worker, out var evaluation))
            return;
        building.Production.Start(evaluation.Recipe, evaluation.DurationTicks);
    }

    public void AdvanceWork(BuildingState building)
    {
        if (building.IsActive)
            building.Production.Advance();
    }

    public void AbandonWork(CharacterState worker)
    {
        if (!worker.AssignedWorkplace.IsAssigned)
            return;
        if (!Buildings.TryGet(worker.AssignedWorkplace.Building, out var building))
            return;
        building.Production.Clear();
    }

    public bool TryCompleteWork(CharacterState worker)
    {
        if (!worker.AssignedWorkplace.IsAssigned)
            return false;
        if (!Buildings.TryGet(worker.AssignedWorkplace.Building, out var building))
            return false;
        if (!building.IsActive)
            return false;
        if (!TryEvaluate(building, worker, out var evaluation))
            return false;
        if (!Resolver.Recipes.TryGet(evaluation.Recipe, out var recipe))
            return false;
        if (!recipe.TryExecute(building.Inventory, evaluation.Outputs))
            return false;

        RouteToStorage(building);
        building.Production.Clear();
        Events.Publish(new ResourceProducedEvent(Clock.Tick, building.Id, evaluation.Recipe));
        return true;
    }

    public BuildingState? FindNearestStorage(LogicalGridCoordinate from)
    {
        BuildingState? best = null;
        var bestDistance = int.MaxValue;
        foreach (var building in Buildings.All)
        {
            if (!building.Definition.IsStorage || !building.IsActive)
                continue;
            var distance = World.Topology.HorizontalDistance(from, building.AccessCell)
                + Math.Abs(from.Y - building.AccessCell.Y);
            if (distance >= bestDistance)
                continue;
            bestDistance = distance;
            best = building;
        }

        return best;
    }

    public BuildingState? FindStorageWith(ResourceType type, int quantity)
    {
        foreach (var building in Buildings.All)
        {
            if (building.Definition.IsStorage && building.IsActive && building.Inventory.Has(type, quantity))
                return building;
        }

        return null;
    }

    public BuildingState? FindShelter()
    {
        foreach (var building in Buildings.All)
        {
            if (building.Definition.IsShelter && building.IsActive)
                return building;
        }

        return null;
    }

    public WorkplaceId? FindFreeWorkplace(CharacterState character)
    {
        if (character.AssignedWorkplace.IsAssigned
            && Buildings.TryGet(character.AssignedWorkplace.Building, out var assigned)
            && assigned.IsActive
            && character.AssignedWorkplace.Slot < assigned.Workplaces.Count)
        {
            var slot = assigned.Workplaces[character.AssignedWorkplace.Slot];
            if (slot.IsFree || slot.Worker == character.Id)
                return character.AssignedWorkplace;
        }

        foreach (var building in Buildings.All)
        {
            if (!building.IsActive || building.Definition.WorkplaceCount == 0)
                continue;
            foreach (var slot in building.Workplaces)
            {
                if (!slot.IsFree && slot.Worker != character.Id)
                    continue;
                return new WorkplaceId(building.Id, slot.Slot);
            }
        }

        return null;
    }

    public void AssignWorkplace(CharacterState character, WorkplaceId workplace)
    {
        ReleaseWorkplace(character);
        if (!Buildings.TryGet(workplace.Building, out var building))
            return;
        if (workplace.Slot < 0 || workplace.Slot >= building.Workplaces.Count)
            return;
        building.Workplaces[workplace.Slot].Worker = character.Id;
        character.AssignedWorkplace = workplace;
    }

    public void ReleaseWorkplace(CharacterState character)
    {
        if (!character.AssignedWorkplace.IsAssigned)
            return;
        if (Buildings.TryGet(character.AssignedWorkplace.Building, out var building)
            && character.AssignedWorkplace.Slot < building.Workplaces.Count)
        {
            var slot = building.Workplaces[character.AssignedWorkplace.Slot];
            if (slot.Worker == character.Id)
                slot.Worker = CharacterId.None;
        }

        character.AssignedWorkplace = WorkplaceId.None;
    }

    public LogicalGridCoordinate? AccessFor(WorkplaceId workplace)
    {
        if (!Buildings.TryGet(workplace.Building, out var building))
            return null;
        if (workplace.Slot < 0 || workplace.Slot >= building.Workplaces.Count)
            return null;
        return building.Workplaces[workplace.Slot].AccessCell;
    }

    public BuildingState? BuildingAtAccess(LogicalGridCoordinate cell)
    {
        foreach (var building in Buildings.All)
        {
            if (building.AccessCell.Equals(cell))
                return building;
        }

        return null;
    }

    private void RouteToStorage(BuildingState source)
    {
        if (source.Definition.IsStorage)
            return;
        var storage = FindNearestStorage(source.Origin);
        if (storage is null)
            return;
        source.Inventory.TransferAllPossibleTo(storage.Inventory);
    }
}
