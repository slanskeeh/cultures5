# PHASE 6 — EMERGENT SETTLEMENTS

## 0. ROLE

You are implementing **Phase 6 — Emergent Settlements** of Kinlands.

Read the project documentation before changing code:

* `README.md`
* `ARCHITECTURE.md`
* `SIMULATION_ARCHITECTURE.md`
* `WORLD_ARCHITECTURE.md`
* `CURSOR_RULES.md`
* `docs/DECISIONS.md`
* `docs/MVP_ROADMAP.md`
* `DEVELOPMENT_LOG.md`
* `docs/DEVELOPMENT_LOG.md`
* previous phase prompts and decisions

Treat accepted Architecture Decisions as authoritative.

Do not rewrite working systems merely to make them stylistically different.

Current systems include:

* authoritative logical world/grid;
* horizontal world wrapping;
* deterministic seed-based terrain;
* chunk-on-demand terrain;
* persistent autonomous characters;
* needs / health / aging / survival;
* buildings;
* workplaces;
* resources and production;
* storage;
* food consumption;
* shelter;
* deterministic simulation;
* parent/child relationships;
* children;
* skills;
* skill progression;
* skill inheritance;
* teaching;
* player character commands;
* compositional character state.

Current project health must remain green.

---

# 1. PHASE GOAL

Introduce **emergent settlements**.

A settlement must NOT be implemented as a manually placed gameplay object that defines where people live.

Instead:

> A settlement is a persistent social/geographical grouping that emerges from characters, buildings, workplaces, storage, shelter and sustained co-location.

The goal is to establish the first layer above individual characters and buildings:

```text
Characters
     ↓
Families
     ↓
Buildings
     ↓
Shared living / working area
     ↓
Community
     ↓
Settlement
```

The settlement should be a consequence of simulation rather than the cause of it.

---

# 2. CRITICAL DESIGN PRINCIPLE

Do NOT implement:

```text
Settlement
    Position
    Radius
    Population
```

as the authoritative model.

That would make settlements artificial map objects.

Instead the architecture should support:

```text
Characters
    ↓
where they live
where they work
where they store resources
where they sleep
who they interact with
    ↓
spatial/social clustering
    ↓
settlement state
```

A settlement may have an identity and state, but its population must be derived from actual characters.

---

# 3. SETTLEMENT EMERGENCE

A group of characters should become a settlement when sufficiently many people:

* live in the same general area;
* use the same infrastructure;
* have access to shared storage;
* have access to shelter;
* work in the area;
* remain there for a sustained period.

Do not use a single condition such as:

```text
Population >= 10
```

as the only criterion.

The final formula should be data-driven and configurable.

A settlement should require both:

```text
population density
+
persistence
```

and preferably some evidence of shared infrastructure.

---

# 4. NO HARD MAP BOUNDARY

Do not create a visible hard settlement border.

The player should not see:

```text
+----------------+
|   SETTLEMENT   |
|                |
+----------------+
```

as a permanent geometric object.

The settlement's area should be derived from actual occupied/lived/worked locations.

A settlement may be represented internally by:

* core location;
* population;
* associated buildings;
* associated characters;
* estimated area;
* settlement identity.

But the exact geographic boundary remains a derived concept.

---

# 5. SETTLEMENT CORE

When a settlement emerges, determine a stable **settlement core**.

The core should be based on the concentration of:

* inhabitants;
* shelters;
* storage;
* workplaces;
* buildings.

Do not simply use the first character's position.

The core should be deterministic.

The core is useful for:

* UI;
* future administration;
* future leadership;
* future trade;
* future diplomacy;
* future taxation;
* future political systems.

The core must not become the only source of truth for population membership.

---

# 6. SETTLEMENT MEMBERSHIP

Characters should belong to a settlement through derived or explicit membership state.

The architecture must support:

```text
Character
    ↓
Settlement membership
```

and:

```text
Settlement
    ↓
Population
```

However, avoid duplicating large lists unnecessarily.

The authoritative character identity remains `CharacterId`.

Do not create a second character identity for settlement membership.

---

# 7. MOVEMENT BETWEEN SETTLEMENTS

The architecture must allow a character to eventually:

* leave a settlement;
* join another settlement;
* live outside a settlement;
* establish a new settlement;
* migrate with a family.

Do NOT implement full migration gameplay yet.

But do not design the system so that:

```text
Character.SettlementId
```

is permanent and immutable.

Settlement membership is a social/geographical state, not an intrinsic property of the person.

---

# 8. SETTLEMENT LIFECYCLE

Settlement lifecycle should support at minimum:

```text
Emerging
Established
Declining
Abandoned
```

