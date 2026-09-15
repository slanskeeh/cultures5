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

## AD-020 — Central Euclidean wrap

Status: Accepted

Horizontal wrap uses Euclidean modulo in `WorldTopology` only. Other systems call `WrapX` / `Resolve` and must not copy remainder formulas.

Reason:
C# `%` is remainder, not mathematical modulo, and fails for negative X. One implementation keeps wrap tests authoritative.

## AD-021 — Configurable dimensions; complete chunks

Status: Accepted

World width/height are not hardcoded. They must be positive and divisible by chunk width/height so every chunk is a complete rectangle.

Reason:
Exact production size is still OD-001. Partial edge chunks would complicate conversion and are not needed yet.

## AD-022 — Distinct coordinate types, 1:1 cells in Phase 1

Status: Accepted

`WorldCoordinate`, `LogicalGridCoordinate`, `ChunkCoordinate`, `ChunkLocalCoordinate`, `IsometricRenderCoordinate` and `ScreenCoordinate` are separate types. After wrap + bounds, a valid world cell maps 1:1 to a logical grid cell.

Reason:
Matches WORLD_ARCHITECTURE coordinate spaces. Sub-cell positions and final tile geometry remain OD-002.

## AD-023 — Grid-authoritative occupancy

Status: Accepted

Occupancy lives on `TerrainCell` in `LogicalGrid`. Presentation sprites are not occupancy. `OccupantKind` is a placeholder until buildings/characters attach typed IDs.

Reason:
A visual object's position must not be the simulation source of truth.

## AD-024 — ChunkCoordinate is spatial, ChunkId is persistent identity

Status: Accepted

`ChunkCoordinate` identifies a rectangle in the chunk lattice. Existing `ChunkId` remains a persistent entity ID and is not derived from array index in Phase 1.

Reason:
Streaming later needs both a spatial key and an identity that can survive layout changes.

## AD-025 — On-demand chunk terrain

Status: Accepted

Static terrain is sampled per cell and cached per chunk. The constructor does not allocate the whole planet.

Reason:
Phase 2 forbids generating the final planet as one array. Occupancy is a sparse overlay on generated cells.

## AD-026 — Built-in cylindrical value noise

Status: Accepted

Phase 2 uses a small hash-based value-noise implementation sampled on a cylinder so X is periodic. No third-party noise library is adopted as a project standard.

Reason:
The prompt forbids prematurely committing to a generator package. The algorithm is provisional (OD-011).

## AD-027 — Causal geography: elevation → water → climate → biome

Status: Accepted

Water is elevation compared to configurable sea level. Climate is computed from latitude and elevation (plus a moisture field). Biome is a classifier over those facts, not the source of climate.

Reason:
Matches WORLD_ARCHITECTURE causality and keeps later soil/resource layers attachable.

## AD-028 — Temporary latitude mapping

Status: Accepted (temporary)

Latitude 0 is y = 0 and latitude 1 is y = Height - 1. Both ends are polar. This does **not** decide which pole is geographic north.

Reason:
The design requires polar bands now; the north/south label is still an open decision.

## AD-029 — Reconstruct static terrain from generation contract

Status: Accepted

Saves store seed, generation version, and world size metadata. They do not duplicate the generated heightmap.

Reason:
Generated terrain is a pure function of that contract. Occupancy and future mutations will need extra save data later.

## AD-030 — Compositional characters, no CharacterManager

Status: Accepted

`CharacterState` is composed of needs, health, inventory and activity. Separate systems age, decide and act. There is no god-object CharacterManager.

Reason:
Phase 3 must not block families/skills/politics, and must not put AI, movement and rendering in one class.

## AD-031 — Extensible action instance

Status: Accepted

A character has one `CharacterActivity` with `ActionKind`, duration, progress and optional path. Phase 3 kinds: Idle, Move, Eat, Sleep, Work.

Reason:
Future actions can reuse the same progress/interrupt model without a DoEverything method.

## AD-032 — Replaceable wrap-aware grid navigator

Status: Accepted

