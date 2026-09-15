# PHASE 5 — FAMILIES AND SKILLS

## 0. ROLE

You are implementing **Phase 5 — Families and Skills** of Kinlands.

Read the project documentation before changing code:

* `README.md`
* `ARCHITECTURE.md`
* `SIMULATION_ARCHITECTURE.md`
* `WORLD_ARCHITECTURE.md`
* `CURSOR_RULES.md`
* `docs/DECISIONS.md`
* `docs/MVP_ROADMAP.md`
* `DEVELOPMENT_LOG.md`
* `docs/DEVELOPMENT_LOG.md`
* previous phase prompts and decisions if present

Treat the existing architecture and accepted Architecture Decisions as authoritative.

Do not rewrite working systems merely to make them stylistically different.

The project currently has:

* authoritative logical world/grid;
* horizontal world wrapping;
* deterministic seed-based terrain generation;
* chunk-on-demand terrain;
* autonomous persistent characters;
* needs / health / aging / survival;
* buildings;
* workplaces;
* resources and production;
* storage and food consumption;
* shelter and sleep;
* deterministic simulation;
* `CharacterId`;
* building identities;
* compositional character state;
* extensible activity/action system;
* contextual player-command architecture reserved for direct character control.

Current project health should remain green throughout the phase.

---

# 1. PHASE GOAL

Introduce the foundations of **persistent family relationships and character skills**.

The purpose of this phase is not merely to add two fields to `CharacterState`.

The purpose is to establish the beginning of long-term individual histories:

```text
Character
    ↓
Family
    ↓
Parent / Child relationships
    ↓
Skills
    ↓
Learning
    ↓
Teaching
```

Characters must begin to differ from one another and acquire knowledge over their lifetime.

The implementation must prepare the architecture for future:

* professions;
* personality;
* relationships;
* education;
* inheritance;
* family history;
* culture/faction preferences;
* political influence;
* leadership;
* social status;
* direct player interaction.

Do NOT implement those complete systems in this phase.

---

# 2. CORE DESIGN PRINCIPLE

A character is an individual persistent person, not merely a worker attached to a building.

Their skills and family relationships must survive changes in:

* workplace;
* building;
* activity;
* location;
* settlement;
* faction;
* political status.

Do not couple:

```text
Skill → Building
```

or:

```text
Skill → Profession
```

Skills belong to the character.

A workplace may use a skill to modify production, but the workplace does not own the skill.

---

# 3. FAMILY MODEL

Introduce explicit family relationships.

At minimum, the simulation must be able to represent:

* parent;
* child;
* siblings through shared parents.

Use persistent `CharacterId` references.

Conceptually:

```text
Character A
    ↓
Parent
    ↓
Character B
```

and:

```text
Parent
 ├── Child A
 ├── Child B
 └── Child C
```

Do not create a heavyweight `FamilyManager` god-object.

Family information should remain composable and queryable through appropriate domain abstractions.

---

# 4. FAMILY IDENTITY

The architecture should support a future family/household identity without forcing every character to be permanently tied to a single household structure.

A future system may require:

* family names;
* family reputation;
* inheritance;
* household;
* property;
* genealogy;
* family history.

Therefore do not encode family relationships as anonymous boolean flags such as:

```text
IsParent
HasChildren
```

Use persistent relationships.

---

# 5. GENERATIONAL MODEL

Characters must have enough information to distinguish generations.

At minimum:

* parent references;
* child references or queryable reverse relationships;
* birth time/age;
* deterministic identity.

The model must support:

```text
Grandparent
    ↓
Parent
    ↓
Child
    ↓
Grandchild
```

without changing the fundamental character identity model.

Do not implement unlimited genealogy traversal as an expensive operation on every simulation tick.

Prefer explicit relationships and targeted queries.

---

# 6. REPRODUCTION / CHILD CREATION

Phase 5 should introduce the **minimum domain capability required for new children to exist**.

The exact final reproductive system is not part of this phase.

Do not implement:

* romance simulation;
* detailed marriage;
* pregnancy simulation;
* sexual behaviour;
* complex fertility;
* courtship;
* full relationship AI.

Instead establish a deterministic, extensible mechanism for creating a child from appropriate parent characters.

The mechanism must:

