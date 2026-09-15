# PHASE 7 — LARGE WORLD AND SIMULATION LOD

## 0. ROLE

You are implementing **Phase 7 — Large World and Simulation LOD** of Kinlands.

Read all project documentation before changing code:

* `README.md`
* `ARCHITECTURE.md`
* `SIMULATION_ARCHITECTURE.md`
* `WORLD_ARCHITECTURE.md`
* `CURSOR_RULES.md`
* `docs/DECISIONS.md`
* `docs/MVP_ROADMAP.md`
* `GAME_DESIGN_BIBLE.md`
* `TECHNICAL_BIBLE.md`
* `DEVELOPMENT_LOG.md`
* `docs/DEVELOPMENT_LOG.md`
* previous phase prompts and relevant implementation

Inspect the actual existing world, population, building and settlement implementation before coding.

Do not rewrite working systems without a concrete architectural reason.

---

# 1. OBJECTIVE

Implement the first scalable simulation architecture for a very large world.

The goal is NOT merely visual chunk loading.

The goal is to support different levels of simulation detail depending on player proximity while preserving authoritative world state.

The world must conceptually support:

```text
Tier 0 — Full local simulation
Tier 1 — Nearby simplified simulation
Tier 2 — Regional aggregate simulation
Tier 3 — Very distant macro simulation
```

The exact final LOD thresholds are provisional.

---

# 2. CORE PRINCIPLE

The simulation is authoritative.

Presentation is optional.

A character/building/settlement does not cease to exist merely because it is not currently rendered.

The following principle is mandatory:

> **Visual representation ≠ simulation existence.**

Never solve LOD by deleting entities and recreating random replacements when the player returns.

---

# 3. CURRENT WORLD MODEL

The existing project already has:

* world coordinates;
* horizontal wrapping;
* chunks;
* deterministic terrain generation;
* characters;
* buildings;
* resources;
* families;
* skills;
* settlements.

Preserve these systems.

Phase 7 is an architectural expansion, not a replacement.

---

# 4. LOD TIERS

Implement explicit simulation detail levels.

## Tier 0 — Full Detail

Used for the player vicinity.

At this level the simulation may contain:

* individual characters;
* individual needs;
* current activity;
* exact position;
* inventory;
* family relationships;
* buildings;
* workplaces;
* local movement;
* exact production.

This is the most detailed simulation.

---

## Tier 1 — Reduced Detail

Used for nearby but not fully visible regions.

May simplify:

* character updates;
* movement;
* repeated routine actions;
* production;
* social activity.

However, important identity and historical state must remain available.

Do not yet aggressively aggregate people if it is unnecessary.

---

## Tier 2 — Regional Aggregate

Used for distant regions.

The simulation may represent population through aggregate state such as:

* total population;
* demographic composition;
* adult/child/elder counts;
* food stores;
* food production;
* consumption;
* housing capacity;
* employment;
* births/deaths;
* migration pressure;
* major events;
* settlement state.

Individual character simulation may be suspended.

However:

**important persistent individuals and relationships must remain representable.**

Do not assume every individual can simply be discarded.

---

## Tier 3 — Macro Simulation

Used for extremely distant areas.

Only major trends need to be simulated:

* population trend;
* food trend;
* economic trend;
* migration;
* settlement growth/decline;
* major political/diplomatic events when those systems exist.

Do not implement future politics/diplomacy now.

The architecture only needs extension points.

---

# 5. CHUNK STATE

Introduce explicit chunk simulation state.

A chunk may be:

```text
Unloaded
LoadedPresentation
FullSimulation
ReducedSimulation
AggregateSimulation
```

Exact naming may differ.

The important requirement is that:

**presentation loading and simulation detail are separate concepts.**

A chunk may have:

```text
No Godot nodes
+
Active aggregate simulation
```

and still be fully valid.

---

# 6. SIMULATION VS PRESENTATION

Do NOT use:

```text
Node exists → entity exists
```

or:

```text
Node removed → entity destroyed
```

The authoritative state remains in Domain/Application.

Godot nodes are projections.

This must remain true when LOD changes.

---

# 7. TRANSITION INTO AGGREGATE MODE

