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
**Status:** Closed for current JSON envelope; compression/binary still later.

Versioned JSON `SaveEnvelope` v4 is the current format (AD-107, AD-118). Final binary/compressed strategy is not fixed.

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
**Status:** Open (partial)

Partnership (`FormPartnershipCommand`) and household-aware birth exist. Fertility rates, pregnancy duration, marriage ceremony, and automatic pairing are not modeled. Debug `CreateChildCommand` remains.

### OD-017 — Household versus genealogy
**Status:** Closed

`FamilyLinks` remain parent/child genealogy. `HouseholdState` is co-residence with optional shelter home (AD-113). `FamilyId` is still unused as a named-lineage object.

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

Status: Superseded in part

Settlements still store default `CultureId.Neutral` and `Leader = None`. Households are implemented in Phase 16 (AD-113). Factions exist since Phase 9. Leadership selection remains OD-021.

Reason:
The data model must not assume Family == Household == Settlement. That separation still holds.

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

**Status:** Closed (minimal)

Infants may eat from caregiver inventory (AD-114). Carrying, dedicated care activities, and nursing economy are not simulated.

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

## AD-082 — Culture and Faction are distinct entities

Status: Accepted

`CultureState` is a shared identity with compact `CultureTraits` data. `FactionState` is an organized group that references a `CultureId`. Neither inherits from `SettlementState`. A culture may exist without a faction. `CivilizationId` remains an unused later seam.

Reason:
Faction is not a culture and not a settlement.

## AD-083 — Neutral is the unaffiliated default culture

Status: Accepted

`CultureId.Neutral` (value 1) is a real directory entry named `Unaffiliated`. Generated cultures use `EntityIdFactory.NextCulture()` starting at 2. Spawned people and new settlements still default to Neutral. Debug baseline also creates two generated cultures and three factions.

Reason:
Keep the Phase 6 Neutral seam as a valid identity instead of a magic missing value.

## AD-084 — Faction membership lives on the character

Status: Accepted

`CharacterState.Faction` and `CharacterState.Culture` are authoritative. Member lists and counts are derived from the roster. Joining a faction does not copy that faction's culture (OD-034). Newborns inherit the first parent's culture and start with `FactionId.None`.

Reason:
Do not duplicate population onto the faction object. Affiliation is not UI state.

## AD-085 — Faction relations are symmetric and sparse

Status: Accepted

`FactionRelationDirectory` stores unordered pairs `(minId, maxId)`. Missing means Neutral. Self-relations are rejected. Neutral is not stored. Stance values are Neutral / Friendly / Hostile data only — no diplomacy behavior.

Reason:
Avoid duplicate A→B / B→A state and order-dependent bugs.

## AD-086 — Factions do not own geography

Status: Accepted

A faction has an optional `HomeSettlement` seam defaulting to none. Creating or joining a faction does not reveal chunks, mutate exploration, assign territory, or own every chunk where members stand.

Reason:
World is geography; factions are social entities.

## AD-087 — Culture and faction names are hashed, not Host.Random

Status: Accepted (temporary generator)

`FictionalName` mixes world seed + entity id into invented syllables and trait bytes. It does not consume `SimulationHost.Random` and does not use real-world culture names.

Reason:
Generation must be deterministic without shifting later RNG sequences.

## AD-088 — Civilization persistence is a mapper seam only

Status: Accepted (temporary)

`CultureRecord` / `FactionRecord` / `FactionRelationRecord` exist. Save envelope remains v2 and does not store civilizations.

Reason:
Do not pretend a full save exists.

## AD-089 — Diplomacy is a domain system, not a faction field

Status: Accepted

`DiplomacySystem` owns stance mutations. Faction identity and culture stay on their own types. A diplomatic pair is not a property of one faction.

Reason:
Culture → Faction → Diplomatic relationship remain three layers (AD-082, AD-085).

## AD-090 — Diplomatic mutations go through commands

Status: Accepted

