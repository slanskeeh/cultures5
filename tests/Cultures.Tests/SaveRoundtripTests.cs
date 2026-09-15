using Cultures.Application;
using Cultures.Application.Persistence;

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

        Assert.Equal(original, restored);
        Assert.Equal(SaveEnvelope.CurrentVersion, restored.SaveVersion);
        Assert.Equal(20260915UL, restored.WorldSeed);
        Assert.Equal(1234UL, restored.SimulationTick);
        Assert.Contains("\"saveVersion\"", json, StringComparison.Ordinal);
        Assert.Contains("\"worldSeed\"", json, StringComparison.Ordinal);
        Assert.Contains("\"simulationTick\"", json, StringComparison.Ordinal);
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
    }
}
