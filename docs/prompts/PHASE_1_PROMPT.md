# PHASE 1 — LOGICAL WORLD FOUNDATION

You are continuing development of the Cultures Successor project.

Phase 0 is complete. Before coding, read the following project documents:

* `GAME_DESIGN_BIBLE.md`
* `TECHNICAL_BIBLE.md`
* `ARCHITECTURE.md`
* `WORLD_ARCHITECTURE.md`
* `SIMULATION_ARCHITECTURE.md`
* `CURSOR_RULES.md`
* `DECISIONS.md`
* `MVP_ROADMAP.md`
* `DEVELOPMENT_LOG.md`

Phase 0 has already established:

* pure Domain assembly;
* typed persistent IDs;
* simulation clock;
* deterministic RNG;
* command system;
* event system;
* basic versioned save envelope;
* headless simulation host;
* automated tests.

Do NOT rewrite the Phase 0 foundation unless a real incompatibility with the canonical documents is discovered.

---

# OBJECTIVE

Implement **Phase 1 — Logical World Foundation**.

The goal is to establish the authoritative logical representation of the game world before implementing procedural generation or gameplay.

At the end of this phase the project must have a deterministic, testable logical world/grid system with horizontal world wrapping.

This is an ENGINEERING FOUNDATION task.

Do not attempt to make a complete game.

---

# STRICT SCOPE

Implement ONLY:

1. World coordinates
2. Chunk coordinates
3. Logical grid coordinates
4. Coordinate conversion
5. Horizontal wrapping
6. North/south world boundaries
7. Logical grid occupancy
8. Basic terrain cell data
9. Chunk boundaries
10. Minimal debug visualization/testing of the coordinate system

Do NOT implement:

* procedural terrain generation;
* biomes;
* climate;
* oceans generation;
* rivers;
* resources;
* animals;
* characters;
* buildings;
* pathfinding;
* economy;
* settlements;
* diplomacy;
* factions;
* fog of war;
* exploration;
* simulation LOD;
* save-game integration beyond whatever minimal interfaces are required;
* final art;
* production gameplay UI.

If something is required architecturally but not implemented yet, create the smallest appropriate abstraction/interface and document it rather than implementing future gameplay.

---

# 1. WORLD DIMENSIONS

Create a world configuration/domain concept.

It must define at minimum:

* world width in logical cells;
* world height in logical cells;
* chunk width;
* chunk height.

Do NOT hard-code these values throughout the codebase.

The exact final game dimensions are NOT decided yet.

Therefore dimensions must be configurable.

For tests, use small deterministic dimensions.

Example test world:

* width = 100 logical cells
* height = 50 logical cells
* chunk width = 10
* chunk height = 10

These are test values only.

---

# 2. COORDINATE TYPES

Create explicit coordinate/value types.

At minimum:

* `WorldCoordinate`
* `ChunkCoordinate`
* `LogicalGridCoordinate`

Do not represent these concepts everywhere using arbitrary `Vector2`, `Vector2I`, tuples or primitive integers.

Godot's vector types may be used at the presentation boundary, but domain code must have explicit logical coordinate concepts.

Each coordinate type must have:

* equality;
* hashing;
* useful string representation;
* serialization support where appropriate;
* deterministic behavior.

---

# 3. WORLD COORDINATES

The world is finite and horizontally wrapped.

Logical X must behave like a circular coordinate.

For a world width `W`:

```text
x = -1       → W - 1
x = 0        → 0
x = W - 1    → W - 1
x = W        → 0
x = W + 1    → 1
```

Create a central, tested normalization operation.

Do not duplicate wrap formulas across systems.

Negative coordinates MUST work correctly.

Use mathematical modulo behavior, not language-specific `%` assumptions that produce incorrect negative results.

---

# 4. NORTH / SOUTH BOUNDARIES

Y does NOT wrap.

For a world height `H`:

```text
y = 0        → valid
y = H - 1    → valid

y < 0        → outside world
y >= H       → outside world
```

Create explicit boundary validation.

Do not silently clamp invalid Y coordinates.

The system should distinguish:

* valid coordinate;
* horizontally normalized coordinate;
* outside-world coordinate.

---

# 5. WORLD POSITION NORMALIZATION

Provide a single authoritative way to normalize a world coordinate.

