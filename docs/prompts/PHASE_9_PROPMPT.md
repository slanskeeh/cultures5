# Phase 9 — Factions and Cultures

## Context

You are working on the Kinlands / Cultures-like simulation project.

Phase 8 — Exploration is COMPLETE.

Current verified state:

* 166/166 tests passing.
* Solution builds with 0 warnings / 0 errors.
* Godot 4.7.2.stable.mono headless runtime boots successfully.
* Exploration knowledge is a separate domain from geography, simulation LOD and presentation.
* Exploration is chunk-level and keyed by `ChunkCoordinate`.
* Exploration knowledge is sparse and monotonic.
* Do not re-key exploration to `ChunkId`.
* Do not add expeditions or an explorer profession.
* `Q` is only a debug knowledge/fog visualization, not the final player map.

Relevant accepted architecture decisions:

* AD-076 — World state is not player knowledge.
* AD-077 — Sparse exploration directory.
* AD-078 — Exploration knowledge progression is monotonic.
* AD-079 — Exploration knowledge is chunk-level and spatial.
* AD-080 — Wrap-adjacent chunks have independent knowledge records.
* AD-081 — Exploration does not own LOD, presentation or geography.

Phase 9 goal:

> Introduce the domain model for factions/cultures and their relationship to the world, without prematurely implementing diplomacy, wars, economies, settlements AI or a complete population simulation.

This phase must establish a clean foundation for later simulation phases.

---

# 1. First inspect the existing project

Before changing code:

1. Inspect the repository structure.
2. Read:

   * `docs/DECISIONS.md`
   * `docs/MVP_ROADMAP.md`
   * `docs/WORLD_ARCHITECTURE.md`
   * `docs/SIMULATION_ARCHITECTURE.md`
   * `docs/DEVELOPMENT_LOG.md`
3. Inspect existing:

   * World domain
   * Simulation domain
   * Settlement-related code
   * Character/person-related code, if present
   * Persistence models
   * Application commands/systems
   * Existing coordinate/wrap abstractions
   * Existing IDs and deterministic generation infrastructure
4. Inspect the Phase 8 implementation and tests.
5. Do not duplicate abstractions that already exist.
6. Preserve existing architecture and naming conventions.

Do not start coding until you understand the existing domain boundaries.

---

# 2. Phase 9 scope

Implement the minimum domain foundation for:

* Factions
* Cultures
* faction membership / affiliation
* cultural identity
* deterministic faction/culture creation
* faction/culture directories
* basic relationships between factions

The implementation must be simulation/domain-first.

Do NOT implement yet:

* diplomacy UI
* diplomacy negotiations
* treaties
* trade
* war
* military combat
* reputation system
* complex political simulation
* population growth
* genealogy
* migration simulation
* full NPC AI
* profession system
* resource economy
* production chains
* complete settlement simulation
* procedural civilizations spanning the entire world
* final presentation/UI
* final map visualization

Those belong to later phases unless the existing architecture requires a minimal seam.

---

# 3. Core conceptual distinction

Keep these concepts separate:

## Culture

Culture represents a shared cultural identity.

Examples of possible cultural traits:

* naming style
* language family
* architectural tendency
* clothing tendency
* social customs
* preferred food
* worldview/value tendencies

Do not overbuild this into a complete anthropology system.

For MVP, culture should be a stable domain entity with a small deterministic set of traits.

## Faction

Faction represents an organized social/political group.

A faction has:

* stable identity
* name
* culture reference
* members/population reference or count where appropriate
* home/territory reference only if the existing architecture supports it
* relationships with other factions

A faction is NOT synonymous with a settlement.

A settlement can exist independently from the faction model.

A faction may eventually contain multiple settlements.

---

# 4. IDs

Use strongly typed IDs consistent with the existing project architecture.

Expected conceptual IDs:

* `CultureId`
* `FactionId`

Do not use raw strings/integers throughout the domain if the project already uses value-object IDs.

IDs must be:

* stable
* deterministic where generated procedurally
* serializable
* independent of presentation
* independent of Godot nodes

Do not use Godot instance IDs as domain identity.

---

# 5. Culture model

Create a minimal `Culture` domain model.

It should have a stable identity and deterministic characteristics.

Possible structure:

```text
Culture
 ├── CultureId
 ├── Name
 ├── LanguageFamily / LanguageId (only if justified by existing architecture)
 ├── Traits
 └── Generation metadata
```

Do not introduce a huge hierarchy of classes for individual traits.

Prefer compact value objects / enums / immutable records where appropriate.

Culture traits should be data, not hardcoded behavior.

The model must allow future expansion without rewriting faction membership.

---

# 6. Faction model

Create a minimal `Faction` domain model.

Conceptually:

```text
Faction
 ├── FactionId
 ├── Name
 ├── CultureId
 ├── Members / Population reference
 ├── Home settlement reference (optional seam)
 └── State
```

Do not make `Faction` inherit from `Settlement`.

