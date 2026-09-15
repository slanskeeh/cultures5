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