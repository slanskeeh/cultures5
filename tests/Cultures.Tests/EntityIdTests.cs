using System.Text.Json;
using Cultures.Core.Ids;

namespace Cultures.Tests;

public sealed class EntityIdTests
{
    [Fact]
    public void Equal_ids_are_equal_regardless_of_array_index()
    {
        var stored = new[] { new CharacterId(7), new CharacterId(8), new CharacterId(9) };
        var loaded = new CharacterId(8);

        Assert.NotEqual(1, (int)loaded.Value);
        Assert.Equal(stored[1], loaded);
        Assert.True(stored[1] == loaded);
    }

    [Fact]
    public void Different_values_are_not_equal()
    {
        Assert.NotEqual(new CharacterId(1), new CharacterId(2));
        Assert.NotEqual(new FamilyId(1), new FamilyId(2));
    }

    [Fact]
    public void Same_numeric_value_is_not_equal_across_id_kinds()
    {
        IEntityId character = new CharacterId(4);
        IEntityId family = new FamilyId(4);

        Assert.False(character.Equals(family));
        Assert.NotEqual(character.ToString(), family.ToString());
    }

    [Fact]
    public void Factory_issues_stable_monotonic_ids_not_indexes()
    {
        var factory = new EntityIdFactory();
        var first = factory.NextCharacter();
        var second = factory.NextCharacter();
        var discarded = new CharacterId[] { first, second };

        Assert.Equal(1UL, first.Value);
        Assert.Equal(2UL, second.Value);
        Assert.NotEqual((ulong)Array.IndexOf(discarded, second), second.Value);
    }

    [Fact]
    public void Ids_roundtrip_through_json()
    {
        var original = new CharacterId(1842);
        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<CharacterId>(json);

        Assert.Equal(original, restored);
        Assert.Contains("1842", json, StringComparison.Ordinal);
    }

    [Fact]
    public void None_is_unassigned()
    {
        Assert.False(CharacterId.None.IsAssigned);
        Assert.True(new SettlementId(1).IsAssigned);
    }
}
