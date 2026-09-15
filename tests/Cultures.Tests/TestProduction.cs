using Cultures.Application;
using Cultures.Buildings;
using Cultures.Core.Events;
using Cultures.Core.Time;
using Cultures.Population;
using Cultures.World;

namespace Cultures.Tests;

internal static class TestProduction
{
    public static ProductionSystem ForWorld(LogicalWorld world, BuildingDirectory? buildings = null)
    {
        buildings ??= new BuildingDirectory();
        return new ProductionSystem(
            world,
            buildings,
            new ProductionResolver(RecipeCatalog.Development, new NeutralEnvironmentProductionModifier()),
            new EventBus(),
            new SimulationClock());
    }

    public static TeachingSystem Teaching(LogicalWorld world, PopulationRoster? population = null) =>
        new(population ?? new PopulationRoster(), world, new EventBus(), new SimulationClock());
}
