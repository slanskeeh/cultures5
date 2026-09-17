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

## AD-051 — Family is parent/child IDs, not a FamilyManager

Status: Accepted

Parent and child links are stored on `FamilyLinks` using `CharacterId`. Siblings and grandparents are queries. `FamilyId` remains a future household identity and is not required for genealogy.

Reason:
Phase 5 needs persistent generations without a god-object or a forced household.

## AD-052 — Skills belong to the character

Status: Accepted

`CharacterSkills` uses integer experience (`Level = XP / XpPerLevel`). Skills are not subclasses, buildings, or professions. Phase 5 catalogue: Farming, Woodworking, Stoneworking, Crafting.

Reason:
A person keeps knowledge when they change workplace. Integer XP avoids long-term float drift.

## AD-053 — Inheritance and teaching are separate

Status: Accepted

Inheritance runs once at birth from the best parent level, scaled by `InheritancePermille`. Teaching is a timed `Teach`/`Learn` activity that adds XP during life. Parenthood may bonus teaching XP but is not the only teaching path.

Reason:
The prompt forbids merging the two mechanisms or copying a parent's full skill.

## AD-054 — Teaching is an activity and a command

Status: Accepted

Autonomous parent/child teaching and `TeachCharacterCommand` start the same `TeachingSystem` activity. UI must not add skill XP directly.

Reason:
Preserves AD-045 player-command architecture and one activity pipeline.

## AD-055 — Worker skill modifies production through the resolver

Status: Accepted

`ProductionRecipe.Skill` names the relevant character skill. `ProductionResolver` shortens duration at higher skill and adds a bonus output at `BonusOutputLevel`. No per-building resolver subclasses.

Reason:
Worker must be able to affect output without FarmProductionResolver chains.

## AD-056 — Children cannot take workplaces

Status: Accepted

`SkillRules.CanWork` is adult/elder only. Children may move, eat, sleep and learn.

Reason:
Children exist for family and learning, not as default farm labor.

## AD-057 — Birth is an explicit bounded command

Status: Accepted (temporary)

`CreateChildCommand` / `CharacterCreation` creates a child with a new `CharacterId`. Population is capped (`MaxPopulation`, `MaxChildrenPerParent`). No romance/pregnancy simulation.

Reason:
Need a deterministic birth seam without uncontrolled growth.

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

### OD-015 — Exact skill curve and rates
Not fixed. XP per work/teach, inheritance permille, and the production bonus threshold are provisional (`SkillRules`).

### OD-016 — Reproduction, marriage, and fertility
Not fixed. Phase 5 birth is a debug/command mechanism with a population cap.

### OD-017 — Household versus genealogy
`FamilyLinks` are parent/child IDs. Characters carry `HouseholdId.None` as a Phase 6 seam. `FamilyId` is unused. Household membership, home, and property remain unimplemented.

## AD-058 — Settlements emerge from occupancy clustering

Status: Accepted

A settlement is created when a wrap-aware connected component of occupied chunks has enough living people, active buildings, shelter, and storage. Detection uses chunk 4-neighbour BFS, not pairwise character scans.

Reason:
Settlements must be a consequence of co-location and infrastructure, not a placed map object (AD-013).

## AD-059 — Settlement identity is SettlementId, not coordinates

Status: Accepted

`SettlementId` is issued by `EntityIdFactory`. Core location is a derived wrap-aware centroid for UI/debug. Two communities at the same place across time can have different IDs.

Reason:
Coordinates are not a stable identity. Abandoned sites may be reused.

## AD-060 — Settlement lifecycle uses hysteresis

Status: Accepted (temporary thresholds)

Stages: Emerging → Established → Declining → Abandoned. Consecutive qualifying evaluations promote Emerging to Established. Unmatched evaluations decline then abandon. A later qualifying match can recover Declining back to Established. Abandoned identity is kept.

Reason:
Prevent Established ↔ Abandoned flipping every evaluation.

## AD-061 — Settlement membership is derived and mutable

Status: Accepted

Each evaluation writes `CharacterState.Settlement` from the cluster a living character currently occupies. Membership is not radius-only and is not a permanent personal property.

Reason:
Characters must be able to leave, join, or live outside a settlement later.

## AD-062 — Buildings associate with settlements without exclusive ownership

Status: Accepted

`BuildingState.AssociatedSettlement` can be none, an active settlement, or a leftover abandoned association. Buildings are not deleted on abandon; a later settlement may reassign them.

Reason:
Buildings remain independent domain entities (ruins, frontier farms, reuse).

## AD-063 — Settlement statistics are derived

Status: Accepted

