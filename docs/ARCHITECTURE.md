# ARCHITECTURE v0.2

## 1. The core idea

The game has two worlds:

1. The **simulation world** — authoritative.
2. The **presentation world** — what the player sees.

The simulation must remain meaningful when no visual object exists.

Example:

Character 1842 can exist in the simulation while the player is on another continent. A CharacterView is created only when the relevant area is presented in detail.

## 2. Layers

Application
- receives player intent;
- schedules commands;
- coordinates loading/saving.

Simulation
- world;
- ecology;
- population;
- economy;
- settlements;
- civilizations;
- diplomacy;
- internal politics;
- military;
- social life;
- history.

Domain
- entities;
- value objects;
- rules;
- events;
- commands.

Infrastructure
- serialization;
- file access;
- deterministic random;
- logging.

Presentation
- Godot scenes;
- sprites;
- animation;
- camera;
- UI;
- audio;
- effects.

Dependency direction:

Presentation → Application → Simulation/Domain
Infrastructure → Domain contracts

Domain must never depend on Presentation.

## 3. Entity vs View

Entity:
- persistent identity;
- authoritative state;
- serializable.

View:
- sprite;
- animation;
- selection;
- visual effects;
- interpolation.

Never put authoritative hunger, inventory, family, profession or history only in a Node.

Presentation selection and camera live in Application (`PresentationCamera`, `PresentationSelection`). The play camera is a continuous isometric point (`IsoX`/`IsoY`); Godot pans it with middle mouse and screen-edge scroll and must not snap it to hexes. Godot draws from domain IDs. The debug map uses procedural 64×64 2.5D hex textures for biomes, buildings and life stages; this is not final art. Exploration overlay must not leak raw unexplored terrain facts.

## 4. Stable IDs

Use typed IDs conceptually:

CharacterId
FamilyId
HouseholdId
BuildingId
SettlementId
CultureId
FactionId
PoliticalGroupId
MilitaryUnitId
HistoryEventId
ResourceDepositId
CivilizationId
RegionId
ChunkId

A persistent ID survives:
- unloading;
- saving;
- loading;
- LOD transitions.

Array indexes are never IDs.

## 5. Commands

Commands are requests.

Examples:
- MoveCharacterCommand
- BuildBuildingCommand
- AssignProfessionCommand
- SetDiplomaticStanceCommand
- CreateMilitaryUnitCommand
- AssignCharacterToMilitaryUnitCommand
- TradeCommand
- OfferTreatyCommand

Commands can fail.

The command layer validates intent and invokes domain/application logic.

## 6. Events

Events are facts.

Examples:
- CharacterBornEvent
- CharacterDiedEvent
- CharacterMarriedEvent
- ProfessionChangedEvent
- SkillImprovedEvent
- BuildingCompletedEvent
- ResourceProducedEvent
- SettlementFoundedEvent

Events can feed:
- history;
- UI notifications;
- economy;
- politics;
- achievements;
- diplomacy.

## 7. Systems

A system owns one coherent responsibility.

Good:
CharacterNeedsSystem
ProductionSystem
NaturalResourceSystem
HistoryRecorder
SocialLifeSystem
MigrationSystem

Bad:
MegaGameSystem containing all game logic.

## 8. Data definitions

Static content is data.

Examples:
BuildingDefinition
ProfessionDefinition
BiomeDefinition
ResourceDefinition
FactionDefinition
CultureDefinition

Runtime state references definitions by stable ID.

## 9. Godot usage

Godot is responsible for:
- application lifecycle;
- input;
- rendering;
- scenes;
- audio;
- UI;
- asset loading;
- presentation.

Debug presentation currently paints procedural 2.5D hex `ImageTexture` tiles (`SimpleTextures`) from a 60° isometric projection. The play camera pans freely (middle mouse, screen edges) in isometric space and does not snap to hexes. Click a person to select (dashed outline + inspector/preview). Click ground to order a walk (`OrderMoveCommand`). F2/F3 starting jobs, F4/F6 labor, F7 constructing huts, -/= play speeds 1–3 (25/40/60% of 1×), F5/F9 save, F1 help, F11 high contrast, and F12 HUD size are presentation. Domain labor lives in `LaborSystem`.

Godot does not own the hex grid. The authoritative cell is `LogicalGridCoordinate` (AD-123). Buildings occupy hex-connected polyominoes (AD-126). The playable map is `WorldConfiguration.Playtest`; tests use `DebugSample`. Godot's tile tools are a presentation/authoring aid, not the simulation source of truth.

## 10. Threading

No custom multithreading in Phase 0.

Later, independent expensive tasks may use worker threads. Godot exposes WorkerThreadPool for this purpose. citeturn0search7

The simulation API must be designed so that future parallel execution is possible without requiring it now.