`SetDiplomaticStanceCommand` is the Phase 10 authority. `SetFactionRelationCommand` is a compatibility alias that calls the same `DiplomacySystem.TrySetStance`. Presentation (H) must not write `FactionRelationDirectory` itself.

Reason:
UI must not own political state.

## AD-091 — Stance has no automatic consequences

Status: Accepted

Neutral / Friendly / Hostile are diplomatic stances only. Changing them does not start war, form an alliance, move people, reveal exploration, claim land, or change the economy. `DiplomaticStanceChangedEvent` is a fact, not a gameplay trigger.

Reason:
Phase 10 is political state. Consequences belong to later phases.

## AD-092 — Political groups are faction-local

Status: Accepted

`PoliticalGroupState` references exactly one `FactionId`. Two factions may have similarly named groups; they are different identities. Groups are not cultures, not factions, and not global classes.

Reason:
Internal politics lives inside a faction (Culture → Faction → Group → Character).

## AD-093 — Political affiliation is separate from culture and faction

Status: Accepted

`CharacterState.PoliticalGroup` is optional (`None`). Joining a faction does not assign a group. Joining a group does not change culture. Cross-faction affiliation is rejected. Changing faction clears a mismatched group.

Reason:
Keep AD-084 membership and AD-082 culture distinct from internal blocs.

## AD-094 — Influence is not population

Status: Accepted

`PoliticalGroupState.Influence` is explicit integer state in 0–100. Member counts are derived from the roster. A group may have high influence with zero members.

Reason:
Elders can outweigh a larger constituency. No formula yet (OD-045).

## AD-095 — Internal stability is sparse faction state

Status: Accepted

`InternalPoliticsDirectory` stores 0–100 stability per faction. Missing means 50. Default is not stored. Diplomacy does not write this directory.

Reason:
Need a faction-level political condition without a tick-based unrest simulation.

## AD-096 — Internal politics has no automatic consequences

Status: Accepted

Commands change group membership, influence and stability only. Events do not start rebellions, wars, diplomatic shifts, or economic effects. `SimulationHost.Step` does not tick politics.

Reason:
Phase 11 is state + validation, not political AI.

## AD-097 — Military is separate from Faction identity

Status: Accepted

Military units live in `MilitarySystem` / `MilitaryUnitDirectory`, not as a field on `FactionState`. Culture, faction, diplomacy, internal politics and military remain related but distinct.

Reason:
A faction is political identity. A military unit is a force belonging to that identity. Embedding units on `FactionState` would collapse the domains.

## AD-098 — Military units are faction-owned

Status: Accepted

Every unit references exactly one existing `FactionId`. Neutral/mercenary forces are not modeled.

Reason:
Phase 12 has no mercenaries. Ownership must be validatable against `FactionDirectory`.

## AD-099 — Character military membership is optional

Status: Accepted

`CharacterState.MilitaryUnit` defaults to `None`. A living character belongs to at most one unit. Faction must match. Leaving a faction clears membership the same way political group is cleared.

Reason:
Not every person is a soldier. Membership is roster-derived, not duplicated on the unit.

## AD-100 — Military does not imply war

Status: Accepted

Creating, assigning, or disbanding units does not declare war, move people, or change diplomatic stance. Hostile diplomacy remains a stance only (AD-085).

Reason:
Phase 12 is identity and lifecycle, not a war game.

## AD-101 — Military has no automatic consequences

Status: Accepted

Military commands do not change culture, political group, influence, stability, exploration, LOD, terrain, settlements, or resources. Events have no gameplay listeners. `SimulationHost.Step` does not tick military.

Reason:
Automatic combat, recruitment, or political fallout belongs to later phases.

## AD-102 — Military stores no world occupancy in Phase 12

Status: Accepted

Units have no location, path, or chunk ownership. `LogicalGridCoordinate` remains the world-cell type (OD-002). Military code does not encode 4-neighbor adjacency, Chebyshev distance, or a private grid.

Reason:
Movement and hex conversion are out of scope. Future occupancy can attach world cells without rewriting military identity.

## AD-103 — History is a first-class fact store

Status: Accepted

