# Phase 11 — Internal Politics

## Context

You are working on the Kinlands / Cultures-like simulation project.

Phase 10 — Diplomacy is COMPLETE.

Current verified state:

* 179/179 tests passing.
* Solution builds with 0 warnings / 0 errors.
* Godot 4.7.2.stable.mono headless runtime boots successfully.
* `CultureState` and `FactionState` are separate domain entities.
* `CharacterState.Culture` and `CharacterState.Faction` are authoritative.
* Faction membership is stored on characters; faction member counts are derived from the roster.
* `FactionRelationDirectory` is sparse and symmetric.
* Missing faction relation = Neutral.
* `DiplomacySystem` is the only diplomatic stance mutator.
* `SetDiplomaticStanceCommand` is authoritative.
* Neutral / Friendly / Hostile have no automatic gameplay consequences.
* `DiplomaticStanceChangedEvent` is informational only and has no gameplay listeners.
* Factions do not own geography.
* Factions do not reveal exploration.
* `CivilizationId` remains an unused future seam.
* Civilization persistence is currently mapper-only; save envelope v2 remains unchanged.
* Exploration remains independent and keyed by `ChunkCoordinate`.

Relevant accepted architecture decisions:

* AD-082 — Culture ≠ Faction.
* AD-083 — Neutral is a real unaffiliated culture.
* AD-084 — Membership lives on the character.
* AD-085 — Faction relations are sparse and symmetric.
* AD-086 — Factions do not own geography.
* AD-087 — Names are deterministic and do not consume `SimulationHost.Random`.
* AD-088 — Civilization persistence is mapper-only.
* AD-089 — Diplomacy is a domain system, not a faction field.
* AD-090 — Diplomatic mutations go through commands.
* AD-091 — Diplomatic stances have no automatic consequences.

Phase 11 goal:

> Introduce the domain foundation for internal politics inside a faction: social/political groups, their influence/support, and faction-level internal political state.

The purpose of this phase is to establish a clean simulation foundation for future internal conflicts, leadership, elections, succession, unrest and political consequences.

Do not implement the full political simulation yet.

---

# 1. Inspect before coding

Before making any changes:

1. Inspect the repository.
2. Read:

   * `docs/DECISIONS.md`
   * `docs/MVP_ROADMAP.md`
   * `docs/WORLD_ARCHITECTURE.md`
   * `docs/SIMULATION_ARCHITECTURE.md`
   * `docs/ARCHITECTURE.md`
   * `docs/DEVELOPMENT_LOG.md`
3. Inspect all Phase 9 and Phase 10 civilization/faction/diplomacy code.
4. Inspect:

   * `FactionState`
   * `CultureState`
   * `CharacterState`
   * existing roster/member logic
   * settlement model
   * command patterns
   * system patterns
   * event patterns
   * persistence mapper patterns
   * ID infrastructure
   * deterministic generation
5. Identify whether any existing social/group concepts already exist.
6. Reuse existing abstractions wherever possible.
7. Do not create duplicate representations of faction membership or population.

Do not start coding until the architecture is understood.

---

# 2. Phase 11 scope

Implement the minimum domain foundation for:

* internal political/social groups
* group identity
* faction-local political groups
* group membership/affinity where appropriate
* political influence/support
* faction-level internal political stability/state
* authoritative commands for modifying internal political state
* deterministic behavior
* sparse storage
* tests
* minimal debug inspection
* documentation

The system must be designed so later phases can build:

* leaders
* elections
* succession
* political factions
* internal conflicts
* unrest
* legitimacy
* policy preferences
* rebellions

without rewriting the basic model.

---

# 3. Strict scope boundary

DO NOT implement yet:

* elections
* voting rounds
* succession
* kings/leaders
* offices
* government types
* laws
* taxation
* political parties with complex platforms
* rebellions
* civil wars
* coups
* assassinations
* riots
* prison systems
* punishment
* propaganda
* espionage
* religion system
* detailed social classes
* genealogy
* migration
* economy
* trade
* war
* military
* territory
* resource ownership
* resource generation
* resource regeneration
* faction AI
* autonomous political decisions
* final politics UI

Phase 11 establishes the data model and authoritative state transitions only.

---

# 4. Important conceptual distinction

Do not collapse these concepts:

```text
Culture
    ↓
Faction
    ↓
Internal political groups
    ↓
Individual character affiliation / support
```

They represent different layers.

### Culture

Shared identity.

### Faction

Organized political/social entity.

### Internal political group

A group of interests or internal constituency inside one faction.

Examples conceptually:

* Elders
* Merchants
* Artisans
* Farmers
* Warriors
* Clergy

However:

> These are examples of future concepts, not a requirement to hardcode real-world social classes.

