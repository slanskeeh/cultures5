# PHASE 3 — FIRST LIVING CHARACTERS

You are continuing development of the Cultures Successor project.

Phase 0, Phase 1 and Phase 2 are complete.

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

Do not silently change accepted architecture.

---

# OBJECTIVE

Implement **Phase 3 — First Living Characters**.

The goal is to introduce the first autonomous persistent characters into the simulation.

At the end of this phase:

* characters have persistent identities;
* characters have age/life state;
* characters have basic needs;
* characters can move through the logical world;
* characters can perform simple actions;
* characters can eat;
* characters can sleep;
* characters can work in a minimal placeholder occupation;
* characters can autonomously survive for many simulation days;
* character simulation is deterministic;
* the simulation does not depend on Godot rendering.

Target population:

**20–30 characters.**

This phase must prove that the project has a living population simulation.

---

# IMPORTANT

Do NOT implement the entire character system planned in the Game Design Bible.

This phase is deliberately minimal.

Do NOT implement yet:

* families;
* children;
* marriage;
* inheritance;
* skill inheritance;
* teaching;
* complex personality;
* political influence;
* councils;
* professions as a full system;
* diplomacy;
* military;
* factions;
* civilization;
* settlements;
* advanced AI;
* advanced social relationships;
* final character rendering;
* final animation system.

However, the architecture must leave clean extension points for those systems.

---

# 1. CHARACTER IDENTITY

Create a persistent `CharacterId`.

A character must have stable identity independent of:

* array position;
* chunk;
* Godot Node;
* rendering object.

The identity must survive:

* movement;
* unloading/reloading presentation;
* future save/load.

Do not use array indexes as character identity.

---

# 2. CHARACTER STATE

Create a minimal authoritative `CharacterState`.

It should conceptually support:

* CharacterId;
* age;
* life state;
* position;
* needs;
* current action;
* basic inventory/food state if required;
* minimal work state.

Do not put Godot Nodes inside `CharacterState`.

Do not put presentation information inside the authoritative domain state.

---

# 3. AGE AND LIFE

Characters must age according to simulation time.

Use the existing simulation clock.

Do NOT tie aging to:

* FPS;
* real-world seconds;
* rendering frames.

At minimum support:

```text
Child
Adult
Elder
Dead
```

Exact age thresholds are provisional.

Do not treat them as final game design.

Characters should have a meaningful lifespan suitable for the game's slow simulation.

The exact final lifespan remains an open design decision.

Document temporary values.

---

# 4. HEALTH

Introduce only the minimal health state necessary for survival.

Example conceptual model:

```text
health
maxHealth
alive/dead
```

Do not implement diseases, injuries or complex medicine.

Those belong to later systems.

Health should be deterministic.

---

# 5. NEEDS

Implement a minimal needs system.

Initial needs:

* hunger;
* fatigue/rest.

Optional:

* health if needed as a state rather than a separate need.

Do NOT implement:

* dozens of needs;
* detailed mood;
* complex psychological simulation.

Needs must change according to simulation time, not render frames.

Example:

```text
time passes
→ hunger increases
→ fatigue increases
```

Eating:

```text
food consumed
→ hunger decreases
```

Sleeping:

```text
resting
→ fatigue decreases
```

---

# 6. ACTION MODEL

Establish a reusable action abstraction.

A character should not contain one giant method such as:

```text
DoEverything()
```

Instead, actions should conceptually have:

```text
Action
├── Preconditions
├── Target
├── Duration
├── Progress
├── Completion
├── Effects
└── Cancellation/interruption
```

For this phase implement only a few actions:

* Move
* Eat
* Sleep
* Work
* Idle

The abstraction must be extensible.

Future actions such as:

* Build;
* Teach;
* Hunt;
* Trade;
* Talk;
* Fight;
* Farm;

should be able to use the same action framework.

---

# 7. AUTONOMOUS DECISION MAKING

Implement a minimal deterministic decision system.

It should choose a next action based on urgent needs and available context.

Example priority:

```text
Critical hunger
    ↓
Find food
    ↓
Eat

Critical fatigue
    ↓
Find sleeping location
    ↓
Sleep

Otherwise
    ↓
Work / idle
```

This priority is a starting rule, not final game design.

Do NOT implement a sophisticated AI architecture yet.

A small deterministic utility/priority selector is preferable.

---

# 8. FOOD

Introduce the smallest possible food model.

We need enough to prove:

```text
food exists
→ character obtains food
→ food is consumed
→ hunger decreases
```

Do NOT build the final economy.

Food can initially be a simple simulation resource.

