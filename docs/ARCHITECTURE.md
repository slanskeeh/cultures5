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
- population;
- economy;
- settlements;
- civilizations;
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

## 4. Stable IDs

Use typed IDs conceptually:

CharacterId
FamilyId
BuildingId
SettlementId
CultureId
FactionId
PoliticalGroupId
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

Godot's TileSet/TileMapLayer systems support isometric tile shapes, but the project's authoritative logical grid remains our own domain model. Godot's tile tools are therefore a presentation/authoring aid, not the simulation source of truth. citeturn0search6

## 10. Threading

No custom multithreading in Phase 0.

Later, independent expensive tasks may use worker threads. Godot exposes WorkerThreadPool for this purpose. citeturn0search7

The simulation API must be designed so that future parallel execution is possible without requiring it now.
