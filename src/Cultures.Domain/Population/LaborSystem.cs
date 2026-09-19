using Cultures.Buildings;
using Cultures.Core.Ids;
using Cultures.Economy;
using Cultures.Exploration;
using Cultures.World;

namespace Cultures.Population;

public readonly record struct LaborPlan(
    ActionKind Kind,
    int DurationTicks,
    LogicalGridCoordinate? Target,
    ResourceType? Resource,
    BuildingId Building);

/// <summary>
/// Field labor for starting professions. Not a workplace recipe loop.
/// </summary>
public sealed class LaborSystem
{
    public LaborSystem(
        LogicalWorld world,
        PopulationRoster population,
        ProductionSystem production,
        NaturalResourceSystem ecology,
        ExplorationSystem exploration,
        ProfessionCatalog professions)
    {
        World = world ?? throw new ArgumentNullException(nameof(world));
        Population = population ?? throw new ArgumentNullException(nameof(population));
        Production = production ?? throw new ArgumentNullException(nameof(production));
        Ecology = ecology ?? throw new ArgumentNullException(nameof(ecology));
        Exploration = exploration ?? throw new ArgumentNullException(nameof(exploration));
        Professions = professions ?? throw new ArgumentNullException(nameof(professions));
        Navigator = new GridNavigator(world);
    }

    public LogicalWorld World { get; }
    public PopulationRoster Population { get; }
    public ProductionSystem Production { get; }
    public NaturalResourceSystem Ecology { get; }
    public ExplorationSystem Exploration { get; }
    public ProfessionCatalog Professions { get; }
    public GridNavigator Navigator { get; }

    public bool TryPlan(CharacterState character, out LaborPlan plan)
    {
        plan = default;
        if (!SkillRules.CanWork(character))
            return false;
        if (!Professions.TryGet(character.Profession, out var definition))
            return false;
        if (definition.Labor is LaborKind.None or LaborKind.Workplace)
            return false;

        return definition.Labor switch
        {
            LaborKind.Haul => TryPlanHaul(character, out plan),
            LaborKind.Build => TryPlanBuild(character, out plan),
            LaborKind.Scout => TryPlanScout(character, out plan),
            LaborKind.Hunt => TryPlanHunt(character, out plan),
            LaborKind.Fish => TryPlanGather(character, ResourceType.Fish, out plan),
            LaborKind.Gather when definition.FieldResource is { } resource =>
                TryPlanGather(character, resource, out plan),
            _ => false
        };
    }

    public void Complete(CharacterState character)
    {
        switch (character.Activity.Kind)
        {
            case ActionKind.Gather:
                CompleteGather(character);
                break;
            case ActionKind.Hunt:
                CompleteHunt(character);
                break;
            case ActionKind.Haul:
                CompleteHaul(character);
                break;
            case ActionKind.Construct:
                CompleteConstruct(character);
                break;
            case ActionKind.Scout:
                CompleteScout(character);
                break;
        }
    }

    public static bool NeedsMaterials(BuildingState building)
    {
        foreach (var cost in building.Definition.ConstructionCost)
        {
            if (!building.Inventory.Has(cost.Type, cost.Quantity))
                return true;
        }

        return false;
    }

    public static ResourceType? FirstMissing(BuildingState building)
    {
        foreach (var cost in building.Definition.ConstructionCost)
        {
            var have = building.Inventory.GetQuantity(cost.Type);
            if (have < cost.Quantity)
                return cost.Type;
        }

        return null;
    }

    private bool TryPlanGather(CharacterState character, ResourceType resource, out LaborPlan plan)
    {
        if (character.Inventory.GetQuantity(resource) >= CharacterRules.HaulWhenCarrying
            || character.Inventory.TotalQuantity >= CharacterRules.PersonalInventoryCapacity - 1)
        {
            return TryPlanHaul(character, out plan);
        }

        if (IsGatherCell(character.Position, resource))
        {
            plan = new LaborPlan(
                ActionKind.Gather,
                CharacterRules.GatherDurationTicks,
                character.Position,
                resource,
                default);
            return true;
        }

        if (TryFindGatherCell(character.Position, resource, out var cell))
        {
            plan = MovePlan(cell, resource);
            return true;
        }

        plan = default;
        return false;
    }