Example:

Input:

```text
(-1, 10)
```

becomes:

```text
(99, 10)
```

for width 100.

Input:

```text
(100, 10)
```

becomes:

```text
(0, 10)
```

Input:

```text
(101, 10)
```

becomes:

```text
(1, 10)
```

But:

```text
(50, -1)
```

must remain outside the world.

Likewise:

```text
(50, 50)
```

is outside for height 50.

---

# 6. CHUNKS

Implement logical chunk coordinates.

A chunk identifies a rectangular region of logical cells.

For example, with:

```text
world width = 100
chunk width = 10
```

there are:

```text
10 chunks horizontally
```

Chunk conversion must be deterministic:

```text
Logical cell
→ ChunkCoordinate
```

and:

```text
ChunkCoordinate + local coordinate
→ Logical cell
```

must be reversible for valid coordinates.

Be careful with negative X values because horizontal wrapping occurs before/while resolving chunk coordinates.

---

# 7. LOCAL CHUNK COORDINATES

If useful for the architecture, introduce an explicit local coordinate type rather than mixing:

```text
world coordinate
chunk coordinate
local coordinate
```

The following relationship must hold:

```text
world = chunk * chunkSize + local
```

with:

```text
0 <= local.x < chunkWidth
0 <= local.y < chunkHeight
```

After horizontal normalization, the relationship must remain deterministic across the world seam.

---

# 8. LOGICAL GRID

Create a minimal logical grid representation.

The grid must NOT depend on Godot TileMap.

It should conceptually support:

```text
GetCell(position)
SetCell(position, value)
IsInside(position)
```

and occupancy.

Do not overengineer this into a full ECS or terrain engine.

The purpose is to establish the authoritative spatial abstraction.

---

# 9. TERRAIN CELL DATA

Create a minimal `TerrainCell` or equivalent data structure.

For now it should contain ONLY information necessary for the foundation.

Possible initial fields:

* passability;
* terrain placeholder/type;
* occupancy information if architecturally appropriate.

Do not implement the final biome/resource system.

Create extension points for future terrain information without prematurely implementing it.

---

# 10. OCCUPANCY

The logical world must be able to determine whether a cell is occupied.

Do not create the full building/character occupancy system yet.

For now establish a generic or minimal occupancy abstraction that future systems can use.

Important:

A visual object's position must NOT be the authoritative occupancy state.

The logical grid is authoritative.

---

# 11. COORDINATE DISTANCE

Because the world wraps horizontally, ordinary:

```text
abs(a.x - b.x)
```

is NOT always the correct horizontal distance.

For example, with width 100:

```text
x = 99
x = 0
```

are one cell apart.

Create a reusable wrap-aware horizontal distance helper if it is useful to the architecture.

It must support:

```text
99 ↔ 0 = 1
0 ↔ 99 = 1
```

and choose the shortest horizontal distance.

Do not implement full pathfinding yet.

---

# 12. ISOMETRIC / RENDER COORDINATES

The project uses an isometric/pseudo-isometric visual presentation.

Do NOT make rendering coordinates authoritative.

Create a clean boundary between:

```text
LogicalGridCoordinate
        ↓
Render/IsometricCoordinate
        ↓
ScreenCoordinate
```

If a conversion is implemented in this phase, keep it minimal and deterministic.

The exact final tile dimensions and camera presentation are NOT decided yet.

Do not spend significant time on visual polish.

---

# 13. GODOT INTEGRATION

Create only enough Godot integration to verify the logical world.

A minimal debug scene/tool is acceptable.

For example:

* draw a small test grid;
* show coordinate values;
* demonstrate crossing the horizontal seam;
* demonstrate north/south boundaries.

The debug representation is NOT the final game map.

Do not create production UI.

---

# 14. TESTS

This phase is heavily test-driven.

Create deterministic tests for at least:

### Coordinate equality

Same values:

```text
(10, 20) == (10, 20)
```

Different values:

```text
(10, 20) != (11, 20)
```

### Horizontal wrapping

For width 100:

```text
-1 → 99
0 → 0
99 → 99
100 → 0
101 → 1
-101 → 99
```

### Vertical bounds

For height 50:

```text
0 → valid
49 → valid
-1 → invalid
50 → invalid
```

### World normalization

Test combinations of:

* negative X;
* X > width;
* valid Y;
* invalid Y.

### Chunk conversion

Test:

```text
(0,0)
(9,9)
(10,0)
(99,49)
```

with 10×10 chunks.

### Seam conversion

Verify that:

```text
x = 99
x + 1
```

wraps to:

```text
x = 0
```

and resolves to the correct chunk/local coordinate.

### Round trip

For valid positions:

```text
World
→ Chunk + Local
→ World
```

must return the original normalized world position.

### Wrap-aware distance

For width 100:

```text
distance(99, 0) = 1
distance(0, 99) = 1
```

### Occupancy

Verify:

* empty cell;
* occupied cell;
* setting occupancy;
* clearing occupancy;
* querying invalid positions.

---

# 15. DETERMINISM

The world coordinate system must not depend on random state.

Given the same world configuration and coordinate:

```text
result A == result B
```

always.

No uncontrolled randomness.

---

# 16. ARCHITECTURAL REQUIREMENTS

Respect:

* simulation-first architecture;
* Domain must not depend on Godot;
* stable IDs;
* separation of Domain/Application/Infrastructure/Presentation;
* definitions vs runtime state;
* commands/events;
* deterministic systems.

Do not introduce a `WorldManager` god object.

If a world abstraction is required, keep responsibilities separated.

For example, conceptually:

```text
WorldConfiguration
WorldCoordinateService
ChunkCoordinateService
LogicalGrid
TerrainCell
```

Exact class names may differ if a better design exists, but document the reasoning.

---

# 17. DO NOT OVERENGINEER

This is extremely important.

Do NOT build:

* ECS;
* complete terrain engine;
* complete navigation system;
* procedural generator;
* chunk database;
* multithreaded streaming;
* complete world serialization;
* final renderer.

We are establishing contracts that later systems can safely build on.

Prefer a small correct abstraction over a huge framework.

---

# 18. DOCUMENTATION

If implementation reveals an architectural decision that is not already covered:

1. update `DECISIONS.md`;
2. update the relevant Bible if necessary;
3. explain why.

Do not silently change an accepted architecture decision.

If a decision is genuinely unresolved, record it as an Open Decision instead of pretending it is final.

---

# 19. DEVELOPMENT REPORT

At the end of the task, update:

`DEVELOPMENT_LOG.md`

Use the project's development report format.

The report MUST include:

## Task

What Phase 1 was supposed to accomplish.

## Implemented

Exact functionality created.

## Verified

What was actually tested and confirmed working.

## Tests

Include:

* command used;
* number of tests;
* passed;
* failed;
* warnings/errors.

## Bugs found

Every meaningful bug encountered during implementation.

## Bugs fixed

For every fixed bug:

* symptom;
* root cause;
* solution;
* regression test.

## Known limitations

Anything intentionally incomplete.

## Architecture decisions

Any new decisions.

## Files changed

Important files.

## Current project health

At minimum:

* build;
* tests;
* runtime/debug verification.

## Next recommended step

Recommend ONLY the next logical development step.

## Notes for architectural review

If you encountered something that should be reviewed by the project owner/ChatGPT, explicitly state it here.

Do not hide uncertainty.

---

# 20. COMPLETION CRITERIA

Phase 1 is complete ONLY when:

* [ ] logical world coordinates exist;
* [ ] chunk coordinates exist;
* [ ] logical grid exists;
* [ ] horizontal wrapping works;
* [ ] negative X wrapping works;
* [ ] north/south boundaries work;
* [ ] chunk conversion works;
* [ ] chunk/local roundtrip works;
* [ ] occupancy foundation works;
* [ ] terrain cell foundation exists;
* [ ] wrap-aware horizontal distance works;
* [ ] coordinate tests pass;
* [ ] domain remains independent of Godot;
* [ ] project builds;
* [ ] existing Phase 0 tests still pass;
* [ ] new Phase 1 tests pass;
* [ ] development log is updated.

Do not start Phase 2 automatically.

---

# FINAL RESPONSE

When finished, report:

1. what was implemented;
2. architecture used;
3. files created/modified;
4. build result;
5. test result;
6. bugs found;
7. bugs fixed;
8. known limitations;
9. architecture decisions;
10. next recommended task.

Do not claim something works unless it was actually verified.
