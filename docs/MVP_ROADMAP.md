# MVP ROADMAP v0.3

## Philosophy

Each phase must create a technically testable layer.

Do not jump to content-heavy features before the underlying simulation can support them.

## Phase 0 — Foundation

Status: COMPLETED

Delivered:
- Godot .NET project;
- pure domain assembly;
- typed IDs;
- simulation clock;
- deterministic RNG;
- events;
- commands;
- save envelope;
- headless simulation host;
- automated tests;
- development logging.

## Phase 0.5 — Documentation synchronization

Status: CURRENT

Goals:
- restore all canonical project documents;
- ensure Cursor knows the full design;
- record accepted/open architectural decisions;
- make development log authoritative.

Exit criteria:
- all required docs exist;
- no conflicting core architecture statements;
- Cursor rules reference the development log;
- repository builds.

## Phase 1 — Logical World Foundation

Goals:
- WorldCoordinate;
- ChunkCoordinate;
- LogicalGridCoordinate;
- coordinate conversion;
- horizontal wrap;
- north/south bounds;
- grid occupancy;
- terrain cell data;
- chunk boundaries.

No procedural world generation yet.

Exit criteria:
- deterministic coordinate conversions;
- wrap tests;
- grid occupancy tests;
- debug visualization of grid;
- player can move a debug cursor across the wrap boundary.

## Phase 2 — Minimal World Generation

Goals:
- seed;
- macro geography;
- basic elevation;
- water;
- climate;
- biome;
- deterministic generation;
- chunk generation.

Exit criteria:
- same seed produces same world;
- different seeds produce meaningfully different worlds;
- generated world wraps correctly;
- chunks can be loaded/unloaded.

## Phase 3 — First Living Characters

Goals:
- character identity;
- age;
- needs;
- simple personality;
- movement;
- basic pathfinding;
- work;
- food;
- sleep.

Target:
20–30 characters.

Exit criteria:
A small population can survive autonomously for many in-game days.

## Phase 4 — Buildings and Production

Goals:
- building placement;
- construction;
- workers;
- storage;
- resources;
- production;
- environmental production variants.

First building set should be deliberately small.

Exit criteria:
A settlement can produce food and basic materials autonomously.

## Phase 5 — Families and Skills

Goals:
- families;
- children;
- aging;
- skills;
- professions;
- teaching;
- skill inheritance/familiarity;
- family history.

Exit criteria:
At least one generation transition works and is persisted.

## Phase 6 — Emergent Settlements

Goals:
- population clustering;
- settlement formation;
- growth/shrinkage;
- settlement leadership;
- basic happiness/health;
- taxes;
- food storage.

Exit criteria:
Settlements emerge without explicit player placement.

## Phase 7 — Large World and Simulation LOD

Goals:
- chunk streaming;
- distant simulation;
- aggregate populations;
- LOD transitions;
- migration.

Exit criteria:
World can contain substantially more inhabitants than the fully detailed simulation area without frame rate collapsing.

## Phase 8 — Exploration

Goals:
- fog/unknown world;
- scouting;
- maps;
- discoveries;
- landmarks;
- reports.

Exit criteria:
Exploration produces persistent knowledge and meaningful decisions.

## Phase 9 — Factions and Cultures

Goals:
- player faction selection;
- visual identity;
- names/language;
- cultural preferences;
- cultural production modifiers;
- cultural relations.

Exit criteria:
Different factions feel different without being simple +10% modifiers.

## Phase 10 — Diplomacy

Goals:
- trade;
- treaties;
- tribute;
- access;
- information;
- alliances;
- war/peace foundation;
- diplomatic history.

Exit criteria:
Two autonomous civilizations can maintain evolving relations without scripted sequences.

## Phase 11 — Internal Politics

Goals:
- leader;
- council;
- influence;
- offices;
- factions/interests;
- succession/appointment logic.

Exit criteria:
Political positions emerge from character/social state.

## Phase 12 — Military

Goals:
- recruitment;
- commanders;
- units;
- supply;
- morale;
- battles;
- war history.

Exit criteria:
Military outcomes feed back into population, economy and diplomacy.

## Phase 13 — History and Presentation

Goals:
- historical UI;
- family records;
- notable buildings;
- civilization timeline;
- improved graphics;
- audio;
- polish.

## Phase 14 — Alpha

Focus:
- balance;
- performance;
- save stability;
- simulation stability;
- UX;
- emergent story quality.

## Phase 15 — Beta / Content Expansion

Focus:
- more biomes;
- animals;
- factions;
- professions;
- buildings;
- events;
- diplomacy depth;
- world variety.

## Phase 16 — Release Candidate

Focus:
- optimization;
- bug fixing;
- save migrations;
- accessibility;
- onboarding;
- packaging;
- crash reporting.

## Golden rule

A phase is complete when its behavior is verified, not when code merely exists.