    private bool TryPlanHunt(CharacterState character, out LaborPlan plan)
    {
        if (character.Inventory.GetQuantity(ResourceType.Meat) >= CharacterRules.HaulWhenCarrying)
            return TryPlanHaul(character, out plan);

        Ecology.EnsureCell(character.Position);
        var chunk = World.Chunks.ToAddress(character.Position).Chunk;
        if (Ecology.Wildlife.TryGet(chunk, out var here) && here.Total > 0)
        {
            plan = new LaborPlan(
                ActionKind.Hunt,
                CharacterRules.HuntDurationTicks,
                character.Position,
                ResourceType.Meat,
                default);
            return true;
        }

        if (TryFindWildlifeCell(character.Position, out var cell))
        {
            plan = MovePlan(cell, ResourceType.Meat);
            return true;
        }

        plan = default;
        return false;
    }

    private bool TryPlanBuild(CharacterState character, out LaborPlan plan)
    {
        var site = FindConstructionSite();
        if (site is null)
        {
            plan = default;
            return false;
        }

        var missing = FirstMissing(site);
        if (missing is { } need)
        {
            if (character.Inventory.Has(need, 1))
            {
                if (character.Position.Equals(site.AccessCell))
                    return HaulHere(character, site, out plan);
                plan = MovePlan(site.AccessCell, need, site.Id);
                return true;
            }

            var storage = Production.FindStorageWith(need, 1);
            if (storage is not null)
            {
                if (character.Position.Equals(storage.AccessCell))
                    return HaulHere(character, storage, out plan, need);
                plan = MovePlan(storage.AccessCell, need, storage.Id);
                return true;
            }
        }

        if (character.Position.Equals(site.AccessCell))
        {
            plan = new LaborPlan(
                ActionKind.Construct,
                CharacterRules.ConstructDurationTicks,
                site.AccessCell,
                null,
                site.Id);
            return true;
        }

        plan = MovePlan(site.AccessCell, null, site.Id);
        return true;
    }

    private bool TryPlanScout(CharacterState character, out LaborPlan plan)
    {
        var here = World.Chunks.ToAddress(character.Position).Chunk;
        if (Exploration.LevelOf(here) < ExplorationKnowledgeLevel.Mapped)
        {
            plan = new LaborPlan(
                ActionKind.Scout,
                CharacterRules.ScoutDurationTicks,
                character.Position,
                null,
                default);
            return true;
        }

        if (TryFindUnmappedCell(character.Position, out var cell))
        {
            plan = MovePlan(cell);
            return true;
        }

        plan = default;
        return false;
    }

    private bool TryPlanHaul(CharacterState character, out LaborPlan plan)
    {
        if (character.Inventory.TotalQuantity > 0)
        {
            var site = FindConstructionSite();
            if (site is not null && FirstMissing(site) is { } need && character.Inventory.Has(need, 1))
            {
                if (character.Position.Equals(site.AccessCell))
                    return HaulHere(character, site, out plan);
                plan = MovePlan(site.AccessCell, need, site.Id);
                return true;
            }

            var storage = Production.FindNearestStorage(character.Position);
            if (storage is not null)
            {
                if (character.Position.Equals(storage.AccessCell))
                    return HaulHere(character, storage, out plan);
                plan = MovePlan(storage.AccessCell, null, storage.Id);
                return true;
            }
        }

        var pickupSite = FindConstructionSite();
        if (pickupSite is not null && FirstMissing(pickupSite) is { } missing)
        {
            var storage = Production.FindStorageWith(missing, 1);
            if (storage is not null)
            {
                if (character.Position.Equals(storage.AccessCell))
                    return HaulHere(character, storage, out plan, missing);
                plan = MovePlan(storage.AccessCell, missing, storage.Id);
                return true;
            }
        }

        var producer = FindProducerWithGoods();
        if (producer is not null)
        {
            if (character.Position.Equals(producer.AccessCell))
                return HaulHere(character, producer, out plan);
            plan = MovePlan(producer.AccessCell, null, producer.Id);
            return true;
        }

        plan = default;
        return false;
    }

