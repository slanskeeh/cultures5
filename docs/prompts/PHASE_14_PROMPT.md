# KINLANDS — PHASE 14: FULL PERSISTENCE

## ROLE

Continue the existing Kinlands project from completed Phase 13.

Before coding, read all canonical project docs and inspect the implementation.

Current constraints remain:

* pure `Cultures.Domain`
* Godot presentation shell
* deterministic RNG
* typed stable IDs
* world seed + generation contract
* chunk-based world
* LOD independent from presentation
* exploration independent from geography
* culture/faction/diplomacy/politics/military are separate domains
* history is a fact store
* save envelope v2 currently does not contain full runtime state

---

# PHASE GOAL

Replace the foundation-only save envelope with a real deterministic save/load system.

The save must preserve the actual living world rather than merely reconstructing the static terrain.

After loading:

```text
world state
+
simulation time
+
characters
+
families
+
skills
+
buildings
+
resources
+
settlements
+
exploration
+
cultures
+
factions
+
diplomacy
+
politics
+
military
+
history
+
LOD-relevant state
```

must represent the same simulation state as before saving.

---

# CORE RULE

Static generated geography remains reconstructed from:

```text
world seed
generation version
world dimensions
generation configuration
```

Do NOT serialize giant terrain arrays merely because save/load is being implemented.

Dynamic state must be persisted separately.

---

# VERSIONING

Introduce a new save envelope version.

Do NOT silently replace v2 semantics.

Implement explicit version handling.

At minimum:

```text
v2
v3
```

or another clean version progression.

The exact new version must match project conventions.

Older saves must either:

* migrate safely,
* or fail explicitly with a clear unsupported-version result.

Never silently interpret an old save as a new save.

---

# SAVE CONTRACT

The save format must be deterministic and explicit.

Use serializable DTOs/records.

Do NOT serialize:

* Godot Nodes
* runtime Godot scenes
* caches that can be reconstructed
* pathfinder internals
* presentation-only state
* derived data that can safely be rebuilt

Persist authoritative simulation state.

---

# REQUIRED DOMAINS

At minimum persist what currently exists for:

## Simulation

* simulation tick
* calendar state if not derivable solely from tick

## Characters

* CharacterId
* identity
* age/life stage
* needs
* health
* inventory
* position
* activity
* workplace
* family links
* culture
* faction
* political group
* military unit
* skills
* any other authoritative CharacterState fields

## Buildings

* BuildingId
* definition/type reference
* origin/footprint information required for reconstruction
* lifecycle
* inventory
* workers
* settlement association
* all other authoritative mutable state

## Settlements

* SettlementId
* lifecycle
* associations
* derived/reconstructable state rules
* any authoritative non-derived fields

Do not persist purely derived membership if it can be safely rebuilt.

## Exploration

Persist sparse exploration records.

Unknown chunks remain absent.

Do not turn exploration into a dense world array during serialization.

## Civilization/Faction

Persist:

* cultures
* faction identity
* character affiliation
* faction relationships
* diplomacy state
* political groups
* political affiliations
* influence
* internal stability
* military units
* military memberships

## History

Persist historical records according to the Phase 13 model.

---

# RESTORATION ORDER

Define an explicit deterministic load order.

Recommended conceptual order:

```text
1. validate save header/version
2. restore world contract
3. reconstruct static world services
4. restore IDs/directories
5. restore cultures/factions
6. restore characters
7. restore families/skills/relationships
8. restore buildings
9. restore settlement references
10. restore diplomacy
11. restore politics
12. restore military
13. restore exploration
14. restore history
15. rebuild derived indexes/caches
16. validate invariants
17. resume simulation
```

Adapt to actual implementation.

The critical rule is:

> Restore authoritative state first. Rebuild derived state afterwards.

---

# ID INTEGRITY

Save/load must preserve exact IDs.

Never assign new IDs during load.

Examples:

```text
CharacterId before save == CharacterId after load
BuildingId before save == BuildingId after load
SettlementId before save == SettlementId after load
FactionId before save == FactionId after load
MilitaryUnitId before save == MilitaryUnitId after load
HistoryEventId before save == HistoryEventId after load
```