The Phase 11 model must support fictional/internal groups without binding the architecture to a specific historical model.

---

# 5. Internal political group model

Introduce a minimal domain entity such as:

```text
PoliticalGroup
```

or another name consistent with the existing architecture.

Conceptually:

```text
PoliticalGroup
 ├── PoliticalGroupId
 ├── FactionId
 ├── Name
 ├── Traits / Preferences
 └── State
```

A political group belongs to exactly one faction.

It must NOT be a global group shared directly between unrelated factions.

Two factions may independently have groups with similar names or traits.

Do not use faction IDs as political group IDs.

---

# 6. Strong identity

Introduce a strongly typed:

```text
PoliticalGroupId
```

if the existing ID architecture supports it.

The ID must be:

* stable
* unique
* serializable
* independent of Godot nodes
* independent of presentation

If generated procedurally, generation must be deterministic.

Do not use Godot instance IDs as domain identity.

Do not use random GUIDs if deterministic IDs are the established architecture.

---

# 7. Political group traits

Use a compact data model.

Do NOT create dozens of classes.

Potential conceptual dimensions:

```text
Tradition
Change
Authority
Collectivism
Militarism
Commerce
```

But do not blindly implement all of these.

Inspect the existing roadmap and architecture first.

Only introduce traits that are actually useful for later systems.

The important requirement is:

> Political group preferences must be data, not hardcoded behavior.

A future political simulation should be able to evaluate a group's preferences without changing the group's identity model.

---

# 8. Group membership / affiliation

This is a critical architectural decision.

Do NOT duplicate membership lists unnecessarily.

The existing architecture already establishes:

```text
CharacterState.Faction
CharacterState.Culture
```

Phase 11 may introduce:

```text
CharacterState.PoliticalGroup
```

if that is the correct model.

However, inspect the existing character architecture first.

The preferred model is:

```text
Character → FactionId
Character → PoliticalGroupId?
```

with validation that:

```text
PoliticalGroup.FactionId == Character.FactionId
```

A character must not belong to a political group belonging to another faction.

Do not automatically assign a political group merely because a character joins a faction.

Do not automatically change culture when joining a political group.

---

# 9. Political group influence

Introduce a minimal concept of political influence/support.

Avoid creating a complicated simulation.

The system should support a faction having internal groups with different levels of influence.

For example conceptually:

```text
PoliticalGroupInfluence
```

or a field on the group/faction-local state.

Possible properties:

* Support
* Influence
* Size

But do not duplicate information that can be derived from the character roster.

Important distinction:

### Population / membership

Should preferably be derived from characters.

### Political influence

May be explicit simulation state because influence is not necessarily equal to population.

For example:

```text
100 farmers
20 elders
```

does not necessarily mean farmers have five times the political influence.

Do not implement the formula for this yet unless the existing architecture already requires one.

---

# 10. Faction internal political state

Introduce a minimal faction-level state that represents internal political condition.

Possible conceptual values:

```text
Stable
Tense
Unstable
```

or a numeric stability/legitimacy value.

Do NOT automatically choose a complicated model.

First inspect the roadmap.

If no existing model exists, prefer a small numeric/value-object representation such as:

```text
InternalStability
```

with explicit bounds.

The state must be deterministic and authoritative.

Do not make diplomacy automatically modify internal stability.

Do not make hostile foreign factions automatically cause unrest.

Those are future gameplay rules.

---

# 11. No automatic political simulation

Phase 11 MUST NOT contain an autonomous political AI.

There should be no loop like:

```text
every tick:
    calculate dissatisfaction
    change influence
    create rebellion
```

That belongs to a later simulation phase.

Phase 11 provides:

```text
state + commands + validation + deterministic storage
```

not autonomous political behavior.

---

# 12. Commands

Follow the existing command/application architecture.

Potential commands:

```text
CreatePoliticalGroupCommand
AssignPoliticalGroupCommand
SetPoliticalGroupInfluenceCommand
SetInternalStabilityCommand
```

Only create commands that are actually required.

All mutations must go through the authoritative Domain/Application layer.

Presentation/debug code must never directly mutate political state.

---

# 13. Validation rules

At minimum:

### Political group creation

* faction must exist
* group ID must be unique
* group must belong to exactly one faction

### Political group assignment

* character must exist
* political group must exist
* character's faction must match political group's faction
* invalid cross-faction assignment must fail
* assigning no group must be supported if the domain allows unaffiliated internal politics

### Influence

* invalid faction/group references must fail
* values must obey defined bounds
* no NaN/infinite values
* deterministic result

### Internal stability

* values must obey explicit bounds
* invalid values must fail or be normalized according to existing project conventions
* no implicit random changes

