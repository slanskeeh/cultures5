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

Status: COMPLETED

Delivered:
- parent/child `FamilyLinks` and queries;
- bounded `CreateChildCommand`;
- child/adult work restriction;
- generic integer skills;
- work XP and production skill modifier;
- Teach/Learn activity + `TeachCharacterCommand`;
- small birth inheritance, separate from teaching;
- debug inspect + N/T/K commands.

Professions, personality, marriage, and family history signs were intentionally not implemented.

## Phase 6 — Emergent Settlements

Status: COMPLETED

Delivered:
- wrap-aware chunk clustering of people and buildings;
- emergent `SettlementId` with derived core;
- lifecycle Emerging → Established → Declining → Abandoned with hysteresis;
- mutable derived membership on characters;
- building association without deleting buildings on abandon;
- derived settlement statistics (population, shelter, stored food, workers);
- `CultureId.Neutral` and `HouseholdId.None` seams;
- newborns at age 0 as Infant, with caregiver links;
- debug inspect (M/U/E) and `EvaluateSettlementsCommand`.

Intentionally not implemented (later phases):
- leadership gameplay, happiness, taxes, trade, diplomacy, factions, migration.

Exit criteria:
Settlements emerge without explicit player placement.

## Phase 7 — Large World and Simulation LOD

Status: COMPLETED

Delivered:
- explicit Full / Reduced / Aggregate / Macro tiers;
- sparse `ChunkSimulationState` independent of Godot nodes;
- wrap-aware classification from simulation cursor + protected characters;
- aggregation without deleting people, buildings or settlements;
- deterministic reconstruction of the same IDs;
- bulk aggregate/macro time steps for food/aging;
- player protection and failed commands against aggregated targets;
- `MigrationGroup` seam (no migration gameplay);
- debug overlay L, refresh O, force 9/0.

Not done (later / open):
- streaming a huge world or proving 100k-character performance (only DebugSample was tested);
- compacting ordinary people out of the roster (OD-024);
- migration gameplay.

Exit criteria:
World can contain substantially more inhabitants than the fully detailed simulation area without frame rate collapsing.

## Phase 8 — Exploration

Status: COMPLETED

Delivered:
- world state ≠ player knowledge (`ExplorationSystem` + sparse `ExplorationKnowledgeDirectory`);
- monotonic Unknown → Rumored → Scouted → Mapped → Confirmed → Analyzed;
- chunk-level compositional facts (terrain / biome / climate);
- debug commands R/S/D/F/A and overlay Q (not expeditions);
- wrap-adjacent chunks remain independent records;
- independence from LOD, presentation and generated geography;
- save DTO seam (`ExplorationKnowledgeRecord`) not wired into envelope v2.

Not done (later / open):
- visibility radius, travel/scout gameplay, expeditions, explorer profession (OD-026, OD-029);
- tile-level knowledge, landmarks, rivers (OD-028);
- rumor provenance (OD-030);
- final fog/player map (OD-031);
- persisting knowledge in the save envelope.

Exit criteria:
Exploration produces persistent in-memory knowledge independent of the actual world. Meaningful strategic decisions from that knowledge remain later phases.

## Phase 9 — Factions and Cultures

Status: COMPLETED

Delivered:
- `CultureState` / `FactionState` as separate domain entities;
- `CultureId.Neutral` as a real Unaffiliated directory entry; generated cultures from seed;
- `CharacterState.Culture` and `CharacterState.Faction` membership;
- sparse symmetric Neutral/Friendly/Hostile relations;
- deterministic invented names and trait bytes;
- debug P/J/H; mapper seam not in save envelope v2.

Not done (later / open):
- player faction pick, clothing/architecture presentation, production modifiers (OD-038);
- language/naming content (OD-032);
- territory/home (OD-035);
- diplomacy gameplay (Phase 10);
- persisting civilizations (OD-036).

Exit criteria:
Cultures and factions exist as simulation state distinct from settlements and geography. Feeling different in play (not +10% modifiers) remains later content.

## Phase 10 — Diplomacy

Status: NEXT

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