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

Status: COMPLETED

Canonical documents are present in `docs/` (`GAME_DESIGN_BIBLE.md`, `TECHNICAL_BIBLE.md`, `DECISIONS.md`, `MVP_ROADMAP.md`, architecture files, Cursor rules).

## Phase 1 — Logical World Foundation

Status: COMPLETED

Delivered:
- configurable `WorldConfiguration`;
- explicit coordinate types;
- Euclidean horizontal wrap;
- north/south bounds without Y clamp;
- chunk/local conversion and roundtrip;
- in-memory `LogicalGrid`, `TerrainCell`, occupancy;
- wrap-aware horizontal distance;
- non-authoritative isometric/screen mapping;
- debug cursor commands and orthographic debug map.

No procedural world generation.

Exit criteria were: deterministic conversions, wrap tests, occupancy tests, debug grid visualization, debug cursor across the wrap boundary.

## Phase 2 — Minimal World Generation

Status: COMPLETED

Delivered:
- seed + generation version contract;
- cylindrical wrap-aware macro geography and elevation;
- sea-level water/land;
- latitude/elevation climate;
- placeholder biome classifier;
- on-demand deterministic chunk cache;
- debug biome coloring.

Exit criteria were: same seed same world, different seeds differ, wrap continuity, chunk load without full-planet allocation.

## Phase 3 — First Living Characters

Status: COMPLETED

Delivered:
- persistent `CharacterId` / compositional `CharacterState`;
- aging, hunger, fatigue, health/death;
- actions Idle/Move/Eat/Sleep/Work;
- deterministic priority AI;
- wrap-aware 4-direction BFS;
- 24-character land spawn;
- debug markers + inspect HUD.

Personality, families and professions were intentionally not implemented.

Exit criteria: autonomous population for multiple simulation days, determinism tests, water/wrap/pole movement rules.

## Phase 4 — Buildings and Production

Status: COMPLETED

Delivered:
- `BuildingId` / `BuildingDefinition` / `BuildingState`;
- placement, footprints, occupancy, removal;
- Food/Wood/Stone inventories;
- data-driven recipes and workplaces;
- production → storage → eat;
- shelter rest;
- buildings block navigation;
- lifecycle with instant debug completion;
- debug letters F/S/H/W.

Not a settlement. Construction economy, biome product catalogue, seasons and skills were intentionally not implemented.

Exit criteria: autonomous food/material loop, occupancy/wrap tests, determinism.

## Phase 5 — Families and Skills

Status: NEXT

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