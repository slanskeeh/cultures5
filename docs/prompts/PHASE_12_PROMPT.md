# Phase 12 — Military

## Context

Phase 11 — Internal Politics is COMPLETE.

Current verified state:

* 188/188 tests passing.
* Solution build: 0 warnings / 0 errors.
* Godot 4.7.2.stable.mono headless runtime boots.
* Culture, Faction, Diplomacy and Internal Politics are separate domains.
* Faction relations are sparse and symmetric.
* Internal political groups are faction-local.
* Political group influence is explicit 0–100 and is NOT population.
* Internal stability is sparse 0–100, missing = 50.
* Internal Politics has no automatic consequences and does not tick in `SimulationHost.Step`.
* Exploration is independent from civilization/politics.
* Factions do not own geography.
* No resource system exists yet.
* No war or military system exists yet.

## Goal

Implement the **domain foundation for Military**.

The goal is to introduce the minimum authoritative military model required for future combat, armies, units and war.

Do NOT build a complete combat game in this phase.

---

# 1. Inspect first

Before coding, inspect:

* `docs/DECISIONS.md`
* `docs/MVP_ROADMAP.md`
* `docs/WORLD_ARCHITECTURE.md`
* `docs/SIMULATION_ARCHITECTURE.md`
* `docs/ARCHITECTURE.md`
* Phase 9 civilization code
* Phase 10 diplomacy code
* Phase 11 internal politics code
* CharacterState
* SettlementState
* existing IDs
* command/event patterns
* deterministic generation
* simulation step architecture

Do not duplicate existing abstractions.

---

# 2. Phase 12 scope

Implement only:

* military identity
* military unit/force foundation
* faction ownership
* character assignment to military units where appropriate
* basic military state
* authoritative commands
* validation
* deterministic creation
* sparse storage
* events where useful
* tests
* minimal debug inspection
* documentation

---

# 3. Strictly DO NOT implement

Do NOT implement:

* actual combat resolution
* damage
* weapons
* armor calculations
* attack animations
* pathfinding
* battles
* sieges
* armies moving across the world
* war declaration
* war goals
* casualties simulation
* morale simulation
* military AI
* recruitment economy
* resource consumption
* equipment inventory
* territory
* conquered land
* diplomacy consequences
* rebellions
* player combat UI

Hostile diplomacy is NOT war.

Military must remain independent from diplomacy in this phase.

---

# 4. Military domain

Introduce a separate military domain, for example:

```text
MilitarySystem
MilitaryCommands
MilitaryEvents
MilitaryRules
```

Use naming consistent with the existing project.

Military must not become a property directly embedded into `FactionState`.

---

# 5. Military Unit

Introduce a minimal strongly-typed:

```text
MilitaryUnitId
```

and:

```text
MilitaryUnitState
```

Conceptually:

```text
MilitaryUnit
 ├── Id
 ├── FactionId
 ├── Name
 ├── Type
 └── State
```

Keep it minimal.

Potential unit types may include:

```text
Warband
Guard
Army
```

but inspect the roadmap before hardcoding terminology.

If the roadmap does not require multiple unit types yet, use one generic military unit rather than inventing a hierarchy.

---

# 6. Faction ownership

Every military unit belongs to exactly one faction.

Invariant:

```text
MilitaryUnit.FactionId
```

must reference an existing faction.

Do not allow a military unit without a faction unless the architecture explicitly requires mercenaries/neutral forces.

Do not allow a unit to belong to multiple factions.

---

# 7. Character membership

If characters can be assigned to military units in the current architecture, support:

```text
CharacterState.MilitaryUnit
```

as optional membership.

Rules:

* character may belong to zero or one military unit
* military unit must exist
* character faction must match unit faction
* cross-faction assignment fails
* leaving a faction clears military membership
* assigning/removing military membership does not change culture
* assigning/removing military membership does not change political group automatically
* assigning/removing military membership does not modify diplomacy

Do not duplicate the entire roster inside the military unit.

Member counts should be derived from the character roster where practical.

---

# 8. Military state

Introduce only state that is needed for future military simulation.

Do not invent detailed combat statistics yet.

A minimal state could represent:

* Active
* Disbanded

or another small lifecycle model justified by the roadmap.

If a lifecycle is unnecessary, omit it.

Do NOT add HP, attack, defense, damage, armor or morale yet.

---

# 9. Position / geography

Do not make military units own chunks.

If a military unit needs a location field for future expansion, use the existing world-coordinate abstraction rather than inventing a military-specific coordinate system.

However:

> Do not implement movement in Phase 12.

A unit's position, if introduced, is only authoritative state.

Do not connect military positioning to exploration knowledge.

---

# 10. Spatial footprint compatibility

Current debugging treats characters/buildings as occupying one tile.

Do not rewrite the entire spatial system in this phase unless required.

However, DO NOT introduce architecture that permanently assumes:

```text
every entity occupies exactly one tile
```

Future buildings will occupy arbitrary sets of map points/cells rather than necessarily rectangular footprints.

Where spatial occupancy is required, prefer an abstraction compatible with:

```text
SpatialFootprint
```

where the current implementation can simply represent one cell.

Do not implement the full multi-cell building system yet.

---

# 11. Commands

Use the existing authoritative command pattern.

Potential commands:

```text
CreateMilitaryUnitCommand
AssignCharacterToMilitaryUnitCommand
RemoveCharacterFromMilitaryUnitCommand
DisbandMilitaryUnitCommand
```

Only implement commands justified by the final model.

Presentation/debug code MUST NOT directly mutate military state.

---

# 12. Validation

At minimum:

### Create unit

* faction must exist
* ID must be unique
* invalid references fail

### Character assignment

* character must exist
* unit must exist
* character faction must equal unit faction
* cross-faction assignment fails
* character cannot silently switch units unless explicitly commanded

### Removal

* valid membership is removed
* removing nonexistent membership behaves consistently with project command conventions

### Disband

If implemented:

* unit enters valid terminal state
* character memberships are cleared or handled explicitly
* no hidden faction/diplomacy consequences

Do not silently repair invalid state.

---

# 13. Determinism

Military generation must be deterministic.

Do not consume `SimulationHost.Random` for names or static identity generation if deterministic hashing/generation is already established.

Same:

```text
world seed
+
faction identity
+
unit identity
```

must produce the same generated result.

Do not use Godot instance IDs as domain identity.

---

# 14. Diplomacy isolation

Phase 12 MUST NOT turn:

```text
Hostile
```

into:

```text
War
```

Do not automatically create military units from diplomacy.

Do not automatically move military units because of diplomatic relations.

Do not modify:

* `FactionRelationDirectory`
* diplomatic stance
* internal stability
* political group influence

as a side effect of military state changes.

---

# 15. Internal Politics isolation

Military membership does not automatically:

* increase political influence
* change political group
* change internal stability
* create political conflict

Future military/political interaction belongs to later phases.

---

# 16. Exploration isolation

Military state must not:

* reveal chunks
* scout terrain
* change exploration knowledge
* modify fog
* generate terrain

Exploration remains its own domain.

---

# 17. Resource isolation

There is still no resource system.

Do not implement:

* iron requirements
* food consumption
* weapons resources
* recruitment costs
* upkeep
* mining
* resource regeneration

These belong to future resource/economy phases.

---

# 18. Events

Use events only for meaningful facts.

Possible:

```text
MilitaryUnitCreatedEvent
MilitaryMembershipChangedEvent
MilitaryUnitDisbandedEvent
```

Events must not automatically trigger:

* combat
* war
* diplomacy changes
* political unrest
* economy changes

---

# 19. Persistence

Follow the existing mapper architecture.

Add DTO/mapper seams if appropriate.

Do not redesign save architecture.

Do not falsely claim military persistence if the save envelope does not yet persist civilization/military state.

Keep envelope version unchanged unless absolutely required by existing architecture.

---

# 20. Debug

Existing controls must remain unchanged:

```text
P — faction
J — faction membership
H — diplomacy
I — political group
Y — political affiliation
W — stability
1/2 — political influence
```

Add a new debug key only if necessary and only after checking conflicts.

A minimal military debug HUD may show:

```text
Military
Faction: <name>
Units: <count>

Selected unit:
  Name
  Faction
  Members
  State
```

Debug functionality is developer tooling, not final UI.

---

# 21. Tests

Add focused tests.

### Identity

* MilitaryUnitId is strongly typed.
* IDs are unique.
* generated identity is deterministic.

### Creation

* valid faction creates unit
* invalid faction fails
* duplicate ID fails

### Membership

* character can join unit
* character can leave unit
* cross-faction assignment fails
* character cannot silently belong to two units
* leaving faction clears military membership
* military membership does not change culture
* military membership does not change political group

### Lifecycle

If disbanding exists:

* disband works
* membership is handled correctly
* invalid lifecycle transitions fail

### Isolation

Military changes must not modify:

* terrain
* biome
* climate
* exploration
* LOD
* settlement state
* culture
* diplomacy
* internal politics
* resources

### Determinism

Same seed + same command sequence = same result.

### Regression

All previous tests must remain passing.

---

# 22. Architecture decisions

Review AD-082 through AD-096 first.

Add new decisions only where necessary.

Likely decisions:

* Military is separate from Faction identity.
* Military units are faction-owned.
* Character military membership is optional.
* Military does not imply war.
* Hostile diplomacy is not automatically military conflict.
* Military state has no automatic consequences in Phase 12.

Use OD entries for unresolved future questions such as:

* army hierarchy
* commanders
* recruitment
* military professions
* equipment
* combat model
* morale
* war model
* unit movement
* formations

Do not prematurely resolve these.

---

# 23. Documentation

Update:

```text
docs/DECISIONS.md
docs/MVP_ROADMAP.md
docs/SIMULATION_ARCHITECTURE.md
docs/ARCHITECTURE.md
docs/DEVELOPMENT_LOG.md
```

Clearly document:

```text
Culture
    ↓
Faction
    ├── Internal Politics
    ├── Diplomacy
    └── Military
```