* generate a new persistent `CharacterId`;
* establish parent relationships;
* initialize age appropriately;
* initialize basic needs/health;
* initialize skills;
* preserve simulation determinism.

The implementation may use a simplified debug/test birth mechanism if required.

Do not create uncontrolled population explosions.

Population growth must remain bounded by explicit rules.

---

# 7. CHILD DEVELOPMENT

Characters should no longer exist only as adults.

Introduce age-dependent life stages sufficient to support:

* child;
* adult.

If the current architecture already supports age continuously, do not replace it unnecessarily.

Instead add a derived or domain-level life-stage concept.

The exact final life-stage catalogue remains open.

Future stages may include:

```text
Infant
Child
Adolescent
Adult
Elder
```

but do not implement unnecessary complexity unless required by the existing architecture.

---

# 8. CHILDREN AND WORK

Children must not automatically become normal workers.

Introduce basic age restrictions for activities where appropriate.

The current implementation must not require a full education system.

Children may:

* move;
* eat;
* sleep;
* socialize if implemented;
* perform limited safe activities;
* learn.

Do not add a large child-specific gameplay system.

The primary purpose of children in Phase 5 is to establish:

```text
family
+
development
+
learning
```

---

# 9. SKILL SYSTEM

Introduce a generic character skill system.

Skills must be data-driven and extensible.

Do NOT hard-code skills into separate character subclasses.

Bad:

```text
BlacksmithCharacter
FarmerCharacter
WoodcutterCharacter
```

Good conceptual model:

```text
Character
    Skills
       ├── Farming
       ├── Woodworking
       ├── Blacksmithing
       └── ...
```

The exact skill catalogue should remain small in this phase.

Suggested initial skills:

* Farming
* Woodworking
* Stoneworking
* Crafting

The final list is not fixed.

---

# 10. SKILL REPRESENTATION

Skills should support:

* current level/value;
* deterministic modification;
* querying;
* increasing through activity;
* future teaching;
* future inheritance.

A skill should not be represented as an arbitrary string + integer scattered through the codebase.

Prefer a domain value such as:

```text
SkillType
SkillValue
CharacterSkills
```

or an equivalent architecture consistent with the existing code.

Avoid prematurely implementing:

* perk trees;
* skill branches;
* hundreds of skills;
* experience UI;
* talent trees.

---

# 11. SKILL PROGRESSION

Skills should increase through relevant activity.

For example:

```text
Farming work
    ↓
Farming experience
    ↓
Farming skill increases
```

Skill progression must be deterministic.

Avoid floating-point accumulation if it creates long-term drift.

Use a representation that remains stable over long simulations.

The progression rate must be configurable.

Current values are provisional.

---

# 12. SKILL EFFECT ON PRODUCTION

Connect skills to the production system introduced in Phase 4.

The existing architecture already has:

```text
ProductionRecipe
ProductionResolver
environment modifier seam
worker input
```

Extend this system so that worker skills can affect production.

Conceptually:

```text
Recipe
   ↓
Environment
   ↓
Worker Skill
   ↓
Final Production
```

Do NOT create:

```text
FarmProductionResolver
ForestFarmProductionResolver
ExpertFarmProductionResolver
```

or large `if/else` chains based on character type.

The production system must remain data-driven.

---

# 13. SKILL LEVEL EFFECTS

The first implementation may use a simple modifier.

Example:

```text
low skill → normal/slightly reduced output
medium skill → normal output
high skill → increased output
```

The exact mathematical curve is provisional.

Do not spend the phase balancing production.

The important requirement is:

> worker skill must be capable of affecting production through the existing resolver architecture.

---

# 14. LEARNING

Introduce the concept of one character learning a skill from another.

The system must support:

```text
Teacher
    ↓
Teaching
    ↓
Student
    ↓
Skill increase
```

The exact final teaching rules are provisional.

At minimum:

* teacher must possess the relevant skill;
* student must be capable of learning;
* teaching must consume simulation time;
* student skill must increase deterministically;
* teacher/student relationship must be represented by the activity/action system rather than a direct instantaneous stat mutation.

---

# 15. PARENTAL TEACHING

Parents should have a natural path to teaching children.

Example:

```text
Father
  Farming 70
       ↓
     teaches
       ↓
Son
  Farming 8 → 9 → 10...
```

