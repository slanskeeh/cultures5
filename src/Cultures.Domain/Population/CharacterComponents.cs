using Cultures.Core.Ids;
using Cultures.World;

namespace Cultures.Population;

public enum CharacterLifeStage : byte
{
    Infant = 0,
    Child = 1,
    Adolescent = 2,
    Adult = 3,
    Elder = 4,
    Dead = 5
}

public enum ActionKind : byte
{
    None = 0,
    Idle = 1,
    Move = 2,
    Eat = 3,
    Sleep = 4,
    Work = 5,
    Teach = 6,
    Learn = 7
}

public sealed class CharacterNeeds
{
    public float Hunger { get; set; }
    public float Fatigue { get; set; }

    public void Clamp()
    {
        Hunger = Math.Clamp(Hunger, 0f, 1f);
        Fatigue = Math.Clamp(Fatigue, 0f, 1f);
    }
}

public sealed class CharacterHealth
{
    public float Current { get; set; } = 1f;
    public float Max { get; set; } = 1f;
    public bool IsAlive => Current > 0f;
}

/// <summary>
/// Current action instance. Extensible by Kind; not a per-verb god class.
/// </summary>
public sealed class CharacterActivity
{
    public ActionKind Kind { get; private set; }
    public LogicalGridCoordinate? Target { get; private set; }
    public int DurationTicks { get; private set; }
    public int ProgressTicks { get; private set; }
    public CharacterId PartnerId { get; private set; }
    public SkillType? Skill { get; private set; }
    public Queue<LogicalGridCoordinate> RemainingPath { get; } = new();

    public bool IsComplete => Kind != ActionKind.None && ProgressTicks >= DurationTicks;
    public bool NeedsDecision => Kind is ActionKind.None || IsComplete;

    public void Start(
        ActionKind kind,
        int durationTicks,
        LogicalGridCoordinate? target = null,
        CharacterId partner = default,
        SkillType? skill = null)
    {
        Kind = kind;
        DurationTicks = Math.Max(1, durationTicks);
        ProgressTicks = 0;
        Target = target;
        PartnerId = partner;
        Skill = skill;
        RemainingPath.Clear();
    }

    public void Cancel()
    {
        Kind = ActionKind.None;
        DurationTicks = 0;
        ProgressTicks = 0;
        Target = null;
        PartnerId = CharacterId.None;
        Skill = null;
        RemainingPath.Clear();
    }

    public void Advance()
    {
        if (Kind == ActionKind.None)
            return;
        ProgressTicks++;
    }
}