Exact final states are provisional.

A settlement can:

```text
Emerging
    ↓
Established
```

or:

```text
Established
    ↓
Declining
    ↓
Abandoned
```

A settlement must not necessarily disappear immediately when population falls below a threshold.

Use persistence/hysteresis to prevent:

```text
Established
→ Abandoned
→ Established
→ Abandoned
```

every few ticks.

---

# 9. SETTLEMENT ABANDONMENT

A settlement may become abandoned when:

* its population remains below a minimum;
* infrastructure is no longer maintained/used;
* inhabitants leave or die;
* no meaningful activity remains.

Abandoned settlements may retain:

* buildings;
* ruins;
* historical identity;
* former inhabitants in genealogy/history.

Do NOT automatically delete all associated buildings when a settlement disappears.

Future gameplay may allow players to discover abandoned settlements.

---

# 10. SETTLEMENT RE-EMERGENCE

A previously abandoned area should be capable of becoming inhabited again.

Do not permanently reserve an area for an old settlement.

Future behaviour should support:

```text
Old settlement abandoned
        ↓
years pass
        ↓
new population arrives
        ↓
new settlement emerges
```

Whether the new community inherits the old settlement identity remains an open question.

Do not silently make a permanent decision.

---

# 11. POPULATION CLUSTERS

The system should detect population clusters without scanning every character against every other character.

Avoid O(N²) algorithms.

Use spatially appropriate structures or existing world/chunk abstractions.

The intended scale is eventually:

```text
100
→ 1,000
→ 10,000
→ 100,000+ characters
```

The implementation does not need to support the final scale yet, but must not introduce obviously quadratic per-tick processing.

---

# 12. SPATIAL GROUPING

Characters are distributed across the logical world.

Settlement detection should operate on spatial regions/chunks or another scalable spatial abstraction.

The existing chunk architecture should be reused where appropriate.

Do NOT generate a separate full-world settlement grid.

A candidate region should be inspected only when necessary.

---

# 13. SHARED INFRASTRUCTURE

A settlement should be strongly associated with infrastructure.

Relevant infrastructure includes:

* shelters;
* storage;
* farms;
* workshops;
* other active buildings.

A group of wandering characters standing near each other should not automatically become a settlement.

A meaningful settlement requires some degree of built infrastructure.

---

# 14. BUILDINGS AND SETTLEMENTS

Buildings remain independent domain entities.

Do NOT make:

```text
Building → permanently belongs to Settlement
```

the only possible relationship.

Instead support the concept that a building can be:

* outside any settlement;
* associated with an emerging settlement;
* part of an established settlement;
* abandoned;
* reused by another settlement.

This prepares the system for:

* ruins;
* frontier buildings;
* isolated farms;
* camps;
* villages;
* cities.

---

# 15. FAMILIES AND SETTLEMENTS

Family relationships must remain independent from settlement membership.

Example:

```text
Family
├── Father
├── Mother
├── Child
└── Grandparent
```

may:

* live in one settlement;
* split between two settlements;
* migrate;
* live outside a settlement.

Do not assume:

```text
Family == Household == Settlement
```

These are different concepts.

---

# 16. HOUSEHOLD PREPARATION

Phase 6 should establish the architectural seam for a future **household** system.

Do NOT implement the complete household system yet.

The architecture should allow future concepts such as:

```text
Household
├── Members
├── Home
├── Property
├── Food
└── Family relationships
```

without changing the settlement model.

This distinction is mandatory:

```text
Genealogy
≠
Household
≠
Settlement
```

---

# 17. SETTLEMENT RESOURCES

A settlement should not magically own all resources produced by its inhabitants.

Current resource ownership remains with the existing inventory/storage architecture.

Settlement-level resource statistics may be derived from:

* active storage;
* buildings;
* household inventories;
* production.

Do not introduce a giant:

```text
SettlementInventory
```

that becomes the owner of everything.

Future taxation, trade and administration will need more granular ownership.

---

# 18. FOOD SECURITY

Phase 6 should expose enough settlement-level information to determine whether a community can sustain itself.

Possible derived values:

* stored Food;
* estimated consumption;
* production rate;
* population;
* shelter capacity.

Do not implement a complex economic model yet.

The settlement should simply be able to answer questions such as:

```text
Is this community producing enough food?
How many people live here?
How much food is stored?
How many shelters exist?
```

These values will later support happiness, taxation and political systems.

---

# 19. SHELTER CAPACITY

Use the Phase 4 shelter system.

A settlement's capacity should be derived from actual active shelter buildings.

Do not introduce arbitrary population capacity directly onto Settlement unless required.

