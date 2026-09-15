# PHASE 4 — BUILDINGS AND PRODUCTION

You are continuing development of the Cultures Successor project.

Phase 0, Phase 1, Phase 2 and Phase 3 are complete.

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

Also inspect the actual Phase 1–3 implementation before modifying it.

Treat the existing code and documentation as authoritative unless a real architectural incompatibility is discovered.

---

# OBJECTIVE

Implement **Phase 4 — Buildings and Production**.

Replace the temporary Phase 3 work/food loop with the first real building and production system.

At the end of this phase the simulation must support:

```text
Building
    ↓
Workplace
    ↓
Character works
    ↓
Production recipe
    ↓
Resource produced
    ↓
Resource stored
    ↓
Character can consume food
```

The system must be designed so that future environmental factors can modify production without requiring separate building types for every biome.

Example future behavior:

```text
Farm + temperate environment → Wheat
Farm + forest environment    → Berries
Farm + desert environment    → Dates
```

Do NOT implement the final biome-specific production catalogue yet.

The purpose of this phase is to establish the architecture that makes it possible.

---

# 1. CORE PRINCIPLE — BUILDING TYPE VS BUILDING INSTANCE

Clearly separate:

```text
BuildingDefinition
```

from:

```text
BuildingState / BuildingInstance
```

Definition describes what a building type is.

Instance describes a specific building existing in the world.

Conceptually:

```text
Farm Definition
    ↓
Farm Instance at (x, y)
    ↓
condition / workers / inventory / production state
```

Do not encode a specific world location into a building definition.

Do not use Godot Nodes as building identity.

---

# 2. BUILDING IDENTITY

Create a persistent:

```text
BuildingId
```

It must be independent of:

* array position;
* Godot Node;
* chunk;
* visual object.

Do not use array indexes as building identity.

The building ID must be suitable for future save/load.

---

# 3. BUILDING DEFINITIONS

Create a minimal definition system.

A definition should be able to describe at least:

* building type ID;
* name/key;
* footprint;
* whether it is passable/occupies cells;
* workplace information;
* production information where appropriate.

Do NOT create dozens of buildings.

For Phase 4 implement only a tiny development set.

Suggested:

* House / Shelter
* Farm
* Storage / Granary
* Workshop or simple production building

The exact names may follow existing project conventions.

Do not implement final art.

---

# 4. BUILDING PLACEMENT

Buildings must have an authoritative logical position.

Placement must validate:

* world bounds;
* horizontal wrapping;
* terrain passability;
* footprint availability;
* occupancy rules.

Do not put authoritative placement logic in Godot.

Godot should request a placement through the application/domain layer.

---

# 5. FOOTPRINTS

Do not assume every building occupies exactly one cell.

Create a minimal footprint concept.

For Phase 4 it is acceptable for most buildings to use:

```text
1×1
```

but the architecture must support:

```text
2×2
3×2
etc.
```

later.

A building footprint must be evaluated in logical world coordinates and must respect horizontal wrapping.

---

# 6. OCCUPANCY

Replace the Phase 3 assumption that occupancy is irrelevant.

The world must now distinguish at least:

* empty;
* building occupied;
* passable terrain;
* blocked terrain.

Do not make `TerrainCell` responsible for knowing every entity.

Keep terrain and dynamic occupancy conceptually separate.

A useful model is:

```text
Terrain
+
Dynamic Occupancy
```

The occupancy layer should be able to evolve later to support:

* buildings;
* characters;
* resources;
* workstations;
* roads;
* temporary objects.

Do not turn occupancy into a giant universal entity manager.

---

# 7. RESOURCE SYSTEM

Replace the Phase 3 personal integer food abstraction with a minimal generic resource model.

Introduce concepts such as:

```text
ResourceType
ResourceStack
Inventory
```

The exact class names may differ.

At minimum support:

* Food;
* Wood;
* Stone.

These are initial development resources, not the final catalogue.

Do not hard-code resource behavior into characters.

---

# 8. RESOURCE QUANTITIES

Resources must have quantities.

For example:

```text
Wood × 20
Food × 10
Stone × 15
```

The inventory must support:

```text
Add
Remove
Has
GetQuantity
```

and reject invalid operations deterministically.