The parent does not simply transfer skill.

Teaching is an activity.

This is important because later:

* player can explicitly order a parent to teach;
* children can autonomously learn;
* other adults can teach;
* schools may teach;
* masters may teach apprentices.

Do not hard-code:

```text
if (student.IsChild && teacher.IsParent)
```

as the only implementation of teaching.

Parent-child status should be one condition that may provide bonuses or unlocks.

---

# 16. DIRECT PLAYER CONTROL COMPATIBILITY

Phase 5 must remain compatible with the direct character-control architecture.

The player must eventually be able to:

```text
Select character
    ↓
Context menu
    ↓
Teach...
    ↓
Select target
    ↓
Select skill
    ↓
Issue command
```

Do not necessarily implement the complete polished UI in Phase 5.

However, the domain/application layer must make teaching a real action/command rather than a UI-only operation.

The future command must conceptually be:

```text
TeachCharacterCommand
{
    TeacherId,
    StudentId,
    SkillType
}
```

or an equivalent architecture.

Do not pass Godot Nodes into this command.

---

# 17. AUTONOMOUS LEARNING

Characters should be capable of learning without direct player control.

For example:

```text
Child
   ↓
Nearby skilled parent
   ↓
Learning opportunity
   ↓
Teaching activity
   ↓
Skill improvement
```

Autonomous learning should be slower or less efficient than deliberate/direct teaching if that is required by the game design.

The exact balance is provisional.

The important architectural requirement is that:

```text
Player-directed teaching
```

and:

```text
Autonomous teaching
```

use the same underlying teaching activity.

---

# 18. INHERITED / INITIAL SKILLS

Children should receive a small initial amount of knowledge influenced by their parents.

Example:

```text
Parent Farming = 70

Child starts with:
Farming = small inherited value
```

Do not copy the parent's entire skill.

Do not make inheritance a direct percentage copy that makes children instantly talented.

Use a small configurable inheritance contribution.

The inheritance mechanism must be deterministic.

Potential future factors:

* parent skill;
* both parents;
* culture;
* family tradition;
* personality;
* teaching;
* environment.

Only the basic parent-skill contribution is required now.

---

# 19. IMPORTANT DISTINCTION: INHERITANCE VS TEACHING

These are separate mechanisms.

### Inheritance

Happens during child creation.

```text
Parent skill
      ↓
Child initial skill
```

### Teaching

Happens during the child's life.

```text
Teacher
      ↓
Repeated activity
      ↓
Child skill increases
```

Do not merge these into one mechanism.

---

# 20. CHARACTER PERSONALITY

Do NOT implement the full personality/trait system in Phase 5.

However, avoid architectures that assume all characters behave identically.

Future personality may affect:

* learning speed;
* teaching willingness;
* work preference;
* social behaviour;
* risk;
* loyalty;
* leadership;
* political influence.

Leave appropriate extension points without implementing the system.

---

# 21. PROFESSION

Do NOT implement the full profession system in Phase 5.

A character may have skills without having a formal profession.

For example:

```text
Character
  Farming: 65
  Woodworking: 12
  Profession: none
```

Later a profession may be assigned based on skills, preference, culture and player decisions.

Do not couple skills to professions.

---

# 22. WORKPLACE INTEGRATION

Preserve the Phase 4 distinction:

```text
AssignedWorkplace
```

is not:

```text
Profession
```

and neither is:

```text
Skill
```

A worker may use different skills depending on the task performed at a workplace.

The production resolver should receive enough context to determine the relevant skill without coupling buildings directly to character classes.

---

# 23. FAMILY + BUILDING HISTORY PREPARATION

The architecture should allow future historical relationships such as:

```text
Family X
    ↓
Father works at Workshop
    ↓
Son later works at Workshop
    ↓
Family association emerges
```

Do NOT implement building family-name signs yet.

However, do not make buildings unable to reference the historical identities of their workers.

Future systems may derive building history from simulation events.

---

# 24. EVENTS / HISTORY

If the current architecture supports domain events, use them where appropriate for significant family events.

Potential future events include:

```text
ChildBorn
CharacterDied
SkillImproved
TeachingStarted
TeachingCompleted
```

Do not create an event bus solely for this phase if one does not already exist.

Do not create large persistent histories for every simulation tick.

