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
- culture
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

Phase 5 represents genealogy as parent/child `CharacterId` links and queries (siblings, grandparents). A full `FamilyState` (name, reputation, household) is future work. `FamilyId` is reserved and unused.

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

Settlement emergence evaluates:
- population density;
- social cohesion;
- shared culture;
- infrastructure;
- economic activity;
- persistence over time.

A settlement may:
- grow;
- shrink;
- split;
- merge;
- migrate;
- disappear.

## 13. Politics

Influence is derived from multiple factors:
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

Political positions should not be arbitrary UI assignments.

## 14. Diplomacy

Diplomatic relations are state + history.

A relation can remember:
- treaties;
- trade;
- aid;
- betrayals;
- wars;
- territorial conflicts;
- tribute;
- insults/refusals.

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

At low LOD, individual actions become aggregate flows.

Example:

Detailed:
100 farmers produce individual amounts.

Macro:
Region calculates agricultural output from:
- farmland;
- population;
- skill distribution;
- climate;
- inputs;
- season.

When detail resumes, the aggregate state becomes the initial condition for individual simulation.

## 18. Save/load

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

## 19. Deterministic tests

Core systems should support:
same initial state + same commands + same seed
→ same resulting state.

This is essential for debugging AI-generated code.