Conceptually:

```text
Shelters
    ↓
Housing capacity
    ↓
Population
    ↓
Housing situation
```

This will later support:

* happiness;
* health;
* overcrowding;
* household formation.

---

# 20. COMMUNITY IDENTITY

An emergent settlement should receive a persistent identity once it becomes significant enough.

Introduce a `SettlementId` or equivalent persistent identity.

Do not use its map coordinates as its identity.

Two different settlements may exist at different times in approximately the same location.

Identity must be deterministic and saveable in the future.

---

# 21. SETTLEMENT NAME

Phase 6 may generate a temporary deterministic settlement name.

The final naming system is not required.

Names should be generated from deterministic data such as:

* settlement seed;
* settlement identity;
* culture placeholder;
* world seed.

Do NOT implement the final faction-language naming system yet.

However, avoid hard-coding English/Russian names directly into domain logic.

---

# 22. CULTURE PREPARATION

The future game contains multiple playable civilizations/factions with different:

* architecture;
* clothing;
* language;
* preferences;
* cultural identity.

Phase 6 does NOT implement the faction system.

However, settlement architecture must not assume:

```text
All settlements use one culture.
```

Prepare for:

```text
Settlement
    ↓
Culture/Faction identity
```

without implementing the full system.

A neutral/default culture may be used during this phase.

---

# 23. SETTLEMENT ECONOMIC ACTIVITY

A settlement should be able to derive basic activity from existing systems:

* production;
* consumption;
* work;
* storage;
* shelter.

Do not implement:

* taxation;
* trade;
* markets;
* currency;
* diplomacy.

Those belong to later phases.

---

# 24. SETTLEMENT LEADERSHIP PREPARATION

Do NOT implement full internal politics yet.

But the settlement model must allow future association with:

```text
Leader
Council
Influential characters
Offices
```

Do not make the settlement itself the leader.

A future leader must be a real `CharacterId`.

This is important because future political gameplay will depend on actual individuals with:

* age;
* skills;
* family;
* personality;
* reputation;
* influence.

---

# 25. SETTLEMENT STATISTICS

Introduce derived settlement statistics sufficient for debugging.

At minimum:

* total population;
* adults;
* children;
* elders;
* active buildings;
* shelters;
* storage;
* food stored;
* workers;
* unemployed adults;
* estimated food production;
* settlement stage.

These should be derived from authoritative domain state.

Do not duplicate values that can be cheaply derived.

---

# 26. CHARACTER LIFE CYCLE — IMPORTANT REVISION FROM PHASE 5

Phase 6 must correct the temporary newborn model from Phase 5.

Characters must now begin their lives at **age 0**.

Do NOT create newborns at age 6.

The lifecycle should begin at:

```text
Birth
  ↓
Newborn / Infant
  ↓
Child
  ↓
Adolescent
  ↓
Adult
  ↓
Elder
  ↓
Death
```

The exact age thresholds remain provisional.

---

# 27. NEWBORN BEHAVIOUR

Newborns must NOT behave like ordinary children.

At minimum they should:

* depend on caregivers;
* not navigate independently for normal activities;
* not work;
* not teach;
* not independently select workplaces;
* require appropriate care.

Do not implement a complex childcare system yet.

The architecture should provide a place for future caregiver logic.

---

# 28. INFANT DEPENDENCY

A newborn should have a relationship to one or more caregivers.

Initially this can be derived from parents.

Do not hard-code:

```text
Mother is always caregiver
```

because future systems may include:

* fathers;
* grandparents;
* adoptive parents;
* households;
* other caregivers.

The caregiver concept should remain extensible.

---

# 29. CHILD AGE TRANSITIONS

Life-stage transitions must happen naturally as simulation time passes.

Do not manually assign life stages at creation except for the initial newborn stage.

Example:

```text
Age = 0
LifeStage = Infant
```

then:

```text
Age reaches child threshold
→ Child
```

then:

```text
Age reaches adolescent threshold
→ Adolescent
```

etc.

Transitions must be deterministic.

---

# 30. CHILDREN IN SETTLEMENTS

Children should count toward:

* settlement population;
* food consumption;
* housing;
* demographic statistics.

But they should not count as:

* adult workers;
* available workforce;
* political candidates.

The exact political/worker rules remain future systems.

---

# 31. BIRTH AND SETTLEMENT GROWTH

The existing `CreateChildCommand` remains the basic birth seam.

Do not implement full reproduction yet.

However, settlement population must naturally change as:

```text
Births
+
Immigration
-
Deaths
-
Emigration
```

over time.

Only births/deaths are currently required.

Migration is future functionality but the architecture must not prevent it.

