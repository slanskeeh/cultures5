# Phase 10 — Diplomacy

## Context

You are working on the Kinlands / Cultures-like simulation project.

Phase 9 — Factions and Cultures is COMPLETE.

Verified state:

* 175/175 tests passing.
* Solution builds with 0 warnings / 0 errors.
* Godot 4.7.2.stable.mono headless runtime boots successfully.
* `CultureState` and `FactionState` are separate domain entities.
* `CharacterState.Culture` and `CharacterState.Faction` are authoritative.
* Faction membership is stored on characters; faction member counts are derived from the roster.
* `FactionRelationDirectory` is sparse and symmetric.
* Missing faction relation = Neutral.
* Self-relations are rejected.
* Friendly / Hostile currently represent data only.
* Factions do not own geography.
* Factions do not reveal exploration.
* `CivilizationId` remains an unused future seam.
* Civilization persistence is currently only a mapper seam; save envelope v2 remains unchanged.
* Exploration remains keyed by `ChunkCoordinate`.

Relevant accepted decisions:

* AD-082 — Culture ≠ Faction.
* AD-083 — Neutral is a real unaffiliated culture.
* AD-084 — Membership lives on the character.
* AD-085 — Faction relations are symmetric and sparse.
* AD-086 — Factions do not own geography.
* AD-087 — Names are deterministic and do not consume `SimulationHost.Random`.
* AD-088 — Civilization persistence is currently mapper-only.

Phase 10 goal:

> Turn the existing faction relationship data into a minimal, authoritative diplomacy domain without implementing war, trade, territory, economy or AI.

---

# 1. Inspect before coding

Before making changes:

1. Inspect the repository.
2. Read:

   * `docs/DECISIONS.md`
   * `docs/MVP_ROADMAP.md`
   * `docs/WORLD_ARCHITECTURE.md`
   * `docs/SIMULATION_ARCHITECTURE.md`
   * `docs/ARCHITECTURE.md`
   * `docs/DEVELOPMENT_LOG.md`
3. Inspect all Phase 9 civilization/faction code.
4. Inspect existing command/system patterns.
5. Inspect persistence architecture.
6. Inspect existing IDs and deterministic infrastructure.
7. Inspect existing settlement and character models.
8. Reuse existing abstractions instead of creating duplicates.

Do not start implementation until the existing architecture is understood.

---

# 2. Phase 10 scope

Implement the foundation for:

* diplomatic stance
* diplomatic relationship state
* authoritative diplomacy commands
* relationship validation
* deterministic state transitions
* diplomacy events/results if consistent with existing architecture
* persistence mapper support if appropriate
* focused tests
* minimal debug inspection

The goal is to make diplomacy an actual domain/application system instead of merely storing `Friendly` / `Hostile`.

---

# 3. Strict scope boundary

DO NOT implement:

* war
* military combat
* armies
* soldiers
* weapons
* sieges
* territorial conquest
* borders
* claimed chunks
* resource ownership
* trade
* markets
* economy
* taxation
* diplomacy UI
* AI diplomacy
* faction AI
* reputation
* espionage
* treaties with complex conditions
* alliances with military obligations
* migration
* population simulation
* resource generation
* resource regeneration

Friendly / Hostile must remain diplomatic state only.

Do not infer that Hostile means war.

Do not infer that Friendly means alliance.

---

# 4. Diplomacy conceptual model

Keep the following concepts separate:

```text
Culture
    ↓
Faction
    ↓
Diplomatic relationship
```

Culture describes shared identity.

Faction describes organized social/political affiliation.

Diplomatic relationship describes how two factions currently regard each other.

Do not merge these concepts.

---

# 5. Diplomatic stance

Use the existing relationship values where possible:

```text
Neutral
Friendly
Hostile
```

These are diplomatic stances.

The Phase 9 representation was intentionally data-only.

Phase 10 adds controlled state transitions.

Do not add dozens of diplomatic states.

Do not introduce:

* Alliance
* War
* Truce
* Vassal
* Protectorate
* Embargo

