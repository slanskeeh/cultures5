# Development Log

This file is the chronological, persistent development report for the project.

Its purpose is to make the current implementation state understandable to the user and to another AI agent without relying on chat history.

Canonical copy also lives in `docs/DEVELOPMENT_LOG.md`.

## Status meanings

- **Implemented** — code/content has been created or changed.
- **Verified** — behavior was actually tested or directly inspected and confirmed.
- **Partial** — only part of the intended behavior works.
- **Not tested** — implemented but not verified yet.
- **Broken** — known not to work correctly.

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

---

## [2026-09-15] — Task: Phase 2 Minimal World Generation

### 1. Task
Implement Phase 2 from `docs/prompts/PHASE_2_PROMPT.md`: deterministic seed-based geography (macro → elevation → water → climate → biome), chunk-on-demand access, wrap continuity, debug coloring. No civilizations, resources, or gameplay.

### 2. Done
- `WorldGenerator` with cylindrical hash value-noise; generation version mixed into the noise seed.
- `TerrainCell` now carries elevation, water, climate, biome; occupancy remains a sparse overlay.
- `LogicalGrid` no longer allocates Width×Height at construction; chunks generate on first sample.
- `WorldConfiguration` gained generation version, sea level, polar band (provisional defaults).
- Save envelope v2 stores seed + generation version + world size metadata (not the heightmap).
- Debug map colors biomes/water/highlands; HUD shows elev/climate/biome.
- Tests for determinism, wrap, chunks, climate, water/sea-level, polar cold, land+water presence.
- AD-025..AD-029, OD-010, OD-011.

### 3. Working / Verified
- 78/78 domain tests pass (Phase 0 and Phase 1 included).
- Solution/Godot C# build: 0 warnings, 0 errors.
- Headless Godot loads Main (`--quit-after 45`, exit 0).
- Geographic plausibility of colors in a visible window was **not** inspected; domain tests confirm land+water, colder poles, and seam continuity vs a far cut.

### 4. Tests
- Command: `dotnet test tests/Cultures.Tests/Cultures.Tests.csproj`
- Result: PASS — 78 passed, 0 failed, 0 skipped
- `dotnet build Cultures.sln` — PASS, 0 warnings, 0 errors
- Godot 4.7.2.stable.mono headless `--quit-after 45` — PASS (exit 0)

### 5. Bugs found
- None in test/headless runtime. Duplicate test method names appeared during editing and were corrected before the green run.

### 6. Bugs fixed
- Symptom: two tests named `Horizontal_seam_is_more_continuous_than_a_far_cut` after a bad edit, one containing the cache-count assertions.
- Cause: search/replace overwrote the wrong method.
- Solution: restored `Constructor_does_not_pregenerate_the_whole_world`.
- Regression: that test plus the seam-continuity test both pass.

### 7. Known limitations / TODO
- Noise frequencies, sea level 0.42, polar band 0.14, biome thresholds are temporary (OD-011).
- Biome list is a placeholder (Ocean/Ice/Tundra/TemperateLand/Forest/Desert/Highland).
- World size remains DebugSample 100×50 (OD-001).
- Latitude: y=0 is the low-Y pole, not a declared geographic north (OD-010).
- No rivers/lakes/soil/resources.
- Occupancy still not in saves.
- Debug view is orthographic placeholder art.
- Visible Godot window / seam coloring was not interactively inspected.

### 8. Architecture decisions
- AD-025 on-demand chunk terrain
- AD-026 cylindrical value noise, no third-party lib
- AD-027 elevation → water → climate → biome
- AD-028 temporary latitude mapping
- AD-029 save generation contract, not cells

### 9. Files changed
- `src/Cultures.Domain/World/Generation/**`
- `src/Cultures.Domain/World/TerrainCell.cs`, `LogicalGrid.cs`, `LogicalWorld.cs`, `WorldConfiguration.cs`, `WorldTopology.cs`
- `src/Cultures.Domain/Application/SimulationHost.cs`, `Persistence/SaveEnvelope.cs`
- `presentation/WorldDebugMap.cs`, `Main.cs`
- `tests/Cultures.Tests/WorldGenerationTests.cs` and updated Phase 1/save tests
- `docs/DECISIONS.md`, `docs/MVP_ROADMAP.md`, both development logs

### 10. Current project health
- Build: working
- Tests: 78/78 passing
- Runtime: headless Main boots
- Known broken areas: none identified; visual geography not GUI-verified

### 11. Next step
Phase 3 — First Living Characters. Do not start automatically.

### 12. Notes for ChatGPT
- Review OD-010 (which pole is north) and OD-011 (keep or replace the noise/biome constants).
- Dense Phase 1 grid was replaced by chunk cache; occupancy is sparse. Confirm this is the intended step toward Phase 7 streaming.
- Save version jumped 1 → 2; no migration of old envelopes.
- Debug world is still 100×50; the generator is wrap-aware but not yet stressed at “huge planet” size.


