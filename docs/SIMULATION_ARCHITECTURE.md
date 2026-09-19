# SIMULATION ARCHITECTURE v0.2

## 1. Simulation clock

The simulation has its own clock.

Hierarchy:

Tick
→ Minute
→ Hour
→ Day
→ Season
→ Year

Rendering FPS does not control game time.

## 2. Update frequencies

Different systems update at different frequencies.

Example:

Movement:
high frequency nearby.

Needs:
periodic.

Production:
work-cycle based.

Economy:
hour/day.

Demography:
day/season.

Politics:
day/season/event.

World macro simulation:
day/season/year.

These are starting defaults, not hard-coded final values.

## 3. Character state

CharacterState contains conceptual groups:

Identity
- id
- name
- age
- `CultureId` (Phase 9; default Neutral / Unaffiliated)
- `FactionId` (Phase 9; default none)
- appearance seed

Social
- family
- relationships
- reputation
- memories

Needs
- hunger
- rest
- social
- future needs

Work
- profession
- skills
- current task
- workplace

Physical
- health
- location

Economic
- wealth
- inventory

History
- important life events

## 4. Character behavior

Character behavior is driven by:

Needs
+
Personality
+
Preferences
+
Responsibilities
+
Available actions
+
Current context

The AI chooses an action; the action changes simulation state.

Do not write hundreds of special-case if/else rules.

## 5. Actions

Examples:
- GoToWork
- GatherResource
- ProduceItem
- Eat
- Sleep
- Socialize
- TeachChild
- LearnSkill
- Build
- Trade
- Travel

An action has:
- preconditions;
- target;
- duration;
- effects;
- cancellation rules.

## 6. Needs

Needs should be interpretable.

For example:

Hunger ↓
→ character seeks food
→ food consumed
→ inventory/storage changes
→ happiness/health may change

Avoid opaque formulas where the player cannot understand the cause.

## 7. Professions

ProfessionDefinition describes:
- required skills;
- workplace tags;
- actions;
- production abilities;
- teaching abilities;
- status/influence implications.

A character can change profession through simulation or player intervention.

## 8. Skills

Skills increase through:
- practice;
- work;
- teaching;
- observation.

Parents may transmit familiarity but not full mastery.

Phase 5 stores integer skill experience on the character. Work and teaching add XP; birth inheritance is a small one-time contribution. Skills are not professions.

## 9. Families

Phase 5 represents genealogy as parent/child `CharacterId` links and queries (siblings, grandparents). A full `FamilyState` (name, reputation, household) is future work. `FamilyId` is reserved and unused. Phase 6 adds `HouseholdId.None` on characters and caregiver links (initially parents). Family membership is independent of settlement membership.

Birth now starts at age 0 as `Infant`. Life stages: Infant → Child → Adolescent → Adult → Elder → Dead. Infants cannot work, teach, or navigate independently.

## 10. Buildings

BuildingState contains:
- id;
- definition;
- location;
- condition;
- construction date;
- workers;
- owner/family where applicable;
- production state;
- history.

Buildings are persistent historical places.

## 11. Production

Production is:

Inputs
+
Building
+
Worker
+
Skills
+
Environment
+
Context
→ Output

A Farm does not inherently mean Wheat.

The production resolver evaluates context.

Phase 4 implements this as `ProductionResolver` + `IEnvironmentProductionModifier`. The current modifier is neutral (1.0x). Do not add biome-specific building types; change the resolver/modifier instead.

## 12. Settlements

Phase 6 implements settlements as persistent groupings that emerge from living characters plus active infrastructure.

Detection:
- occupied chunks (people and active buildings);
- 4-neighbour connected components with horizontal chunk wrap;
- qualification: enough people, enough buildings, shelter and storage;
- periodic evaluation (not every tick, not O(N²) pairwise).

A settlement has:
- `SettlementId` (not coordinates);
- derived wrap-aware core;
- lifecycle (Emerging / Established / Declining / Abandoned);
- derived membership and statistics;
- `CultureId` (default Neutral until assigned);
- optional `Leader` seam.

A settlement may later:
- grow;
- shrink;
- split;
- merge;
- migrate;
- disappear.

Split, merge, migration, politics, taxation, and trade are not implemented in Phase 6.

Abandoned identity is kept. Re-inhabitation currently creates a new `SettlementId` (OD-018).