Do not use floating point for ordinary resource quantities unless there is a concrete reason.

---

# 9. PRODUCTION RECIPES

Create a generic production recipe abstraction.

Conceptually:

```text
ProductionRecipe
    Inputs
    Outputs
    Duration
    Requirements
```

Example:

```text
Farm Recipe
    Input: none
    Output: Wheat/Food
    Duration: X
```

Example future recipe:

```text
Lumber
    Input: none
    Output: Wood
```

Do not implement crafting trees or a huge technology system.

---

# 10. PRODUCTION MUST BE DATA-DRIVEN

Do not implement:

```text
if building == Farm:
    produce wheat

if building == Workshop:
    produce tools
```

through a giant conditional chain.

Production should use definitions/recipes.

The system should conceptually support:

```text
BuildingDefinition
        ↓
ProductionDefinition
        ↓
ProductionRecipe
        ↓
Inputs / Outputs
```

This is important because later we want environmental modifiers.

---

# 11. ENVIRONMENTAL PRODUCTION MODIFIERS

Create an extension point for environmental production.

Do NOT implement the final system yet.

The production system must eventually be able to evaluate:

```text
Building
+
Terrain
+
Biome
+
Climate
+
Season
+
Recipe
```

and determine output.

For Phase 4, the environment may simply produce a neutral/default modifier:

```text
1.0x
```

Do not hard-code biome-specific products yet.

Document this as a future requirement.

---

# 12. WORKPLACES

Replace the Phase 3 shared work cell with actual workplaces.

A building may contain one or more logical work positions.

For Phase 4 a simple model is enough:

```text
Farm
 └── Workplace
```

A character assigned to that workplace performs work.

The architecture must eventually support:

```text
Building
 ├── Workplace A
 ├── Workplace B
 └── Workplace C
```

without rewriting the character activity system.

---

# 13. CHARACTER → WORKPLACE

Characters must be able to:

1. identify an available workplace;
2. travel to it;
3. start working;
4. remain there for the production action;
5. cause production;
6. receive/route the resulting resource.

Do not create a permanent employment/profession system yet.

For now a character can simply be:

```text
AssignedWorkplace
```

or equivalent.

Future professions will build on this.

---

# 14. PRODUCTION CYCLE

A minimal production cycle:

```text
Character
    ↓
Find workplace
    ↓
Move to workplace
    ↓
Work action
    ↓
Production duration
    ↓
Recipe completion
    ↓
Resource output
```

Production must happen through authoritative simulation state.

Do not spawn visual resource objects just to represent quantities.

---

# 15. STORAGE

Implement a minimal storage building.

The purpose is to establish:

```text
production
    ↓
storage
    ↓
consumption
```

A Storage/Granary building should have an inventory.

For Phase 4, it may have a large/simple capacity.

Do not implement logistics networks yet.

---

# 16. FOOD CONSUMPTION

Replace Phase 3 personal food generation.

Characters should obtain food from the new resource/storage system.

The exact logistics can be simplified.

For example:

```text
Character needs food
    ↓
Find accessible food storage
    ↓
Take food
    ↓
Consume
    ↓
Hunger decreases
```

The important thing is that food now comes from actual production.

Do not preserve the old:

```text
Work → personal integer food
```

mechanic.

That is now obsolete.

---

# 17. FOOD SHORTAGE

The system must support the possibility of insufficient food.

Do not guarantee infinite food.

For development:

```text
No food
→ character cannot eat
→ hunger continues increasing
```

This should eventually connect to survival.

Do not yet implement starvation balancing as final game design.

---

# 18. HOUSE / SHELTER

Implement a minimal shelter building.

It does NOT need a complete housing system yet.

Its purpose is to establish the building concept for future:

* sleep;
* family;
* household;
* ownership;
* population capacity.

Phase 4 may allow:

```text
Sleep
→ assigned/available shelter
```

instead of sleeping on any arbitrary land cell.

If this requires changing Phase 3's OD-012, update the decision accordingly.

---

# 19. BUILDING CONSTRUCTION

Do NOT implement a complete construction economy.

However, the architecture must distinguish:

```text
planned building
→ under construction
→ completed building
```

If implementation complexity is low, implement a minimal construction state.

Do not implement worker hauling, construction animations, or complex material logistics yet.