Entity ID generators must be restored to safe positions after loading.

A subsequently created entity must not collide with a previously loaded ID.

---

# DETERMINISTIC ROUND TRIP

Implement tests such as:

```text
Host A
→ simulate N ticks
→ save
→ load into Host B
→ compare authoritative snapshots
```

The resulting state must match.

Then:

```text
Host A
→ continue M ticks

Host B
→ continue M ticks
```

The resulting states must also match.

This is more important than comparing serialized byte order.

---

# SAVE / LOAD API

Introduce clean application-level APIs.

For example:

```text
SaveSimulation()
LoadSimulation()
```

or equivalent services.

Do not let Godot directly serialize domain objects.

---

# ATOMICITY

A failed load must not leave a partially restored world.

Use:

```text
validate
→ construct/restore
→ validate invariants
→ commit
```

or an equivalent transactional approach.

If load fails, the current host must remain valid or the operation must fail before replacing the current state.

---

# VALIDATION

Validate:

* duplicate IDs
* references to nonexistent characters
* invalid building definitions
* invalid faction references
* cross-faction military membership
* invalid political group membership
* invalid settlement references
* impossible lifecycle combinations
* invalid inventory amounts
* invalid exploration coordinates
* invalid history references
* malformed version metadata

Fail deterministically.

---

# LOD

Persist whatever LOD state is authoritative.

Do not persist presentation presence.

Do not reconstruct LOD from Godot rendering.

Presentation can be recreated after loading.

If a chunk is Aggregate in the save, its simulation state must remain Aggregate after load unless the documented load policy intentionally recalculates it.

Document the decision.

---

# EXPLORATION

Exploration knowledge must survive save/load exactly.

Test:

```text
Unknown
Rumored
Scouted
Mapped
Confirmed
Analyzed
```

including wrap-adjacent chunks.

---

# HISTORY

History must survive save/load without:

* duplicated events
* changed event IDs
* reordered chronological facts
* fabricated aggregate history

---

# PERFORMANCE

Do not optimize prematurely.

But avoid:

* serializing entire generated terrain
* serializing Godot objects
* rebuilding the whole world multiple times during load
* repeatedly scanning all entities for every record

Use indexes and two-pass reconstruction where useful.

---

# TESTING

Add tests for:

* empty/minimal save
* full debug-world save
* character round-trip
* family round-trip
* skills round-trip
* building inventories
* settlement associations
* exploration
* cultures/factions
* diplomacy
* politics
* military
* history
* LOD
* ID generator restoration
* invalid references
* unsupported save version
* deterministic continuation after load
* no duplicate IDs
* save without presentation loaded
* save with aggregated chunks

The test suite must continue passing completely.

---

# DOCUMENTATION

This phase may close or update:

* OD-005 exact save format
* OD-036 persistence of civilizations

Only close an OD if the implementation truly resolves it.

Add AD decisions for:

* authoritative vs derived save data
* save versioning
* deterministic restoration order
* ID preservation
* load validation/atomicity

Use the next free IDs.

---

# STRICT OUT OF SCOPE

Do NOT implement:

* multiplayer
* cloud saves
* compression optimization unless trivial and justified
* background saving threads
* autosave schedules
* modding serialization
* migration gameplay
* war
* combat
* professions
* resource ecology overhaul

---

# DEVELOPMENT LOG

Update `docs/DEVELOPMENT_LOG.md` with exact test/build/runtime results.

Never claim a round-trip works unless a real round-trip test passed.

---

# DEFINITION OF DONE

Phase 14 is complete when:

1. A complete runtime world can be saved.
2. The save is versioned.
3. IDs survive exactly.
4. Static terrain remains reconstructed from the generation contract.
5. Dynamic state survives.
6. Exploration survives.
7. History survives.
8. LOD state follows documented rules.
9. Invalid saves fail deterministically.
10. Load cannot leave a partially restored world.
11. Continuing simulation after load remains deterministic.
12. Tests pass.
13. Build has 0 warnings/errors.
14. Godot headless boots.
15. Development log is updated.
