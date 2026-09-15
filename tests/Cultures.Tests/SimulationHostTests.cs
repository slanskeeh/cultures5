using Cultures.Application;

namespace Cultures.Tests;

public sealed class SimulationHostTests
{
    [Fact]
    public void Headless_host_advances_without_rendering()
    {
        var host = new SimulationHost(worldSeed: 3);
        Assert.Equal(10UL, host.Step(10));
        Assert.Equal(10UL, host.Clock.Tick);
        Assert.Equal(3UL, host.WorldSeed);
    }

    [Fact]
    public void Paused_host_does_not_advance()
    {
        var host = new SimulationHost(worldSeed: 3);
        host.Clock.Pause();
        Assert.Equal(0UL, host.Step(99));
        Assert.Equal(0UL, host.Clock.Tick);
    }
}
