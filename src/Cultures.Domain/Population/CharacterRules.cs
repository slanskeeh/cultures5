namespace Cultures.Population;

/// <summary>
/// Provisional Phase 3 survival numbers. Not final game design (OD-003 / OD-004).
/// </summary>
public static class CharacterRules
{
    public const int DefaultPopulation = 24;
    public const float StartingAgeYears = 22f;
    public const float ChildUntilYears = 16f;
    public const float ElderFromYears = 60f;
    public const float MaxLifespanYears = 80f;
    public const float StartingHunger = 0.15f;
    public const float StartingFatigue = 0.10f;
    public const float HungerPerDay = 0.35f;
    public const float FatiguePerDay = 0.40f;
    public const float HungerCritical = 0.75f;
    public const float FatigueCritical = 0.80f;
    public const float EatHungerRestore = 0.55f;
    public const int PersonalInventoryCapacity = 8;
    public const int EatDurationTicks = 8;
    public const int SleepDurationTicks = 60;
    public const int WorkDurationTicks = 24;
    public const int IdleDurationTicks = 8;
    public const float StarvationDamagePerDay = 0.20f;
    public const int PathSearchLimit = 120;
}
