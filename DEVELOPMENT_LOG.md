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