Do not silently repair invalid domain state.

Prefer explicit validation errors consistent with existing commands.

---

# 14. Sparse storage

Follow the project's existing sparse-directory philosophy.

Possible structures:

```text
PoliticalGroupDirectory
PoliticalGroupInfluenceDirectory
```

Do not allocate world-sized arrays.

Do not store a political group on every chunk.

Internal politics is a faction/social domain.

Do not make political groups part of the terrain or exploration cache.

---

# 15. Relationship to diplomacy

Diplomacy and internal politics are separate systems.

```text
DiplomacySystem
    Faction A ↔ Faction B
```

versus:

```text
InternalPoliticsSystem
    Faction A
       ├── Group 1
       ├── Group 2
       └── Group 3
```

Changing diplomacy MUST NOT automatically change:

* political group membership
* influence
* internal stability

Changing internal politics MUST NOT automatically change:

* diplomatic stance
* war
* territory
* trade

No cross-system consequences in Phase 11.

---

# 16. Relationship to culture

Culture and political group are not interchangeable.

A culture may contain multiple political groups.

A political group may contain characters with the same culture.

Do not make:

```text
PoliticalGroup == Culture
```

Do not automatically change culture when assigning a political group.

Preserve the Phase 9 decision:

`CharacterState.Culture` is independent from faction membership.

---

# 17. Relationship to geography

Do not attach political groups to:

* chunks
* terrain
* biomes
* climate
* exploration knowledge

A political group is social state.

A character can belong to a political group regardless of whether the player's exploration system knows the character's location.

Do not reveal exploration through politics.

Do not create territory.

---

# 18. Determinism

All generated political groups must be deterministic.

Use the established project deterministic generation architecture.

Do not consume `SimulationHost.Random` merely to generate names or traits if doing so would alter unrelated simulation sequences.

The same:

```text
world seed
+
faction identity
+
group identity/index
```

must produce the same generated group.

Names must remain fictional and deterministic.

Do not copy real historical political party names.

---

# 19. Events

If the project already has domain event patterns, introduce minimal events where useful.

Possible:

```text
PoliticalGroupCreatedEvent
PoliticalGroupMembershipChangedEvent
PoliticalGroupInfluenceChangedEvent
InternalStabilityChangedEvent
```

Do not create events just for completeness.

Events are facts.

They must not automatically trigger:

* rebellions
* wars
* diplomacy
* economy
* migration

unless a later system explicitly subscribes to them.

---

# 20. Persistence

Inspect the existing mapper architecture.

Reuse the Phase 9/10 civilization mapper seams where possible.

Potential DTOs:

```text
PoliticalGroupRecord
PoliticalGroupInfluenceRecord
InternalPoliticsRecord
```

Do not redesign save architecture.

Do not increase save envelope version unless the existing architecture explicitly requires it.

If persistence remains deferred, document that clearly.

Never claim full persistence if only mapper seams exist.

---

# 21. Debug functionality

Add minimal debug inspection.

The existing controls include:

```text
P — faction
J — membership
H — diplomacy
```

Do not break these controls.

If additional controls are needed, use keys that do not conflict with existing debug functionality.

The debug view should allow inspection of something like:

```text
Faction: <name>

Internal stability: Stable

Political groups:
  <group A>
    members: X
    influence: Y

  <group B>
    members: X
    influence: Y
```

This is developer inspection only.

Do not build the final player politics UI.

---

# 22. Tests

Add focused tests.

## Identity

Test:

* PoliticalGroupId is strongly typed.
* IDs are unique.
* IDs are deterministic where generated.

## Creation

Test:

* group can be created for existing faction
* nonexistent faction fails
* duplicate group identity fails
* group always references its faction

## Membership

Test:

* character can join a valid political group
* cross-faction membership is rejected
* removing membership works if supported
* invalid references fail
* membership does not modify culture
* membership does not modify diplomacy
* membership does not modify exploration

## Influence

Test:

* influence can be changed through the authoritative system
* bounds are enforced
* invalid references fail
* influence is independent from raw population count

## Internal stability

Test:

* stability can be changed through the authoritative system
* bounds/invariants are enforced
* changing stability has no automatic diplomatic consequences
* changing stability has no automatic economic consequences

## Isolation

Verify that Phase 11 does NOT modify:

* terrain
* biome
* climate
* exploration knowledge
* LOD
* settlements
* diplomacy
* faction identity
* culture

## Determinism

Same seed + same initial state + same command sequence must produce the same result.

## Regression

All Phase 1–10 tests must continue passing.

Do not weaken or delete previous tests.

---

# 23. Architecture decisions

Review existing AD-082 through AD-091 before adding new decisions.