It may be represented through a minimal inventory/resource abstraction.

Do not hard-code the future farming system into the character.

---

# 9. SLEEP

Characters need a way to rest.

For Phase 3, sleeping may use a minimal abstract sleep location or a simple world-cell rule.

Do not implement housing yet.

The important behavior is:

```text
fatigue high
→ character seeks valid rest
→ sleep action
→ fatigue decreases
```

Later housing/buildings will replace the placeholder mechanism.

Keep that replacement in mind architecturally.

---

# 10. MOVEMENT

Characters must move through the logical world.

Movement must use the authoritative logical coordinates from Phase 1.

Do NOT make Godot transform position authoritative.

The logical state should be:

```text
CharacterState.Position
```

Presentation derives its visual position from it.

---

# 11. PATHFINDING

Implement ONLY the minimum navigation needed for Phase 3.

A character must be able to move between nearby valid cells.

The navigation API should be replaceable.

Do not implement:

* global navigation;
* sophisticated hierarchical pathfinding;
* full-world pathfinding;
* navigation mesh;
* complex traffic simulation.

A simple deterministic grid pathfinder is sufficient for this phase.

The architecture should allow replacement later when world scale requires it.

---

# 12. WORLD WRAPPING

Character movement must respect the Phase 1/2 topology.

Example for width 100:

```text
x = 99
move east
→ x = 0
```

Likewise:

```text
x = 0
move west
→ x = 99
```

North/south must remain bounded.

Characters must never move outside the world.

Add regression tests.

---

# 13. TERRAIN PASSABILITY

Characters must respect terrain passability.

At minimum:

* water should normally be non-walkable;
* land should normally be walkable.

Do not implement:

* swimming;
* boats;
* bridges;
* climbing;
* terrain movement costs.

Those are future features.

---

# 14. WORK

Implement a minimal placeholder work system.

A character should be able to have:

```text
WorkTarget
WorkAction
```

For example:

```text
WorkArea
```

or a simple designated work cell.

The purpose is only to prove that characters can spend time performing work.

Do not implement final professions.

Do not implement buildings yet.

The work system must later be replaceable/extendable by:

```text
Profession
Building
Workstation
Skill
Production
```

---

# 15. SIMULATION POPULATION

Create a minimal population host.

For development/testing, spawn approximately:

```text
20–30 characters
```

from deterministic seed/configuration.

Do not use random GUIDs that make the simulation irreproducible.

The same seed must produce the same initial population.

Initial positions must be deterministic.

Avoid spawning characters into water or invalid cells.

---

# 16. CHARACTER DISTRIBUTION

Initial characters may be placed in a suitable land area.

Do NOT implement settlements yet.

The characters can simply start near one another.

This is a temporary simulation setup.

Document it as such.

---

# 17. CHARACTER SIMULATION LOOP

Characters should update through the simulation clock.

Do NOT update every character every render frame.

The architecture should conceptually be:

```text
Simulation Tick
      ↓
Character Simulation
      ↓
Needs
      ↓
Decision
      ↓
Action Progress
      ↓
World State Changes
```

Rendering is separate.

---

# 18. UPDATE FREQUENCY

Do not make expensive AI decisions every tick if unnecessary.

It is acceptable to separate:

```text
needs update
action progress
decision evaluation
movement
```

into appropriate frequencies.

However, do not prematurely introduce multithreading.

Correctness first.

---

# 19. GODOT PRESENTATION

Create a minimal debug presentation.

Each character should be visible as a simple placeholder.

For example:

* colored square/circle;
* simple sprite;
* debug marker.

It is enough to visually verify:

* multiple characters exist;
* they move;
* they stop/rest;
* they react to food/fatigue.

Do NOT create final character art.

Do NOT build final UI.

---

# 20. DEBUG INFORMATION

When selecting or inspecting a debug character, it should be possible to inspect at minimum:

* CharacterId;
* age;
* life state;
* position;
* hunger;
* fatigue;
* current action.

This may be a simple debug HUD.

The purpose is development verification.

---

# 21. DETERMINISM

The simulation must be deterministic.

Given:

```text
same world seed
+
same configuration
+
same initial simulation time
+
same commands
```

the character simulation should produce equivalent state.

Avoid:

* wall-clock time;
* uncontrolled random numbers;
* random GUID generation;
* FPS-dependent state changes.

---

# 22. TESTS

Add automated tests.

At minimum:

### Character identity

Different characters have different stable IDs.

### Population creation

Same seed/configuration creates equivalent initial population.

### Age progression

Simulation time causes deterministic age progression.

### Hunger

Time increases hunger.