Population, demographics, shelters, stored food, workers, and estimated farm food output are computed from characters and building inventories. There is no `SettlementInventory` that owns resources.

Reason:
Future taxation/trade need granular ownership. Current inventories stay on characters and buildings.

## AD-064 — Culture and household remain seams

Status: Accepted (temporary)

Settlements store `CultureId.Neutral` and `CharacterId Leader = None`. Characters store `HouseholdId.None`. Phase 6 does not implement factions, leadership, or households.

Reason:
The data model must not assume one culture or Family == Household == Settlement.

## AD-065 — Newborns start at age 0 as Infant

Status: Accepted (temporary age thresholds)

Birth sets `AgeYears = 0` and `LifeStage = Infant`. Infants are dependents: they do not work, teach, learn, or path independently. Parents are recorded as caregivers; caregiver is not hard-coded as mother.

Reason:
Phase 5 age-6 child placeholder was incorrect for later care, demography, and settlement stats.

## AD-066 — Settlement evaluation uses a per-tick counter

Status: Accepted

`SettlementSystem` counts character ticks inside `SimulationHost.Step`. It does not use `Clock.Tick % interval`, because `Clock.Advance` jumps the tick before the per-tick loop.

Reason:
Keep periodic detection deterministic and avoid evaluating once per inner tick after a batched advance.

## OD-018 — Abandoned identity on re-inhabitation

**Status:** Open

Whether a new community on an abandoned site inherits the old `SettlementId` or always receives a new one is not a final design decision. Phase 6 issues a new ID.

## OD-019 — Exact emergence and lifecycle numbers

**Status:** Open

`SettlementRules` values (min people/buildings, evaluation interval, established/decline/abandon counts) are provisional.

## OD-020 — Settlement naming and culture language

**Status:** Open

Phase 6 uses a deterministic `set.{hex}` name key. Final faction-language names are future work.

## OD-021 — Leadership and offices

**Status:** Open

`SettlementState.Leader` exists as `CharacterId.None`. How a leader is chosen is not implemented.

## OD-022 — Infant care

**Status:** Open

Infants idle unless they already have food to eat. Caregiver feeding, carrying, and household care are not simulated yet.

## AD-067 — Simulation LOD is independent of presentation

Status: Accepted

`SimulationLodTier` (Full / Reduced / Aggregate / Macro) is authoritative domain state. `ChunkPresentationPresence` is a separate flag. A chunk may be Unloaded in Godot and still run aggregate simulation.

Reason:
Visual representation is not simulation existence (AD-008).

## AD-068 — Sparse chunk simulation state

Status: Accepted

`ChunkSimulationDirectory` stores on-demand `ChunkSimulationState` (tier, presentation, derived census). It is not a planet-sized array and is not the terrain chunk cache.

Reason:
Terrain generation cache and simulation LOD are different concerns.

## AD-069 — LOD classification is wrap-aware chunk distance

Status: Accepted (temporary radii)

Tier is Chebyshev distance in chunk space from the simulation focus (`SimulationCursor`) and from protected characters. Horizontal chunk wrap is used; Y does not wrap. Classification runs on an interval, not every character every tick.

Reason:
Avoid O(N²) scans and keep wrap consistent with world topology.

## AD-070 — Aggregation does not delete entities

Status: Accepted (temporary representation)

Ordinary characters keep `CharacterId` and `CharacterState`. Aggregate mode skips individual AI/needs ticks and applies bulk aging/food/production. Buildings and settlements remain in their directories.

Reason:
Do not respawn random NPCs. Identity, family, skills and inventories must survive Detailed → Aggregate → Detailed.

## AD-071 — Protected individuals stay in detailed simulation

Status: Accepted

`IsPersistentIndividual` (selected) and `IsPlayerCommanded` (explicit command) force `LodTier.Full`. Commands targeting aggregated characters fail instead of mutating missing entities.

Reason:
Player control must not hit an entity destroyed by LOD.

## AD-072 — LOD update frequencies are centralized

Status: Accepted (temporary)

`LodRules` holds radii and cadences. Full ticks every step; Reduced may skip behavior ticks; Aggregate uses hour-scale bulk steps; Macro uses day-scale bulk steps (`SimulationCalendar`).

Reason:
Do not scatter magic intervals across systems.

## AD-073 — Reconstruction re-enables retained individuals

Status: Accepted

Reconstruction does not spawn a new population. It restores detailed ticking on the same IDs. Deterministic because no random replacement occurs.

Reason:
Same aggregate state + seed + time must match. Exact identity is guaranteed for retained characters in Phase 7.

## AD-074 — Aggregate events are demographic

Status: Accepted