Phase 3 movement is 4-direction BFS with a small search limit, using `WorldTopology` wrap. Water is impassable. The navigator is a seam, not the final pathfinder.

Reason:
The prompt forbids world-scale pathfinding now but requires a replaceable API.

## AD-033 — Temporary clustered land spawn

Status: Accepted (temporary)

24 characters spawn on nearby passable land around a deterministic origin. This is not a settlement.

Reason:
Need a reproducible 20–30 population without implementing Phase 6.

## AD-034 — Shared cells in Phase 3

Status: Superseded by AD-043

Characters do not reserve occupancy. Multiple people may stand on one cell.

Reason:
Avoid coupling population to the debug occupancy overlay before buildings exist.

## AD-035 — Placeholder food and work cell

Status: Superseded by AD-038 / AD-040 / AD-044

Food is an integer in personal inventory. Work is a shared land cell that grants food on completing Work. Sleep is allowed on any passable cell.

Reason:
Prove eat/sleep/work loops without economy, professions or housing.

## AD-036 — Building definition vs instance

Status: Accepted

`BuildingDefinition` describes a type (footprint, workplaces, recipe, storage, shelter). `BuildingState` is a world instance with `BuildingId`, origin, lifecycle, inventory and workers. Location is never stored on the definition. Godot Nodes are not identity.

Reason:
Matches ARCHITECTURE data-vs-state and keeps future LOD/save independent of presentation.

## AD-037 — Occupancy overlay carries BuildingId

Status: Accepted

Terrain remains generated geography. Dynamic occupancy is a sparse overlay that can mark a cell as a building (with `BuildingId`) or a debug marker. Occupancy is not a universal entity manager.

Reason:
Phase 4 must distinguish empty / building-occupied / blocked without putting entity lists on `TerrainCell`.

## AD-038 — Integer resource inventory

Status: Accepted

`ResourceType` + `Inventory` with integer quantities. Add/Remove/Has/GetQuantity fail deterministically on invalid amounts. Phase 4 resources: Food, Wood, Stone.

Reason:
Replace Phase 3 personal food int with a generic model that characters, buildings and recipes can share.

## AD-039 — Data-driven recipes and environmental modifier seam

Status: Accepted

Production uses `ProductionRecipe` plus `ProductionResolver`. Phase 4 environment is `NeutralEnvironmentProductionModifier` (1.0x). Duration and outputs are resolved, not hard-coded per building type in `if (Farm)` chains. Worker is passed into evaluate/complete so skills can matter later.

Reason:
Farm is a function; biome/season/skill must be able to change the result later without TemperateFarm/ForestFarm types.

## AD-040 — Workplaces with access cells

Status: Accepted

A building may have zero or more `WorkplaceSlot`s. Workers stand on a passable access neighbor, not inside a blocked footprint. Characters hold `AssignedWorkplace`, not a profession.

Reason:
Support multiple workplaces later without rewriting character activity, and keep navigation around blocking buildings.

## AD-041 — Instant debug construction

Status: Accepted (temporary)

Lifecycle is Planned / Constructing / Active / Disabled. Phase 4 placement completes immediately to Active. No hauling or construction workers.

Reason:
The architecture must exist now; full construction economy is out of scope.

## AD-042 — Temporary development site, not a settlement

Status: Accepted (temporary)

Bootstrap places storage, shelter, two farms and a workshop near the land origin. This is not Phase 6 settlement formation.

Reason:
Exercise production without inventing civilization.

## AD-043 — Building footprints block movement

Status: Accepted

Building occupancy sets `BlocksMovement` from the definition (Phase 4: all development buildings block). Characters still do not occupy cells (AD-034 remainder). Navigator treats blocked occupancy as impassable.

Reason:
Characters must walk around buildings. Doors/entrances are future work.

## AD-044 — Food from storage, sleep at shelter

Status: Accepted

Work produces recipe outputs onto the building, then routes to storage. Characters take Food from storage access and eat it. Sleep requires an active shelter access cell (OD-012 superseded).

Reason:
Replace the Phase 3 work→personal-food and sleep-anywhere loops.

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

