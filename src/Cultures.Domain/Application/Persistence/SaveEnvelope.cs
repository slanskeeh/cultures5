using System.Text.Json;
using System.Text.Json.Serialization;
using Cultures.Core.Time;

namespace Cultures.Application.Persistence;

/// <summary>
/// Versioned save envelope. Phase 0 stores only the foundation fields.
/// </summary>
public sealed record SaveEnvelope
{
    public const int CurrentVersion = 1;

    public int SaveVersion { get; init; } = CurrentVersion;
    public ulong WorldSeed { get; init; }
    public ulong SimulationTick { get; init; }
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
    public static SaveEnvelope FromClock(ulong worldSeed, SimulationClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        return new SaveEnvelope
        {
            SaveVersion = SaveEnvelope.CurrentVersion,
            WorldSeed = worldSeed,
            SimulationTick = clock.Tick
        };
    }
}
