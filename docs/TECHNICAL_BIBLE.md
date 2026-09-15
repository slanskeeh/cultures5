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