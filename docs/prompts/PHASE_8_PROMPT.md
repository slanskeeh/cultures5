# KINLANDS — PHASE 8: EXPLORATION

## ROLE

You are the lead gameplay/domain architect and senior C# engineer for the Kinlands project.

You are continuing an existing project. Do NOT redesign the architecture from scratch.

Before changing code:

1. Read:

   * `docs/GAME_DESIGN_BIBLE.md`
   * `docs/TECHNICAL_BIBLE.md`
   * `docs/DECISIONS.md`
   * `docs/MVP_ROADMAP.md`
   * `docs/ARCHITECTURE.md`
   * `docs/WORLD_ARCHITECTURE.md`
   * `docs/SIMULATION_ARCHITECTURE.md`
   * `docs/CURSOR_RULES.md`
   * `docs/DEVELOPMENT_LOG.md`
2. Inspect the current implementation of:

   * world coordinates/topology
   * terrain generation
   * chunks
   * `SimulationHost`
   * simulation cursor
   * LOD system
   * character/player command flow
   * settlement state
   * current debug presentation
3. Treat the existing implementation as authoritative where it is already implemented.
4. Check `DECISIONS.md` for the next free Architecture Decision ID and Open Decision ID. Do not reuse an existing ID.

Current known baseline:

* Phase 0–7 completed.
* 155/155 tests passing.
* Build: 0 warnings / 0 errors.
* Godot 4.7.2 .NET.
* .NET 8.
* Domain is a pure `net8.0` library with no Godot dependency.
* Deterministic seeded RNG.
* World supports horizontal wrap.
* World is generated on demand by chunks.
* Characters, buildings, production, families, skills and settlements exist.
* LOD has Full / Reduced / Aggregate / Macro.
* Presentation presence is independent from simulation state.
* Aggregation must not create replacement NPCs.
* Migration is only a seam and is NOT gameplay.
* Save system is still foundation-only and does NOT persist complete runtime world state.

---

# PHASE GOAL

Implement the first real version of **Exploration**.

The world exists independently of what the player knows about it.

The player must have a separate, persistent-in-memory representation of **knowledge about the world**.

Exploration must NOT change the actual world.

Example:

A forest exists whether the player knows about it or not.

The player may initially know nothing about a chunk.

After scouting, the player gains limited knowledge.

After mapping, the player obtains more reliable geographical information.

After confirmation/analysis, more detailed facts become available.

The central rule is:

> World state and player knowledge are different domains.

Do not implement exploration as simply "hide/show terrain".

---

# CORE EXPLORATION MODEL

Introduce a domain-level knowledge progression.

Initial required levels:

```text
Unknown
→ Rumored
→ Scouted
→ Mapped
→ Confirmed
→ Analyzed
```

Use a strongly typed enum or equivalent domain type.

The exact names may be adapted to existing project terminology if the documentation already defines another equivalent model.

Knowledge level must be monotonic for now:

```text
Unknown < Rumored < Scouted < Mapped < Confirmed < Analyzed
```

A later action must never accidentally downgrade knowledge.

Example:

```text
Analyzed → Scouted
```

must not happen.

---

# EXPLORATION STATE

Introduce a sparse exploration/knowledge directory.

Suggested conceptual structure:

```text
ExplorationKnowledgeDirectory
    └── ChunkId -> ChunkExplorationKnowledge
```

Do NOT create a planet-sized dense array.

Unknown chunks should normally have no stored exploration record.

A missing record means:

```text
KnowledgeLevel = Unknown
```

This follows the same philosophy as `ChunkSimulationDirectory`.

Exploration knowledge is NOT terrain cache.

Do not merge exploration state with:

* terrain generation cache
* simulation LOD state
* presentation presence
* character state
* settlement state

These remain separate systems.

---

# CHUNK-LEVEL MVP

For Phase 8, exploration knowledge is primarily **chunk-level**.

Do not build a full per-tile fog-of-war system yet.

A `ChunkExplorationKnowledge` should be capable of representing:

* current knowledge level
* when knowledge was acquired
* optionally last update/source
* known geographic facts appropriate for the current phase

The implementation must remain extensible toward future tile-level knowledge.

Do not hard-code the architecture so that exploration can only ever exist at chunk level.

---

# WHAT KNOWLEDGE CAN EXIST IN PHASE 8

Use only information that the current world implementation actually provides.

At minimum the exploration layer must be able to represent knowledge of:

* whether the chunk has explored land/water
* broad terrain/elevation information
* climate information if currently available
* biome information if currently available

Do NOT invent future systems just to populate exploration.

Do not implement:

* resources that do not yet exist
* landmarks that do not yet exist
* civilizations that do not yet exist
* diplomacy
* trade routes
* military scouting
* migration gameplay
* political intelligence

However, the architecture should leave room for future discoveries such as:

```text
Resource discovered
Landmark discovered
Settlement discovered
Civilization discovered
River discovered
Pass discovered
Rare deposit discovered
```

without requiring a rewrite of the exploration system.

---

# KNOWLEDGE VS WORLD

The most important invariant:

```text
ExplorationKnowledge != TerrainCell
```

The world remains authoritative.

Exploration stores what the player knows about the world.

For example:

```text
World:
    Chunk 100,42
    biome = Forest

Player knowledge:
    Unknown
```

The forest still exists.

After scouting:

```text
Player knowledge:
    Scouted
    broad terrain known
```

The world itself remains unchanged.

Do not mutate terrain generation data as a side effect of exploration.

---

# EXPLORATION SOURCES

Phase 8 only needs a minimal set of exploration operations.

Introduce domain/application commands for deliberate exploration.

At minimum:

### Scout

```text
ScoutChunkCommand
```

Purpose:

Move a chunk from:

```text
Unknown/Rumored
→ Scouted
```

or otherwise improve knowledge according to the final documented rules.

### Map

```text
MapChunkCommand
```

Purpose:

Advance:

```text
Scouted
→ Mapped
```

### Confirm

```text
ConfirmChunkCommand
```

Purpose:

Advance:

```text
Mapped
→ Confirmed
```

### Analyze

```text
AnalyzeChunkCommand
```

Purpose:

Advance:

```text
Confirmed
→ Analyzed
```

These are primarily development/MVP commands.

Do NOT pretend that the game already has explorers, expeditions, scouts or navigation gameplay.

Those systems come later.

The architecture must allow future commands such as:

```text
SendExplorer
EstablishOutpost
SurveyRegion
ExploreRiver
InvestigateLandmark
```

without coupling the knowledge system to them.

---

# RUMORED STATE

`Rumored` exists as a real state but may be minimally populated in Phase 8.

Do NOT generate fake narratives or NPC rumors merely to force the state to appear.

A deterministic debug/application command may be used to create a rumor.

Future systems may create rumors from:

* travelers
* merchants
* neighboring settlements
* migrants
* diplomats
* expeditions
* historical records

Do not implement those future systems now.

---

# DETERMINISM

Exploration must be deterministic.

The same:

```text
world seed
simulation state
exploration commands
simulation time
```

must produce the same knowledge state.

Do not use:

```csharp
System.Random
```

or uncontrolled randomness.

Use the project's existing deterministic RNG infrastructure when randomness is actually necessary.

Do not add randomness unless the design requires it.

---

# WRAP-AWARE WORLD

Horizontal world wrapping is already authoritative.

Exploration must respect it.

For example:

```text
chunk X = 0
```

and:

```text
chunk X = maxX - 1
```

are neighbors when the world wraps horizontally.

Never implement exploration distance using naive absolute X distance.

Use existing topology/distance helpers.

Do not duplicate wrap logic.

---

# EXPLORATION FOCUS

For Phase 8 use the existing simulation/debug cursor as the temporary exploration focus.

Do NOT create a new parallel "player world coordinate" architecture just for exploration.

The existing cursor may be used to identify a chunk for debug commands.

Later the focus can come from:

* camera
* selected character
* explorer
* settlement
* expedition
* player-controlled unit

without rewriting the exploration domain.

---

# PRESENTATION

Add a minimal debug exploration visualization.

Do NOT turn this phase into a final UI phase.

The debug presentation should make it possible to see:

* Unknown
* Rumored
* Scouted
* Mapped
* Confirmed
* Analyzed

for chunks.

Unknown should be visually distinguishable from known chunks.

The visualization must be based on `ExplorationKnowledgeDirectory`, not directly on terrain data.

Important:

The existing developer/debug world map may continue showing raw generated terrain for debugging.

Do not pretend that this debug map is already the final player-facing map.

If a separate exploration overlay is cleaner, use one.

---

# REQUIRED DEBUG CONTROLS

Create simple deterministic debug controls.

Suggested:

```text
R = create Rumor for current chunk
S = Scout current chunk
M = Map current chunk
C = Confirm current chunk
A = Analyze current chunk
O = refresh exploration/debug view
```

If these conflict with existing controls, inspect current input mappings and choose non-conflicting keys.

Do not remove existing LOD/settlement controls.

The debug HUD should display:

```text
Exploration:
Chunk: <id>
Knowledge: <level>
Known facts: <summary>
```

---

# COMMAND VALIDATION