These are related domains but are not interchangeable.

---

# 24. Architecture boundary

Maintain separation:

```text
World
Simulation
Exploration
Civilization
Diplomacy
Internal Politics
Military
Presentation
```

Military must work headlessly.

Do not put authoritative military state into Godot nodes.

---

# 25. Execution order

1. Inspect existing architecture.
2. Inspect Phase 9–11 code.
3. Design the smallest military model.
4. Implement IDs.
5. Implement military state.
6. Implement sparse storage.
7. Implement authoritative commands/system.
8. Implement validation.
9. Implement character membership if justified.
10. Implement deterministic generation.
11. Add events if justified.
12. Add tests.
13. Add minimal debug inspection.
14. Update documentation.
15. Run:

```bash
dotnet test tests/Cultures.Tests/Cultures.Tests.csproj -warnaserror
```

16. Build the full solution with warnings treated as errors.
17. Run Godot 4.7.2.stable.mono headless smoke test.
18. Review the diff and remove accidental scope expansion.

---

# 26. Acceptance criteria

Phase 12 is complete only when:

1. Military exists as a separate domain.
2. Military unit identity is strongly typed.
3. Units are faction-owned.
4. Character military affiliation is optional if implemented.
5. Cross-faction military membership is impossible.
6. Military mutations go through authoritative commands.
7. Military state is deterministic.
8. Storage is sparse where appropriate.
9. No combat system exists yet.
10. No war system exists yet.
11. Hostile diplomacy does not automatically create war.
12. No territory is implemented.
13. No resources/economy are implemented.
14. No military AI exists.
15. No exploration consequences exist.
16. No internal-politics consequences exist.
17. Previous Phase 1–11 tests pass.
18. Solution builds with 0 warnings and 0 errors.
19. Godot headless runtime boots.
20. Documentation is updated.
21. No unrelated systems are introduced.

---

# 27. Final report

When finished, report:

* implementation summary
* military domain structure
* commands
* validation rules
* files changed
* architecture decisions
* open decisions
* tests before/after
* exact test command/result
* build result
* Godot smoke-test result
* known limitations
* recommended next phase

Do NOT start Phase 13 automatically.

## CRITICAL SPATIAL CONSTRAINT — HEXAGONAL WORLD GRID

The world map is a **hexagonal grid**, not a square/tile grid.

This is an existing fundamental world-design constraint and must be respected by all new systems.

Do NOT introduce architecture that assumes:

```text
4-neighbor square grid
up/down/left/right
rectangular tiles
(x + 1, y), (x, y + 1) as universal adjacency
```

The authoritative spatial abstraction must remain compatible with the project's existing hex-grid coordinate system.

Before implementing anything spatial, inspect the existing world coordinate/grid implementation and reuse it.

Use the project's existing hex coordinate types and neighbor/topology rules.

If a new spatial abstraction is required, it must represent a **hex cell**, not a square tile.

---

### Hexagonal adjacency

Any future spatial logic must use the existing hex-grid neighbor calculation.

Do not manually implement square-grid adjacency.

Do not assume that:

```text
distance = max(abs(dx), abs(dy))
```

is correct unless that is explicitly how the existing hex coordinate system defines distance.

Reuse the project's existing coordinate conversion and distance/topology abstractions.

---

### Future building footprint compatibility

Buildings will eventually occupy an arbitrary set of hex cells rather than exactly one cell or necessarily a rectangular area.

The future conceptual model is:

```text
Building
 ├── Anchor: HexCoordinate
 └── Footprint: set/list of relative HexCoordinates
```

For example, a building may occupy:

```text
  ⬡ ⬡
⬡ ⬡ ⬡
  ⬡
```

rather than a rectangle.

Do NOT implement the complete multi-cell building system in Phase 12.

However, do not introduce a new military/spatial abstraction that permanently assumes:

```text
every entity occupies exactly one square tile
```

The current one-cell debugging representation is temporary.

If military units require a location, use the existing hex-world coordinate abstraction.

---

### Character occupancy

Characters currently occupy one map cell for debugging.

Treat this as:

```text
Character → current HexCoordinate
```

not as proof that the world uses square tiles.

Future spatial occupancy should remain compatible with:

```text
Character → spatial footprint
Building → arbitrary hex footprint
Military unit → spatial location / future footprint if required
```

Do not redesign the entire occupancy system during Phase 12 unless the existing architecture makes this necessary.

---

### Exploration compatibility

Phase 8 exploration is currently keyed by `ChunkCoordinate`.

Do not change exploration to tile-level hex knowledge in Phase 12.

Do not reinterpret the chunk system as a square world grid.

Keep the existing distinction:

```text
World hex grid
    ↓
Chunk partitioning
    ↓
Exploration knowledge
```

The chunk system is an implementation/partitioning concept and must not erase the underlying hex topology.

---

### Acceptance criterion

Phase 12 must not introduce any square-grid assumptions.

The final implementation must remain compatible with the existing hexagonal world and future arbitrary multi-hex building footprints.
