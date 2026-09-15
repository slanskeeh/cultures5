using Cultures.Core.Events;
using Cultures.Core.Ids;

namespace Cultures.Population;

public sealed record ChildBornEvent(ulong Tick, CharacterId Child, CharacterId ParentA, CharacterId ParentB)
    : ISimulationEvent;

public sealed record SkillImprovedEvent(ulong Tick, CharacterId Character, SkillType Skill, int NewLevel)
    : ISimulationEvent;

public sealed record TeachingStartedEvent(ulong Tick, CharacterId Teacher, CharacterId Student, SkillType Skill)
    : ISimulationEvent;

public sealed record TeachingCompletedEvent(ulong Tick, CharacterId Teacher, CharacterId Student, SkillType Skill)
    : ISimulationEvent;
