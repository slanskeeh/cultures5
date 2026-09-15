using Cultures.Application;
using Cultures.Buildings;
using Cultures.Core.Events;
using Cultures.Core.Time;
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
}
