# Cultures Successor — TECHNICAL BIBLE v0.3

## 1. Technical goals

Primary goals:
- deterministic simulation;
- persistent entities;
- scalable world;
- testable domain;
- data-driven content;
- safe save/load;
- replaceable presentation;
- AI-agent-friendly architecture.

The technical architecture must support future complexity without requiring the core simulation to depend on Godot scene structure.

## 2. Current stack

- Engine: Godot 4.x .NET
- Language: C#
- Runtime target: .NET 8 initially
- Rendering: Godot 2D
- Visual style: pixel art, isometric/pseudo-isometric
- Source control: Git
- Tests: xUnit
- Data: typed C# runtime models + data definitions
- Serialization: versioned JSON foundation; format may evolve later

Technology can change only through an explicit architecture decision.

## 3. Project layers

Domain
→ pure game rules and state.

Application
→ orchestration, commands, simulation lifecycle.

Infrastructure
→ persistence, filesystem, deterministic utilities, adapters.

Presentation
→ Godot scenes, rendering, UI, audio, input.

The Domain must not reference Godot.

## 4. Authoritative state

The simulation is the source of truth.

Godot Nodes are projections of state.

If a visual object disappears, the entity must remain valid in simulation.

## 5. Entity model

Persistent entities use stable typed IDs.

Required families:
- CharacterId
- FamilyId
- BuildingId
- SettlementId
- CivilizationId
- RegionId
- ChunkId

Additional IDs must be introduced when a domain concept needs persistent identity.

## 6. Definitions vs state

Static content:

BuildingDefinition
ProfessionDefinition
BiomeDefinition
ResourceDefinition
CultureDefinition
FactionDefinition
AnimalDefinition

Mutable runtime state:

BuildingState
CharacterState
FamilyState
etc.

Definitions are referenced by stable identifiers.

Do not duplicate large static definitions into every runtime entity.

## 7. Commands and events

Commands represent intent.

Events represent facts.

Example:

BuildBuildingCommand
→ validation
→ BuildingCompletedEvent

Events may be consumed by multiple systems.

Systems should not call unrelated systems directly unless an explicit application-level dependency is justified.

## 8. Simulation clock

The simulation clock is independent of rendering FPS.

Time must be represented as deterministic simulation ticks.

Calendar conversion:
tick → minute → hour → day → season → year.

Systems subscribe to appropriate time boundaries.

## 9. Determinism

Determinism is required for:
- world generation;
- tests;
- debugging;
- reproducible bug reports;
- save verification.

Randomness must be seeded and controlled.

Avoid unseeded `System.Random` in domain logic.

## 10. World representation

The world is finite, very large and horizontally wrapped.

World hierarchy:

World
→ Region
→ Chunk
→ Logical Grid

World data is stored separately from visual scenes.

## 11. Coordinate systems

Keep explicit types/conversions for:
- world coordinates;
- chunk coordinates;
- logical grid coordinates;
- render/isometric coordinates;
- screen coordinates.

Do not mix coordinate spaces implicitly.

## 12. Streaming

Chunks are the primary world streaming unit.

A chunk can exist in:
- unloaded presentation state;
- loaded presentation state;
- detailed simulation;
- aggregate simulation.

The entity state must not be destroyed merely because presentation is unloaded.

## 13. Simulation LOD

Detailed simulation:
- nearby characters;
- exact movement;
- exact actions;
- individual inventories.

Aggregate simulation:
- population counts;
- resource flows;
- demographic trends;
- production estimates;
- major events.

LOD transitions must preserve important totals and historical facts.

## 14. Character simulation

Character behavior is the combination of:
- needs;
- personality;
- preferences;
- responsibilities;
- skills;
- relationships;
- available actions;
- environment.

AI should choose from domain actions.

Actions expose:
- preconditions;
- target;
- duration;
- effects;
- interruption/cancellation.

## 15. Production

