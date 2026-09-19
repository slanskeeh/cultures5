# KINLANDS — PHASE 13: HISTORY AND PRESENTATION

## ROLE

You are the lead gameplay/domain architect and senior C# engineer for Kinlands.

Continue the existing project. Do NOT redesign the architecture from scratch.

The current implementation is Phase 12 complete.

Current verified baseline:

* 197/197 tests passing with `-warnaserror`
* Solution build: 0 warnings / 0 errors
* Godot 4.7.2.stable.mono headless boots successfully
* `Cultures.Domain` is pure `net8.0` and must not reference Godot
* Godot is presentation/application shell
* Simulation is authoritative
* Stable typed IDs are authoritative identity
* Commands represent intent
* Events represent facts
* Deterministic seeded randomness is mandatory
* World is finite, huge, horizontally wrapping, with polar Y termination
* Chunks are the main world/storage unit
* LOD is independent from presentation
* Exploration knowledge is independent from geography and presentation
* Culture, factions, diplomacy, politics and military are separate domains
* Full save/load is not implemented
* Phase 12 military has identity/lifecycle only; no combat/war/movement
* No automatic consequences may be invented for existing foundation systems

Read before changing anything:

* `docs/GAME_DESIGN_BIBLE.md`
* `docs/TECHNICAL_BIBLE.md`
* `docs/DECISIONS.md`
* `docs/MVP_ROADMAP.md`
* `docs/ARCHITECTURE.md`
* `docs/WORLD_ARCHITECTURE.md`
* `docs/SIMULATION_ARCHITECTURE.md`
* `docs/CURSOR_RULES.md`
* `docs/DEVELOPMENT_LOG.md`

Inspect the current implementation before coding.

---

# PHASE GOAL

Establish the first real **world history layer** and the first real **presentation architecture**.

The project currently has many systems that create important facts, but those facts are not yet represented as persistent history.

At the same time, Godot currently acts primarily as a debug shell.

This phase should establish:

```text
World / Simulation
        ↓
      Facts
        ↓
     History
        ↓
 Presentation / inspection
```

History must not become a second simulation.

Presentation must not become the simulation authority.

---

# PART 1 — HISTORY

Implement a domain-level history system.

The history system records important facts that already happen in the simulation.

Examples:

* character birth
* character death
* family-related event
* building created
* building disabled/destroyed if such state exists
* settlement emerged
* settlement established
* settlement declined
* settlement abandoned
* culture created
* faction created
* faction membership changed
* diplomatic stance changed
* political group created
* military unit created/disbanded
* other major facts already exposed through authoritative events

Do NOT invent gameplay solely to generate history.

---

# HISTORY MODEL

Introduce a stable historical record type.

It should contain at minimum:

* stable history/event identifier
* simulation time
* event/fact kind
* primary subject ID
* optional secondary subject ID
* optional location/chunk/settlement context
* human-readable summary only if appropriate
* importance level/category

Do not use strings as the only identity of a history event.

History identity must be independent of Godot presentation.

---

# HISTORY IS FACTS, NOT AI MEMORY

Do not implement:

* fake memories for every person
* psychological recollection
* conversation simulation
* historical AI reasoning
* reputation consequences
* automatic diplomacy changes
* automatic political reactions

History is currently a record of authoritative facts.

Future systems may consume those facts later.

---

# IMPORTANT EVENT RULE

Existing domain events remain facts.

Do not make history listeners mutate the world.

Example:

```text
DiplomaticStanceChangedEvent
        ↓
History recorder
```

is valid.

But:

```text
DiplomaticStanceChangedEvent
        ↓
History
        ↓
War automatically starts
```

is NOT allowed in this phase.

The history layer observes.

It does not create gameplay consequences.

---

# HISTORICAL IMPORTANCE

Not every low-level simulation tick should become history.

Do NOT record:

* every movement step
* every hunger update
* every work tick
* every pathfinding operation
* every LOD transition
* every presentation update

Use an explicit rule for what constitutes a historical event.

The exact importance policy may remain provisional.

Document it.

---

# SPARSE HISTORY

Do not create per-character log streams.

Use a centralized sparse historical event store.

Potential structure:

```text
HistoryDirectory
    -> HistoryEventId -> HistoryRecord
```

or another clean equivalent.

The API should support:

```text
GetEvent(id)
GetRecentEvents(...)
GetEventsForCharacter(characterId)
GetEventsForSettlement(settlementId)
GetEventsForFaction(factionId)
GetEventsForMilitaryUnit(unitId)
```

Do not expose mutable internals.

---

# HISTORY + LOD

History must remain independent from simulation LOD.

Aggregate simulation must NOT fabricate individual historical events for years of aggregated simulation.

This respects AD-074.

For aggregate simulation:

valid:

```text
Population decline recorded as a regional/aggregate fact
```

invalid:

```text
Invent 183 fake individual deaths
```

Preserve the distinction.

---

# HISTORY + EXPLORATION

History must not automatically become player knowledge.

A fact can exist historically while the player does not know it.