Future historical systems should be able to subscribe to meaningful events.

---

# 25. SIMULATION PERFORMANCE

The world is intended to become very large.

Do not introduce:

```text
every character checks every other character every tick
```

for family or teaching.

Avoid O(N²) population scans.

Use targeted relationships and localized searches where possible.

The system should remain viable when population grows from:

```text
24
→ 100
→ 1,000
→ 10,000+
```

Do not optimize prematurely, but avoid obviously non-scalable architecture.

---

# 26. DETERMINISM

All family and skill simulation must remain deterministic.

Given:

```text
same seed
+
same world
+
same initial characters
+
same commands
+
same number of ticks
```

the resulting:

* characters;
* relationships;
* births;
* ages;
* skills;
* activities;

must match.

Randomness must use the simulation's deterministic random source.

Never use:

```text
System.Random
```

or time-dependent randomness directly inside domain simulation if it breaks reproducibility.

---

# 27. SAVE/LOAD

Do not postpone architecture until save/load.

Characters and family relationships are persistent simulation state.

If the current save system is not yet capable of persisting dynamic characters, document this limitation.

Do not implement a large serialization system unless Phase 5 explicitly requires it.

However, design family and skill state so it can be serialized later without reconstructing relationships from presentation objects.

---

# 28. PRESENTATION

Update the debug presentation enough to verify the system.

The debug view should allow inspection of:

* selected character;
* age/life stage;
* parents;
* children;
* at least several skills;
* current activity;
* teacher/student state if active.

Keep the presentation deliberately simple.

This is still a systems-development phase.

Do NOT spend time on final pixel art.

---

# 29. DEBUG / TEST CONTROLS

Provide deterministic debug functionality where useful for verification.

Examples:

* inspect character;
* create test child;
* increase skill;
* start teaching;
* inspect family relationships.

Debug tools must call application/domain commands.

They must not directly mutate domain state.

---

# 30. TEST REQUIREMENTS

Add automated tests covering at minimum:

### Family

* child has correct parents;
* parent-child relationship is queryable;
* siblings share parents;
* multi-generation relationships work;
* invalid/self relationships are rejected.

### Birth

* child receives unique `CharacterId`;
* child starts at appropriate age;
* parent relationships are established;
* birth is deterministic;
* population does not explode under test conditions.

### Life stages

* age correctly determines life stage;
* child/adult restrictions work where defined.

### Skills

* character can possess multiple skills;
* skill lookup works;
* skill progression is deterministic;
* relevant work increases relevant skill;
* irrelevant work does not increase unrelated skills.

### Production

* worker skill affects production;
* production remains deterministic;
* existing Phase 4 recipe tests remain valid.

### Teaching

* valid teacher can teach;
* invalid teacher cannot teach;
* student skill increases over time;
* teaching consumes simulation time;
* teaching is deterministic;
* teaching can terminate;
* player-directed teaching can be represented as an application command.

### Inheritance

* child receives small initial skill contribution;
* contribution is deterministic;
* parent skill is not copied wholesale;
* inheritance and teaching remain separate.

### Regression

All previous tests must continue passing.

---

# 31. ACCEPTANCE CRITERIA

Phase 5 is complete only when:

* [ ] Characters can have persistent parent/child relationships.
* [ ] Children can exist as persistent characters.
* [ ] Life stage can be determined from age.
* [ ] Characters have a generic skill system.
* [ ] Skills can increase through relevant work.
* [ ] Skills can affect production.
* [ ] Children can receive small inherited skill values.
* [ ] Characters can teach one another.
* [ ] Teaching is represented through the activity/action system.
* [ ] Parent teaching is possible without making it the only teaching mechanism.
* [ ] Direct player-command architecture can represent teaching.
* [ ] Autonomous learning/teaching is possible at a minimal level.
* [ ] No profession system is hard-coded into skills.
* [ ] No personality system is prematurely implemented.
* [ ] No civilization/settlement system is introduced.
* [ ] No full diplomacy/politics system is introduced.
* [ ] Determinism is preserved.
* [ ] All automated tests pass.
* [ ] Solution builds with 0 warnings and 0 errors.
* [ ] Godot headless runtime starts successfully.
* [ ] Debug presentation can inspect family and skill state.

---

# 32. ARCHITECTURAL RESTRICTIONS

