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

Phase 7 tiers:

- Tier 0 Full — individual characters, needs, activity, production.
- Tier 1 Reduced — same entities; behavior ticks may be thinned.
- Tier 2 Aggregate — individuals retained but not ticked; bulk food/aging/production.
- Tier 3 Macro — same as aggregate with a coarser (day) step; trend events only.

Presentation load (`ChunkPresentationPresence`) is independent of these tiers.

Focus is the simulation cursor plus protected character chunks. Distance is wrap-aware Chebyshev in chunk space.

## 9. LOD invariant

When converting detailed → aggregate:
characters, buildings, settlements, family links, skills and resource totals remain. Individual AI stops.

When converting aggregate → detailed:
the same IDs resume individual simulation. No random NPC spawn.

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

Phase 8 stores player knowledge in `ExplorationKnowledgeDirectory`. Missing entries are Unknown. Knowledge is keyed by `ChunkCoordinate`, not by terrain cache or `ChunkId`.

Discovery state:
Unknown
→ Rumored
→ Scouted
→ Mapped
→ Confirmed
→ Analyzed

Rumored does not sample geography. Scouted and above may generate that one chunk on demand to copy facts; they do not rewrite elevation/biome. Wrap-adjacent chunks (X=0 and X=last) are separate knowledge records.

Tile-level fog, landmarks and the player map are not in Phase 8.

Factions and cultures are not geography. A faction does not own chunks where its members stand. Creating a faction does not reveal exploration knowledge.

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
