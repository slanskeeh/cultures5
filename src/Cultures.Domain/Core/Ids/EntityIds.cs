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

public readonly record struct HouseholdId(ulong Value) : IEntityId
{
    public static HouseholdId None => new(0);
    public bool IsAssigned => Value != 0;
    public bool Equals(IEntityId? other) => other is HouseholdId id && Value == id.Value;
    public override string ToString() => $"Household:{Value}";
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

public readonly record struct CultureId(ulong Value) : IEntityId
{
    public static CultureId None => new(0);
    public static CultureId Neutral => new(1);
    public bool IsAssigned => Value != 0;
    public bool Equals(IEntityId? other) => other is CultureId id && Value == id.Value;
    public override string ToString() => $"Culture:{Value}";
}

public readonly record struct FactionId(ulong Value) : IEntityId
{
    public static FactionId None => new(0);
    public bool IsAssigned => Value != 0;
    public bool Equals(IEntityId? other) => other is FactionId id && Value == id.Value;
    public override string ToString() => $"Faction:{Value}";
}

public readonly record struct PoliticalGroupId(ulong Value) : IEntityId
{
    public static PoliticalGroupId None => new(0);
    public bool IsAssigned => Value != 0;
    public bool Equals(IEntityId? other) => other is PoliticalGroupId id && Value == id.Value;
    public override string ToString() => $"PoliticalGroup:{Value}";
}

public readonly record struct MilitaryUnitId(ulong Value) : IEntityId
{
    public static MilitaryUnitId None => new(0);
    public bool IsAssigned => Value != 0;
    public bool Equals(IEntityId? other) => other is MilitaryUnitId id && Value == id.Value;
    public override string ToString() => $"MilitaryUnit:{Value}";
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

public readonly record struct MigrationGroupId(ulong Value) : IEntityId
{
    public static MigrationGroupId None => new(0);
    public bool IsAssigned => Value != 0;
    public bool Equals(IEntityId? other) => other is MigrationGroupId id && Value == id.Value;
    public override string ToString() => $"MigrationGroup:{Value}";
}
