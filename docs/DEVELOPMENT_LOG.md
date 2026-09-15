# Development Log

This file is the chronological, persistent development report for the project.

Its purpose is to make the current implementation state understandable to the user and to another AI agent without relying on chat history.

## Status meanings

- **Implemented** — code/content has been created or changed.
- **Verified** — behavior was actually tested or directly inspected and confirmed.
- **Partial** — only part of the intended behavior works.
- **Not tested** — implemented but not verified yet.
- **Broken** — known not to work correctly.

---

## Entry template

### [YYYY-MM-DD] — Task: <short name>

**Task**
- <what was requested>

**Done**
- <concrete implementation changes>

**Working / verified**
- <what was actually confirmed>

**Tests**
- `<command or test>` — PASS / FAIL / NOT RUN

**Bugs found**
- <bug, or "None">

**Bugs fixed**
- <bug + cause/fix, or "None">

**Known limitations / TODO**
- <remaining work>

**Architecture decisions**
- <new/changed decisions, or "None">

**Next step**
- <recommended next task>

---

### [2026-09-15] — Task: Phase 0 technical foundation

**Task**
- Implement only the technical foundation from `docs/PHASE_0_PROMPT.md`: Godot 4.x .NET project, domain structure, stable IDs, simulation clock, deterministic random, events, commands, versioned save envelope, headless host, deterministic tests. No gameplay.

**Done**
- Installed missing toolchain: .NET SDK 8.0.425 and Godot 4.7.2 Mono (`GodotEngine.GodotEngine.Mono`).
- Created Godot 4.7 C# project (`project.godot`, `Cultures.csproj`, `Cultures.sln`).
- Added domain assembly `src/Cultures.Domain` with folders Core / World / Population / Economy / Settlement / Civilization / Exploration / History / Application.
- Typed IDs: `CharacterId`, `FamilyId`, `BuildingId`, `SettlementId`, `CivilizationId`, `RegionId`, `ChunkId` plus `EntityIdFactory`.
- `SimulationClock` + `SimulationCalendar` (tick → minute → hour → day → season → year), pause/resume, explicit `Advance`.
- `SeededRandom` (PCG-XSH-RR), no domain use of global RNG.
- `EventBus` + immutable `TickAdvancedEvent`.
- `CommandProcessor` + trivial `PingCommand`.
- Versioned `SaveEnvelope` (`saveVersion`, `worldSeed`, `simulationTick`) and JSON serializer.
- Headless `SimulationHost` (no Godot types).
- Godot shell `presentation/Main.tscn` + `Main.cs`: debug label, Space pause, fixed-timestep tick requests.
- xUnit tests in `tests/Cultures.Tests`.
- Ignored Godot scans of `src/`, `tests/`, `docs/` via `.gdignore`.

**Working / verified**
- Solution and Godot C# project build with 0 warnings / 0 errors.
- 25/25 domain tests pass.
- Headless Godot loads `Main.tscn` and runs frames (`--quit-after 20`, exit 0). Interactive window (Space pause) was not clicked in a GUI session.

**Tests**
- `dotnet test tests/Cultures.Tests/Cultures.Tests.csproj` — PASS (25 passed, 0 failed)
- `dotnet build Cultures.sln` — PASS
- `dotnet build Cultures.csproj` — PASS
- Godot 4.7.2.stable.mono headless `--quit-after 20` — PASS (exit 0)

**Bugs found**
- Godot scanned domain/test `.cs` files and generated stray `.uid` files.
- Putting a guessed UID on `Main.tscn` produced `invalid UID` warning.

**Bugs fixed**
- Added `.gdignore` under `src/`, `tests/`, `docs/` and deleted stray UIDs. Cause: Godot imports every file under `res://` unless ignored.
- Reverted `Main.tscn` to path-based script reference. Cause: UID was not in Godot's resource cache yet.

**Known limitations / TODO**
- Requested docs are missing: `GAME_DESIGN_BIBLE.md`, `TECHNICAL_BIBLE.md`, `DECISIONS.md`, `MVP_ROADMAP.md`. Phase 0 followed existing `ARCHITECTURE.md` / `SIMULATION_ARCHITECTURE.md` / `WORLD_ARCHITECTURE.md` / `CURSOR_RULES.md`.
- World / Population / Economy / Settlement / Civilization / Exploration / History are empty placeholders.
- Save envelope is foundation-only (no entity state).
- No complete save system, no world generation, no characters/buildings/UI beyond debug boot.
- Godot editor first-open may rewrite `project.godot` and add `.import` / `.uid` files.
- Debug screen pause via Space is implemented but not interactively verified in a visible window.

