using Cultures.Application;
using Cultures.Application.Persistence;
using Cultures.World;

namespace Cultures.Tests;

public sealed class SaveRoundtripTests
{
    [Fact]
    public void Serialize_deserialize_compare_preserves_envelope()
    {
        var host = new SimulationHost(worldSeed: 20260915);
        host.Step(1234);

        var serializer = new SaveSerializer();
        var original = host.CreateSave();
        var json = serializer.Serialize(original);
        var restored = serializer.Deserialize(json);

        Assert.Equal(original.SaveVersion, restored.SaveVersion);
        Assert.Equal(original.WorldSeed, restored.WorldSeed);
        Assert.Equal(original.SimulationTick, restored.SimulationTick);
        Assert.Equal(original.Characters!.Count, restored.Characters!.Count);
        Assert.Equal(original.Buildings!.Count, restored.Buildings!.Count);
        Assert.Equal(original.History!.Count, restored.History!.Count);
        Assert.Equal(SaveEnvelope.CurrentVersion, restored.SaveVersion);
        Assert.Equal(20260915UL, restored.WorldSeed);
        Assert.Equal(1234UL, restored.SimulationTick);
        Assert.Equal(WorldGeneration.CurrentVersion, restored.GenerationVersion);
        Assert.Equal(100, restored.WorldWidth);
        Assert.Contains("\"saveVersion\"", json, StringComparison.Ordinal);
        Assert.Contains("\"worldSeed\"", json, StringComparison.Ordinal);
        Assert.Contains("\"simulationTick\"", json, StringComparison.Ordinal);
        Assert.Contains("\"generationVersion\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_can_resume_from_save_without_a_scene()
    {
        var original = new SimulationHost(worldSeed: 9);
        original.Step(40);
        var envelope = original.CreateSave();

        var resumed = SimulationHost.FromSave(envelope);
        Assert.Equal(original.WorldSeed, resumed.WorldSeed);
        Assert.Equal(original.Clock.Tick, resumed.Clock.Tick);

        resumed.Step(2);
        Assert.Equal(42UL, resumed.Clock.Tick);
        Assert.Equal(
            original.World.Grid.GetCell(new LogicalGridCoordinate(12, 8)).Generated,
            resumed.World.Grid.GetCell(new LogicalGridCoordinate(12, 8)).Generated);
    }
}
