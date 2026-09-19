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

Status: COMPLETED

Delivered:
- `DiplomacySystem` as the authority for Neutral / Friendly / Hostile;
- `SetDiplomaticStanceCommand` with validation, sparse storage, symmetry;
- `DiplomaticStanceChangedEvent` with no gameplay listeners;
- debug H uses the command path;
- no war, trade, territory, economy or AI effects.

Not done (later / open):
- treaties, tribute, access, diplomatic history (OD-039);
- war derived from Hostile (OD-040);
- AI diplomacy (OD-041);
- extra stances such as Alliance (OD-042);
- trade (later).

Exit criteria:
Two factions can hold an evolving diplomatic stance without scripted sequences. Autonomous civilization diplomacy remains later.

## Phase 11 — Internal Politics

Status: COMPLETED

Delivered:
- faction-local `PoliticalGroupState` with typed `PoliticalGroupId`;
- optional `CharacterState.PoliticalGroup` with cross-faction rejection;
- explicit 0–100 influence independent of member count;
- sparse 0–100 internal stability (missing = 50);
- commands and events without autonomous political AI;
- debug I/Y/W/1/2.

Not done (later / open):
- taxonomy, mandatory affiliation (OD-043, OD-044);
- influence formula (OD-045);
- leaders, offices, elections, succession (OD-047, OD-048);
- ideologies, rebellions, laws, taxation.

Exit criteria:
Internal political groups, influence and stability exist as simulation state. Political positions emerging from character/social state remain later.

## Phase 12 — Military

Status: COMPLETED

Delivered:
- `MilitarySystem` as a separate domain, not a field on `FactionState`;
- typed `MilitaryUnitId` and faction-owned `MilitaryUnitState`;
- optional `CharacterState.MilitaryUnit` with matching-faction validation;
- Active / Disbanded lifecycle; disband clears memberships;
- commands and events without combat, war, movement, or AI;
- one empty generated unit per faction; names from `FictionalName`;
- debug X cycle unit, Z enlist/leave, 3 disband.

Not done (later / open):
- army hierarchy, commanders, recruitment (OD-050, OD-051, OD-052);
- equipment, combat, morale (OD-053, OD-054, OD-055);
- war, movement, formations (OD-056, OD-057, OD-058);
- territory, supply, military professions.

Exit criteria:
Military identity and membership exist as simulation state. Combat outcomes feeding population, economy and diplomacy remain later.

## Phase 13 — History and Presentation Architecture

Status: COMPLETED

Delivered:
- `HistoryRecorder` / `HistoryDirectory` as a fact store over domain events;
- typed `HistoryEventId`; queries by character, settlement, faction, unit;
- no gameplay consequences from recording;
- presentation camera, ID selection, recreatable identity map;
- organized debug HUD; exploration overlay does not leak raw terrain;
- procedural debug textures for landscape, buildings, characters.

Not done (later / open):
- chronicle UI, family albums, audio, final art;
- history as AI memory or player knowledge.

Exit criteria:
History exists as simulation facts. Presentation binds to domain IDs. Views remain recreatable.

## Phase 14 — Full Persistence

Status: COMPLETED

Delivered:
- `SaveEnvelope` v3 with authoritative dynamic state;
- ID preservation and factory counter restore;
- static terrain from seed + generation contract;
- atomic restore; unsupported versions fail;
- v2 remains header-only contract spawn.

Not done (later / open):
- binary/compressed format;
- migrations beyond v2→v3 contract spawn;
- autosave UX.

Exit criteria:
A debug world survives save, load, and continued ticks with the same IDs.

## Phase 15 — Resources and Ecology

Status: COMPLETED

Delivered:
- generated fertility and rivers;
- persistent `ResourceDeposit` stocks (deplete/regenerate);
- biome-driven recipes via `ContextualRecipeTable`;
- chunk wildlife aggregates;
- wrap-aware ecology; natural state in v3 saves.

Not done (later / open):
- named resource sites, hunting as character actions, hydrology simulation;
- individual animals.

Exit criteria:
World resources exist independently of inventories. Production can depend on biome without forking building types.

## Phase 16 — Professions, Households, Family Life

Status: COMPLETED

Delivered:
- professions distinct from skills (`ProfessionCatalog`);
- households distinct from genealogy; shelter homes;
- partnership and household-aware birth;
- infant caregiver feeding;
- history facts for profession/household/partnership/home;
- new state in v3 saves.

Not done (later / open):
- fertility rates, marriage ceremony, household property;
- carrying infants; more professions.

Exit criteria:
Vocation, co-residence, and minimal infant care exist as simulation state without collapsing into skills or family trees.

## Phase 17 — Alpha

Status: COMPLETED

Delivered:
- `SimulationBalance` tunables;
- clock speed 1–8x, saved with the world;
- per-tick `Step` with diagnostics and last fault;
- slot save/load (`ISaveStore`, F5/F9) and optional autosave;
- history chronicle strings for HUD;
- F1 help / first-run copy.

Not done (later / open):
- final balance numbers;
- compressed saves;
- GUI-verified HUD.

Exit criteria:
A headless debug world can be saved, loaded, sped up, and continued with diagnostics.

## Phase 18 — Beta / Content Expansion

Status: COMPLETED

Delivered:
- biomes Swamp, Savanna, Taiga as classifier remaps (generation version unchanged);
- hunt command on wildlife aggregates;
- Hunting Camp / Fishery buildings; Hunter / Fisher professions;
- explicit diplomatic pacts (trade / non-aggression / alliance) without war;
- season-changed history facts.

Not done (later / open):
- individual animals, hydrology, war, fertility rates;
- more factions than the existing seed of 3.

Exit criteria:
Content expands existing systems. Hostile is still not war.

## Phase 19 — Release Candidate

Status: COMPLETED

Delivered:
- save envelope v4 with v3 migration;
- atomic file slots;
- accessibility: high-contrast map, HUD font cycle;
- onboarding help text;
- Windows Desktop export preset;
- fault string on the host.

Not done (later / open):
- shipping an exported binary from this session;
- crash reporter service;
- accessibility beyond contrast/font;
- onboarding tutorial beyond F1 copy.

Exit criteria:
Playtest build can be exported and a v3 save still loads. GUI keys were not clicked in an interactive window.

## Golden rule

A phase is complete when its behavior is verified, not when code merely exists.

The numbered foundation and playtest roadmap ends at Phase 19. Further work is content, balance, war, and polish under existing open ODs — not a Phase 20 unless a new prompt says so.