    private bool HaulHere(
        CharacterState character,
        BuildingState building,
        out LaborPlan plan,
        ResourceType? resource = null)
    {
        plan = new LaborPlan(
            ActionKind.Haul,
            CharacterRules.HaulDurationTicks,
            character.Position,
            resource,
            building.Id);
        return true;
    }

    private static LaborPlan MovePlan(
        LogicalGridCoordinate target,
        ResourceType? resource = null,
        BuildingId building = default) =>
        new(ActionKind.Move, 1, target, resource, building);

    private void CompleteGather(CharacterState character)
    {
        var resource = character.Activity.JobResource ?? ResourceType.Wood;
        var cell = character.Activity.Target ?? character.Position;
        if (!Ecology.TryExtract(cell, resource, 1))
            return;
        character.Inventory.TryAdd(resource, 1);
    }

    private void CompleteHunt(CharacterState character)
    {
        if (!Ecology.TryHunt(character.Position, out _, out _))
            return;
        character.Inventory.TryAdd(ResourceType.Meat, 1);
    }

    private void CompleteHaul(CharacterState character)
    {
        if (!Production.Buildings.TryGet(character.Activity.JobBuilding, out var building)
            && Production.BuildingAtAccess(character.Position) is { } here)
        {
            building = here;
        }

        if (building is null)
            return;

        if (building.Definition.IsStorage || building.Lifecycle is BuildingLifecycle.Constructing or BuildingLifecycle.Planned)
        {
            character.Inventory.TransferAllPossibleTo(building.Inventory);
            return;
        }

        building.Inventory.TransferAllPossibleTo(character.Inventory);
    }

    private void CompleteConstruct(CharacterState character)
    {
        if (!Production.Buildings.TryGet(character.Activity.JobBuilding, out var building))
            return;
        if (building.Lifecycle is not (BuildingLifecycle.Planned or BuildingLifecycle.Constructing))
            return;

        building.Lifecycle = BuildingLifecycle.Constructing;
        if (building.ConstructionProgress < building.Definition.ConstructionTicks)
            building.ConstructionProgress++;

        if (building.ConstructionProgress < building.Definition.ConstructionTicks)
            return;
        if (NeedsMaterials(building))
            return;

        foreach (var cost in building.Definition.ConstructionCost)
        {
            if (!building.Inventory.TryRemove(cost.Type, cost.Quantity))
                return;
        }

        building.Lifecycle = BuildingLifecycle.Active;
    }

    private void CompleteScout(CharacterState character)
    {
        var chunk = World.Chunks.ToAddress(character.Position).Chunk;
        var level = Exploration.LevelOf(chunk);
        var target = level < ExplorationKnowledgeLevel.Scouted
            ? ExplorationKnowledgeLevel.Scouted
            : ExplorationKnowledgeLevel.Mapped;
        Exploration.TryAdvance(chunk, target, out _);
    }

    private bool IsGatherCell(LogicalGridCoordinate cell, ResourceType resource)
    {
        if (!World.Topology.TryGetCell(cell.ToWorld(), out _))
            return false;
        var terrain = World.Grid.GetCell(cell);
        if (!terrain.Generated.Passable || terrain.Occupancy.BlocksMovement)
            return false;
        if (resource == ResourceType.Fish)
            return !terrain.IsWater && HasWaterNeighbor(cell);

        Ecology.EnsureCell(cell);
        if (Ecology.Deposits.TryGetAt(cell, resource, out var deposit) && deposit.Stock > 0)
            return true;
        return resource == ResourceType.Mushrooms
            && terrain.Biome is BiomeId.Forest or BiomeId.Taiga or BiomeId.Swamp;
    }

