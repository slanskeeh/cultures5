# ARCHITECTURE DECISIONS

## AD-001 — Godot 4.x + C#/.NET

Status: Accepted

Reason:
The project is simulation-heavy and benefits from a strongly typed language and a clean separation between a pure domain library and the engine presentation layer.

## AD-002 — Simulation-first architecture

Status: Accepted

The simulation is authoritative. Godot Nodes are presentation.

Reason:
Characters and civilizations must continue to exist when they are not rendered.

## AD-003 — Pure Domain assembly

Status: Accepted

`Cultures.Domain` must not reference Godot.

Reason:
Enables headless tests, deterministic simulation and future tooling.

## AD-004 — Finite huge world with horizontal wrap

Status: Accepted

The world is not infinite.

Reason:
Provides planetary continuity while keeping generation, saving and simulation finite.

## AD-005 — Polar termination

Status: Accepted

North and south do not wrap.

Reason:
The intended world model has polar ice/glacial boundaries and a clear physical end.

## AD-006 — Custom logical grid

Status: Accepted

The simulation uses its own logical grid independent of visual TileMap representation.

Reason:
Building, movement and simulation rules must not depend on rendering implementation.

## AD-007 — Chunks

Status: Accepted

Chunks are used for world storage and presentation streaming.

Reason:
A huge world cannot be represented as one always-active scene.

## AD-008 — Simulation LOD

Status: Accepted

Distant populations are simulated at lower detail.

Reason:
The game should support a world much larger than the set of fully simulated visible characters.

## AD-009 — Stable typed IDs

Status: Accepted

Persistent entities use typed IDs, never array indexes.

Reason:
Save/load, history, relationships and streaming require identity to survive memory/layout changes.

## AD-010 — Deterministic RNG

Status: Accepted

Domain randomness is seeded.

Reason:
Reproducible worlds, tests and debugging.

## AD-011 — Commands/events

Status: Accepted

Commands represent intent; events represent facts.

Reason:
Reduces coupling between systems and keeps future features extensible.

## AD-012 — Content over object-count

Status: Accepted

Prefer contextual variants of existing buildings/resources over hundreds of new building types.

Reason:
Matches the desired evolution of the Cultures formula.

## AD-013 — Emergent settlements

Status: Accepted

Settlements are simulation groupings, not necessarily rigid map objects.

Reason:
The user wants populations to form settlements organically without hard visual boundaries.

## AD-014 — Anti-empty-world mechanism

Status: Accepted

A controlled deterministic mechanism can create/move founding groups into persistently empty inhabited areas.

Reason:
Prevent total civilization extinction from making a huge world permanently empty.

Constraint:
This mechanism must be rare, explainable and invisible as a gamey cheat.

## AD-015 — Direct control is optional

Status: Accepted

Characters can be directly controlled, but autonomous simulation remains functional.

Reason:
Supports the desired CK-like character involvement without mandatory micromanagement.

## AD-016 — History as a first-class feature

Status: Accepted

Important historical facts persist across character, family, building, settlement and civilization levels.

Reason:
Long lifespans and generational gameplay require continuity.

## AD-017 — No premature multithreading

Status: Accepted

Phase 0/early Phase 1 will remain single-threaded.

Reason:
Correctness and debuggability first. Parallelism will be introduced only after profiling identifies a real bottleneck.

## AD-018 — Desktop-first

Status: Accepted

The initial target is desktop PC.

Reason:
The simulation and control density are better suited to desktop, and browser export is not a project requirement.

## AD-019 — New architectural changes require documentation

Status: Accepted

Any change that affects core contracts must update this file and the relevant Bible.

Reason:
AI agents need a stable source of truth.

## Open decisions

### OD-001 — Exact world dimensions
Not fixed yet.

### OD-002 — Exact tile/grid geometry
Not fixed yet.

### OD-003 — Exact character lifespan
Not fixed yet.

### OD-004 — Exact simulation tick duration
Foundation exists; final gameplay pacing not fixed.

### OD-005 — Exact save format
Versioned JSON foundation exists; final binary/compressed strategy not fixed.

### OD-006 — Exact rendering pipeline
Godot 2D is accepted; final TileMap/atlas/animation conventions are not fixed.

### OD-007 — Exact number of starting factions
Not fixed.

### OD-008 — Exact political institutions
Not fixed.

### OD-009 — Military model
Not fixed.