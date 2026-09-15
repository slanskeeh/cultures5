using Cultures.Buildings;
using Cultures.Core.Events;
using Cultures.Core.Ids;
using Cultures.Core.Time;
using Cultures.Economy;
using Cultures.Population;
using Cultures.World;

namespace Cultures.Settlement;

/// <summary>
/// Detects emergent settlements from co-located people and infrastructure.
/// Chunk connected-components with X wrap; not an O(N²) pairwise scan.
/// </summary>
public sealed class SettlementSystem
{
    private int _ticksSinceEvaluation;

    public SettlementSystem(
        LogicalWorld world,
        PopulationRoster population,
        BuildingDirectory buildings,
        SettlementDirectory settlements,
        EntityIdFactory ids,
        EventBus events,
        SimulationClock clock,
        RecipeCatalog? recipes = null)
    {
        World = world ?? throw new ArgumentNullException(nameof(world));
        Population = population ?? throw new ArgumentNullException(nameof(population));
        Buildings = buildings ?? throw new ArgumentNullException(nameof(buildings));
        Settlements = settlements ?? throw new ArgumentNullException(nameof(settlements));
        Ids = ids ?? throw new ArgumentNullException(nameof(ids));
        Events = events ?? throw new ArgumentNullException(nameof(events));
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
        Recipes = recipes ?? RecipeCatalog.Development;
    }

    public LogicalWorld World { get; }
    public PopulationRoster Population { get; }
    public BuildingDirectory Buildings { get; }
    public SettlementDirectory Settlements { get; }
    public EntityIdFactory Ids { get; }
    public EventBus Events { get; }
    public SimulationClock Clock { get; }
    public RecipeCatalog Recipes { get; }

    public void Tick()
    {
        _ticksSinceEvaluation++;
        if (_ticksSinceEvaluation < SettlementRules.EvaluationIntervalTicks)
            return;

        _ticksSinceEvaluation = 0;
        Evaluate();
    }

    public void Evaluate()
    {
        _ticksSinceEvaluation = 0;
        var clusters = DiscoverClusters();
        clusters.Sort(CompareClusters);

        var live = Settlements.Active.ToList();
        var claimed = new HashSet<ulong>();
        var usedClusters = new HashSet<int>();

        foreach (var settlement in live.OrderBy(s => s.Id.Value))
        {
            var bestIndex = -1;
            var bestScore = 0;
            for (var i = 0; i < clusters.Count; i++)
            {
                if (usedClusters.Contains(i))
                    continue;
                var score = Overlap(settlement, clusters[i]);
                if (score < bestScore)
                    continue;
                if (score == bestScore && bestIndex >= 0 && CompareClusters(clusters[i], clusters[bestIndex]) >= 0)
                    continue;
                if (score == 0)
                    continue;
                bestScore = score;
                bestIndex = i;
            }

            if (bestIndex < 0)
            {
                Miss(settlement, remnant: null);
                continue;
            }

            usedClusters.Add(bestIndex);
            claimed.Add(settlement.Id.Value);
            var cluster = clusters[bestIndex];
            if (cluster.Qualified)
                Hit(settlement, cluster);
            else
                Miss(settlement, cluster);
        }

        for (var i = 0; i < clusters.Count; i++)
        {
            if (usedClusters.Contains(i) || !clusters[i].Qualified)
                continue;
            Create(clusters[i]);
        }

        ClearUnassignedMembership();
    }

    private List<OccupancyCluster> DiscoverClusters()
    {
        var occupied = new HashSet<ChunkCoordinate>();
        foreach (var character in Population.Alive)
            occupied.Add(ChunkOf(character.Position));
        foreach (var building in Buildings.All)
        {
            if (!building.IsActive)
                continue;
            foreach (var cell in building.FootprintCells)
                occupied.Add(ChunkOf(cell));
        }

        var remaining = new HashSet<ChunkCoordinate>(occupied);
        var clusters = new List<OccupancyCluster>();
        while (remaining.Count > 0)
        {
            var start = remaining.OrderBy(c => c.Y).ThenBy(c => c.X).First();
            var component = new HashSet<ChunkCoordinate>();
            var queue = new Queue<ChunkCoordinate>();
            queue.Enqueue(start);
            remaining.Remove(start);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                component.Add(current);
                foreach (var neighbor in ChunkNeighbors(current))
                {
                    if (!remaining.Remove(neighbor))
                        continue;
                    queue.Enqueue(neighbor);
                }
            }

            clusters.Add(BuildCluster(component));
        }

