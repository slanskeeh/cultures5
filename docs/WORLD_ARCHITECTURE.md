# WORLD ARCHITECTURE v0.2

## 1. World hierarchy

World
→ Regions
→ Chunks
→ Logical cells / navigation points

A Region is a simulation-scale geographic area.

A Chunk is a streaming/storage unit.

A cell/node is the fine-grained terrain/navigation representation.

## 2. Coordinate spaces

Keep these separate:

WorldCoordinate
ChunkCoordinate
LogicalGridCoordinate
IsometricRenderCoordinate
ScreenCoordinate

Conversion functions must be explicit.

The renderer must never decide where an entity exists in the simulation.

## 3. Horizontal wrap

The world has a finite horizontal circumference.

Logical X is normalized into [0, WorldWidth).

Crossing:
WorldWidth - 1 → 0
0 → WorldWidth - 1

All distance/pathfinding helpers must be wrap-aware.

North/south do not wrap. They terminate in polar regions.

## 4. Polar regions

North and south are generated as extreme climate regions, not invisible walls.

Possible future content:
- glaciers;
- ice fields;
- polar seas;
- unique wildlife;
- rare resources;
- extreme weather.

## 5. Generation pipeline

WorldSeed
→ macro geography
→ continents/oceans
→ elevation
→ mountains
→ climate
→ rivers/lakes
→ biome
→ soil/fertility
→ resources
→ wildlife
→ landmarks
→ civilizations
→ settlements
→ prehistory

The same seed + same generation version must produce the same initial world.

## 6. Geography causality

Geography should influence:

mountains
→ rivers
→ fertility
→ vegetation
→ wildlife
→ resources
→ settlement viability
→ trade routes

Do not generate each layer independently.

## 7. Chunks

A chunk contains:
- terrain;
- local resource information;
- navigation data;
- wildlife summary;
- local entity references;
- modifications.

Chunks may be loaded for:
- rendering;
- detailed simulation;
- editing/debugging.

A chunk may remain in simulation storage without an active scene.

## 8. Simulation LOD

Tier 0:
full nearby simulation.

Tier 1:
simplified local simulation.

Tier 2:
macro population/economy simulation.

Tier 3:
strategic trends and major events.

Every major system must define what information is retained at lower LOD.

## 9. LOD invariant

When converting detailed → aggregate:
important totals and relationships must be preserved.

When converting aggregate → detailed:
the result must be plausible and derived from stored state.

Do not generate random NPCs merely because a region became visible.

## 10. Navigation

Navigation is based on logical world data.

It must support:
- terrain cost;
- blocked cells;
- buildings;
- roads;
- water;
- elevation;
- future transport types.

The first prototype can use simple grid pathfinding.

## 11. Discovery

Knowledge is separate from geography.

The world can contain a resource without the player knowing it exists.

Discovery state examples:
Unknown
→ Rumored
→ Scouted
→ Mapped
→ Confirmed
→ Analyzed

## 12. Rendering

The desired visual direction is:
- pixel art;
- simple textures;
- top-down angled/isometric;
- readable silhouettes;
- limited visual noise.

Large visual assets can occupy multiple logical cells.

## 13. Building placement

Building placement uses logical coordinates and footprints.

A building definition contains:
- footprint;
- anchor point;
- allowed terrain;
- construction requirements;
- supported production rules.

The renderer calculates the visual position from the logical placement.