Phase 9 adds `CivilizationSystem`: cultures and factions are social entities, not settlements. Membership is on `CharacterState`. Relations are sparse symmetric stances without diplomacy behavior. Factions do not claim chunks.

## 13. Politics

Phase 11 implements internal politics as `InternalPoliticsSystem`.

A political group belongs to exactly one faction. Membership is `CharacterState.PoliticalGroup` and must match the character's faction. Influence is explicit 0–100 state and is not equal to member count. Faction internal stability is a sparse 0–100 value (missing = 50).

There is no autonomous political loop, no offices, elections, succession, or rebellions.

Later influence may still be derived from:
- wealth;
- family reputation;
- profession;
- skill;
- age;
- office;
- relationships;
- achievements;
- military reputation;
- popularity.

Political positions should not be arbitrary UI assignments. Phase 11 only stores group affiliation, influence and stability.

Internal politics is social/faction state. It does not create territory, diplomacy consequences, war, economy, or autonomous political behavior.

Culture, Faction, Internal Politics, Diplomacy and Military are related domains but are not interchangeable:

```text
Culture
    ↓
Faction
    ├── Internal Politics
    ├── Diplomacy
    └── Military
```

## 14. Diplomacy

Phase 10 implements diplomacy as `DiplomacySystem` over the existing sparse symmetric `FactionRelationDirectory`.

Stances: Neutral (implicit/missing), Friendly, Hostile.

These are diplomatic stances only. Friendly is not an alliance. Hostile is not war. Changing stance does not move people, reveal geography, claim land, or alter the economy.

A later relation can remember:
- treaties;
- trade;
- aid;
- betrayals;
- wars;
- territorial conflicts;
- tribute;
- insults/refusals.

Those histories are not stored yet (OD-039).

Diplomacy must be able to react to events without rewriting the character system.

## 15. Migration

Migration groups have an origin.

Possible origins:
- refugees;
- nomads;
- colonists;
- explorers;
- merchants;
- diaspora;
- survivors.

A new settlement should be explainable by simulation history.

## 16. History

History has levels:

Personal
- birth;
- marriage;
- profession;
- achievements;
- death.

Place
- construction;
- owners;
- famous workers;
- destruction;
- reconstruction.

Settlement
- founding;
- growth;
- famine;
- war;
- migration;
- political changes.

Civilization
- wars;
- treaties;
- migrations;
- rulers;
- collapse;
- expansion.

## 17. Macro simulation

Phase 7 implements LOD as `LodSystem` + `AggregateSimulation`.

At Full/Reduced, individuals tick (Reduced may skip behavior ticks).

At Aggregate/Macro, `CharacterSimulation` skips those people. Bulk steps apply hunger, aging, farm output and optional macro births. Events are demographic (`AggregateBirthsOccurredEvent`, `AggregateDeathsOccurredEvent`, `AggregateFoodShortageEvent`), not fake personal histories.

When detail resumes, the same `CharacterId`s continue. Census on `ChunkSimulationState` is derived, not a second inventory.

Exploration knowledge is not a LOD concern. A chunk may be Analyzed while Aggregate, or Unknown while Full. `ExplorationSystem` does not classify or mutate simulation tiers.

## 18. Military

Phase 12 implements military as `MilitarySystem` over a sparse `MilitaryUnitDirectory`.

A unit belongs to exactly one existing faction. Membership is optional on `CharacterState.MilitaryUnit`. Member counts are derived from the roster. Units do not own chunks, cells, or territory.

Lifecycle is Active / Disbanded. Disbanding clears memberships. There is no combat, HP, equipment, movement, or war.

Hostile diplomacy is not war. Military commands do not change diplomacy, politics, exploration, LOD, or resources. `SimulationHost.Step` does not tick military.

World cell identity remains `LogicalGridCoordinate` (OD-002). Military does not encode neighbor topology and is compatible with a future hexagonal cell set.

## 19. Save/load

Every mutable system must define serialization.

Saves are versioned.

A save contains:
- seed;
- generation version;
- simulation time;
- mutable state;
- persistent IDs;
- important history;
- discoveries.

## 20. Deterministic tests

Core systems should support:
same initial state + same commands + same seed
→ same resulting state.

This is essential for debugging AI-generated code.