    private bool TryFindGatherCell(LogicalGridCoordinate origin, ResourceType resource, out LogicalGridCoordinate cell)
    {
        cell = origin;
        for (var radius = 1; radius <= CharacterRules.LaborSearchRadius; radius++)
        {
            for (var dx = -radius; dx <= radius; dx++)
            {
                for (var dy = -radius; dy <= radius; dy++)
                {
                    if (Math.Abs(dx) != radius && Math.Abs(dy) != radius)
                        continue;
                    var resolution = World.Topology.Resolve(origin.X + dx, origin.Y + dy);
                    if (!resolution.TryGetCell(out var candidate))
                        continue;
                    if (IsGatherCell(candidate, resource))
                    {
                        cell = candidate;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool TryFindWildlifeCell(LogicalGridCoordinate origin, out LogicalGridCoordinate cell)
    {
        cell = origin;
        var originChunk = World.Chunks.ToAddress(origin).Chunk;
        for (var dy = -2; dy <= 2; dy++)
        {
            for (var dx = -2; dx <= 2; dx++)
            {
                var chunk = new ChunkCoordinate(
                    WorldTopology.EuclideanMod(originChunk.X + dx, World.Configuration.ChunkCountX),
                    originChunk.Y + dy);
                if (chunk.Y < 0 || chunk.Y >= World.Configuration.ChunkCountY)
                    continue;
                Ecology.EnsureCell(new LogicalGridCoordinate(
                    chunk.X * World.Configuration.ChunkWidth + World.Configuration.ChunkWidth / 2,
                    Math.Clamp(
                        chunk.Y * World.Configuration.ChunkHeight + World.Configuration.ChunkHeight / 2,
                        0,
                        World.Configuration.Height - 1)));
                if (!Ecology.Wildlife.TryGet(chunk, out var wildlife) || wildlife.Total <= 0)
                    continue;
                if (!World.Chunks.TryToCell(chunk, new ChunkLocalCoordinate(0, 0), out var candidate))
                    continue;
                if (Navigator.IsPassable(candidate))
                {
                    cell = candidate;
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryFindUnmappedCell(LogicalGridCoordinate origin, out LogicalGridCoordinate cell)
    {
        cell = origin;
        var originChunk = World.Chunks.ToAddress(origin).Chunk;
        for (var radius = 1; radius <= 4; radius++)
        {
            for (var dy = -radius; dy <= radius; dy++)
            {
                for (var dx = -radius; dx <= radius; dx++)
                {
                    if (Math.Abs(dx) != radius && Math.Abs(dy) != radius)
                        continue;
                    var chunk = new ChunkCoordinate(
                        WorldTopology.EuclideanMod(originChunk.X + dx, World.Configuration.ChunkCountX),
                        originChunk.Y + dy);
                    if (chunk.Y < 0 || chunk.Y >= World.Configuration.ChunkCountY)
                        continue;
                    if (Exploration.LevelOf(chunk) >= ExplorationKnowledgeLevel.Mapped)
                        continue;
                    if (!World.Chunks.TryToCell(
                            chunk,
                            new ChunkLocalCoordinate(World.Configuration.ChunkWidth / 2, World.Configuration.ChunkHeight / 2),
                            out var candidate))
                        continue;
                    if (Navigator.IsPassable(candidate) || World.Grid.GetCell(candidate).Generated.Passable)
                    {
                        cell = candidate;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private BuildingState? FindConstructionSite()
    {
        foreach (var building in Production.Buildings.All)
        {
            if (building.Lifecycle is BuildingLifecycle.Planned or BuildingLifecycle.Constructing)
                return building;
        }

        return null;
    }

    private BuildingState? FindProducerWithGoods()
    {
        foreach (var building in Production.Buildings.All)
        {
            if (!building.IsActive || building.Definition.IsStorage)
                continue;
            if (building.Inventory.TotalQuantity > 0)
                return building;
        }

        return null;
    }

    private bool HasWaterNeighbor(LogicalGridCoordinate cell)
    {
        foreach (var neighbor in World.Topology.HexNeighbors(cell))
        {
            if (World.Grid.GetCell(neighbor).IsWater)
                return true;
        }

        return false;
    }
}
