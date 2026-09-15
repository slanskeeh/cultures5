# Cultures Successor — GAME DESIGN BIBLE v0.3

## 1. Vision

This project is a spiritual successor to the Cultures series: a slow, character-focused settlement and civilization simulation in which the player cares about individual people while also shaping a larger society.

The game must preserve the core feeling of:
- watching ordinary people live;
- assigning and observing work;
- building a settlement organically;
- discovering the world;
- following families and individual stories;
- making long-term decisions whose consequences unfold over years.

The project is NOT intended to become a pure 4X game, a city-builder spreadsheet, or a fast RTS.

The central design principle is:

> The player should be able to zoom mentally from one person to the whole civilization without either level making the other meaningless.

## 2. Design pillars

### Pillar A — People first

Individual characters are persistent people with:
- identity;
- family;
- personality;
- preferences;
- skills;
- health;
- needs;
- relationships;
- profession;
- history.

Characters should feel like inhabitants rather than disposable workers.

### Pillar B — A living world

The world continues to exist when the player is not looking at it.

Civilizations:
- grow;
- migrate;
- trade;
- form alliances;
- fight;
- decline;
- recover;
- discover;
- split;
- merge.

The world should create stories without requiring scripted quests for every event.

### Pillar C — Simple rules, rich combinations

We prefer expanding existing systems over adding hundreds of separate systems.

Example:
One Farm building can produce different outputs depending on:
- biome;
- climate;
- soil;
- culture;
- technology/knowledge;
- season.

The goal is systemic variety rather than building-count inflation.

### Pillar D — History matters

People, families, buildings, settlements and civilizations accumulate history.

A building can become associated with:
- a family;
- a famous craftsman;
- a political event;
- a long-running profession.

A character can inherit:
- family reputation;
- familiarity with a profession;
- social connections;
- cultural traditions.

### Pillar E — Player agency without micromanagement overload

The player can directly manage important individuals when desired, but the simulation must remain playable through observation, delegation and high-level policies.

Direct control should improve precision, not be mandatory for every action.

## 3. World

### 3.1 Scale

The world is finite but very large.

It is horizontally wrapped:
- travelling east indefinitely eventually returns to the same longitude;
- north and south terminate in polar regions;
- polar regions contain ice/glacial barriers and extreme environments.

The world should feel like a planet rather than a collection of isolated levels.

### 3.2 Procedural generation

A world is generated from:
- world seed;
- generation version.

Generation should be deterministic.

Pipeline:

Seed
→ macro geography
→ elevation
→ climate
→ hydrology
→ biome
→ soil/fertility
→ resources
→ wildlife
→ landmarks
→ initial civilizations
→ initial settlements.

Geography should create causal relationships instead of independent random layers.

### 3.3 Exploration

The player should not know the complete world at game start.

Knowledge is progressively acquired through:
- travel;
- scouting;
- maps;
- encounters;
- trade;
- reports;
- diplomacy.

Unknown territory should remain meaningful.

A known resource can still be:
- unknown;
- rumored;
- scouted;
- mapped;
- confirmed;
- analyzed.

### 3.4 Climate and biomes

The world is divided into climatic/biome zones.

Biome influences:
- available resources;
- wildlife;
- fertility;
- production;
- building suitability;
- hazards;
- settlement attractiveness;
- trade opportunities.

The same building can behave differently in different environments.

Example:

Farm:
- meadow → wheat;
- forest clearing → berries;
- desert oasis → dates.

This is a content multiplier: more variety without requiring a separate building for every resource.

### 3.5 Wildlife

Wildlife is part of the living ecosystem.

Animals may:
- inhabit suitable biomes;
- migrate;
- reproduce;
- become scarce;
- provide food/materials;
- interact with settlements.

Wildlife should not be merely decorative resource nodes.

## 4. Factions and cultures

The player can choose a faction/cultural identity.

Faction influences:
- architecture;
- clothing;
- language;
- names;
- visual motifs;
- preferences;
- starting tendencies;
- social norms;
- possible diplomatic attitudes.

Faction must not simply be a set of numerical bonuses.

Different cultures should encourage different play styles through preferences and context.

## 5. Characters

### 5.1 Life

Characters have meaningful lifespans.

The simulation is intentionally slow enough that:
- a child can grow into an adult;
- a profession can be learned;
- relationships can develop;
- a character can become socially important;
- the player can remember individuals.

### 5.2 Personality

Characters have traits and preferences.

Traits influence tendencies, not deterministic behavior.

Examples of preference categories:
- food;
- work;
- social behavior;
- environment;
- wealth;
- leadership;
- risk;
- culture.

A character can dislike a profession without becoming incapable of doing it.

### 5.3 Needs

Needs influence behavior.

Initial examples:
- hunger;
- rest;
- social interaction;
- health.

Future needs may be added only when they produce meaningful gameplay.

## 6. Skills and professions

Skills improve through:
- practice;
- work;
- teaching;
- observation.

A parent's expertise can give a child a small inherited starting familiarity.

A parent can actively teach a child.