unless the existing roadmap explicitly requires one of them for this phase.

If a future concept is needed, record it as an open decision instead.

---

# 6. Relationship invariants

Preserve Phase 9 invariants:

* relationships are symmetric
* relationships are sparse
* missing relationship = Neutral
* self-relations are invalid
* Neutral should not need to be stored
* unordered pair identity must remain deterministic

For factions A and B:

```text
Relation(A, B) == Relation(B, A)
```

There must never be two independent records for the same unordered pair.

Do not regress AD-085.

---

# 7. Diplomatic state transitions

Introduce explicit application/domain operations for changing diplomatic stance.

For example:

```text
SetDiplomaticStance
```

or an equivalent name consistent with the existing command architecture.

The operation must:

1. Validate both factions exist.
2. Reject self-relations.
3. Validate the requested stance.
4. Apply the relationship atomically.
5. Preserve sparse storage.
6. Treat Neutral as removal of the stored relation.
7. Produce deterministic state.
8. Avoid changing any unrelated simulation state.

Example:

```text
Unknown pair
→ Friendly
```

stores one relation.

```text
Friendly
→ Neutral
```

removes the stored relation.

```text
Friendly
→ Hostile
```

replaces the existing relation.

Do not add automatic consequences yet.

---

# 8. No hidden side effects

Changing diplomacy MUST NOT:

* change character culture
* change faction membership
* move characters
* change settlements
* reveal exploration
* change terrain
* change resources
* change LOD
* change population
* create armies
* start combat
* modify economy
* create territory

Diplomacy is currently pure political state.

---

# 9. Authority

All diplomatic mutations must pass through the authoritative Domain/Application simulation layer.

Do NOT mutate:

```text
FactionRelationDirectory
```

directly from:

* Godot UI
* debug presentation
* scene nodes
* rendering code

Debug commands may invoke application commands, but presentation must not own diplomatic state.

---

# 10. Determinism

Diplomacy must not consume `SimulationHost.Random`.

Changing a relationship must be deterministic.

Given the same initial faction state and the same sequence of diplomacy commands, the resulting state must be identical.

No:

* wall-clock time
* random probability
* Godot random state
* uncontrolled GUIDs

unless an existing deterministic abstraction explicitly requires them.

---

# 11. Optional diplomacy result/event seam

If the existing architecture already supports domain events/results, introduce a minimal seam such as:

```text
DiplomaticStanceChanged
```

containing:

* faction A
* faction B
* previous stance
* new stance

Do not build an event bus solely for this phase if the project does not already have one.

Do not create infrastructure that is not justified by the existing architecture.

The event/result must not automatically trigger gameplay consequences.

---

# 12. Persistence

Inspect the existing mapper architecture.

If the Phase 9 `FactionRelationRecord` already represents the necessary state, reuse it.

Do not redesign the save system.

Do not change the save envelope version merely to claim persistence.

If the project architecture intentionally keeps civilization persistence deferred, keep that decision.

If Phase 10 introduces a meaningful new persistence requirement, document the exact limitation rather than silently modifying unrelated persistence architecture.

---

# 13. Debug commands

Extend the existing debug functionality only as necessary.

Current Phase 9:

```text
P — cycle faction
J — join/leave faction
H — cycle faction relation
```

Preserve these controls unless the existing architecture makes a better mapping necessary.

The debug layer should be able to demonstrate:

```text
Faction A ↔ Faction B
Neutral
Friendly
Hostile
```

It must use the authoritative diplomacy command/application path.

Do not build a final diplomacy interface.

---

# 14. Tests

Add focused tests.

## Relationship invariants

Test:

* A ↔ B equals B ↔ A.
* Self relation is rejected.
* Missing relation is Neutral.
* Neutral is not stored.
* Friendly is stored once.
* Hostile is stored once.
* Changing Friendly → Hostile replaces the same pair.
* Changing Hostile → Neutral removes the pair.

## Command validation

Test:

* nonexistent faction A fails
* nonexistent faction B fails
* same faction fails
* invalid stance fails if the type system allows invalid input
* successful command changes only diplomacy state

## Side-effect isolation

Verify that diplomacy changes do NOT modify:

* character culture
* faction membership
* exploration knowledge
* geography
* settlements
* LOD
* resource state

## Determinism

The same initial state and command sequence must produce the same final diplomatic state.

## Regression

All existing Phase 1–9 tests must continue passing.

Do not weaken or delete existing tests.

---

# 15. Architecture decisions

Review existing decisions before adding new ones.

If Phase 10 introduces an important architectural rule, add the next `AD-xxx` number.

Likely candidates:

* diplomacy state is separate from faction identity
* diplomatic relationships remain sparse/symmetric
* diplomacy commands are authoritative
* diplomatic stance has no automatic war/combat consequences

Do not duplicate AD-085.

If a decision cannot safely be finalized yet, use an `OD-xxx`.

---

# 16. Documentation

Update:

* `docs/DECISIONS.md`
* `docs/MVP_ROADMAP.md`
* `docs/SIMULATION_ARCHITECTURE.md`
* `docs/ARCHITECTURE.md`
* `docs/DEVELOPMENT_LOG.md`

Update `WORLD_ARCHITECTURE.md` only if Phase 10 genuinely affects world architecture.

Do not rewrite historical documentation.

Clearly state that:

> Friendly and Hostile are diplomatic stances only. They do not imply alliance, war, combat, territory or economic consequences.

---

# 17. Important future boundaries

Do not implement these systems now, but preserve clean seams for them:

```text
Diplomacy
    ↓
Future diplomacy consequences

Faction
    ↓
Future territory

Faction
    ↓
Future settlements

Settlement
    ↓
Future economy

World
    ↓
Future natural resources

Character
    ↓
Future AI / professions / needs
```

Natural resources remain a separate future World/Simulation concern:

* trees
* stone
* clay
* ores
* minerals
* berries
* plants
* renewable resource regeneration

Do not attach resource ownership to factions during Phase 10.

---

# 18. Execution procedure

Work in this order:

### Step 1

Inspect architecture and Phase 9.

### Step 2

Identify what can be reused.

### Step 3

Design the smallest diplomacy model.

### Step 4

Implement domain/application changes.

### Step 5

Preserve sparse symmetric relationship storage.

### Step 6

Add tests.

### Step 7

Add/update debug commands if required.

### Step 8

Update documentation.

### Step 9

Run:

```bash
dotnet test tests/Cultures.Tests/Cultures.Tests.csproj -warnaserror
```

### Step 10

Build the complete solution with warnings treated as errors.

### Step 11

Run Godot 4.7.2.stable.mono headless smoke test.

### Step 12

Review the diff for accidental scope expansion.

---

# 19. Acceptance criteria

Phase 10 is complete only when:

1. Diplomacy is represented as an authoritative domain/application system.
2. Neutral/Friendly/Hostile transitions are explicit.
3. Missing relationship still means Neutral.
4. Relationships remain sparse.
5. Relationships remain symmetric.
6. Self-relations remain invalid.
7. Diplomacy mutations go through the authoritative simulation layer.
8. Diplomacy has no hidden combat/economy/territory consequences.
9. Exploration remains completely independent.
10. Culture and faction models remain unchanged conceptually.
11. Determinism is preserved.
12. Appropriate tests exist.
13. All previous tests pass.
14. Solution builds with 0 warnings and 0 errors.
15. Godot headless runtime boots.
16. Documentation is updated.
17. No war, trade, territory, resource or AI systems are introduced.

---

# 20. Final report

When finished, report:

1. What was implemented.
2. What existing Phase 9 code was reused.
3. Files changed.
4. Architecture decisions added.
5. Open decisions added.
6. Tests before/after.
7. Test command and result.
8. Build result.
9. Godot headless result.
10. Known limitations.
11. Recommended next phase.

Do NOT start Phase 11 automatically.

Stop after Phase 10 and wait for further instructions.
