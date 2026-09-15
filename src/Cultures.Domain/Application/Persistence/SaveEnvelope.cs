using System.Text.Json;
using System.Text.Json.Serialization;
using Cultures.Core.Time;
using Cultures.World;

namespace Cultures.Application.Persistence;

/// <summary>
/// Versioned save envelope. Static terrain is reconstructed from seed + generation contract,
/// not stored cell-by-cell.
/// </summary>
public sealed record SaveEnvelope
{
    public const int CurrentVersion = 2;

    public int SaveVersion { get; init; } = CurrentVersion;
    public ulong WorldSeed { get; init; }
    public ulong SimulationTick { get; init; }
    public int GenerationVersion { get; init; } = WorldGeneration.CurrentVersion;
    public int WorldWidth { get; init; }
    public int WorldHeight { get; init; }
    public int ChunkWidth { get; init; }
    public int ChunkHeight { get; init; }
}

public sealed class SaveSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public string Serialize(SaveEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        return JsonSerializer.Serialize(envelope, Options);
    }

    public SaveEnvelope Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("Save JSON is empty.", nameof(json));

        var envelope = JsonSerializer.Deserialize<SaveEnvelope>(json, Options)
            ?? throw new InvalidOperationException("Save JSON deserialized to null.");

        if (envelope.SaveVersion <= 0)
            throw new InvalidOperationException("Save version must be positive.");

        return envelope;
    }
}

public static class SaveEnvelopeFactory
{
    public static SaveEnvelope FromHost(ulong worldSeed, SimulationClock clock, LogicalWorld world)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(world);
        var cfg = world.Configuration;
        return new SaveEnvelope
        {
            SaveVersion = SaveEnvelope.CurrentVersion,
            WorldSeed = worldSeed,
            SimulationTick = clock.Tick,
            GenerationVersion = cfg.GenerationVersion,
            WorldWidth = cfg.Width,
            WorldHeight = cfg.Height,
            ChunkWidth = cfg.ChunkWidth,
            ChunkHeight = cfg.ChunkHeight
        };
    }
}
