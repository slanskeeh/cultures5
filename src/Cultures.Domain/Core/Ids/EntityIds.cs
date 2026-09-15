namespace Cultures.Core.Ids;

/// <summary>
/// Persistent identity independent of array index, scene presence, or load order.
/// </summary>
public interface IEntityId : IEquatable<IEntityId>
{
    ulong Value { get; }
    bool IsAssigned { get; }
}

public readonly record struct CharacterId(ulong Value) : IEntityId
{
    public static CharacterId None => new(0);
    public bool IsAssigned => Value != 0;
    public bool Equals(IEntityId? other) => other is CharacterId id && Value == id.Value;
    public override string ToString() => $"Character:{Value}";
}

public readonly record struct FamilyId(ulong Value) : IEntityId
{
    public static FamilyId None => new(0);
    public bool IsAssigned => Value != 0;
    public bool Equals(IEntityId? other) => other is FamilyId id && Value == id.Value;
    public override string ToString() => $"Family:{Value}";
}

public readonly record struct BuildingId(ulong Value) : IEntityId
{
    public static BuildingId None => new(0);
    public bool IsAssigned => Value != 0;
    public bool Equals(IEntityId? other) => other is BuildingId id && Value == id.Value;
    public override string ToString() => $"Building:{Value}";
}

public readonly record struct SettlementId(ulong Value) : IEntityId
{
    public static SettlementId None => new(0);
    public bool IsAssigned => Value != 0;
    public bool Equals(IEntityId? other) => other is SettlementId id && Value == id.Value;
    public override string ToString() => $"Settlement:{Value}";
}

public readonly record struct CivilizationId(ulong Value) : IEntityId
{
    public static CivilizationId None => new(0);
    public bool IsAssigned => Value != 0;
    public bool Equals(IEntityId? other) => other is CivilizationId id && Value == id.Value;
    public override string ToString() => $"Civilization:{Value}";
}

public readonly record struct RegionId(ulong Value) : IEntityId
{
    public static RegionId None => new(0);
    public bool IsAssigned => Value != 0;
    public bool Equals(IEntityId? other) => other is RegionId id && Value == id.Value;
    public override string ToString() => $"Region:{Value}";
}

public readonly record struct ChunkId(ulong Value) : IEntityId
{
    public static ChunkId None => new(0);
    public bool IsAssigned => Value != 0;
    public bool Equals(IEntityId? other) => other is ChunkId id && Value == id.Value;
    public override string ToString() => $"Chunk:{Value}";
}