Do not reveal history merely because the history record exists.

Future knowledge systems may selectively expose historical facts.

---

# HISTORY + PERSISTENCE

Do not implement the complete save system in this phase.

History must nevertheless use serializable domain structures so Phase 14 can persist it cleanly.

No Godot-specific history objects.

---

# PART 2 — PRESENTATION ARCHITECTURE

Establish a proper presentation layer around the existing simulation.

Do not turn this into the final art production phase.

The goal is architecture and a usable visual foundation.

---

# PRESENTATION PRINCIPLE

Maintain:

```text
Domain
    = truth

Application
    = orchestration / commands / queries

Presentation
    = visualization + input

Godot Node
    != authoritative state
```

Never put simulation truth into Godot Nodes.

---

# WORLD PRESENTATION

Create a proper presentation-facing representation of:

* terrain
* water
* buildings
* characters
* settlements
* exploration knowledge overlay

Use the existing debug world as the starting point.

Do not throw away the world/debug renderer without replacement.

---

# CHARACTER PRESENTATION

The presentation must identify characters by `CharacterId`.

Do not identify them by Node instance.

Prepare a clean mapping:

```text
CharacterId
    ↓
CharacterView
```

A view can be recreated when presentation streaming changes.

This must remain compatible with LOD.

---

# BUILDING PRESENTATION

Likewise:

```text
BuildingId
    ↓
BuildingView
```

Do not make the Godot node the building identity.

---

# SETTLEMENT PRESENTATION

Likewise:

```text
SettlementId
    ↓
SettlementView
```

Do not turn settlements into rigid map objects merely because they now have a visual marker.

Keep AD-013 and AD-061 intact.

---

# EXPLORATION PRESENTATION

The player-facing presentation must distinguish:

```text
Unknown
Rumored
Scouted
Mapped
Confirmed
Analyzed
```

without leaking raw terrain when knowledge is insufficient.

Developer/debug views may intentionally show raw world truth when explicitly marked as debug.

---

# CAMERA

Introduce a basic presentation camera abstraction.

It must not become the simulation cursor.

The simulation cursor remains an application/debug concept.

The camera is presentation state.

Later, the camera can be used to determine desired presentation streaming, but do not couple it directly to authoritative simulation.

---

# SELECTION

Establish a proper presentation selection model using:

```text
CharacterId
BuildingId
SettlementId
```

as appropriate.

Selection itself must not mutate domain state.

Contextual character commands continue to use existing command architecture.

---

# HISTORY PRESENTATION

Add a minimal debug/history inspection view.

Examples:

```text
Recent history:
- Character born
- Building created
- Settlement established
- Faction created
- Diplomatic stance changed
- Military unit created
```

This is an inspection tool.

Do NOT build the final encyclopedia/history UI.

---

# DEBUG HUD

Do not overload the HUD indefinitely.

Organize debug information into logical sections:

* simulation
* world
* selected entity
* exploration
* faction/politics
* military
* recent history

Preserve existing debug controls.

Do not remove working debug controls merely for visual cleanup.

---

# TESTING

Add tests for:

## History

* important events are recorded
* low-level ticks are not recorded as spam
* stable IDs
* query by subject
* chronology
* deterministic event identity where applicable
* aggregate simulation does not fabricate personal histories
* history is independent from LOD
* history is independent from exploration knowledge

## Presentation-facing state

Where testable without Godot:

* entity identity mapping uses stable IDs
* selection references IDs
* queries do not mutate simulation state

---

# DOCUMENTATION

Use next available AD IDs.

Document at minimum:

* history is a first-class fact store
* history does not create gameplay consequences
* history is independent of LOD
* presentation identity uses domain IDs
* camera is presentation state
* presentation views are recreatable

Review and update relevant ODs if this phase resolves any.

Do not close unrelated ODs.

---

# DEVELOPMENT LOG

Update `docs/DEVELOPMENT_LOG.md`.

Record:

* task
* done
* verified
* exact tests
* bugs
* fixes
* limitations
* decisions
* files
* project health
* next step
* notes for ChatGPT

Never claim GUI behavior was verified if it was not manually inspected.

---

# STRICT OUT OF SCOPE

Do NOT implement:

* complete save/load
* final art pipeline
* final UI
* history encyclopedia
* character memories
* reputation
* diplomacy consequences
* war
* combat
* migration
* professions
* households
* resource overhaul
* multithreading

---

# DEFINITION OF DONE

Phase 13 is complete when:

1. Important domain facts can become persistent history records.
2. History has stable identity.
3. History is queryable.
4. History does not mutate simulation.
5. Aggregate simulation does not fabricate personal histories.
6. Presentation entities are identified by domain IDs.
7. World/character/building/settlement views are recreatable.
8. Exploration remains separate from raw terrain presentation.
9. Basic camera/selection architecture exists.
10. Existing debug controls still work.
11. Tests pass.
12. Build has 0 warnings / 0 errors.
13. Godot headless boots.
14. Development log is truthful.

Before implementation, report any contradiction discovered between this prompt and the existing documentation.