Direct player control can make training more effective/focused; autonomous teaching still occurs at a slower rate.

Skill progression should be gradual and persistent.

## 7. Families and lineage

Families are persistent social units.

A family can accumulate:
- reputation;
- wealth;
- professions;
- traditions;
- notable members;
- ownership history.

Family relationships must survive individual character death.

Possible emergent story:

Family A operates the same workshop for generations.

The building can develop a recognizable family identity, such as a family initial/sign.

This is a cosmetic/history feature derived from actual simulation history, not an arbitrary decoration.

## 8. Buildings

Buildings are persistent places.

A building has:
- type;
- location;
- construction history;
- condition;
- workers;
- ownership/association;
- production;
- historical events.

Buildings should be reused as systemic containers rather than multiplying building types unnecessarily.

### Environmental production

A building's output is resolved from context:

Building
+ biome
+ climate
+ season
+ workers
+ skills
+ inputs
→ output.

## 9. Settlements

A settlement is primarily a simulation concept, not necessarily a hard visual boundary.

If a population cluster:
- persists;
- shares infrastructure;
- has economic activity;
- has social cohesion;

the simulation can recognize it as a settlement.

The player does not need to see a rigid "city border".

Settlements may:
- grow;
- shrink;
- split;
- merge;
- migrate;
- disappear.

### Settlement emergence safeguard

The world should avoid becoming permanently empty.

If a sufficiently large inhabited region has remained without a viable settlement for a configured period, the simulation may generate or migrate a suitable founding group.

This should be:
- deterministic;
- explainable;
- rare;
- controlled by world rules.

It exists to preserve a living world, not to cheat the player.

## 10. Happiness and health

Settlement and character wellbeing matter.

Happiness can be influenced by:
- food security;
- housing;
- health;
- social relationships;
- work;
- environment;
- taxes;
- safety;
- political conditions;
- personal preferences.

Health affects:
- productivity;
- ability to work;
- survival;
- quality of life.

The systems should remain understandable rather than becoming hundreds of hidden modifiers.

## 11. Economy

The economy is based on:
- resources;
- production;
- storage;
- consumption;
- trade;
- wealth;
- taxes.

### Food storage

Food must be physically/logically stored and consumed.

Storage capacity and spoilage can create meaningful strategic decisions.

### Taxes

Taxes are a political/economic lever.

Tax policy can affect:
- settlement income;
- personal wealth;
- happiness;
- migration;
- political support.

## 12. Diplomacy

Diplomacy is expanded beyond simple friendship/war states.

Possible interactions:
- trade agreements;
- resource exchange;
- long-term contracts;
- gifts;
- tribute;
- access rights;
- non-aggression;
- alliances;
- mutual defense;
- information exchange;
- diplomatic demands;
- migration/settlement agreements;
- sanctions/embargoes;
- declarations of war;
- peace agreements.

Relations retain history.

A betrayal can matter years later.

## 13. Internal politics

Settlements/civilizations can have:
- leader;
- council;
- influential people;
- offices;
- factions/interests.

Influence can come from:
- wealth;
- family reputation;
- profession;
- skill;
- age;
- office;
- relationships;
- military reputation;
- achievements;
- popularity.

Important offices should generally be available to socially influential characters.

Examples:
- settlement leader;
- council member;
- commander;
- mayor/administrator;
- representative.

Politics should emerge from character relationships and material conditions.

## 14. Civilization simulation

Civilizations have:
- culture;
- territory/influence;
- population;
- economy;
- diplomacy;
- political structure;
- military capability;
- knowledge/discoveries;
- history.

Civilizations can:
- expand;
- contract;
- migrate;
- split;
- merge;
- collapse.

Other civilizations must continue acting without player supervision.

## 15. Military

Military is a future major system.

The architecture should leave room for:
- commanders;
- units;
- recruitment;
- supply;
- morale;
- battles;
- diplomacy;
- war history.

Military power must depend on civilization and character systems rather than existing as an isolated RTS layer.

## 16. Player control levels

The player can operate at multiple levels:

Character level:
- directly guide a person;
- teach;
- prioritize tasks.

Settlement level:
- construction;
- production priorities;
- storage;
- taxes;
- policies.

Civilization level:
- diplomacy;
- expansion;
- strategic policies;
- major appointments.

Observation:
- allow the simulation to operate autonomously.

The game should remain fun at all four levels.

## 17. Content philosophy

Do not solve variety by creating hundreds of unique objects.

Prefer:
- variants;
- environmental modifiers;
- cultural appearance;
- production rules;
- professions;
- traits;
- historical context.

Example:
One workshop can produce many different things depending on:
- available resources;
- worker skill;
- culture;
- biome;
- demand.

## 18. What the game must NOT become

Avoid:
- a spreadsheet with people-shaped icons;
- a conventional RTS with a huge map;
- a pure 4X where individual people are irrelevant;
- a colony sim where the rest of the planet is fake;
- a building catalog where every new mechanic requires a new building;
- mandatory micromanagement of every citizen.

## 19. MVP philosophy

