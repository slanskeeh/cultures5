# Cultures Successor — Technical Foundation v0.2

This package refines the initial Bible into a concrete technical foundation for Cursor.

## Read order
1. GAME_DESIGN_BIBLE.md
2. TECHNICAL_BIBLE.md
3. ARCHITECTURE.md
4. WORLD_ARCHITECTURE.md
5. SIMULATION_ARCHITECTURE.md
6. CURSOR_RULES.md
7. PHASE_0_PROMPT.md

## Current technical decision
Godot 4.x + C#/.NET is the current implementation direction. Godot provides a dedicated .NET editor/runtime path for C# projects; current documentation notes that C# projects require the .NET-enabled Godot editor and .NET SDK. Desktop export is supported, while web export is not currently supported for Godot 4 C# projects. citeturn0search0turn0search3

The game will use:
- pixel-art presentation;
- isometric/pseudo-isometric view;
- logical grid independent from rendering;
- simulation-first architecture;
- deterministic procedural generation;
- chunks and simulation LOD;
- command/event communication;
- versioned saves;
- data-driven content.

Do not implement gameplay before Phase 0 passes.
