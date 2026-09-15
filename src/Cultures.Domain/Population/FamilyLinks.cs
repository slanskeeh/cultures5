using Cultures.Core.Ids;

namespace Cultures.Population;

/// <summary>
/// Persistent parent/child links. Not a household and not a FamilyManager.
/// </summary>
public sealed class FamilyLinks
{
    private readonly List<CharacterId> _parents = new();
    private readonly List<CharacterId> _children = new();
    private readonly List<CharacterId> _caregivers = new();

    public IReadOnlyList<CharacterId> Parents => _parents;
    public IReadOnlyList<CharacterId> Children => _children;
    public IReadOnlyList<CharacterId> Caregivers => _caregivers;

    public bool TryAddParent(CharacterId parent)
    {
        if (!parent.IsAssigned || _parents.Contains(parent) || _parents.Count >= SkillRules.MaxParents)
            return false;
        _parents.Add(parent);
        TryAddCaregiver(parent);
        return true;
    }

    public bool TryAddCaregiver(CharacterId caregiver)
    {
        if (!caregiver.IsAssigned || _caregivers.Contains(caregiver))
            return false;
        _caregivers.Add(caregiver);
        return true;
    }

    public bool TryAddChild(CharacterId child)
    {
        if (!child.IsAssigned || _children.Contains(child) || _children.Count >= SkillRules.MaxChildrenPerParent)
            return false;
        _children.Add(child);
        return true;
    }
}

public static class FamilyQueries
{
    public static IEnumerable<CharacterState> CaregiversOf(PopulationRoster population, CharacterState character)
    {
        ArgumentNullException.ThrowIfNull(population);
        ArgumentNullException.ThrowIfNull(character);
        foreach (var id in character.FamilyLinks.Caregivers)
        {
            if (population.TryGet(id, out var caregiver))
                yield return caregiver;
        }
    }

    public static IEnumerable<CharacterState> ParentsOf(PopulationRoster population, CharacterState character)
    {
        ArgumentNullException.ThrowIfNull(population);
        ArgumentNullException.ThrowIfNull(character);
        foreach (var id in character.FamilyLinks.Parents)
        {
            if (population.TryGet(id, out var parent))
                yield return parent;
        }
    }

    public static IEnumerable<CharacterState> ChildrenOf(PopulationRoster population, CharacterState character)
    {
        ArgumentNullException.ThrowIfNull(population);
        ArgumentNullException.ThrowIfNull(character);
        foreach (var id in character.FamilyLinks.Children)
        {
            if (population.TryGet(id, out var child))
                yield return child;
        }
    }

    public static IEnumerable<CharacterState> SiblingsOf(PopulationRoster population, CharacterState character)
    {
        var seen = new HashSet<ulong> { character.Id.Value };
        foreach (var parent in ParentsOf(population, character))
        {
            foreach (var sibling in ChildrenOf(population, parent))
            {
                if (seen.Add(sibling.Id.Value))
                    yield return sibling;
            }
        }
    }

    public static IEnumerable<CharacterState> GrandparentsOf(PopulationRoster population, CharacterState character)
    {
        var seen = new HashSet<ulong>();
        foreach (var parent in ParentsOf(population, character))
        {
            foreach (var grand in ParentsOf(population, parent))
            {
                if (seen.Add(grand.Id.Value))
                    yield return grand;
            }
        }
    }

    public static bool TryLinkParentChild(CharacterState parent, CharacterState child)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(child);
        if (parent.Id == child.Id || !parent.IsAlive || !child.IsAlive)
            return false;
        if (!child.FamilyLinks.TryAddParent(parent.Id))
            return false;
        if (parent.FamilyLinks.TryAddChild(child.Id))
            return true;
        return false;
    }

    public static bool AreParentAndChild(CharacterState parent, CharacterState child) =>
        child.FamilyLinks.Parents.Contains(parent.Id) && parent.FamilyLinks.Children.Contains(child.Id);
}
