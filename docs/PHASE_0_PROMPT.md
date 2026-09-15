# CURSOR — PHASE 0 IMPLEMENTATION PROMPT

You are starting a new game project.

Read these files before doing anything:
- GAME_DESIGN_BIBLE.md
- TECHNICAL_BIBLE.md
- ARCHITECTURE.md
- WORLD_ARCHITECTURE.md
- SIMULATION_ARCHITECTURE.md
- CURSOR_RULES.md
- DECISIONS.md
- MVP_ROADMAP.md

## Objective

Implement ONLY the technical foundation.

Do not implement:
- characters walking;
- buildings;
- economy;
- combat;
- diplomacy;
- procedural world generation;
- UI beyond a minimal boot/debug screen.

## Required Phase 0 deliverables

### 1. Godot project
Create the Godot 4.x .NET project.

Use the .NET-enabled Godot editor and a supported .NET SDK. Current Godot documentation states that C# projects require the .NET-enabled editor and .NET SDK. citeturn0search3

### 2. Project structure

Create a clean structure for:
- Core
- World
- Population
- Economy
- Settlement
- Civilization
- Exploration
- History
- Presentation
- Application
- Tests
- Data
- Docs

### 3. Stable IDs

Create typed ID abstractions for:
- Character
- Family
- Building
- Settlement
- Civilization
- Region
- Chunk

They must:
- be serializable;
- support equality;
- not depend on array index.

### 4. Simulation clock

Implement:
- simulation time;
- tick;
- pause;
- controlled advancement.

The clock must not depend on rendering FPS.

### 5. Deterministic random

Create a random abstraction that accepts a seed.

Do not use uncontrolled global random state in domain code.

### 6. Events

Create:
- base event abstraction;
- event dispatcher/bus;
- immutable example event.

Include at least one test showing that two subscribers can react independently.

### 7. Commands

Create:
- command abstraction;
- command execution/result;
- one trivial test command.

### 8. Save/load foundation

Create a versioned save envelope containing at least:
- save version;
- world seed;
- simulation time.

Implement serialize → deserialize → compare test.

Do not build the complete save system yet.

### 9. Headless-friendly simulation

Create a minimal simulation host that can advance time without requiring a visible scene.

This is important: the domain must be testable without rendering.

### 10. Tests

Add deterministic tests for:
- IDs;
- clock;
- random;
- event bus;
- command execution;
- save roundtrip.

## Architecture requirements

Do NOT:
- put domain logic in Node scripts;
- make GameManager a god object;
- use static mutable state as a shortcut;
- couple tests to rendering;
- create unnecessary singletons.

## Godot usage

Godot should currently provide only the application shell.

A minimal Main scene may:
- initialize the simulation;
- display a simple debug label;
- advance/pause the clock.

No gameplay.

## Completion criteria

Phase 0 is complete only when:

1. project builds;
2. tests pass;
3. simulation clock works;
4. deterministic random test passes;
5. event bus test passes;
6. command test passes;
7. save roundtrip test passes;
8. simulation can advance without a visual character/world;
9. architecture matches the documents.

## Before coding

Inspect the repository and installed toolchain.

If Godot/.NET is missing, do not invent fake project files and claim the project runs. Clearly report what is missing.

## After coding

Return:
1. exact files created/changed;
2. architecture summary;
3. commands used to build/test;
4. exact test results;
5. known limitations;
6. next recommended task.

Do not start Phase 1 automatically.