Aggregate births/deaths/food shortage/migration pressure are chunk-level events. LOD must not invent per-character histories for years spent in aggregate mode.

Reason:
No fake history.

## AD-075 — MigrationGroup is a seam only

Status: Accepted (temporary)

`MigrationGroup` can name origin, destination, population, culture and member IDs. Phase 7 does not move groups between regions as gameplay.

Reason:
Prepare for migration without implementing it.

## AD-076 — World state is not player knowledge

Status: Accepted

Geography, simulation LOD and presentation do not imply exploration knowledge. A forest exists whether the player knows it. A chunk may be Analyzed while Aggregate, Unknown while Full, or known while Unloaded in Godot.

Reason:
Fog/show-terrain is presentation. Knowledge is a separate domain.

## AD-077 — Sparse exploration directory

Status: Accepted

`ExplorationKnowledgeDirectory` stores only chunks that have left Unknown. A missing record is Unknown. It is not a planet-sized array and is not the terrain generation cache.

Reason:
Same sparse philosophy as `ChunkSimulationDirectory` (AD-068).

## AD-078 — Knowledge progression is monotonic

Status: Accepted

Levels: Unknown → Rumored → Scouted → Mapped → Confirmed → Analyzed. Commands never decrease knowledge. Already-at-target fails. Allowed advances in Phase 8:

- Rumored only from Unknown
- Scouted from Unknown or Rumored
- Mapped only from Scouted
- Confirmed only from Mapped
- Analyzed only from Confirmed

Reason:
Later systems must not accidentally wipe discoveries.

## AD-079 — Phase 8 knowledge is chunk-level and spatial

Status: Accepted (temporary grain)

`ChunkExplorationKnowledge` is keyed by `ChunkCoordinate` (lattice cell), not `ChunkId`. `PersistentChunk` is an unused seam. Facts are compositional (`TerrainKnowledge` / `BiomeKnowledge` / `ClimateKnowledge`). `DiscoveryNote` / `DiscoveryKind` exist but are not populated.

Reason:
Player knowledge is about a place on the wrap-aware grid (AD-024). Tile-level fog is future (OD-028).

## AD-080 — Wrap-adjacent chunks are independent knowledge records

Status: Accepted

Horizontal wrap does not merge knowledge. Chunk X=0 and X=last are neighbors in Chebyshev distance and still have separate records. Commands require in-range chunk coordinates; Y does not wrap.

Reason:
Wrap is topology, not identity of a place.

## AD-081 — Exploration does not own LOD, presentation or geography

Status: Accepted

Exploration commands do not change LOD tier, presentation presence, settlement state or generated terrain values. Rumored does not sample/generate the chunk. Scouted and above sample that one chunk via `WorldGenerator.GetChunk` (cache fill is not a geography mutation). Debug overlay Q paints from the knowledge directory; overlay off may still show raw debug terrain.

Reason:
Four concerns: world, simulation, knowledge, presentation.

## OD-023 — Exact LOD radii and cadences

**Status:** Open

`LodRules` values are provisional. Debug 100×50 worlds often remain Full around the cursor.

## OD-024 — Compacting ordinary people out of the roster

**Status:** Open

Phase 7 keeps every `CharacterState` even when aggregated. Whether distant ordinary people may later exist only as census + a snapshot blob is not decided.

## OD-025 — Aggregate birth, death and production rates

**Status:** Open

Bulk hunger/aging/farm cycles and optional macro births are placeholders, not final economy.

## OD-026 — Exploration visibility radius

**Status:** Open

Phase 8 advances the cursor chunk only. There is no scout radius, vision cone or travel-based reveal.

## OD-027 — Scouted / Mapped / Confirmed / Analyzed fact split

**Status:** Open

Current provisional facts: Rumored none; Scouted land/water; Mapped + dominant biome / mean elevation / distinct biomes; Confirmed + climate means; Analyzed + land/water cell counts. Not final design.

## OD-028 — Tile-level exploration knowledge

**Status:** Open

Phase 8 is chunk-level only. Per-cell fog, rivers and landmarks are not represented.

## OD-029 — Exploration sources beyond debug commands

**Status:** Open

Phase 8 writes knowledge only through debug/application commands (R/S/D/F/A). Travel, scouts, maps, trade and diplomacy are not sources yet. `ExplorationSource` is a last-writer tag, not a history log.

## OD-030 — Rumor provenance

**Status:** Open

`Rumored` stores no narrative, informant or reliability. Whether rumors should keep source/provenance is undecided.

## OD-031 — Final fog-of-war and player map

**Status:** Open

Q overlay is a debug visualization. It is not the player map, atlas or final fog art.