---

# 32. POPULATION SUSTAINABILITY

Do not hard-code settlements to always survive.

A settlement should be capable of:

* growth;
* stagnation;
* decline;
* abandonment.

This is essential to making the world feel alive.

The player should eventually be able to discover:

```text
thriving settlement
small village
dying community
abandoned settlement
```

rather than a world where every civilization remains permanently static.

---

# 33. DEBUG PRESENTATION

Update debug presentation to visualize:

* settlement core;
* settlement stage;
* population;
* settlement membership;
* buildings associated with the settlement;
* shelters;
* food;
* selected character's settlement;
* newborn/infant life stage.

Use simple debug graphics.

Do NOT create final pixel art.

---

# 34. DEBUG CONTROLS

Provide useful debug functionality where appropriate.

Possible controls:

* inspect settlement;
* cycle between settlements;
* create test population;
* advance simulation;
* inspect selected character's settlement;
* force settlement detection.

All debug operations must use domain/application commands.

Do not directly mutate domain state from Godot presentation code.

---

# 35. DETERMINISM

Settlement detection and creation must be deterministic.

Given:

```text
same world seed
+
same characters
+
same buildings
+
same commands
+
same simulation ticks
```

the same:

* settlements;
* SettlementIds;
* membership;
* lifecycle states;
* core locations;
* names;

must result.

Do not use wall-clock time.

Do not use uncontrolled randomness.

---

# 36. HORIZONTAL WRAP

Settlement spatial calculations must respect the existing horizontal world wrap.

A settlement spanning the seam must not be incorrectly interpreted as two distant groups.

For example:

```text
X = Width - 2
X = 0
```

may belong to the same geographic community.

Do not use ordinary absolute X distance.

Reuse the existing world topology/wrap abstractions.

---

# 37. POLES

The current world has provisional polar bands.

Do not make assumptions about geographic north/south beyond the current architecture.

Settlement detection should work independently of which Y edge is later declared north.

---

# 38. PERFORMANCE

Settlement detection must not execute expensive full-world scans every simulation tick.

Prefer:

```text
changed regions
+
periodic evaluation
+
chunk-local analysis
```

or another scalable strategy.

Settlement detection does not need to happen every simulation tick.

The exact evaluation interval should be configurable.

---

# 39. SAVE/LOAD

Settlement identity is persistent simulation state.

If dynamic save/load is still incomplete, document this limitation.

Do not serialize derived terrain.

Future saves should be able to reconstruct:

```text
Static terrain
+
Characters
+
Buildings
+
Families
+
Skills
+
Settlements
```

from authoritative state.

Do not make settlement identity dependent on Godot Nodes.

---

# 40. ARCHITECTURAL SEPARATION

Keep these concepts separate:

```text
World
Character
Family
Household
Building
Workplace
Settlement
Faction/Culture
```

They are related but not interchangeable.

Especially:

```text
Settlement ≠ Building
Settlement ≠ Family
Settlement ≠ Household
Settlement ≠ Faction
```

This separation is mandatory for future development.

---

# 41. FUTURE SYSTEMS THAT MUST REMAIN POSSIBLE

The architecture should prepare for later:

### Population

* migration;
* immigration;
* emigration;
* demographic growth;
* epidemics;
* population pressure.

### Economy

* taxation;
* markets;
* trade;
* wealth;
* ownership.

### Politics

* settlement leader;
* council;
* offices;
* influential characters;
* factions within settlements;
* political decisions.

### Diplomacy

* relations;
* treaties;
* trade agreements;
* alliances;
* wars.

### Culture

* faction identity;
* architecture;
* clothing;
* language;
* preferences;
* traditions.

### History

* settlement founding;
* settlement growth;
* important characters;
* historical buildings;
* family histories;
* abandoned settlements.

Do not implement these systems now.

---

# 42. TEST REQUIREMENTS

Add automated tests covering at minimum.

## Settlement emergence

* sufficient population can form an emerging settlement;
* isolated individuals do not automatically form settlements;
* infrastructure is considered;
* persistence is required;
* emergence is deterministic.

## Membership

* characters can be associated with the correct settlement;
* membership respects spatial grouping;
* characters outside the settlement are not incorrectly included;
* settlement membership remains independent of genealogy.

## Settlement lifecycle

* Emerging → Established works;
* Established → Declining works;
* Declining → Abandoned works;
* hysteresis prevents rapid state flipping;
* abandoned settlements retain identity where intended.

## Spatial logic

* settlement detection works across horizontal wrap;
* seam-spanning populations can form one settlement;
* distant populations do not merge incorrectly.

## Statistics