When an area leaves detailed simulation range:

```text
Detailed State
      ↓
Aggregation
      ↓
Aggregate State
```

The aggregation process must preserve important information.

At minimum preserve:

* population;
* age distribution;
* family/genealogy integrity;
* important character identities where required;
* settlement identity;
* building count/types;
* shelter capacity;
* resource quantities;
* food;
* production trends;
* historical facts.

Do not silently lose data.

---

# 8. TRANSITION BACK TO DETAILED MODE

When the player approaches:

```text
Aggregate State
      ↓
Reconstruction
      ↓
Detailed State
```

The reconstruction must be deterministic.

It must not simply:

```text
spawn 20 random villagers
```

Instead, it should produce a detailed state compatible with the aggregate state.

---

# 9. RECONSTRUCTION PRINCIPLE

Given:

```text
same aggregate state
+
same seed
+
same simulation time
+
same generation version
```

reconstruction should produce the same detailed result.

Use deterministic allocation/order.

Do not rely on:

* runtime object order;
* hash-map enumeration;
* uncontrolled random generation;
* wall-clock time.

---

# 10. IMPORTANT CHARACTERS

Not all characters necessarily need to disappear into anonymous aggregates.

The architecture must support a distinction between:

```text
Ordinary population
```

and:

```text
Important persistent individuals
```

Future examples:

* settlement leader;
* famous craftsman;
* family head;
* diplomat;
* commander;
* player-followed character;
* historical figure.

Phase 7 does NOT implement political or fame systems.

But the architecture must not make them impossible.

---

# 11. FAMILY INTEGRITY

Families are already represented through parent/child relationships.

LOD must not break genealogy.

If an individual is retained:

* parent links remain valid;
* child links remain valid where represented.

If a population is aggregated:

* aggregate demographic state must remain consistent;
* future detailed reconstruction must not produce impossible genealogy.

Do not create contradictory family structures during reconstruction.

---

# 12. SKILLS

Skills are persistent character state.

When detailed characters enter aggregate mode:

the system must define how skill information is preserved.

At minimum preserve aggregate distributions or sufficient statistics such as:

* mean skill;
* population count by skill band;
* important individual skills.

Do not silently reset everyone to skill 0 when returning to detailed simulation.

---

# 13. INVENTORIES AND RESOURCES

Aggregate simulation must preserve resource totals.

At minimum:

```text
Food
Wood
Stone
```

must remain consistent.

Do not duplicate resources during transitions.

Do not lose resources during transitions.

Invariant:

```text
Before aggregation total resources
=
After aggregation represented resources
```

except for resources legitimately consumed/produced during time advancement.

---

# 14. BUILDINGS

Buildings remain persistent domain entities.

A building must not disappear because its chunk entered aggregate mode.

The simulation may represent some building details in aggregate form.

At minimum preserve:

* BuildingId;
* type;
* location;
* lifecycle;
* settlement association;
* storage totals;
* production state where relevant.

---

# 15. SETTLEMENTS

Settlements are persistent entities.

LOD transitions must preserve:

* SettlementId;
* lifecycle;
* population;
* buildings;
* shelter;
* food;
* identity;
* core location;
* historical continuity.

Do not recreate a settlement as a new random settlement merely because the player approached its region.

---

# 16. POPULATION FLOWS IN AGGREGATE MODE

Distant populations must continue changing.

At minimum support:

```text
births
deaths
food consumption
food production
```

using aggregate rules.

Do not simulate every individual tick in distant regions.

The aggregate simulation should advance in larger steps where appropriate.

---

# 17. AGGREGATE TIME STEPS

Different simulation tiers may update at different frequencies.

Example:

```text
Tier 0
every normal simulation tick

Tier 1
every few ticks

Tier 2
hour/day scale

Tier 3
day/season scale
```

These are examples, not final values.

Centralize update frequencies.

Do not scatter magic intervals across systems.

---

# 18. CHARACTER IDENTITY

Persistent identities must remain globally unique.

Do not regenerate:

```text
CharacterId
```

just because a population moved between LOD tiers.

Entity IDs must survive:

```text
Detailed
→ Aggregate
→ Detailed
```

for preserved individuals.

---