Production is contextual.

Base formula:

Building
+ Worker
+ Skill
+ Environment
+ Season
+ Inputs
+ Demand/context
→ Output

The production engine should not assume one building equals one output.

## 16. Family/lineage

Family is a first-class domain concept.

Relationships use IDs.

Skill inheritance is not genetic simulation. It is social/familial transfer of familiarity and teaching.

## 17. Settlements

Settlement is an emergent simulation grouping.

It may have:
- population;
- infrastructure;
- economy;
- leader;
- council;
- influence network;
- identity;
- history.

There is no requirement for a rigid visual city boundary.

## 18. Politics

Political roles are derived from influence and eligibility.

The political system should consume facts from:
- economy;
- family;
- character;
- military;
- diplomacy;
- social relations.

It must not own duplicate versions of these systems' data.

## 19. Diplomacy

Diplomatic relations are persistent state plus event history.

Relations must support future extension without changing CharacterState.

## 20. History

History should be event-driven where possible.

Important events may be indexed into:
- character history;
- family history;
- building history;
- settlement history;
- civilization history.

Avoid storing every insignificant simulation tick as permanent history.

## 21. Persistence

Every mutable authoritative subsystem must define serialization.

Save data must contain:
- schema/save version;
- world seed;
- generation version;
- simulation time;
- mutable state;
- persistent IDs;
- important history;
- discovery state.

Future save migrations must be possible.

## 22. Debugging

The simulation should expose debug information:
- current simulation time;
- loaded chunks;
- entity counts;
- active systems;
- tick duration;
- RNG seed;
- LOD state.

Debug tools must not become gameplay dependencies.

## 23. Performance

First optimize architecture, then profile.

Do not prematurely optimize every system.

Priority:
1. avoid unnecessary work;
2. update systems at appropriate frequencies;
3. aggregate distant simulation;
4. stream presentation;
5. profile;
6. parallelize only proven bottlenecks.

## 24. Testing

High-value tests:
- deterministic generation;
- coordinate wrap;
- save roundtrip;
- command validation;
- event delivery;
- production calculations;
- needs transitions;
- skill progression;
- family inheritance;
- settlement emergence;
- LOD conversion invariants.

## 25. Compatibility

The project should remain desktop-first.

Browser support is not a current requirement.

## 26. Architectural rule

A future feature must first be represented as a domain concept and contract before being implemented as UI.

# PLAYER COMMANDS AND CHARACTER INTERACTION

## Purpose

Kinlands requires direct individual character control while preserving autonomous simulation.

Player commands must therefore be implemented as commands entering the same authoritative simulation pipeline used by other application-level operations.

The presentation layer must never directly mutate domain state.

---

## Architectural Flow

The intended flow is:

```text
Godot Input
    ↓
Selection System
    ↓
Context Action Provider
    ↓
Player Command
    ↓
Command Processor / Application Layer
    ↓
Domain Validation
    ↓
Character Activity / Simulation State
    ↓
Simulation Tick
```

The exact class names may evolve, but the separation must remain.

---

## Character Selection

Selection is a presentation/application concern.

The selected character is identified by:

```text
CharacterId
```

The UI should retain the selected `CharacterId` rather than a direct reference to a Godot node as the authoritative identity.

Godot nodes are visual representations.

`CharacterId` identifies the simulated individual.

---

## Context Action Provider

Introduce an abstraction capable of determining which player actions are currently available for a selected character.

Conceptually:

```text
ICharacterActionProvider
```

or an equivalent abstraction.

Input:

```text
CharacterId
+
current simulation context
```

Output:

```text
AvailableCharacterAction[]
```

An available action should contain enough information for the presentation layer to display it and issue the appropriate command.

The provider must not mutate simulation state.

---

## Action Availability

Availability should be determined by domain/application rules.

Examples:

```text
Character age
Character skills
Character activity
Character needs
Character position
Nearby terrain
Nearby buildings
Nearby resources
Nearby characters
```

Future conditions may include:

```text
Family relationships
Faction
Culture
Profession
Political status
Ownership
Technology
Diplomatic state
```

Do not implement these future conditions now, but do not architect the system so that they require rewriting the action menu.

---

## Player Command

A player command identifies:

* the command type;
* the target `CharacterId`;
* optional target entity/location;
* any required parameters.

Conceptually:

```text
MoveCharacterCommand
WorkAtBuildingCommand
EatCommand
SleepCommand
TalkToCharacterCommand
CancelCharacterCommand
```

Only commands relevant to implemented mechanics should exist in the current phase.

---

## Command Validation

A command must be validated when it enters the simulation.

The fact that an action was available when the menu was displayed does not guarantee that it remains valid when the player clicks it.

Example:

```text
Menu opened
    ↓
"Work at Farm" available
    ↓
Another simulation tick
    ↓
Farm becomes unavailable
    ↓
Player clicks
    ↓
Command validation fails
```

The command must fail safely.

The UI must never be treated as proof that an action is valid.

---

## Command Execution

Successful commands should modify the character's activity through the authoritative simulation/application layer.

Example:

```text
MoveCharacterCommand
    ↓
validate target
    ↓
calculate/assign movement activity
    ↓
GridNavigator
    ↓
CharacterActivity.Move
```

The presentation layer only displays the result.

---

## Player Priority

Player commands have higher immediate priority than ordinary autonomous decision-making.

Conceptually:

```text
Character
 ├── AutonomousDecision
 └── PlayerCommand
          ↑
       higher priority
```

A player command may:

* replace an autonomous activity;
* interrupt an existing activity;
* assign a new target;
* temporarily suppress autonomous decision-making.

The exact priority/interrupt rules must be explicit rather than implemented through ad-hoc conditionals.

---

## Autonomous AI Resumption

Player control must not permanently disable autonomous simulation.

Every player-controlled activity should have a defined termination state.

Possible outcomes:

```text
Completed
Cancelled
Failed
Invalidated
Interrupted
```

After an appropriate terminal state, the character returns to autonomous decision-making unless another player command is active.

---

## Command Queue

The architecture should support future command queues even if Phase 4 implements only one active player command.

Future example:

```text
Move
 ↓
Gather
 ↓
Return to Storage
 ↓
Deposit
 ↓
Return to Work
```

Do not hard-code the assumption that a character can only ever receive one lifetime action at a time.

---

## Contextual Actions Are Not UI Buttons

An action is a domain/application concept.

The UI is only one possible presentation of available actions.

The same action system should eventually be usable by:

* mouse context menus;
* keyboard shortcuts;
* selection panels;
* future controller input;
* scripted scenarios;
* tutorials;
* potentially AI or replay systems.

---

## Entity Targets

Commands should support typed targets where appropriate.

For example:

```text
CharacterId
BuildingId
ResourceId
WorldCoordinate
```

Do not pass Godot Node references into domain commands.

---

## Determinism

Player commands must preserve deterministic simulation.

Given:

```text
same world state
+
same simulation tick
+
same player commands
```

the resulting state must be identical.

Do not use real-time UI events as simulation state.

Do not perform random behaviour directly in presentation code.

---

## Future Command Sources

The command architecture should eventually allow multiple command sources:

```text
Player
AI
Scenario
Tutorial
Scripted Event
```

All should ultimately interact with the simulation through controlled application/domain operations.

The player should not receive a privileged backdoor into domain state.

---

## Godot Boundary

Godot may handle:

* mouse input;
* click detection;
* selection visuals;
* context menu;
* icons;
* action labels;
* animations;
* camera movement.

Godot must not own:

* character needs;
* inventory;
* position;
* activity;
* production;
* skills;
* relationships;
* family;
* political status.

Those belong to the simulation/domain model.