`HistoryRecorder` observes domain events and stores `HistoryRecord` entries with typed `HistoryEventId`. Queries exist by character, settlement, faction, military unit, and recent chronology. History is not AI memory, not exploration knowledge, and not a notification queue.

Reason:
AD-016 required history as a feature. Phase 13 records facts without inventing a chronicle UI or LLM narration.

## AD-104 — History does not create gameplay

Status: Accepted

Recording a fact never mutates population, economy, diplomacy, politics, military, exploration, LOD, or terrain. `HistoryRecorder.IsEnabled` exists so restore can insert saved records without re-firing listeners. `SimulationHost.Step` does not tick history.

Reason:
History is an observer. Consequences of events already happened in their own systems.

## AD-105 — History is independent of LOD and exploration

Status: Accepted

History does not store per-tick hunger, pathfinding, LOD classification, or exploration spam. Aggregate births/deaths are demographic facts, not fake individual deaths. Distant simulation can still produce history when the originating system publishes an event.

Reason:
LOD must not invent biographies. Exploration knowledge is player/world knowledge, not chronology.

## AD-106 — Presentation identity uses domain IDs

Status: Accepted

`PresentationCamera`, `PresentationSelection`, and `PresentationIdentityMap` live in Application. Camera focus and zoom are not `SimulationCursor`. Selection stores `CharacterId` / `BuildingId` / `SettlementId`. Views are recreatable from IDs in a camera window. Godot nodes must not become identity.

Reason:
The simulation stays meaningful without sprites. Reloading a scene must rebind to the same IDs.

## AD-107 — Save envelope v3 stores authoritative dynamic state

Status: Accepted

`SaveEnvelope.CurrentVersion = 4`. v3 stores dynamic state. v4 adds pacts, clock speed, and onboarding. v2 remains header-only. `SaveMigrations.ToCurrent` upgrades v3. Unsupported versions fail load. Static terrain is reconstructed from seed + generation version (AD-029).

Reason:
OD-005 and OD-036 required a real save, not a seed replay. Silent upgrade of v2 into v3 would hide missing state.

## AD-108 — Restore is atomic and ID-preserving

Status: Accepted

`SimulationPersistence.Restore` validates version, seed, generation, and world size, then restores directories by `Add` with saved IDs. `EntityIdFactory.Restore` continues counters so new IDs never collide. History is disabled during restore so saved facts are not duplicated. Settlement detection is not re-run after load; restored settlements are authoritative. Derived LOD is re-evaluated after restore.

Reason:
A half-loaded world is worse than a failed load. Indexes are never IDs (AD-009).

## AD-109 — Resource deposits are not inventory

Status: Accepted

Natural stocks live in `ResourceDeposit` / `ResourceDepositDirectory` with typed `ResourceDepositId`. Character and building inventories remain `Inventory`. Deposits deplete on extraction and regenerate on a slow ecology cadence. Terrain fertility and rivers are generated fields, not items.

Reason:
World resources must persist independently of who currently holds goods.

## AD-110 — Contextual production is data-driven

Status: Accepted

`ContextualRecipeTable` plus `ContextualEnvironmentProductionModifier` select recipes and modifiers from biome/fertility/rivers. Farm + Forest yields berries; Farm + plains yields food; Workshop + Highland can yield stone. Neutral modifier remains for tests that isolate worker/skill math. Building types are not forked per biome.

Reason:
AD-039 already required a modifier seam. Phase 15 fills the table instead of hard-coding product if-chains.

## AD-111 — Wildlife is a chunk aggregate

Status: Accepted

`WildlifePresence` stores deer/sheep/boar/bird counts per chunk. Wildlife is not a roster of animal `CharacterId`s. Regeneration is wrap-aware and independent of presentation.

Reason:
Individual animals at world scale would explode entity count. Aggregate ecology is enough for Phase 15.

## AD-112 — Profession is not skill

Status: Accepted

