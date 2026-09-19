# KINLANDS — PHASE 16: PROFESSIONS, HOUSEHOLDS AND DEEPER CHARACTER LIFE

## ROLE

Continue from completed Phase 15.

Do not redesign the CharacterState architecture.

The project already has:

* characters
* needs
* activities
* skills
* families/genealogy
* workplaces
* buildings
* settlements
* cultures
* factions
* politics
* military membership
* direct character commands
* deterministic simulation
* full persistence

This phase connects these systems into deeper individual life.

---

# PHASE GOAL

Introduce meaningful **professions, households and adult social life** without turning characters into scripted NPCs.

The intended chain is:

```text
Person
→ skills
→ profession
→ workplace
→ income/resources
→ household
→ family
→ settlement
```

The player should increasingly be able to recognize:

> "This is this person's life."

---

# PART 1 — PROFESSIONS

Introduce a data-driven profession definition.

Profession is NOT the same thing as skill.

A character may have:

```text
Profession: Farmer
Farming skill: 72
```

and later change profession without losing farming knowledge.

---

# PROFESSION MODEL

A profession definition should be data-driven.

Potential information:

* profession ID
* display name
* primary skill
* compatible workplaces
* age eligibility
* optional secondary skills
* future equipment seam
* future social role seam

Do NOT create one C# subclass per profession.

---

# PROFESSION ASSIGNMENT

Introduce explicit profession state on the character or equivalent compositional component.

The system should distinguish:

```text
Assigned workplace
```

from:

```text
Profession
```

A person can temporarily be unemployed or between professions.

---

# JOB MATCHING

Introduce a deterministic matching layer.

It may consider:

* character skills
* age
* settlement needs
* available workplaces
* existing profession
* household needs
* character preferences only if a personality system already exists

Do not implement full economic optimization.

---

# PLAYER CONTROL

Direct character control remains valid.

A manually controlled character must be able to receive context-sensitive profession/work commands where appropriate.

UI must still issue commands.

UI must never directly set profession.

---

# PART 2 — HOUSEHOLDS

Now address OD-017.

Household must be distinct from:

* genealogy
* family
* settlement
* faction

Do not equate:

```text
Family == Household
```

A household is a social/economic living unit.

Genealogy describes ancestry.

---

# HOUSEHOLD MODEL

Introduce stable `HouseholdId`.

A household can contain characters who:

* live together
* share shelter/home
* share resources or food
* care for children
* form an economic unit

The exact rules may be documented as provisional.

---

# HOUSEHOLD MEMBERSHIP

Membership must be authoritative somewhere.

Do not duplicate household membership onto several incompatible collections.

Prefer:

```text
CharacterState.Household
```

with derived household member lists.

---

# HOME

Connect households to shelter/building state.

A character should no longer conceptually:

```text
sleep at any arbitrary shelter
```

forever.

The architecture should support:

```text
Household
→ Home
→ Shelter building
```

without forcing every building to be a house.

---

# FOOD / RESOURCES

Households may act as a small consumption/planning unit.

However:

Do NOT move all inventories from characters/buildings into a giant `HouseholdInventory` without design justification.

Keep ownership granular.

---

# PART 3 — REPRODUCTION

Begin resolving OD-016.

Introduce real reproductive/marriage/family formation only to the extent necessary for believable household life.

Potential lifecycle:

```text
eligible adults
→ relationship
→ partnership/marriage
→ household
→ child
```

Do not implement romantic conversation simulation.

---

# CHILD BIRTH

Replace the Phase 5 debug birth mechanism as the primary gameplay path.

Birth should become deterministic and bounded by actual social/family conditions.

Existing inheritance remains:

```text
inheritance ≠ teaching
```

Teaching remains a separate activity.

---

# INFANT CARE

Now begin resolving OD-022.

Infants should no longer simply idle/eat.

Introduce minimal caregiver behavior:

* caregiver feeding
* carrying if necessary
* household proximity
* dependent state

Do NOT implement advanced child AI.

---

# LIFE STAGES

Preserve the existing age/life-stage model.

Do not hard-code each stage as a giant behavior switch.

Use compositional rules.

---

# PROFESSION + SKILL

Professions should use the existing skill model.

Example:

```text
Farmer
→ Farming skill

Carpenter
→ Woodworking

Mason
→ Stoneworking

Crafter
→ Crafting
```

The exact catalogue is provisional.

Do not turn skills into profession subclasses.

---

# BUILDING INTEGRATION