### Eating

Eating decreases hunger.

### Fatigue

Time increases fatigue.

### Sleeping

Sleeping decreases fatigue.

### Death

If health/survival rules are implemented, verify deterministic death conditions.

### Movement

Character can move to a valid adjacent cell.

### Water

Character cannot normally move into water.

### Horizontal wrap

For width 100:

```text
99 + east → 0
0 + west → 99
```

### Polar boundaries

Characters cannot move outside Y bounds.

### Action completion

Actions progress and complete deterministically.

### Decision making

Given the same character state and world context, the same next action is selected.

### Simulation determinism

Run the same simulation for N ticks twice.

Compare authoritative character state.

Results must be equivalent.

---

# 23. SAVE SYSTEM

Do NOT build complete character save/load yet if the existing save architecture is not ready for it.

However:

* Character IDs must be serializable;
* Character state must be designed with persistence in mind;
* Do not make character identity dependent on runtime memory.

If save integration is introduced, update save schema/version appropriately.

---

# 24. PERFORMANCE

Target:

20–30 detailed characters.

Do not optimize for thousands yet.

However, avoid architecture that assumes:

```text
one Godot Node = one authoritative character
```

The Domain must be able to simulate characters without rendering them.

---

# 25. ARCHITECTURE BOUNDARIES

Maintain:

```text
Domain
    CharacterState
    CharacterId
    Needs
    Actions
    Movement contracts
    Character simulation rules

Application
    Character simulation orchestration
    Population creation
    Commands

Infrastructure
    persistence/utilities

Presentation
    Godot character visuals
    debug HUD
```

Do not move game rules into Godot scripts.

---

# 26. FUTURE EXTENSION REQUIREMENTS

The Phase 3 implementation must not block future:

```text
Character
 ├── Family
 ├── Personality
 ├── Preferences
 ├── Skills
 ├── Profession
 ├── Relationships
 ├── Inventory
 ├── Health
 ├── History
 └── Political Influence
```

This does NOT mean implementing these now.

It means avoid designing `CharacterState` in a way that assumes a character is only:

```text
position + hunger + fatigue
```

The model must have room to grow without becoming a giant god object.

Prefer compositional state/components where appropriate.

---

# 27. IMPORTANT — DO NOT CREATE A CHARACTER GOD OBJECT

Do not create:

```text
CharacterManager
```

that owns every rule related to characters.

Avoid a giant:

```text
Character.cs
```

containing:

* AI;
* movement;
* needs;
* inventory;
* rendering;
* family;
* skills;
* relationships;
* economy;
* politics.

Keep responsibilities separated.

---

# 28. DOCUMENTATION

If implementation requires a new architectural decision:

1. update `DECISIONS.md`;
2. update the appropriate Bible;
3. explain why.

Temporary gameplay values must be documented as temporary.

Do not silently turn placeholder values into final design.

---

# 29. DEVELOPMENT REPORT

At the end, update:

`DEVELOPMENT_LOG.md`

Include:

### Task

### Implemented

### Verified

### Tests

Include exact:

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

### Files changed

### Current project health

### Next recommended step

### Notes for architectural review

Explicitly identify anything that requires design review.

---

# 30. COMPLETION CRITERIA

Phase 3 is complete only when:

* [ ] CharacterId exists;
* [ ] persistent CharacterState exists;
* [ ] deterministic population creation exists;
* [ ] characters age;
* [ ] hunger exists;
* [ ] fatigue exists;
* [ ] eating exists;
* [ ] sleeping exists;
* [ ] movement exists;
* [ ] water is respected;
* [ ] horizontal wrap works;
* [ ] north/south boundaries work;
* [ ] minimal work action exists;
* [ ] deterministic decision system exists;
* [ ] characters can autonomously operate for many simulation days;
* [ ] debug presentation shows characters;
* [ ] character debug state can be inspected;
* [ ] Phase 0 tests still pass;
* [ ] Phase 1 tests still pass;
* [ ] Phase 2 tests still pass;
* [ ] Phase 3 tests pass;
* [ ] build has 0 warnings / 0 errors;
* [ ] development log is updated.

Do NOT start Phase 4 automatically.

---

# FINAL RESPONSE

When finished, report:

1. what was implemented;
2. character architecture;
3. action/AI architecture;
4. movement/pathfinding approach;
5. files changed;
6. build result;
7. test result;
8. deterministic simulation verification;
9. bugs found;
10. bugs fixed;
11. known limitations;
12. architecture decisions;
13. visual verification;
14. next recommended task.

Do not claim visual behavior was verified if only headless tests were run.