* population count is correct;
* children are included;
* adults are correctly classified;
* shelters are counted;
* food storage is derived correctly;
* workers are counted correctly.

## Life cycle

* new character starts at age 0;
* newborn life stage is correct;
* newborn eventually becomes child;
* child eventually becomes adolescent;
* adolescent eventually becomes adult;
* adult eventually becomes elder;
* death occurs according to existing survival/lifespan rules;
* transitions are deterministic.

## Birth

* `CreateChildCommand` creates age 0 character;
* parent relationships are preserved;
* inherited skills still work;
* newborn does not become a worker;
* newborn does not immediately behave as an independent adult.

## Regression

All previous tests must continue passing.

---

# 43. ACCEPTANCE CRITERIA

Phase 6 is complete only when:

* [ ] Settlements emerge from actual population/infrastructure.
* [ ] Settlements are not manually placed map objects.
* [ ] Settlement membership is derived/managed from real characters.
* [ ] Settlement core is deterministic.
* [ ] Settlement lifecycle exists.
* [ ] Settlements can decline and become abandoned.
* [ ] Abandoned settlement identity can persist.
* [ ] Settlement detection respects horizontal world wrapping.
* [ ] Settlement statistics are available.
* [ ] Buildings remain independent entities.
* [ ] Families remain independent from settlements.
* [ ] Household remains a future concept.
* [ ] Faction/culture remains a future concept.
* [ ] Future leadership can reference real `CharacterId`s.
* [ ] Population can grow and decline naturally.
* [ ] New characters are born at age 0.
* [ ] Newborn/infant is a distinct life stage.
* [ ] Newborns cannot work.
* [ ] Life-stage transitions are simulation-driven.
* [ ] Birth/inheritance/skill systems from Phase 5 continue working.
* [ ] Determinism is preserved.
* [ ] No O(N²) per-tick population algorithm is introduced.
* [ ] All tests pass.
* [ ] Solution builds with 0 warnings and 0 errors.
* [ ] Godot headless runtime starts successfully.
* [ ] Debug presentation allows settlement inspection.

---

# 44. ARCHITECTURAL RESTRICTIONS

Do NOT:

* create a player-placed Settlement building;
* make settlement radius the sole source of membership;
* make settlement own all resources;
* make settlement own characters;
* make family equal household;
* make household equal settlement;
* make faction equal settlement;
* implement politics;
* implement diplomacy;
* implement taxation;
* implement trade;
* implement migration gameplay;
* implement final culture system;
* implement final naming system;
* create CharacterManager/SettlementManager god-objects;
* scan every character against every character every tick;
* use Godot Nodes as domain identity;
* use presentation state as simulation state;
* introduce nondeterministic randomness;
* rewrite existing working systems unnecessarily.

---

# 45. DOCUMENTATION

Update:

* `docs/DECISIONS.md`
* `docs/MVP_ROADMAP.md`
* `docs/SIMULATION_ARCHITECTURE.md`
* `DEVELOPMENT_LOG.md`
* `docs/DEVELOPMENT_LOG.md`

Record Architecture Decisions for:

* emergent settlements;
* settlement identity;
* settlement lifecycle;
* settlement membership;
* spatial detection;
* settlement statistics;
* newborn/life-stage correction.

Record unresolved questions as `OD-*`.

Do not silently turn provisional mechanics into permanent design.

---

# 46. DEVELOPMENT REPORT

At the end of implementation update the development log using:

```text
## [DATE] — Task: Phase 6 Emergent Settlements

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

The report must be factual.

Do not claim GUI verification unless the GUI was actually performed.

Include exact:

* test count;
* build result;
* warning/error count;
* Godot runtime result;
* determinism verification.

---

# 47. FINAL VERIFICATION

Before declaring Phase 6 complete:

1. Run all tests.
2. Run regression tests.
3. Build the complete solution.
4. Confirm zero warnings and zero errors.
5. Start Godot headlessly.
6. Verify deterministic settlement generation.
7. Verify settlement emergence.
8. Verify settlement decline/abandonment.
9. Verify horizontal-wrap settlement detection.
10. Verify settlement statistics.
11. Verify newborn starts at age 0.
12. Verify newborn → child transition.
13. Verify existing inheritance and teaching systems still work.
14. Inspect debug presentation if possible.

If something cannot be visually verified, explicitly state that in the development report.

---

# 48. STOP CONDITION

When Phase 6 acceptance criteria are satisfied:

**STOP.**

Do not automatically begin Phase 7.

Do not implement diplomacy, politics, factions, migration, taxation, trade, final culture, final graphics, or world-scale streaming unless explicitly requested in a later phase.