`ProfessionId` / `ProfessionCatalog` (Farmer, Woodcutter, Mason, Crafter) is a vocation. Skills remain XP on `CharacterSkills`. Changing profession does not grant skill. Unemployed adults may still work any compatible workplace; an assigned profession restricts workplaces (`BuildingDefinition.RequiredProfession` on farm). Children cannot take professions.

Reason:
AD-052 already separated skills from jobs. Phase 16 makes vocation explicit without collapsing the two.

## AD-113 — Household is not genealogy

Status: Accepted

`HouseholdState` is a co-residence group with optional shelter `Home`. Genealogy stays on `FamilyLinks`. `CharacterState.Household` is derived membership (at most one household). Partnership is reciprocal `Partner` and may form a household. Birth copies the parent's household. Home must be a shelter building.

Reason:
OD-017. Family tree, house, and settlement are different groupings.

## AD-114 — Infant care is caregiver feeding

Status: Accepted (minimal)

Infants still do not work, teach, or path independently. If hungry, an infant may eat food from a caregiver inventory (parents/household caregivers) without a dedicated Carry action. Detailed carrying, nursing rates, and household property remain later.

Reason:
OD-022 needed a real care path so infants are not stranded idle with food sitting on an adult.

## AD-115 — Simulation balance is a tunable object

Status: Accepted

`SimulationBalance` holds Alpha tunables (hunt yield, autosave interval, max speed). Systems read it from the host. It is not a god-object and does not replace domain rules classes.

Reason:
Phase 17 asked for balance as a layer, not scattered magic numbers in presentation.

## AD-116 — Clock speed is domain state

Status: Accepted

`SimulationClock.Speed` is 1/2/4/8. Presentation requests `Step(Speed)` per tick budget. Speed is saved in the envelope. Pause still blocks `Advance`.

Reason:
Time scale is simulation policy, not a Godot timer hack.

## AD-117 — Step is per-tick and fault-tolerant

Status: Accepted

`SimulationHost.Step` advances one simulation tick at a time so clock and systems stay aligned. A thrown exception is recorded on `SimulationDiagnostics` and stops further ticks in that call. One `TickAdvancedEvent` is published for the whole request.

Reason:
Phase 17 stability. A bulk clock jump before systems tick left calendar ahead of the world.

## AD-118 — Save UX uses slots, not a scene

Status: Accepted

`ISaveStore` (`MemorySaveStore`, `FileSaveStore` with temp+move) writes JSON slots. Presentation maps F5/F9 to `user://`. Autosave is optional via `SimulationBalance.AutosaveIntervalTicks` (0 = off in tests).

Reason:
Headless tests must save without Godot. File writes must be atomic.

## AD-119 — New biomes remap existing climate, not a new noise contract

Status: Accepted

Swamp, Savanna, and Taiga are classifier refinements of Forest/TemperateLand. `WorldGeneration.CurrentVersion` stays 1 so elevation and land/water do not change. Recipes: swamp/taiga farms yield berries; savanna farms yield food.

Reason:
Bumping generation version mixed a new noise seed and broke wrap/settlement tests. Biome variety must not silently relocate continents.

## AD-120 — Wildlife hunting is aggregate extraction

Status: Accepted

`HuntWildlifeCommand` depletes chunk wildlife and adds Food. Unemployed adults may hunt; an assigned non-Hunter may not. Hunting is not combat, not war, and does not create animal `CharacterId`s.

Reason:
AD-111 wildlife is aggregate. Phase 18 asked for animals without an entity explosion.

## AD-121 — Diplomatic pacts are explicit and consequence-free

Status: Accepted

Trade / NonAggression / Alliance pacts are stored with `DiplomaticPactId`. Hostile cannot trade. Alliance requires Friendly. Forming or breaking a pact does not move goods, change membership, or declare war. Hostile remains a stance only.

Reason:
Phase 18 diplomacy depth without violating AD-091 / AD-100 / OD-040.

## AD-122 — Season changes are world facts

Status: Accepted

`WorldEventSystem` publishes `SeasonChangedEvent` when the calendar season index changes. History records it. The first observed season is not recorded. Seasons do not cause famine, war, or migration in this phase.

