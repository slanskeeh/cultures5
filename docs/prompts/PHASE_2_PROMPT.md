# PHASE 2 — MINIMAL DETERMINISTIC WORLD GENERATION

You are continuing development of the Cultures Successor project.

Phase 0 and Phase 1 are complete.

Before coding, read:

* `GAME_DESIGN_BIBLE.md`
* `TECHNICAL_BIBLE.md`
* `ARCHITECTURE.md`
* `WORLD_ARCHITECTURE.md`
* `SIMULATION_ARCHITECTURE.md`
* `CURSOR_RULES.md`
* `DECISIONS.md`
* `MVP_ROADMAP.md`
* `DEVELOPMENT_LOG.md`

Treat these documents as the source of truth.

Do not silently replace architectural decisions.

---

# OBJECTIVE

Implement **Phase 2 — Minimal World Generation**.

The goal is to generate a deterministic large-world foundation from a seed.

The generated world must have:

* deterministic seed;
* macro geography;
* elevation;
* water;
* basic climate;
* basic biome classification;
* deterministic chunk generation/access;
* correct horizontal wrapping.

This is still a FOUNDATION phase.

Do not implement civilizations, characters, resources, wildlife or gameplay.

---

# IMPORTANT DESIGN PRINCIPLE

The world should not feel like several unrelated noise maps layered together.

Generation should follow a causal hierarchy:

```text
WORLD SEED
    ↓
MACRO GEOGRAPHY
    ↓
ELEVATION
    ↓
WATER / SEA
    ↓
CLIMATE
    ↓
BIOME
```

Future systems will extend this:

```text
BIOME
    ↓
SOIL / FERTILITY
    ↓
RESOURCES
    ↓
WILDLIFE
    ↓
SETTLEMENT SUITABILITY
```

Do not implement those future layers yet.

The architecture must leave room for them.

---

# 1. WORLD SEED

Create a deterministic world-generation seed.

The seed must be an explicit value in world configuration/generation input.

Same:

```text
seed + generation version + world configuration
```

must always produce the same world.

Different seeds should normally produce different worlds.

The generation version must be recorded so that future generator changes can be distinguished from old worlds.

Do not use uncontrolled global randomness.

---

# 2. GENERATION VERSION

Introduce an explicit generation version.

Example:

```text
WorldGenerationVersion = 1
```

Do not hard-code this value into unrelated classes.

The purpose is future save compatibility:

```text
Seed = 12345
GenerationVersion = 1
```

is a different generation contract from:

```text
Seed = 12345
GenerationVersion = 2
```

even if the seed is identical.

---

# 3. MACRO GEOGRAPHY

Generate large-scale geographic structure.

The first implementation should create large coherent regions rather than high-frequency noise.

The world should contain recognizable:

* land masses;
* seas/oceans;
* large inland areas;
* geographic variation.

Avoid making every neighboring cell completely independent.

Use deterministic procedural techniques appropriate for a large world.

The exact algorithm is an implementation detail.

Do NOT prematurely commit the entire project to one noise library or third-party generator.

Prefer a small abstraction such as:

```text
IWorldGenerator
IHeightGenerator
IClimateGenerator
IBiomeClassifier
```

only where those abstractions genuinely improve separation.

Do not create dozens of interfaces just for the sake of abstraction.

---

# 4. ELEVATION

Each terrain cell should receive a deterministic elevation value.

At minimum, elevation should be representable numerically.

For example:

```text
0.0 → lowest
1.0 → highest
```

Exact representation can differ if there is a strong architectural reason.

Elevation should be generated before water classification.

The same coordinates and seed must always return the same elevation.

---

# 5. WATER

Introduce a configurable sea-level threshold.

Conceptually:

```text
elevation < seaLevel
    → water

elevation >= seaLevel
    → land
```

Do not create detailed rivers/lakes in this phase unless they naturally fall out of the chosen minimal algorithm.

We will handle hydrology in a later phase.

Water must be represented as authoritative terrain data, not merely a visual effect.

---

# 6. CLIMATE

