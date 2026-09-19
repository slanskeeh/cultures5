namespace Cultures.Population;

/// <summary>
/// Skills belong to the character, not to a building or profession.
/// </summary>
public enum SkillType : byte
{
    Farming = 1,
    Woodworking = 2,
    Stoneworking = 3,
    Crafting = 4,
    Hunting = 5,
    Fishing = 6
}

public readonly record struct SkillValue(SkillType Type, int Experience, int Level);

/// <summary>
/// Integer experience. Level = Experience / XpPerLevel. Avoids float drift.
/// </summary>
public sealed class CharacterSkills
{
    private readonly Dictionary<SkillType, int> _experience = new();

    public int GetExperience(SkillType skill) => _experience.GetValueOrDefault(skill);

    public int GetLevel(SkillType skill) =>
        Math.Min(SkillRules.Cap, GetExperience(skill) / SkillRules.XpPerLevel);

    public bool Has(SkillType skill, int minimumLevel) => GetLevel(skill) >= minimumLevel;

    public void SetExperience(SkillType skill, int experience)
    {
        _experience[skill] = Math.Clamp(experience, 0, SkillRules.MaxExperience);
    }

    public void SetLevel(SkillType skill, int level) => SetExperience(skill, SkillRules.LevelToXp(level));

    public bool TryAddExperience(SkillType skill, int amount, out int newLevel)
    {
        newLevel = GetLevel(skill);
        if (amount <= 0)
            return false;

        var previous = GetExperience(skill);
        var next = Math.Min(SkillRules.MaxExperience, previous + amount);
        if (next == previous)
            return false;

        _experience[skill] = next;
        newLevel = GetLevel(skill);
        return true;
    }

    public void SeedAdult(ulong appearanceSeed)
    {
        SetLevel(SkillType.Farming, SkillRules.AdultFarmingBase + (int)(appearanceSeed % (ulong)SkillRules.AdultFarmingSpan));
        SetLevel(
            SkillType.Woodworking,
            SkillRules.AdultWoodworkingBase + (int)((appearanceSeed >> 8) % (ulong)SkillRules.AdultWoodworkingSpan));
    }

    public void InheritFrom(IEnumerable<CharacterState> parents)
    {
        foreach (SkillType skill in Enum.GetValues<SkillType>())
        {
            var best = 0;
            foreach (var parent in parents)
                best = Math.Max(best, parent.Skills.GetLevel(skill));
            if (best <= 0)
                continue;
            SetExperience(skill, SkillRules.InheritedXp(best));
        }
    }

    public IEnumerable<SkillValue> Enumerate()
    {
        foreach (SkillType skill in Enum.GetValues<SkillType>())
            yield return new SkillValue(skill, GetExperience(skill), GetLevel(skill));
    }
}