**Architecture decisions**
- Domain (`Cultures.Domain`) is a plain `net8.0` library with no Godot dependency. Godot project references it and only contains the presentation shell.
- Clock never reads FPS; the Node converts real time into integer `Step` calls at a fixed 0.1s policy.
- Random uses PCG32 rather than `System.Random` so sequences stay stable across .NET runtime changes.
- No GameManager singleton; `SimulationHost` is constructed by the shell (and by tests).
- Development log is written both at repo root (`DEVELOPMENT_LOG.md`, required by `CURSOR_RULES.md`) and in `docs/DEVELOPMENT_LOG.md` (existing template location).

**Next step**
- Do not start Phase 1 automatically. Recommended next: restore or write the missing design docs (`DECISIONS.md` / `MVP_ROADMAP.md`), then Phase 1 logical world coordinates + wrap-aware grid (no procedural generation yet).

---

## [2026-09-15] — Task: Phase 1 Logical World Foundation

### 1. Task
Implement Phase 1 from `docs/prompts/PHASE_1_PROMPT.md`: authoritative logical world/grid with horizontal wrap, chunk conversion, occupancy foundation, and a minimal debug visualization. No procedural generation or gameplay.

### 2. Done
- Domain world layer: `WorldConfiguration`, coordinate types, `WorldTopology` (Euclidean wrap), `ChunkLayout`, `LogicalGrid`, `TerrainCell`/`Occupancy`, `LogicalWorld`, `RenderProjection`.
- Debug cursor via `MoveDebugCursorCommand` / `SetOccupancyCommand` and `DebugCursorMovedEvent`.
- `SimulationHost` now owns `LogicalWorld` + `SimulationCursor` (optional world config, default `DebugSample` 100×50 / 10×10).
- Godot debug map `presentation/WorldDebugMap.cs` plus updated `Main` HUD and arrow/G/Space controls.
- Tests covering wrap, bounds, chunks, roundtrip, distance, occupancy, cursor seam, configuration.
- Decisions AD-020..AD-024; roadmap Phase 1 marked completed.

### 3. Working / Verified
- 64/64 domain tests pass, including previous Phase 0 tests.
- Solution and Godot C# project build with 0 warnings / 0 errors.
- Headless Godot loads the Phase 1 main scene (`--quit-after 45`, exit 0, no script errors).
- Cursor wrap/pole rejection verified by automated command tests, not by clicking a visible window.

### 4. Tests
- Command: `dotnet test tests/Cultures.Tests/Cultures.Tests.csproj`
- Result: PASS — 64 passed, 0 failed, 0 skipped
- `dotnet build Cultures.sln` — PASS, 0 warnings, 0 errors
- Godot 4.7.2.stable.mono headless `--quit-after 45` — PASS (exit 0)

### 5. Bugs found
- None in test or headless runtime. The C# `%` remainder trap for negative X was treated as a known language pitfall and covered by wrap tests (`-1 → 99`, `-101 → 99`).

### 6. Bugs fixed
- None required after implementation. Wrap uses `WorldTopology.EuclideanMod` with regression tests listed above.

### 7. Known limitations / TODO
- Dense in-memory grid; not a chunk database or streaming system (Phase 7).
- `TerrainKind` is `Unspecified` only; no biomes/resources.
- Occupancy is `OccupantKind` (None/DebugMarker), not entity IDs.
- Save envelope still does not persist grid/cursor.
- Debug map is orthographic; isometric numbers are HUD-only.
- Arrow-key movement in a visible Godot window was not interactively clicked this session.
- Production world size and tile geometry remain OD-001 / OD-002.

### 8. Architecture decisions
- AD-020 Euclidean wrap only in `WorldTopology`.
- AD-021 Configurable size; width/height divisible by chunk size.
- AD-022 Distinct coordinate types; Phase 1 world↔grid is 1:1 after validation.
- AD-023 Occupancy is logical-grid-authoritative.
- AD-024 `ChunkCoordinate` is spatial; `ChunkId` stays persistent identity.

### 9. Files changed
- `src/Cultures.Domain/World/**`
- `src/Cultures.Domain/Application/SimulationHost.cs`
- `presentation/Main.cs`, `presentation/Main.tscn`, `presentation/WorldDebugMap.cs`
- `tests/Cultures.Tests/*` (new world/grid/cursor tests)
- `docs/DECISIONS.md`, `docs/MVP_ROADMAP.md`, `DEVELOPMENT_LOG.md`, `docs/DEVELOPMENT_LOG.md`

### 10. Current project health
- Build: working
- Tests: 64/64 passing
- Runtime: headless Main scene boots
- Known broken areas: none identified; GUI cursor walk not interactively verified

### 11. Next step
Phase 2 — Minimal World Generation (deterministic seed → elevation/water/biome placeholders, wrapping generation). Do not start automatically.

### 12. Notes for ChatGPT
- Requiring world size divisible by chunk size is a reversible Phase 1 contract; review if partial edge chunks are desired.
- Dense grid will not scale to the intended huge world; replace with chunk-backed storage before Phase 7.
- Screen-up currently decreases Y; which pole is north is not assigned.
- Integer isometric mapping truncates odd tile sizes (`tileWidth / 2`).