Workplaces should reference profession compatibility.

For example:

```text
Farm
→ Farmer-compatible workplace
```

rather than:

```text
if building == Farm
    profession = Farmer
```

Use definitions/data.

---

# SETTLEMENT INTEGRATION

Settlement statistics can derive:

* employed population
* profession distribution
* households
* dependents
* workers
* unemployed adults

Do not rewrite the settlement system into a rigid city object.

---

# SOCIAL MOBILITY

Characters should be able to change profession.

Reasons may later include:

* training
* lack of workplace
* settlement demand
* inheritance
* household necessity

For this phase, implement only deterministic foundation rules.

---

# PERSONALITY

Do NOT implement a giant personality system just because professions/households could use one.

Leave the seam open if personality remains an OD.

Do not add arbitrary personality stats.

---

# CULTURE

Culture may later influence:

* preferred profession
* naming
* household customs
* building style

But do NOT introduce cultural global economic multipliers.

Keep culture identity separate.

---

# POLITICS

Do not automatically change political influence because a person changed profession unless a documented Phase 16 rule explicitly requires it.

Existing politics remains its own domain.

---

# MILITARY

Do not automatically recruit characters because they are unemployed.

Military membership remains optional.

Future recruitment can consume profession/household data later.

---

# PERSISTENCE

Persist:

* profession state
* HouseholdId
* household membership
* home association
* marriage/partnership state if introduced
* reproductive state if introduced
* caregiver relationships if introduced

Use the Phase 14 persistence architecture.

---

# DETERMINISM

Marriage/household/birth decisions must be deterministic.

Do not use uncontrolled random generation.

Use the existing seeded RNG infrastructure when randomness is truly required.

---

# TESTING

Add tests for:

## Professions

* assignment
* invalid assignment
* profession/workplace compatibility
* skill compatibility
* profession switching
* direct player command validation

## Households

* creation
* membership
* leaving
* home
* household/genealogy separation
* derived member lists
* settlement integration

## Family life

* partnership/marriage if implemented
* deterministic child creation
* inheritance
* teaching remains separate
* infant caregivers
* household-aware child care

## Persistence

* full household round-trip
* profession round-trip
* family round-trip
* continuation after load

## LOD

Test aggregate handling of households and families carefully.

Important:

Do not invent fake personal history when households are aggregated.

---

# LOD CONSTRAINT

Families and households increase the importance of LOD.

Do NOT solve this by keeping everything Full forever.

The implementation should preserve:

* household identity
* family identity
* important individuals
* demographic continuity

while still allowing distant ordinary populations to aggregate.

Do not close OD-024 casually.

---

# HISTORY

Profession changes, household formation and births should create history facts where appropriate.

Examples:

```text
Character became Farmer
Household formed
Child born
Household moved home
```

Do not record every routine work action.

Use the Phase 13 history importance policy.

---

# DOCUMENTATION

Potential ODs affected:

* OD-014 command queue/interruption
* OD-016 reproduction/marriage/fertility
* OD-017 household vs genealogy
* OD-021 leadership only if this phase exposes a genuine need
* OD-022 infant care
* OD-015 skill rates
* OD-024 aggregate population representation

Only close the decisions actually resolved.

Add Architecture Decisions for:

* profession vs skill
* household vs family
* household home
* authoritative household membership
* caregiver model
* profession/workplace compatibility

Use next available AD IDs.

---

# STRICT OUT OF SCOPE

Do NOT implement:

* elections
* leaders
* laws
* advanced politics
* war
* combat
* military recruitment
* trade
* migration gameplay
* personality overhaul
* complete romance simulation
* multiplayer
* final art production

---

# DEVELOPMENT LOG

Update `docs/DEVELOPMENT_LOG.md` with:

* implementation
* verified behavior
* tests
* bugs
* architecture
* known limitations
* current health
* next phase

Do not claim household or marriage GUI behavior was tested unless it actually was.

---

# DEFINITION OF DONE

1. Professions exist separately from skills.
2. Workplaces can require compatible professions.
3. Characters can change profession.
4. Households exist separately from genealogy.
5. Households have stable IDs.
6. Homes can be represented.
7. Birth is no longer only a debug-only concept.
8. Infant care has a minimal real simulation.
9. Household/family data survives LOD.
10. History records meaningful life events.
11. Persistence works for the new state.
12. Tests pass.
13. Build has 0 warnings / 0 errors.
14. Godot headless boots.
15. Development log is updated truthfully.