# 19. CHUNK CACHE

Phase 2 introduced on-demand chunk terrain caching.

Phase 7 must extend this concept toward simulation chunks without prematurely building a full database.

The system should allow:

```text
World
 ├── generated terrain
 ├── chunk simulation state
 └── entity references/state
```

Do not create a single monolithic world object containing every live entity in every representation.

---

# 20. PLAYER VICINITY

The player camera position or relevant player-controlled area should determine which chunks require detailed simulation.

Do not make rendering FPS the decision mechanism.

Use simulation/application-level spatial state.

Future multiplayer is not required.

---

# 21. PLAYER-CONTROLLED CHARACTERS

A character explicitly selected/followed by the player must remain protected from inappropriate aggregation.

At minimum:

```text
currently selected character
+
currently player-commanded character
```

must remain in detailed simulation while actively controlled.

This must remain compatible with the contextual player-command architecture.

---

# 22. PLAYER COMMAND COMPATIBILITY

The existing direct character-control model must continue to work:

```text
Player
 ↓
CharacterId
 ↓
Command
 ↓
Simulation
```

A player command must not accidentally target an entity that has been destroyed by LOD.

If a target becomes unavailable due to LOD, the system must handle it through explicit state/command rules.

Do not mutate state from UI.

---

# 23. AGGREGATE EVENTS

Aggregate simulation may produce meaningful events.

Examples:

```text
PopulationChanged
FoodShortageDetected
SettlementDeclined
SettlementAbandoned
MigrationPressureChanged
BirthsOccurred
DeathsOccurred
```

Do not create fake per-character events for every aggregate population change.

History should distinguish:

* known individual event;
* aggregate demographic event.

---

# 24. NO FAKE HISTORY

When an aggregate simulation advances 5 years, do not invent 5 years of detailed individual history after the fact merely to make the region look busy.

Important history must be generated from actual aggregate rules or preserved identities.

Future systems can define richer historical summaries.

---

# 25. MIGRATION PREPARATION

Migration is a future major system.

Phase 7 must allow population groups to move between aggregate regions without requiring:

```text
spawn completely unrelated characters
```

The architecture should be able to represent:

```text
MigrationGroup
Origin
Destination
Population
Culture
Family/context
```

but do NOT implement complete migration gameplay.

---

# 26. DETERMINISM

LOD transitions must be deterministic.

Test:

```text
Run A:
Detailed
→ Aggregate
→ Advance
→ Detailed

Run B:
Detailed
→ Aggregate
→ Advance
→ Detailed
```

The resulting represented state must match.

Where exact individual reconstruction is intentionally not possible, define and test the aggregate invariants that must match.

Do not claim exact identity reconstruction if the design does not guarantee it.

---

# 27. PERFORMANCE

Do not optimize based on guesses.

The first goal is architectural correctness.

However:

* do not scan every world character every tick for LOD;
* do not rebuild every chunk every frame;
* do not create Godot nodes for distant populations;
* do not allocate giant arrays for the whole planet.

Use existing chunks and spatial information.

---

# 28. DEBUG PRESENTATION

Create a debug visualization for LOD.

At minimum show:

```text
Chunk
[Full]
[Reduced]
[Aggregate]
[Unloaded]
```

and optionally:

* entity count;
* population count;
* resource totals;
* simulation tier;
* chunk state.

This is a development tool, not final UI.

---

# 29. DEBUG CONTROLS

Useful debug controls may include:

* toggle LOD visualization;
* move test player/camera position;
* force aggregation;
* force reconstruction;
* inspect chunk simulation state;
* print aggregate statistics.

All debug operations must operate through application/domain interfaces.

---

# 30. TESTS

Add automated tests for:

## LOD classification

Given a player position and configured ranges, the expected chunks receive the correct simulation tier.

## Transition to aggregate

Detailed state aggregates successfully.

## Transition back

Aggregate state reconstructs a valid detailed state.

## Resource conservation

Resources are not duplicated/lost during LOD conversion.

## Population conservation

Population totals are preserved except for explicitly simulated births/deaths.

## Demographic conservation

Age-group totals remain coherent.

## Settlement conservation

Settlement identity and major statistics survive transitions.