Do NOT:

* create `CharacterManager` god-object;
* create character subclasses for professions;
* couple skills directly to buildings;
* couple skills directly to professions;
* store family relationships in Godot Nodes;
* mutate domain state directly from UI;
* use UI state as authoritative simulation state;
* scan every character against every other character every tick;
* introduce nondeterministic randomness;
* implement the complete personality system;
* implement complete professions;
* implement politics;
* implement diplomacy;
* implement settlements;
* implement final graphics;
* rewrite the existing world/chunk architecture.

Prefer small composable systems.

---

# 33. EXPECTED DOMAIN STRUCTURE

The exact class names are up to the existing architecture, but conceptually the project should move toward:

```text
Population
│
├── CharacterState
│   ├── Identity
│   ├── Age
│   ├── Health
│   ├── Needs
│   ├── Inventory
│   ├── Skills
│   ├── Family Relationships
│   ├── AssignedWorkplace
│   └── Activity
│
├── CharacterSkills
├── FamilyRelationship
├── FamilyQueries
├── CharacterLifeStage
├── TeachingActivity
└── CharacterCreation
```

Do not blindly create every class listed above.

Adapt to the existing code.

---

# 34. PLAYER COMMAND ARCHITECTURE

The project has already established the requirement that players will eventually directly control characters through contextual actions.

Phase 5 must preserve this architecture.

The intended future flow is:

```text
Click Character
      ↓
Character Selection
      ↓
Context Menu
      ↓
Teach...
      ↓
Select Character
      ↓
Select Skill
      ↓
TeachCharacterCommand
      ↓
Application Layer
      ↓
Simulation
      ↓
Teaching Activity
```

The simulation remains authoritative.

The presentation layer must never perform:

```text
student.Skills[Farming] += 1;
```

directly.

---

# 35. TEMPORARY VALUES

The following are provisional and should be centralized/configurable:

* minimum teaching age;
* maximum child age;
* skill cap;
* skill progression rate;
* teaching rate;
* inherited skill contribution;
* birth/population limits;
* lifespan/life-stage thresholds.

Do not scatter magic numbers throughout the code.

---

# 36. DOCUMENTATION

After implementation update:

* `docs/DECISIONS.md`
* `docs/MVP_ROADMAP.md`
* `docs/SIMULATION_ARCHITECTURE.md`
* `DEVELOPMENT_LOG.md`
* `docs/DEVELOPMENT_LOG.md`

Record new Architecture Decisions as appropriate.

At minimum consider documenting:

* family relationships;
* skill ownership;
* skill progression;
* inheritance vs teaching;
* teaching as an activity;
* player-command compatibility.

Open unresolved questions as `OD-*` rather than silently deciding permanent behaviour.

---

# 37. DEVELOPMENT REPORT

At the end of the phase, update the development log with a structured report.

Use this format:

```text
## [DATE] — Task: Phase 5 Families and Skills

### 1. Task

### 2. Done

### 3. Working / Verified

### 4. Tests

### 5. Bugs found

### 6. Bugs fixed

### 7. Known limitations / TODO

### 8. Architecture decisions

### 9. Files changed

### 10. Current project health

### 11. Next step

### 12. Notes for ChatGPT
```

The report must be factual.

Do not claim GUI verification unless the GUI was actually inspected.

Do not claim a feature works merely because the project compiles.

Include exact test counts and build results.

---

# 38. FINAL VERIFICATION

Before declaring Phase 5 complete:

1. Run all domain tests.
2. Run all existing regression tests.
3. Build the complete solution.
4. Confirm zero warnings and zero errors.
5. Start Godot headlessly.
6. Verify deterministic simulation with at least two equivalent simulation runs.
7. Verify family creation.
8. Verify skill inheritance.
9. Verify skill progression.
10. Verify teaching.
11. Verify production/skill interaction.
12. Inspect the debug presentation if possible.

If something cannot be visually verified, explicitly state that in the development report.

---

# 39. STOP CONDITION

When Phase 5 acceptance criteria are satisfied:

**STOP.**

Do not automatically begin Phase 6.

Do not implement settlements, civilizations, diplomacy, politics, final UI, final art, or world-scale simulation unless explicitly requested in a later phase.

Leave the repository in a clean, tested, documented state ready for review.