If full construction is too much for this phase, create the state abstraction and use instant/debug completion.

Document the limitation.

---

# 20. BUILDING LIFECYCLE

A building should have an explicit lifecycle/state.

At minimum:

```text
Planned
Constructing
Active
Disabled
```

or equivalent.

Production must only occur when the building is active.

---

# 21. BUILDING DESTRUCTION

Implement minimal destruction/removal capability if it fits the current architecture.

At minimum the logical occupancy must be cleared when a building is removed.

Do not implement damage systems.

---

# 22. CHARACTER AI CHANGES

Modify Phase 3 decision logic.

New priority should conceptually become:

```text
Critical hunger
    ↓
Find food
    ↓
Eat

Critical fatigue
    ↓
Find shelter/rest
    ↓
Sleep

Otherwise
    ↓
Find/perform work
```

The work target is now a real workplace.

Do not create a sophisticated profession planner.

---

# 23. MULTIPLE BUILDINGS

The development world should contain several buildings so that the system is actually exercised.

A deterministic bootstrap may create:

* 1 storage;
* 1 shelter;
* 1–2 farms;
* optional simple workshop.

Characters should be able to use them.

This is a temporary development setup.

Do not call this a settlement yet.

---

# 24. WORLD INTERACTION

Buildings must interact with the existing world:

```text
Terrain
+
Buildings
+
Characters
```

A building cannot be placed in invalid terrain.

Characters must be able to navigate around blocked building cells.

Water remains impassable.

Horizontal wrapping continues to work.

---

# 25. MOVEMENT AND BUILDINGS

Update navigation so buildings can block movement.

Do not allow the navigator to blindly walk through occupied building cells.

Do not implement doors/entrances yet unless necessary.

A simple passable/non-passable building footprint is sufficient.

---

# 26. DEBUG PRESENTATION

Extend the existing debug map.

Display:

* buildings;
* characters;
* farms;
* storage;
* shelter;
* workplace/activity state.

Keep visuals extremely simple.

Examples:

```text
F = Farm
S = Storage
H = House
C = Character
```

or equivalent visual markers.

This is development visualization, not final art.

---

# 27. DEBUG INSPECTION

The existing character inspector should show:

* current workplace;
* current action;
* hunger;
* fatigue;
* inventory/food if relevant.

Building inspection should show:

* BuildingId;
* building type;
* position;
* state;
* workers;
* inventory;
* production progress;
* current recipe.

---

# 28. DETERMINISM

The entire building/production loop must be deterministic.

Given:

```text
same seed
+
same world
+
same initial population
+
same building setup
+
same simulation ticks
```

the resulting state must be equivalent.

No uncontrolled randomness.

No wall-clock dependencies.

---

# 29. TESTS

Add tests for:

## Building identity

Different buildings have different stable IDs.

## Placement

Valid building placement succeeds.

Invalid terrain placement fails.

Occupied placement fails.

## Footprint

1×1 works.

At least one multi-cell footprint test should exist if multi-cell support is implemented.

## Horizontal wrap

A footprint crossing the world seam behaves correctly.

## Removal

Removing a building frees its occupied cells.

## Resources

Add/remove/query quantities.

Insufficient resources must fail deterministically.

## Recipes

Production recipe correctly consumes inputs and creates outputs.

## Production duration

Production does not complete before its duration.

Production completes deterministically.

## Workplace

Character can find an available workplace.

Character can move to it.

Character can perform Work.

## Food

Produced food reaches storage.

Character can obtain food.

Eating decreases hunger.

## Shortage

No food means eating cannot occur.

## Shelter

Character can use shelter/rest location if implemented.

## Navigation

Buildings block movement correctly.

## Simulation determinism

Run two identical simulations for a meaningful number of ticks and compare:

* buildings;
* inventories;
* production;
* character positions;
* needs;
* activities.

They must match.

---

# 30. SAVE ARCHITECTURE

Do not build the complete final save system.

However, building/resource state must be designed for future persistence.

Static generated terrain continues to be reconstructed from:

```text
seed
+
generation version
+
world configuration
```

Dynamic state will eventually need:

```text
characters
buildings
inventories
resources
activities
```

Do not duplicate generated terrain into saves.

If save schema changes are required, update the version and document it.