Add new `AD-xxx` decisions only for genuine architectural rules.

Potential decisions may include:

* internal political groups are faction-local
* political affiliation is separate from culture
* political influence is not necessarily equal to population
* internal politics is independent from diplomacy
* internal politics has no automatic consequences in Phase 11

Do not duplicate existing decisions.

If a question is not mature enough to decide, add an `OD-xxx`.

Likely open decisions:

* exact political group taxonomy
* whether every character may belong to a group
* influence calculation
* stability model
* leadership
* elections
* succession
* political ideologies

Do not prematurely solve future systems.

---

# 24. Documentation

Update only relevant documentation:

* `docs/DECISIONS.md`
* `docs/MVP_ROADMAP.md`
* `docs/SIMULATION_ARCHITECTURE.md`
* `docs/ARCHITECTURE.md`
* `docs/DEVELOPMENT_LOG.md`

Update `WORLD_ARCHITECTURE.md` only if necessary.

Clearly document:

> Internal politics is social/faction state. It does not create territory, diplomacy consequences, war, economy, or autonomous political behavior.

Do not rewrite historical decisions.

---

# 25. Future resource-system boundary

Natural resources remain a separate future World/Simulation system.

Future resources include:

* trees
* stone
* clay
* metal ores
* minerals
* wild berries
* plants
* other renewable/non-renewable resources

Future resource mechanics will include:

```text
deterministic distribution
        ↓
resource existence
        ↓
discovery
        ↓
harvesting
        ↓
depletion
        ↓
regeneration for renewable resources
```

Do NOT implement any of this in Phase 11.

Do not give political groups resource ownership.

Do not let political influence modify resource spawning.

---

# 26. Future character simulation boundary

The eventual character simulation will likely include:

* needs
* professions
* tasks
* movement
* social relationships
* skills
* families
* work
* resource gathering
* construction
* autonomous decision-making

Phase 11 must not attempt to implement these systems.

Political affiliation should be a clean input that future character AI can consume.

---

# 27. Important architectural principle

Keep:

```text
World
Simulation
Exploration
Civilization
Diplomacy
Internal Politics
Presentation
```

as separate concerns.

Internal Politics must not become a Godot-specific system.

Do not put political state into:

* scene nodes
* UI controls
* map rendering
* Godot metadata

The domain must be usable headlessly.

---

# 28. Execution procedure

Work in this order:

### Step 1

Inspect architecture and Phase 9–10 implementation.

### Step 2

Identify reusable faction, character, ID, command and event abstractions.

### Step 3

Design the smallest internal politics domain model.

### Step 4

Implement domain entities/value objects.

### Step 5

Implement sparse storage.

### Step 6

Implement authoritative systems and commands.

### Step 7

Add validation.

### Step 8

Add deterministic generation.

### Step 9

Add focused tests.

### Step 10

Add minimal debug inspection.

### Step 11

Update architecture documentation.

### Step 12

Run:

```bash
dotnet test tests/Cultures.Tests/Cultures.Tests.csproj -warnaserror
```

### Step 13

Build the complete solution with warnings treated as errors.

### Step 14

Run the Godot 4.7.2.stable.mono headless smoke test.

### Step 15

Review the diff for accidental scope expansion.

---

# 29. Acceptance criteria

Phase 11 is complete only when:

1. Internal politics exists as a separate domain concept.
2. Political groups are faction-local.
3. Political group IDs are strongly typed.
4. Political group membership is explicitly modeled where appropriate.
5. Cross-faction political membership is impossible.
6. Political influence/state has explicit invariants.
7. Faction internal stability/state exists only if justified by the inspected roadmap/architecture.
8. All mutations go through the authoritative Domain/Application layer.
9. Internal politics does not directly modify diplomacy.
10. Internal politics does not create war.
11. Internal politics does not create territory.
12. Internal politics does not modify exploration.
13. Internal politics does not modify geography.
14. Internal politics does not modify resource state.
15. No autonomous political AI is introduced.
16. Determinism is preserved.
17. Sparse storage is used where appropriate.
18. Appropriate tests exist.
19. All Phase 1–10 tests still pass.
20. Solution builds with 0 warnings and 0 errors.
21. Godot headless runtime boots.
22. Documentation is updated.
23. No unrelated systems are introduced.

---

# 30. Final report

When finished, report:

1. What was implemented.
2. Existing abstractions reused.
3. Files changed.
4. New architecture decisions.
5. New open decisions.
6. Tests before/after.
7. Exact test command and result.
8. Build result.
9. Godot headless result.
10. Known limitations.
11. Recommended next phase.

Do NOT start Phase 12 automatically.

Stop after Phase 11 and wait for further instructions.