Do not make `Culture` inherit from `Faction`.

They are separate concepts.

A faction may use a culture.

A culture may exist without a faction.

---

# 7. Faction membership

Introduce an explicit concept for membership/affiliation.

Avoid embedding arbitrary faction state directly into character presentation objects.

The domain should eventually support:

```text
Character → FactionId
Character → CultureId
```

But if characters are not yet implemented in the required domain form, create only the appropriate seam.

Do not force a large character rewrite during Phase 9.

Membership must be authoritative domain/application state, not UI state.

---

# 8. Faction relationships

Introduce a minimal relationship model.

At this stage we need the foundation for relationships such as:

* Neutral
* Friendly
* Hostile

Use a deterministic/symmetric or explicitly directed model depending on what the existing architecture supports.

Do not implement diplomacy behavior yet.

The relationship should be pure simulation state.

Example conceptual model:

```text
FactionRelation
 ├── FactionA
 ├── FactionB
 └── RelationState
```

If relationships are intended to be symmetric, normalize the pair so:

```text
A → B
B → A
```

does not create duplicate state.

Do not accidentally introduce order-dependent behavior.

---

# 9. Directories / repositories

Follow the existing sparse-directory architecture.

Introduce appropriate directories such as:

```text
CultureDirectory
FactionDirectory
FactionRelationDirectory
```

Do not create planet-sized arrays.

Do not tie these directories to Godot nodes.

Do not make the faction system depend on the exploration cache.

Do not make the faction system depend on presentation.

---

# 10. World relationship

Do not make factions automatically equal to geographical regions.

The world contains geography.

Factions are social entities.

Later phases may introduce:

* territories
* borders
* settlements
* migration
* resource ownership

For Phase 9, create only the smallest necessary seam.

A faction should not automatically "own" every chunk where its members happen to exist.

---

# 11. Deterministic generation

If Phase 9 requires creating initial cultures/factions procedurally, generation MUST be deterministic.

Given the same:

* world seed
* generation parameters
* faction/culture index

the generated result must be identical.

Do not use:

* `System.Random` without controlled seed
* Godot random state
* time
* GUID generation that changes between runs

unless the existing architecture explicitly provides deterministic wrappers.

Use the project's existing deterministic generation infrastructure.

---

# 12. Naming

Generated names must be deterministic.

Do not use hardcoded lists like:

```text
Vikings
Romans
Egyptians
...
```

Do not copy names, lore, terminology or cultural content from Cultures.

Use fictional generated names.

The system should eventually support reproducible names such as:

```text
Aren
Velkari
Dorun
Kareth
...
```

The exact naming algorithm can remain intentionally simple in Phase 9.

The goal is deterministic infrastructure, not a complete name-generation system.

---

# 13. Commands / application layer

If the existing architecture uses commands and systems, faction/culture mutations must go through the authoritative simulation/application layer.

Do not mutate faction state directly from:

* Godot UI
* debug map
* nodes
* presentation scripts

Create appropriate application commands only where useful for testing/debugging.

Possible commands:

* CreateCulture
* CreateFaction
* AssignFactionMembership
* SetFactionRelation

Do not expose unnecessary commands merely for completeness.

---

# 14. Debug functionality

Add minimal developer-facing debug functionality so Phase 9 can actually be verified.

Possible debug output:

```text
Factions: 3
Cultures: 2

Faction:
  ID
  Name
  Culture
  Members
```

And optionally:

```text
Faction A ↔ Faction B
Relation: Neutral
```

Do not build a final faction UI.

Debug output must not become part of the domain model.

---

# 15. Persistence seam

Inspect the existing persistence architecture.

If Phase 9 entities are intended to survive saves, create appropriate DTO/record mapper seams consistent with existing persistence conventions.

Do not redesign the save system.

Do not silently modify the save envelope version unless required.

If persistence is intentionally deferred, document it as an explicit open decision.

Do not pretend persistence exists when only an in-memory model exists.

---

# 16. Exploration independence

Phase 9 must NOT modify Exploration semantics.

Specifically:

* faction creation must not reveal chunks
* faction membership must not automatically reveal geography
* culture knowledge must not be confused with exploration knowledge
* faction territory must not automatically become explored
* exploration knowledge must remain keyed by `ChunkCoordinate`
* `Rumored` must not generate geography
* existing Phase 8 progression must remain unchanged

A player may know a faction exists without knowing its territory.

A player may know a culture exists without knowing its exact location.

---

# 17. Wrap-world compatibility

Respect the existing horizontal world wrap.

Do not introduce special cases that merge identities at the wrap seam.

If factions later acquire spatial positions, they must use the existing world-coordinate abstractions.

Do not use Euclidean map assumptions that break at the horizontal seam.

Do not change the existing wrap implementation during this phase unless a concrete blocker is discovered.

---

# 18. LOD independence

Factions and cultures are domain entities.

They must not depend on:

* Godot scene presence
* rendering
* chunk visual LOD
* terrain cache
* presentation objects