Commands must validate state transitions.

Examples:

```text
Unknown -> Map
```

should fail.

```text
Unknown -> Scout
```

may succeed.

```text
Scouted -> Confirm
```

should fail if `Mapped` is required first.

```text
Confirmed -> Analyzed
```

may succeed.

Exact transition rules should be documented as an explicit architecture/design decision.

Commands must NOT mutate presentation objects directly.

They must go through the authoritative application/domain simulation flow.

The UI/debug layer only requests commands.

---

# KNOWLEDGE QUERY API

Create a clean domain/application query API.

The rest of the game should be able to ask:

```text
GetKnowledge(chunkId)
```

and:

```text
GetKnownFacts(chunkId)
```

without directly accessing internal dictionaries.

Prefer a small explicit API over exposing mutable collections.

---

# KNOWN FACTS

Do not make `ChunkExplorationKnowledge` a giant god object.

Use compositional or typed structures.

For example:

```text
ExploredKnowledge
    KnowledgeLevel
    DiscoveryTick
    TerrainKnowledge
    BiomeKnowledge
    ClimateKnowledge
```

The exact names should follow existing architecture conventions.

The goal is to make future knowledge types independently extensible.

---

# NO CHEATING THROUGH PRESENTATION

Do not allow presentation code to inspect the authoritative world and reveal information that the exploration layer says is Unknown.

For example, a future player map should not do:

```text
world.GetChunk(chunkId).Biome
```

and display the biome while exploration says:

```text
Unknown
```

Presentation must respect the knowledge boundary.

Again, developer/debug maps may intentionally expose raw data when explicitly marked as debug.

---

# INTERACTION WITH LOD

Exploration and simulation LOD are different concepts.

Do NOT make:

```text
Aggregate => Unknown
```

or:

```text
Full => Analyzed
```

or any similar coupling.

A chunk's exploration level survives:

```text
Full
→ Reduced
→ Aggregate
→ Macro
→ Aggregate
→ Full
```

Likewise:

```text
Rendered
↔ Not rendered
```

must not modify exploration knowledge.

A chunk can be:

```text
Unknown + Aggregate
Unknown + Unloaded presentation
Analyzed + Aggregate
Analyzed + Unloaded presentation
```

All of these are valid.

---

# EXPLORATION AND WORLD GENERATION

Do not force the whole world to generate.

Exploring one chunk should only access/generate the world data necessary to inspect that chunk.

Do not generate:

* the entire world
* all regions
* all chunks
* all terrain cells

just because exploration exists.

Maintain on-demand world generation.

---

# EXPLORATION AND CHARACTERS

Phase 8 must NOT add expedition gameplay.

Do NOT implement:

* explorer profession
* scouting units
* expedition parties
* path-based exploration
* travel costs
* survival during exploration
* explorer equipment
* military scouts

The existing characters and player-control architecture should remain compatible with future exploration systems.

No character should be permanently converted into an "explorer" merely for this phase.

---

# SAVE SYSTEM

Do NOT implement a complete save/load system in Phase 8.

However:

`ExplorationKnowledgeDirectory` should be designed so that it can later be serialized into the versioned save envelope.

Do not make exploration state dependent on runtime-only Godot objects.

If a small serialization DTO/schema seam is useful, it may be introduced, but do not expand the scope into full world persistence.

---

# TESTING

Add focused xUnit coverage.

At minimum test:

## Knowledge progression

* Unknown is the default.
* Rumor can be created.
* Scout advances knowledge.
* Map advances knowledge.
* Confirm advances knowledge.
* Analyze advances knowledge.
* Illegal transitions fail.
* Knowledge never decreases accidentally.

## Persistence in memory

* Knowledge remains after unrelated simulation ticks.
* Knowledge survives LOD tier changes.
* Knowledge survives presentation unload.
* Knowledge is not deleted when a chunk becomes Aggregate or Macro.

## World independence

* Exploration does not mutate terrain.
* Exploration does not mutate biome.
* Exploration does not mutate simulation LOD.
* Exploration does not mutate settlement state.

## Wrap

Test exploration of chunks across the horizontal world seam.

## Determinism

Two equivalent hosts receiving the same exploration commands must produce equivalent exploration knowledge.

## Sparse storage

* Unknown chunks do not require explicit entries.
* Exploring one chunk does not allocate the entire world knowledge map.

## Command validation

Test invalid commands and expected failures.

## Query API

Verify that consumers can retrieve knowledge without mutating internal state.

---

# PERFORMANCE

Do not benchmark fake 100k-world performance in this phase.

However, do not introduce:

* full-world exploration arrays
* per-frame scanning of all chunks
* per-character exploration queries every tick
* O(world size) updates for a single explored chunk

Exploration operations should be local and sparse.

---

# ARCHITECTURE DOCUMENTATION

Update:

```text
docs/DECISIONS.md
docs/MVP_ROADMAP.md
docs/WORLD_ARCHITECTURE.md
docs/SIMULATION_ARCHITECTURE.md
```

only where necessary.

Add Architecture Decisions for:

* separation of world state and player knowledge
* sparse exploration knowledge storage
* monotonic knowledge progression
* chunk-level MVP representation
* wrap-aware exploration semantics
* exploration independence from LOD/presentation

Use the next available AD numbers after Phase 7.

Do not duplicate IDs.

Add Open Decisions for anything that is intentionally provisional, for example:

* exact visibility radius
* exact difference between Scouted/Mapped/Confirmed/Analyzed
* future tile-level knowledge
* future exploration sources
* whether rumors should retain source/provenance
* final fog-of-war/map rendering

Do not pretend provisional values are final design.

---

# DEVELOPMENT LOG — MANDATORY

After implementation, update:

```text
docs/DEVELOPMENT_LOG.md
```

and any repository-level development log required by `CURSOR_RULES.md`.

Use the existing project format.

The entry MUST contain:

## Task

Phase 8 — Exploration

## Done

Concrete implementation changes.

## Working / verified

Only things actually tested.

## Tests

Exact commands and exact results.

Example:

```text
dotnet test tests/Cultures.Tests/Cultures.Tests.csproj
dotnet build Cultures.sln
Godot 4.7.2.stable.mono headless ...
```

Never invent test results.

## Bugs found

## Bugs fixed

For each meaningful bug include:

```text
symptom
cause
fix
regression test
```

## Known limitations / TODO

## Architecture decisions

## Files changed

## Current project health

Include:

```text
Build:
Warnings:
Errors:
Tests:
Godot headless:
Known broken areas:
```

## Next step

State:

```text
Phase 9
```

only if the existing roadmap confirms Phase 9.

## Notes for ChatGPT

Mention anything future work should know.

---

# STRICT OUT-OF-SCOPE

Do NOT implement in Phase 8:

* migration gameplay
* diplomacy
* politics
* war
* trade
* expeditions
* explorer profession
* civilization intelligence
* complete map UI
* final fog-of-war graphics
* final player map
* landmarks generation
* advanced resource discovery
* river discovery
* complete save/load
* multiplayer
* networking
* multithreaded simulation
* full-world pre-generation
* arbitrary NPC respawning

Do not "prepare" these by creating speculative systems.

Only create small, explicit extension seams where architecture genuinely benefits.

---

# DEFINITION OF DONE

Phase 8 is complete only when:

1. A chunk can have exploration knowledge independent of its actual world state.
2. Unknown is represented implicitly without a dense world-sized array.
3. Knowledge can progress through the defined levels.
4. Invalid progression commands fail safely.
5. Exploration respects horizontal world wrapping.
6. Exploration is independent of LOD.
7. Exploration is independent of presentation presence.
8. Exploration does not mutate world generation.
9. Exploration state is deterministic.
10. Debug controls allow manually testing progression.
11. Tests cover progression, wrap, determinism, LOD independence and sparse storage.
12. `dotnet test` passes completely.
13. Solution builds with 0 warnings and 0 errors.
14. Godot headless boot still succeeds.
15. Development log is updated truthfully.
16. No out-of-scope gameplay is added.

---

# IMPORTANT ENGINEERING PRINCIPLE

Do not implement exploration as:

```text
if player can see chunk:
    show chunk
```

Implement:

```text
World
    = what actually exists

Simulation
    = what is currently happening

ExplorationKnowledge
    = what the player knows

Presentation
    = how that knowledge/world is visualized
```

These are four different concerns.

The player may know something about a place that is currently not rendered.

A place may be rendered for debugging while still being Unknown to the player.

A place may be Analyzed while its simulation is Aggregate.

A chunk may be Unloaded in Godot while retaining both its world existence and player knowledge.

Preserve these distinctions throughout the implementation.

---

# START

First inspect the repository and the current Phase 7 implementation.

Before writing code, briefly identify:

1. Which existing files/types can be reused.
2. Where the exploration domain should live.
3. What existing input/debug controls conflict with the proposed controls.
4. Which Architecture Decision IDs are currently free.
5. Any contradiction between this prompt and the existing project documentation.

Then implement Phase 8.

Do not ask the user to manually design the architecture unless there is a genuine contradiction in the existing project documentation.

When finished, run all relevant tests/builds, update the development log, and report the exact results.