---

# 31. ARCHITECTURE — IMPORTANT

Maintain separation:

```text
Domain
 ├── BuildingState
 ├── BuildingDefinition
 ├── ResourceState
 ├── Inventory
 ├── ProductionRecipe
 ├── Workplace
 └── production rules

Application
 ├── building commands
 ├── production orchestration
 └── character/building interaction

Presentation
 └── debug rendering
```

Do not put production logic inside Godot nodes.

Do not make `BuildingManager` responsible for every building-related rule.

Do not create a giant economy manager.

---

# 32. FUTURE REQUIREMENT — DO NOT BREAK THIS

The production architecture must eventually support:

```text
Farm
+
Temperate
→ Wheat

Farm
+
Forest
→ Berries

Farm
+
Desert
→ Dates
```

without creating:

```text
TemperateFarm
ForestFarm
DesertFarm
```

The building type represents the **function**.

The environment influences the **result**.

Keep this distinction explicit.

---

# 33. FUTURE REQUIREMENT — SEASONS

Do not implement seasons yet.

However, production should eventually be able to depend on temporal/environmental modifiers.

Do not design recipes so that production duration/output can ONLY ever be a fixed constant.

A clean future extension should be possible:

```text
Base Recipe
    ↓
Environmental Modifier
    ↓
Seasonal Modifier
    ↓
Final Production Result
```

---

# 34. FUTURE REQUIREMENT — SKILLS

Do not implement character skills yet.

But production should eventually be able to depend on:

```text
Worker
+
Skill
+
Building
+
Environment
→
Production quality/output
```

Do not hard-code the worker as irrelevant to production forever.

---

# 35. DOCUMENTATION

Update documentation whenever required.

If introducing a new architectural decision:

1. update `DECISIONS.md`;
2. update the appropriate Bible;
3. explain the reasoning.

Mark temporary values explicitly.

If an old Phase 3 decision becomes obsolete, do not delete history.

Mark it superseded and reference the new decision.

---

# 36. DEVELOPMENT REPORT

Update:

`DEVELOPMENT_LOG.md`

Include:

### Task

### Implemented

### Verified

### Tests

Exact:

* command;
* total;
* passed;
* failed;
* skipped;
* warnings/errors.

### Bugs found

### Bugs fixed

For each:

* symptom;
* cause;
* solution;
* regression test.

### Known limitations / TODO

### Architecture decisions

### Superseded decisions

### Files changed

### Current project health

### Next recommended step

### Notes for architectural review

---

# 37. COMPLETION CRITERIA

Phase 4 is complete only when:

* [ ] BuildingId exists;
* [ ] BuildingDefinition exists;
* [ ] BuildingState/Instance exists;
* [ ] building placement works;
* [ ] building occupancy works;
* [ ] footprint abstraction exists;
* [ ] resource types exist;
* [ ] inventories exist;
* [ ] production recipes exist;
* [ ] workplaces exist;
* [ ] characters can work at buildings;
* [ ] production occurs;
* [ ] produced resources are stored;
* [ ] characters can consume produced food;
* [ ] starvation/food shortage can occur;
* [ ] shelter exists;
* [ ] buildings affect navigation;
* [ ] building lifecycle exists;
* [ ] deterministic bootstrap contains multiple buildings;
* [ ] debug visualization shows buildings;
* [ ] debug inspection works;
* [ ] simulation remains deterministic;
* [ ] Phase 0 tests pass;
* [ ] Phase 1 tests pass;
* [ ] Phase 2 tests pass;
* [ ] Phase 3 tests pass;
* [ ] Phase 4 tests pass;
* [ ] build has 0 warnings / 0 errors;
* [ ] development log is updated.

Do NOT start Phase 5 automatically.

---

# FINAL RESPONSE

When finished, report:

1. implemented building architecture;
2. resource architecture;
3. production architecture;
4. workplace/character integration;
5. food flow;
6. shelter implementation;
7. navigation changes;
8. files changed;
9. build result;
10. test result;
11. determinism verification;
12. bugs found;
13. bugs fixed;
14. known limitations;
15. architecture decisions;
16. superseded decisions;
17. debug/visual verification;
18. next recommended task.

Do not claim visual behavior was verified if only headless tests were run.