Introduce a minimal climate model.

The first version should be based on deterministic spatial factors rather than random values.

At minimum consider:

* latitude;
* elevation.

The exact formula is not final.

The important architectural requirement is:

```text
same world + same coordinate
→ same climate
```

Climate should be represented independently from biome.

For example, do not make:

```text
Biome = Desert
```

the source of climate.

Instead:

```text
Climate
    ↓
Biome classification
```

---

# 7. LATITUDE

The world has:

* north;
* equatorial/middle regions;
* south;
* polar regions.

Horizontal wrapping must NOT affect latitude.

Latitude is derived from Y position.

The exact definition of which Y coordinate is "north" is currently unresolved.

Do not silently declare a final north/south convention as a permanent game-design decision.

If necessary, introduce a configuration value or document the implementation assumption as temporary.

---

# 8. POLAR REGIONS

The design specifies polar ice/glacial regions at the north and south boundaries.

For this phase, represent polar climate/biome conditions.

Do not create detailed ice-sheet simulation.

The goal is only to establish that:

```text
Y near northern boundary
→ cold polar climate

Y near southern boundary
→ cold polar climate
```

---

# 9. BIOMES

Create a minimal biome classification.

The exact final biome list is NOT fixed.

Use a small initial set.

For example:

```text
Ocean
Ice
Tundra
TemperateLand
Forest
Desert
```

These are placeholders, not the final content list.

The biome classifier should consume climate + elevation/water information.

Do not introduce resources or production yet.

---

# 10. TERRAIN DATA

Extend the Phase 1 `TerrainCell`.

It should now be capable of representing at least:

* elevation;
* water/land;
* climate;
* biome.

Keep this data authoritative in the Domain.

Do not put authoritative terrain information into Godot TileMap nodes.

---

# 11. CHUNK GENERATION

World generation must work correctly with chunks.

A chunk should be able to obtain deterministic terrain data for its cells.

Important:

Generation must NOT require generating the entire planet into one giant in-memory array.

For this phase, it is acceptable to generate/load a chunk on demand.

However, do not implement the complete production streaming system yet.

That belongs to later work.

---

# 12. WORLD SEAM

The horizontal seam must behave as a single continuous world.

For world width W:

```text
x = 0
```

and:

```text
x = W
```

represent the same horizontal location after normalization.

Generation must respect this.

Avoid a visible artificial discontinuity at:

```text
x = 0 / x = W - 1
```

If the selected generation technique produces a seam, solve the problem rather than accepting a visible discontinuity.

This is particularly important for macro geography.

---

# 13. WORLD CONTINUITY

The following must be deterministic:

```text
Generate(seed, configuration)
```

and:

```text
Generate(seed, configuration)
```

must produce identical terrain.

Also:

```text
GetCell(x, y)
```

and:

```text
GetCell(normalizedWrappedX, y)
```

must return equivalent terrain.

---

# 14. NO GAMEPLAY CONTENT

Do NOT implement:

* resources;
* trees as resource entities;
* animals;
* characters;
* buildings;
* settlements;
* factions;
* civilizations;
* diplomacy;
* economy;
* pathfinding;
* exploration;
* fog of war;
* simulation LOD.

Those belong to later phases.

You may create placeholder enum/data values required to represent terrain.

---

# 15. DEBUG VISUALIZATION

Upgrade the Phase 1 debug visualization so the generated world can be inspected.

The debug view should allow us to visually distinguish at least:

* water;
* land;
* mountains/high elevation;
* cold/polar areas;
* major biome classes.

This is NOT final art.

A simple debug visualization is preferred.

The purpose is to answer:

> Does the generated world actually look geographically plausible?

The debug visualization should make the horizontal seam easy to inspect.

---

# 16. TESTS

Add automated tests for:

## Seed determinism

Same seed/config:

```text
world A == world B
```

Different seeds should produce different terrain in a reasonable sample.

## Generation version

Generation version must participate in the generation contract.

## Coordinate determinism

Same coordinate + same seed:

```text
result A == result B
```