### OD-010 — Which Y pole is geographic north
Not fixed. Implementation currently treats both Y extremes as cold poles.

### OD-011 — Final world-generation algorithm and biome list
Not fixed. Phase 2 noise frequencies, sea level, polar band and biome thresholds are temporary.

### OD-012 — Rest location vs housing
Superseded by AD-044. Phase 4 sleeps at shelter access. Household ownership remains future work.

### OD-013 — Character movement costs and occupancy
Buildings occupy and can block cells (AD-043). Characters still do not occupy cells. Terrain movement costs remain undecided.

## AD-045 — Direct character control through simulation commands

**Status:** Accepted

**Decision**

Players control individual characters through contextual commands.

The player selects a character by `CharacterId`, receives a context-sensitive list of currently available actions, and issues a player command through the Application/Domain simulation layer.

The UI must never directly mutate `CharacterState`.

**Reason**

Direct individual character control is a fundamental gameplay mechanic of Kinlands, inherited conceptually from the original Cultures control model.

At the same time, characters are autonomous simulated individuals and must remain compatible with autonomous AI.

Therefore player control and autonomous behaviour must use the same activity/action infrastructure.

Conceptually:

```text
Autonomous AI ────────┐
                      ↓
                 Character Activity
                      ↑
Player Command ───────┘
```

This prevents the creation of two incompatible character-control systems.

---

## AD-046 — Character selection uses persistent CharacterId

**Status:** Accepted

**Decision**

Presentation-level character selection identifies the selected character through `CharacterId`.

Godot nodes are visual representations and are not authoritative character identity.

**Reason**

Characters persist for long periods and will eventually participate in:

* families;
* inheritance;
* skills;
* professions;
* relationships;
* politics;
* historical events;
* save/load.

A stable identity must therefore remain independent of presentation objects.

---

## AD-047 — Contextual action availability is evaluated dynamically

**Status:** Accepted

**Decision**

The list of actions shown for a selected character is dynamically generated from current simulation state.

Action availability may depend on:

* character state;
* age;
* needs;
* skills;
* activity;
* position;
* nearby entities;
* terrain;
* buildings;
* future relationships, profession, culture and political state.

**Reason**

Kinlands is intended to have a large number of context-sensitive interactions.

A static menu would either expose invalid actions or require increasingly complex UI-side conditionals.

---

## AD-048 — UI availability does not guarantee command validity

**Status:** Accepted

**Decision**

Every player command must be validated by the authoritative simulation when executed.

The availability of an action in the UI is only a snapshot.

**Reason**

The simulation can change between menu opening and command execution.

Examples:

* another character occupies a workplace;
* a building is destroyed;
* the target moves;
* the character becomes incapacitated;
* the world state changes.

The simulation must therefore remain authoritative.

---

## AD-049 — Player commands temporarily override autonomous activity

**Status:** Accepted

**Decision**

A valid direct player command has higher immediate priority than ordinary autonomous decision-making.

The command may replace or interrupt the current activity according to action-specific rules.

After the command reaches a terminal state, autonomous behaviour can resume.

**Reason**

Direct control must feel responsive and predictable while preserving the autonomous character simulation.

---

## AD-050 — Player control does not equal character possession

**Status:** Accepted

**Decision**

The player gives instructions to characters rather than directly possessing them.

Characters remain autonomous simulated individuals.

**Reason**

This preserves the core design philosophy of Kinlands:

> The player manages and influences a living society rather than manually puppeteering every simulated person.

This distinction becomes increasingly important as families, personalities, skills, politics and social relationships are implemented.

---

## OD-014 — Exact command queue and interruption rules

**Status:** Open

The architecture must support future command queues, cancellation, priorities and interruption.

The exact gameplay rules are not yet fixed.

Examples requiring future design:

* whether a player command can be queued;
* whether critical hunger interrupts an explicit command;
* whether a command can be marked persistent;
* whether repeated commands become a routine;
* how long autonomous AI remains suppressed;
* how commands behave when their target disappears.