A faction may exist while its associated settlement/chunk is:

* unloaded
* aggregate
* full
* absent from presentation

Do not make faction state disappear because a chunk is unloaded.

---

# 19. Tests

Add focused tests.

At minimum cover:

### Culture

* Culture IDs are stable.
* Culture traits are deterministic.
* Generated culture names are deterministic.
* Two generated cultures have distinct IDs.
* Culture does not depend on presentation.

### Faction

* Faction has stable identity.
* Faction references an existing culture.
* Faction generation is deterministic.
* Faction does not become a settlement.
* Faction does not require exploration knowledge.

### Membership

* Character/entity can be associated with a faction where the existing architecture permits.
* Invalid faction references fail safely.
* Membership mutation goes through the correct application/domain path.

### Relationships

* Faction relationships can be created.
* Invalid/self relationships are rejected if self-relations are forbidden.
* Symmetric relationships do not create duplicate state if the model is symmetric.
* Relationship state is independent from geography.

### Architecture regression

All existing Phase 8 tests must continue passing.

Do not weaken or delete existing tests to make Phase 9 pass.

---

# 20. Acceptance criteria

Phase 9 is complete only when:

1. `Culture` is a real domain concept.
2. `Faction` is a separate domain concept.
3. Faction references culture explicitly.
4. Membership/affiliation has an explicit domain seam.
5. Basic faction relationships exist.
6. IDs are strongly typed and stable.
7. Generated data is deterministic.
8. Directories use sparse storage where appropriate.
9. Domain state does not depend on Godot nodes.
10. Domain state does not depend on presentation.
11. Domain state does not depend on exploration knowledge.
12. Horizontal wrap remains unchanged and compatible.
13. Existing Phase 8 behavior is unchanged.
14. Appropriate tests exist.
15. Full test suite passes.
16. Solution builds with `-warnaserror`.
17. Godot headless runtime still boots.
18. Documentation is updated.

---

# 21. Documentation

Update only the documentation that is actually affected.

At minimum:

* `docs/DECISIONS.md`
* `docs/MVP_ROADMAP.md`
* `docs/WORLD_ARCHITECTURE.md`
* `docs/SIMULATION_ARCHITECTURE.md`
* `docs/DEVELOPMENT_LOG.md`

Create architecture decisions for important Phase 9 choices.

Continue numbering from the existing decision numbers.

Do not rewrite historical decisions.

If a design question cannot be resolved safely in Phase 9, record it as an `OD-xxx` instead of inventing a premature solution.

---

# 22. Important future resource-system boundary

Do NOT implement the natural-resource system in Phase 9 unless an existing Phase 9 dependency makes a minimal data seam unavoidable.

The future world will contain procedurally distributed natural resources such as:

* trees
* stone
* clay
* metal ores
* minerals
* wild berries
* plants
* other renewable/non-renewable resources

These resources will eventually:

* spawn/distribute deterministically across the world
* be discoverable by characters
* be harvested
* change state/quantity
* regenerate when renewable

However:

> Natural resources are a separate World/Simulation concern, not a Culture/Faction concern.

A faction does not create a tree merely because it controls a forest.

Do not implement resource spawning or regeneration in Phase 9.

---

# 23. What NOT to do

Do not:

* implement expeditions
* implement an explorer profession
* implement diplomacy UI
* implement wars
* implement combat
* implement trade
* implement resource economy
* implement resource nodes
* implement resource regeneration
* implement population growth
* implement migration
* implement faction AI
* implement territorial borders
* implement a final map
* rewrite Exploration
* change the ChunkCoordinate exploration key
* couple factions to Godot nodes
* couple factions to presentation
* introduce premature abstractions for systems that do not exist yet
* add large frameworks/dependencies

Prefer the smallest architecture that gives later phases a stable foundation.

---

# 24. Execution procedure

Work in this order:

### Step 1

Inspect the repository and existing architecture.

### Step 2

Identify existing entities that can be reused.

### Step 3

Design the minimal Phase 9 domain model.

### Step 4

Implement domain types.

### Step 5

Implement directories/state storage.

### Step 6

Implement application commands/systems where required.

### Step 7

Implement deterministic generation.

### Step 8

Add tests.

### Step 9

Add minimal debug inspection.

### Step 10

Update documentation and decisions.

### Step 11

Run:

```bash
dotnet test tests/Cultures.Tests/Cultures.Tests.csproj -warnaserror
```

### Step 12

Build the complete solution with warnings treated as errors.

### Step 13

Run the Godot headless smoke test.

### Step 14

Review the diff for accidental scope expansion.

---

# 25. Final report

When finished, report:

1. What was implemented.
2. Which files were changed.
3. Which architecture decisions were added.
4. Which open decisions remain.
5. Number of tests before/after.
6. Test command and result.
7. Build result.
8. Godot headless result.
9. Any known limitations.
10. Recommended next phase.

Do not start Phase 10.

Stop after Phase 9 and wait for further instructions.