## Horizontal wrapping

Terrain at:

```text
x = -1
```

must equal terrain at:

```text
x = W - 1
```

after normalization.

Likewise:

```text
x = W
```

must equal:

```text
x = 0
```

## Vertical bounds

Coordinates outside north/south boundaries must remain invalid.

## Chunk determinism

Generating the same chunk twice must produce identical results.

## Chunk seam

Adjacent chunks must agree on shared boundaries.

## Climate determinism

Same seed/config/coordinate produces identical climate.

## Biome determinism

Same seed/config/coordinate produces identical biome.

## Basic causal correctness

At minimum test:

* cells below sea level become water;
* water is classified consistently;
* polar latitude produces cold conditions;
* biome classification is deterministic.

Do not write tests that lock us into arbitrary final biome thresholds unless those thresholds are explicitly documented as temporary.

---

# 17. PERFORMANCE

Do not optimize prematurely.

However:

* do not generate the entire intended final planet in the constructor;
* do not create a Godot Node for every cell;
* do not allocate unnecessary objects per terrain cell if a compact representation is sufficient.

The architecture must allow future chunk streaming.

---

# 18. ARCHITECTURE

Maintain:

```text
Domain
    ↓
Application
    ↓
Infrastructure
    ↓
Presentation
```

Generation belongs primarily to the Domain/Application boundary.

Godot must not become responsible for authoritative world generation.

The debug renderer consumes generated data.

It does not own it.

---

# 19. SAVE SYSTEM

Do not implement complete terrain serialization yet.

Because generation is deterministic, future saves should eventually be able to reconstruct static terrain from:

```text
seed
+
generation version
+
world configuration
```

Do not duplicate the entire static terrain into save data unless later requirements prove this necessary.

You may update the existing save/world metadata contracts if needed.

---

# 20. DOCUMENTATION

If implementation requires a new architectural decision:

1. update `DECISIONS.md`;
2. update the appropriate Bible;
3. document the reason.

If the exact world-generation algorithm is intentionally provisional, say so.

Do not pretend that temporary generation constants are final game design.

---

# 21. DEVELOPMENT REPORT

At the end, update:

`DEVELOPMENT_LOG.md`

Include:

### Task

What Phase 2 was intended to accomplish.

### Implemented

Exact functionality.

### Verified

What was actually checked.

### Tests

* command;
* total;
* passed;
* failed;
* warnings/errors.

### Bugs found

Every meaningful problem.

### Bugs fixed

Symptom → cause → solution → regression test.

### Known limitations

Especially:

* temporary generation parameters;
* provisional biome list;
* world dimensions;
* north/south orientation;
* any seam limitations.

### Architecture decisions

All new AD entries.

### Files changed

### Current project health

### Next recommended step

### Notes for architectural review

Explicitly call out anything where design rather than implementation needs a decision.

---

# 22. COMPLETION CRITERIA

Phase 2 is complete only when:

* [ ] deterministic seed exists;
* [ ] generation version exists;
* [ ] elevation exists;
* [ ] water/land classification exists;
* [ ] climate foundation exists;
* [ ] biome foundation exists;
* [ ] generation is deterministic;
* [ ] horizontal seam is continuous;
* [ ] chunk generation is deterministic;
* [ ] adjacent chunks agree at boundaries;
* [ ] terrain is authoritative in Domain;
* [ ] debug visualization displays generated terrain;
* [ ] Phase 0 tests still pass;
* [ ] Phase 1 tests still pass;
* [ ] Phase 2 tests pass;
* [ ] build has 0 warnings / 0 errors;
* [ ] development log is updated.

Do NOT start Phase 3 automatically.

---

# FINAL RESPONSE

When finished, report:

1. implemented systems;
2. generation architecture;
3. generation algorithm used;
4. files changed;
5. build result;
6. test result;
7. bugs found;
8. bugs fixed;
9. known limitations;
10. architecture decisions;
11. visual/debug verification;
12. next recommended task.

Do not claim visual behavior was verified if only headless tests were run.