        return clusters;
    }

    private OccupancyCluster BuildCluster(HashSet<ChunkCoordinate> chunks)
    {
        var people = new List<CharacterState>();
        foreach (var character in Population.Alive.OrderBy(c => c.Id.Value))
        {
            if (chunks.Contains(ChunkOf(character.Position)))
                people.Add(character);
        }

        var buildings = new List<BuildingState>();
        foreach (var building in Buildings.All.OrderBy(b => b.Id.Value))
        {
            if (!building.IsActive)
                continue;
            if (building.FootprintCells.Any(cell => chunks.Contains(ChunkOf(cell))))
                buildings.Add(building);
        }

        var hasShelter = buildings.Any(b => b.Definition.IsShelter);
        var hasStorage = buildings.Any(b => b.Definition.IsStorage);
        var qualified = people.Count >= SettlementRules.MinPeople
            && buildings.Count >= SettlementRules.MinBuildings
            && hasShelter
            && hasStorage;

        return new OccupancyCluster(chunks, people, buildings, qualified);
    }

    private int Overlap(SettlementState settlement, OccupancyCluster cluster)
    {
        var people = cluster.People.Count(c => c.Settlement == settlement.Id);
        var buildings = cluster.Buildings.Count(b => b.AssociatedSettlement == settlement.Id);
        return people + buildings;
    }

    private void Hit(SettlementState settlement, OccupancyCluster cluster)
    {
        settlement.PresenceEvals++;
        settlement.UnmatchedEvals = 0;
        ApplyCluster(settlement, cluster);
        if (settlement.Lifecycle == SettlementLifecycle.Emerging
            && settlement.PresenceEvals >= SettlementRules.EstablishedAfterEvals)
        {
            ChangeStage(settlement, SettlementLifecycle.Established);
        }
        else if (settlement.Lifecycle == SettlementLifecycle.Declining)
        {
            ChangeStage(settlement, SettlementLifecycle.Established);
        }
    }

    private void Miss(SettlementState settlement, OccupancyCluster? remnant)
    {
        settlement.UnmatchedEvals++;
        if (remnant is not null)
        {
            ApplyCluster(settlement, remnant);
        }
        else
        {
            foreach (var character in Population.Alive)
            {
                if (character.Settlement == settlement.Id)
                    character.Settlement = SettlementId.None;
            }

            settlement.Statistics.Clear();
        }

        if (settlement.Lifecycle == SettlementLifecycle.Established
            && settlement.UnmatchedEvals >= SettlementRules.DeclineAfterUnmatchedEvals)
        {
            ChangeStage(settlement, SettlementLifecycle.Declining);
        }

        if (settlement.UnmatchedEvals >= SettlementRules.AbandonAfterUnmatchedEvals
            && settlement.Lifecycle != SettlementLifecycle.Abandoned)
        {
            Abandon(settlement);
        }
    }

    private void Create(OccupancyCluster cluster)
    {
        var id = Ids.NextSettlement();
        var settlement = new SettlementState(id, SettlementNames.Key(World.Seed, id), Clock.Tick)
        {
            PresenceEvals = 1
        };
        Settlements.Add(settlement);
        ApplyCluster(settlement, cluster);
        Events.Publish(new SettlementEmergedEvent(Clock.Tick, settlement.Id, settlement.Core));
    }

    private void Abandon(SettlementState settlement)
    {
        foreach (var character in Population.Alive)
        {
            if (character.Settlement == settlement.Id)
                character.Settlement = SettlementId.None;
        }

        settlement.Statistics.Population = 0;
        settlement.Statistics.Infants = 0;
        settlement.Statistics.Children = 0;
        settlement.Statistics.Adolescents = 0;
        settlement.Statistics.Adults = 0;
        settlement.Statistics.Elders = 0;
        settlement.Statistics.Workers = 0;
        settlement.Statistics.UnemployedAdults = 0;
        ChangeStage(settlement, SettlementLifecycle.Abandoned);
        Events.Publish(new SettlementAbandonedEvent(Clock.Tick, settlement.Id));
    }

    private void ApplyCluster(SettlementState settlement, OccupancyCluster cluster)
    {
        settlement.Core = ComputeCore(cluster);
        foreach (var character in cluster.People)
            character.Settlement = settlement.Id;
        foreach (var building in cluster.Buildings)
            building.AssociatedSettlement = settlement.Id;
        FillStatistics(settlement, cluster);
    }

    private void ClearUnassignedMembership()
    {
        var claimed = new HashSet<ulong>();
        foreach (var settlement in Settlements.Active)
            claimed.Add(settlement.Id.Value);

        foreach (var character in Population.Alive)
        {
            if (character.Settlement.IsAssigned && !claimed.Contains(character.Settlement.Value))
                character.Settlement = SettlementId.None;
        }
    }

    private void FillStatistics(SettlementState settlement, OccupancyCluster cluster)
    {
        var stats = settlement.Statistics;
        stats.Clear();
        foreach (var character in cluster.People)
        {
            stats.Population++;
            switch (character.LifeStage)
            {
                case CharacterLifeStage.Infant:
                    stats.Infants++;
                    break;
                case CharacterLifeStage.Child:
                    stats.Children++;
                    break;
                case CharacterLifeStage.Adolescent:
                    stats.Adolescents++;
                    break;
                case CharacterLifeStage.Adult:
                    stats.Adults++;
                    break;
                case CharacterLifeStage.Elder:
                    stats.Elders++;
                    break;
            }

            var canWork = SkillRules.CanWork(character);
            var working = character.AssignedWorkplace.IsAssigned
                && cluster.Buildings.Any(b => b.Id == character.AssignedWorkplace.Building);
            if (working)
                stats.Workers++;
            else if (canWork)
                stats.UnemployedAdults++;
        }

        foreach (var building in cluster.Buildings)
        {
            stats.ActiveBuildings++;
            if (building.Definition.IsShelter)
            {
                stats.Shelters++;
                stats.ShelterCapacity++;
            }

            if (building.Definition.IsStorage)
            {
                stats.StorageBuildings++;
                stats.FoodStored += building.Inventory.GetQuantity(ResourceType.Food);
            }

            if (building.Definition.Recipe is { } recipeId
                && Recipes.TryGet(recipeId, out var recipe)
                && building.Workplaces.Any(slot => slot.Worker.IsAssigned))
            {
                foreach (var output in recipe.Outputs)
                {
                    if (output.Type == ResourceType.Food)
                        stats.EstimatedFoodProduction += output.Quantity;
                }
            }
        }
    }

    private LogicalGridCoordinate ComputeCore(OccupancyCluster cluster)
    {
        var points = new List<LogicalGridCoordinate>(cluster.People.Count + cluster.Buildings.Count);
        foreach (var character in cluster.People)
            points.Add(character.Position);
        foreach (var building in cluster.Buildings)
            points.Add(building.Origin);
        if (points.Count == 0)
            return default;

        var originX = points[0].X;
        long sumDx = 0;
        long sumY = 0;
        foreach (var point in points)
        {
            sumDx += World.Topology.SignedHorizontalDelta(originX, point.X);
            sumY += point.Y;
        }

        var x = World.Topology.WrapX(originX + (int)Math.Round(sumDx / (double)points.Count));
        var y = (int)Math.Clamp(
            (int)Math.Round(sumY / (double)points.Count),
            0,
            World.Configuration.Height - 1);
        return new LogicalGridCoordinate(x, y);
    }

    private void ChangeStage(SettlementState settlement, SettlementLifecycle next)
    {
        if (settlement.Lifecycle == next)
            return;
        var previous = settlement.Lifecycle;
        settlement.Lifecycle = next;
        Events.Publish(new SettlementStageChangedEvent(Clock.Tick, settlement.Id, previous, next));
    }

    private ChunkCoordinate ChunkOf(LogicalGridCoordinate cell) => World.Chunks.ToAddress(cell).Chunk;

    private IEnumerable<ChunkCoordinate> ChunkNeighbors(ChunkCoordinate chunk)
    {
        var countX = World.Configuration.ChunkCountX;
        var countY = World.Configuration.ChunkCountY;
        yield return new ChunkCoordinate(WorldTopology.EuclideanMod(chunk.X + 1, countX), chunk.Y);
        yield return new ChunkCoordinate(WorldTopology.EuclideanMod(chunk.X - 1, countX), chunk.Y);
        if (chunk.Y > 0)
            yield return new ChunkCoordinate(chunk.X, chunk.Y - 1);
        if (chunk.Y + 1 < countY)
            yield return new ChunkCoordinate(chunk.X, chunk.Y + 1);
    }

    private static int CompareClusters(OccupancyCluster a, OccupancyCluster b)
    {
        var people = a.MinPersonId.CompareTo(b.MinPersonId);
        if (people != 0)
            return people;
        return a.MinBuildingId.CompareTo(b.MinBuildingId);
    }

    private sealed class OccupancyCluster
    {
        public OccupancyCluster(
            HashSet<ChunkCoordinate> chunks,
            List<CharacterState> people,
            List<BuildingState> buildings,
            bool qualified)
        {
            Chunks = chunks;
            People = people;
            Buildings = buildings;
            Qualified = qualified;
            MinPersonId = people.Count == 0 ? ulong.MaxValue : people.Min(c => c.Id.Value);
            MinBuildingId = buildings.Count == 0 ? ulong.MaxValue : buildings.Min(b => b.Id.Value);
        }

        public HashSet<ChunkCoordinate> Chunks { get; }
        public List<CharacterState> People { get; }
        public List<BuildingState> Buildings { get; }
        public bool Qualified { get; }
        public ulong MinPersonId { get; }
        public ulong MinBuildingId { get; }
    }
}