The first playable slice should prove the core fantasy with very few systems.

The first goal is not "many features".

The first goal is:

> A small group of people lives, works, learns, consumes resources, forms relationships and changes over time inside a persistent world.

# PLAYER CHARACTER CONTROL

## Overview

Kinlands combines autonomous character simulation with direct player commands.

Characters are independent simulated individuals with their own needs, activities, relationships, skills and future personal histories. However, the player must be able to directly intervene in the life of an individual character.

The player interaction model is inspired by the original Cultures series:

1. The player selects an individual character by clicking them.
2. The selected character becomes the active interaction target.
3. A contextual action menu is opened for that character.
4. The menu presents actions currently available to that character.
5. The player selects an action.
6. The command is passed into the authoritative simulation.
7. The character performs the command through the normal activity/action system.

Direct player control is therefore not a separate simulation system. It is a higher-priority source of commands for the same character activity system used by autonomous AI.

---

## Character Selection

Characters are selectable individually.

The player must be able to:

* click a character;
* see which character is selected;
* inspect basic character information;
* open the character's contextual action menu;
* issue direct commands;
* cancel or replace an existing command.

Selection is a presentation/input concept and must not become part of the authoritative character simulation state unless required for multiplayer/replay purposes in the future.

---

## Contextual Action Menu

The available actions are contextual.

The game must not display every possible action for every character.

Available actions depend on factors such as:

* character state;
* character age;
* character skills;
* character needs;
* current activity;
* current location;
* nearby terrain;
* nearby buildings;
* nearby resources;
* nearby characters;
* character relationships;
* available workplaces;
* ownership/access rules;
* future faction/cultural rules.

Example:

A child should not receive an action such as "Teach Craft".

A character without sufficient skill should not receive advanced teaching actions.

An action involving a nearby resource should not be offered when no valid target exists.

The action menu therefore represents the actions the simulation considers valid for the selected character at that moment.

---

## Example Actions

The exact final action catalogue is not fixed.

Potential actions include:

### Movement

* Go to location
* Follow character
* Return home

### Work

* Work at building
* Work at specific workplace
* Gather resource
* Perform specific profession task

### Needs

* Eat
* Sleep
* Rest
* Seek shelter

### Social

* Talk
* Follow
* Invite
* Teach
* Learn
* Visit

### Objects / Environment

* Gather
* Pick up
* Drop
* Transport
* Interact with building
* Hunt
* Fish

### Future management

* Assign workplace
* Assign profession
* Appoint to position
* Join group
* Lead group
* Become commander
* Become representative

These actions are examples of the extensible system, not a requirement to implement them all at once.

---

## Direct Commands vs Autonomous Behaviour

A character normally acts autonomously.

For example:

```text
Hunger rises
    ↓
Character decides to obtain food
    ↓
Character finds food
    ↓
Character eats
```

If the player intervenes:

```text
Player selects character
    ↓
Player chooses "Go to Farm"
    ↓
Player command is issued
    ↓
Character executes command
```

The player command temporarily takes precedence over autonomous decision-making.

After a command finishes, is cancelled, or becomes invalid, the character should be able to return to autonomous behaviour.

---

## Command Interruption

Player commands may interrupt an existing activity.

For example:

```text
Character is working
        ↓
Player orders movement
        ↓
Work activity interrupted
        ↓
Character moves
        ↓
Command completed
        ↓
Autonomous AI resumes
```

The exact interruption rules are not final and must be designed per action type.

Critical survival needs may also invalidate or interrupt player commands in the future.

---

## Player Control Is Not Character Possession

The player does not permanently "possess" characters.

Characters remain autonomous simulated people.

Direct control represents:

> the player giving instructions to an individual.

This distinction is important for the identity and simulation philosophy of Kinlands.

The player is an overseer capable of intervening in individual lives, not a supernatural force that completely replaces their agency.

---

## Long-Term Design Goal

The player should be able to choose their preferred level of micromanagement.

A player may:

### Micromanage individuals

Select specific people and personally instruct them.

### Manage workplaces

Assign people to buildings and let them determine individual actions.

### Manage the settlement

Set priorities and policies while characters handle everyday activity.

### Observe

Allow the simulation to operate mostly autonomously.

All four modes should operate on the same underlying character simulation.

---

## Architectural Principle

The UI must never directly modify authoritative character state.

Conceptually:

```text
Player Input
    ↓
Character Selection
    ↓
Context Action Provider
    ↓
Player Command
    ↓
Application Layer
    ↓
Simulation
    ↓
Character Activity
```

Never:

```text
UI
 ↓
CharacterState.Position = ...
```

or:

```text
UI
 ↓
CharacterState.Inventory.Add(...)
```

The simulation must remain authoritative.

---

## Future Expansion

The contextual action system must eventually support actions involving:

* buildings;
* resources;
* animals;
* other characters;
* families;
* workplaces;
* professions;
* education;
* diplomacy;
* military units;
* politics;
* ownership;
* cultural/faction preferences.

The action system must therefore be extensible and must not be designed around a fixed list of UI buttons.