## Building conservation

Building count/types/IDs remain valid.

## Family integrity

Preserved relationships remain valid.

## Skill preservation

Skill aggregates/reconstruction follow defined rules.

## Character IDs

Persistent IDs remain stable for preserved characters.

## Determinism

Equivalent runs produce equivalent state.

## Player protection

Selected/player-commanded characters are not improperly aggregated.

## Horizontal wrap

LOD spatial calculations respect world wrapping.

## Regression

All existing tests continue passing.

---

# 31. ACCEPTANCE CRITERIA

Phase 7 is complete only when:

* [ ] Explicit LOD tiers exist.
* [ ] Chunk simulation state exists independently from presentation.
* [ ] Detailed and aggregate simulation are distinct.
* [ ] Detailed state can be aggregated.
* [ ] Aggregate state can be reconstructed.
* [ ] Reconstruction is deterministic.
* [ ] Population is conserved across transitions.
* [ ] Resource totals are conserved.
* [ ] Buildings persist across transitions.
* [ ] Settlements persist across transitions.
* [ ] Important character identity can be preserved.
* [ ] Family integrity is preserved.
* [ ] Skill information is preserved according to documented rules.
* [ ] Distant populations continue simulated demographic/resource changes.
* [ ] Player-controlled characters are protected from invalid aggregation.
* [ ] Horizontal world wrap works with LOD spatial calculations.
* [ ] Godot presentation is optional for aggregate regions.
* [ ] No complete world simulation is performed at full detail.
* [ ] No random NPC respawn is used as an LOD mechanism.
* [ ] Phase 0–6 tests continue to pass.
* [ ] Phase 7 tests pass.
* [ ] Solution builds with 0 warnings and 0 errors.
* [ ] Godot headless runtime starts successfully.
* [ ] Development log is updated.

---

# 32. ARCHITECTURAL RESTRICTIONS

Do NOT:

* delete characters simply because they became distant;
* randomly respawn characters when returning to a region;
* treat Godot nodes as authoritative state;
* create a giant `WorldManager`;
* create a giant `LODManager` that owns all simulation rules;
* scan every character against every other character every tick;
* simulate the whole world at full detail;
* serialize only visual state;
* use wall-clock time;
* use nondeterministic randomness;
* implement complete migration;
* implement complete diplomacy;
* implement politics;
* implement final faction/culture simulation;
* redesign the existing terrain generator unnecessarily.

---

# 33. DOCUMENTATION

Update:

* `docs/DECISIONS.md`
* `docs/MVP_ROADMAP.md`
* `docs/SIMULATION_ARCHITECTURE.md`
* `docs/WORLD_ARCHITECTURE.md`
* `DEVELOPMENT_LOG.md`
* `docs/DEVELOPMENT_LOG.md`

Document:

* LOD tiers;
* chunk simulation states;
* aggregation rules;
* reconstruction rules;
* preserved individuals;
* population/resource invariants;
* update frequencies.

Any unresolved design question must be recorded as `OD-*`.

---

# 34. DEVELOPMENT REPORT

Update the development log with:

```text
## [DATE] — Task: Phase 7 Large World and Simulation LOD

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

Be factual.

Do not claim that a large world was performance-tested if only small debug dimensions were used.

---

# 35. FINAL VERIFICATION

Before declaring Phase 7 complete:

1. Run all tests.
2. Run regression tests.
3. Build the full solution.
4. Confirm 0 warnings and 0 errors.
5. Start Godot headlessly.
6. Verify LOD classification.
7. Verify detailed → aggregate conversion.
8. Verify aggregate → detailed reconstruction.
9. Verify population/resource invariants.
10. Verify settlement persistence.
11. Verify family integrity.
12. Verify protected player-controlled characters.
13. Verify horizontal wrap.
14. Verify deterministic repeated LOD transitions.
15. Inspect debug LOD visualization if possible.

If visual behavior was not manually inspected, state that explicitly.

---

# 36. STOP CONDITION

When Phase 7 acceptance criteria are satisfied:

**STOP.**

Do not automatically begin Phase 8.

Do not implement exploration, diplomacy, politics, migration gameplay, final culture systems, or final graphics.