Reason:
Emergent chronology without scripted quests or automatic disasters.

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

## OD-032 — Culture naming and language

**Status:** Open

Phase 9 syllable names are a placeholder. There is no language family content, grammar, or person-name generator.

## OD-033 — Baseline culture and faction counts

**Status:** Open

Debug worlds currently seed 1 unaffiliated + 2 generated cultures and 3 factions. Not a final new-game setup.

## OD-034 — Personal culture versus faction culture

**Status:** Open

Joining a faction does not overwrite `CharacterState.Culture`. Whether members should adopt the faction culture is undecided.

## OD-035 — Faction home and territory

**Status:** Open

`HomeSettlement` is an unused seam. Borders, claimed chunks and resource ownership are not implemented.

## OD-036 — Persisting civilizations in the save envelope

**Status:** Closed

Envelope v3 persists cultures, factions, relations, political groups, stability, military units, and character affiliation (AD-107). v2 remains header-only.

## OD-037 — CivilizationId versus FactionId

**Status:** Open

Phase 9 political groups are `FactionId`. What a future `CivilizationId` means (umbrella of factions, player-level polity, or unused leftover) is not decided.

## OD-038 — Player faction selection

**Status:** Open

There is no new-game faction pick, visual identity, or cultural production modifiers. Debug P/J/H only inspect and assign.

## OD-039 — Treaties and diplomatic history

**Status:** Open

Phase 10 stores the current stance only. Treaties, access, tribute and a history log of insults/aid are not represented.

## OD-040 — Hostile versus war

**Status:** Open

Hostile is not war. Whether a later military phase derives war from Hostile, or uses a separate War state, is undecided.

## OD-041 — AI diplomacy

**Status:** Open

Factions do not autonomously change stance. There is no reputation, espionage or negotiation AI.

## OD-042 — Alliance and other extra stances

**Status:** Open

Alliance, truce, vassal, embargo and similar labels are not added. Keep three stances until a later phase needs a fourth.

## OD-043 — Political group taxonomy

**Status:** Open

Groups are fictional named blocs with compact tradition/authority/commerce bytes. There is no fixed set of Elders/Merchants/Farmers classes.

## OD-044 — Who may belong to a political group

**Status:** Open

Unaffiliated (`PoliticalGroupId.None`) is allowed. Whether every faction member must eventually belong to a group is undecided.

## OD-045 — Influence calculation

**Status:** Open

Influence is set by command. Whether it will later derive from wealth, office, age or population is not decided.

## OD-046 — Stability model

**Status:** Open

0–100 with Unstable/Tense/Stable bands is provisional. Legitimacy is not a separate value.

## OD-047 — Leadership and offices

**Status:** Open

No leader, council, or office types. `CharacterId Leader` on settlements remains unused.

## OD-048 — Elections and succession

**Status:** Open

Not implemented.

## OD-049 — Political ideologies

**Status:** Open

Trait bytes are not named ideologies and do not drive policy.

## OD-050 — Army hierarchy

**Status:** Open

Phase 12 has one generic unit. Warband / Guard / Army ranks, parent armies, and nested formations are not modeled.

## OD-051 — Commanders

**Status:** Open

No commander, officer, or `CharacterId` leadership field on units.

## OD-052 — Recruitment

**Status:** Open

Units are created empty by command or baseline seed. No draft, volunteer, or profession-based recruitment.

## OD-053 — Equipment

**Status:** Open

No weapons, armor, or unit inventory.

## OD-054 — Combat model

**Status:** Open

No damage, HP, attack, defense, battles, or sieges.

## OD-055 — Morale

**Status:** Open

Not stored.

## OD-056 — War model

**Status:** Open

Hostile diplomacy is not war (OD-040). War declaration, goals, casualties, and history are later.

## OD-057 — Unit movement

**Status:** Open

Units have no position. Future location must use existing world-cell coordinates, not a military-specific grid.

## OD-058 — Formations

**Status:** Open

No formation shapes or multi-cell footprints for units.
