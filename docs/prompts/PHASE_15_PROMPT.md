# KINLANDS — PHASE 15: LIVING WORLD, RESOURCES AND ECOLOGY

## ROLE

Continue from completed Phase 14.

This phase deepens the physical world and economy.

Do NOT redesign existing systems.

Read:

* all canonical docs
* `DECISIONS.md`
* `DEVELOPMENT_LOG.md`
* current world generation
* terrain
* production
* resource
* biome
* settlement
* exploration
* persistence implementations

---

# PHASE GOAL

Turn the current placeholder geography/resource model into the beginning of a living world.

The desired causal chain is:

```text
Geography
→ Climate
→ Water
→ Soil / fertility
→ Vegetation
→ Wild resources
→ Animals
→ Production
→ Settlements
```

Do not implement all future world simulation at once.

This phase establishes the physical/resource foundation.

---

# WORLD GENERATION PRINCIPLE

Do not replace the entire world generator with an enormous monolithic generator.

Keep generation layered.

Conceptually:

```text
Seed
↓
Macro geography
↓
Elevation
↓
Water
↓
Climate
↓
Biome
↓
Soil / fertility
↓
Resource suitability
```

Keep generation deterministic.

---

# SOIL / FERTILITY

Introduce a world-level terrain property representing fertility or an equivalent future-ready concept.

It must be derived from physical conditions rather than being a random arbitrary bonus.

Potential inputs:

* biome
* climate
* elevation
* water proximity
* future river information

Do not add Civilization-style:

```text
+10% farming everywhere
```

bonuses.

Production modifiers must arise from world conditions.

---

# RESOURCES

Expand resource definitions beyond the current prototype.

Potential MVP resources from the established design:

```text
Wood
Stone
Clay
Metal
Mineral
WildBerries
```

Use the existing `ResourceType` / inventory model rather than creating separate resource classes for every item.

Resource definitions should contain data.

Runtime resource state should be separate.

---

# RESOURCE NODE VS RESOURCE AMOUNT

Distinguish:

```text
A place can contain a resource
```

from:

```text
A building currently owns 17 units
```

Do not put arbitrary inventory onto terrain cells unless justified.

A resource deposit/node should have its own domain representation if persistent extraction is required.

---

# REGENERATION

Natural resources must be capable of recovery.

Examples:

* trees regrow
* berry patches recover
* wildlife populations recover

Do not implement infinite instantaneous respawning.

Use deterministic time-based recovery.

Resource recovery must be simulation state where appropriate.

Do not depend on whether a chunk is currently rendered.

---

# DEPLETION

Extraction should modify natural resource state.

Example:

```text
forest
→ harvesting
→ reduced available wood
→ recovery
→ forest regrowth
```

Do not make extraction simply call:

```text
terrain.Wood += ...
```

without persistent world resource state.

---

# BIOME-CONDITIONAL PRODUCTION

Use the existing environmental modifier seam.

Production should be able to resolve contextually.

For example conceptually:

```text
Farm + Plains → Wheat
Farm + Forest → Berries
Farm + Desert → Dates
```

But DO NOT hard-code these as a giant `if` chain.

Use data-driven production/environment rules.

The exact final catalogue can remain provisional.

---

# RIVERS / WATER

Introduce only the minimum river representation necessary for this phase.

The architecture should support:

```text
mountains
→ runoff
→ river
→ fertile land
→ settlement suitability
```

Do not build a full hydrology simulator.

Do not regenerate the entire planet every time a chunk is accessed.

River data must remain deterministic and seam-aware.

---

# HORIZONTAL WRAP

All resource/ecology spatial calculations must respect horizontal world wrapping.

A forest or river crossing X=0 must behave as one continuous world structure where appropriate.

Do not duplicate wrap formulas.

Use existing `WorldTopology`.

---

# ANIMALS

Establish a minimal wildlife representation if required by the current design.

Do not create dozens of species.

Use a data-driven species definition.

Example conceptual types:

* deer
* sheep
* boar
* birds

The exact initial set can remain provisional.

Animals must obey the same simulation-first principle.

Presentation is not existence.

---

# WILDLIFE LOD

Wildlife must participate in the existing LOD model.

Do not keep every animal at full simulation everywhere.

Distant wildlife may be represented by aggregate population estimates.

Do not randomly respawn animals because a player enters a chunk.

Use deterministic reconstruction or aggregate population state.

---

# RESOURCE EXPLORATION

Do not automatically reveal resource information simply because terrain exists.

Respect Phase 8 exploration knowledge.

Future resource discoveries can attach to exploration knowledge.

For now, expose only knowledge levels the existing exploration model supports.

Do not merge resource state with player knowledge.

---

# ECONOMY INTEGRATION

Production must consume actual resource availability where applicable.

Examples:

```text
Tree availability
→ Wood production

Clay deposit
→ Clay extraction

Stone deposit
→ Stone extraction
```

Keep building inventories separate from natural resources.

---

# SETTLEMENT EFFECT

This phase may provide data that improves settlement evaluation.

Examples:

* fertile land nearby
* water access
* resource availability

But do NOT rewrite settlement formation into a giant score formula.

Keep the emergent/co-location infrastructure philosophy.

---

# PERSISTENCE

Natural resource depletion/recovery and river/resource state must be included in the full save system from Phase 14.

Do not create a second persistence mechanism.

---

# TESTING

Add tests for:

* deterministic resource generation
* resource regeneration
* resource depletion
* biome-dependent outputs
* soil/fertility determinism
* horizontal wrap
* river seam consistency
* resource state survives save/load
* wildlife deterministic reconstruction
* LOD independence
* exploration independence
* production integration
* no whole-world pre-generation

---

# DOCUMENTATION

Add/update decisions for:

* resource state vs inventory
* natural regeneration
* contextual production
* physical ecology layers
* wildlife aggregation

Review relevant ODs:

* OD-011 world generation
* OD-001 world dimensions if this phase pressures it
* OD-002 world/tile geometry only if absolutely necessary
* exploration ODs where resources become discoverable

Do not close unrelated ODs.

---

# STRICT OUT OF SCOPE

Do NOT implement:

* professions
* households
* marriage
* complex animal AI
* full hydrology simulation
* trade
* migration
* war
* combat
* territory ownership
* final visual resource art
* multiplayer

---

# DEVELOPMENT LOG

Record exact tests and performance observations.

Do not claim the world is "large-scale ready" unless actually measured.

---

# DEFINITION OF DONE

1. World conditions influence resource/ecology suitability.
2. Natural resources have persistent state where required.
3. Resources deplete and recover deterministically.
4. Production uses environmental context.
5. Basic river/water structure exists without whole-world regeneration.
6. Wildlife foundation exists without random visible respawning.
7. LOD remains authoritative.
8. Exploration remains separate.
9. Save/load preserves mutable natural state.
10. Tests pass.
11. Build is clean.
12. Godot boots.
13. Development log is updated.
