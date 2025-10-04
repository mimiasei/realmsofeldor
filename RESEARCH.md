# Realms of Eldor - Research Findings

## Phase 3: Map System Research (2025-10-03)

### VCMI Map System Architecture

Based on analysis of VCMI source code in `/tmp/vcmi-temp/lib/mapping/`:

#### Key Files Analyzed:
- `CMap.h` - Main map class
- `TerrainTile.h` - Individual tile structure
- `CGObjectInstance.h` - Base class for map objects

#### Core Patterns:

**1. Map Storage (CMap.h:286)**
```cpp
boost::multi_array<TerrainTile, 3> terrain;
```
- 3D array indexed as `[z][x][y]` where z is map level (0=surface, 1=underground)
- Bounds checking: `isInTheMap(pos)` validates x < width, y < height, z < levels
- Direct tile access: `getTile(int3 pos)` returns reference to TerrainTile

**2. TerrainTile Structure (TerrainTile.h:26-97)**
Core properties:
- `TerrainId terrainType` - Grass, dirt, water, snow, etc.
- `RiverId riverType` - Optional river overlay
- `RoadId roadType` - Optional road overlay
- `ui8 terView` - Visual variant (for texture variety)
- `ui8 extTileFlags` - Bit flags (coastal, favorable winds, rotation)
- `std::vector<ObjectInstanceID> visitableObjects` - Objects that can be interacted with
- `std::vector<ObjectInstanceID> blockingObjects` - Objects that block movement

Key methods:
- `entrableTerrain()` - Checks if tile is passable (not rock)
- `isClear()` - Checks for blocking objects
- `topVisitableObj()` - Returns top visitable object ID
- `isWater()` / `isLand()` - Terrain type checks
- `blocked()` / `visitable()` - Object presence checks

**3. Map Objects (CGObjectInstance.h)**
Base properties:
- `MapObjectID ID` - Type (town, hero, creature, mine, etc.)
- `MapObjectSubID subID` - Variant within type
- `ObjectInstanceID id` - Unique instance identifier
- `int3 pos` - Bottom-right corner position
- `PlayerColor tempOwner` - Current owner
- `bool blockVisit` - Can only visit from adjacent tiles
- `bool removable` - Can be removed from map

Key methods:
- `visitableAt(pos)` - Check if visitable at position
- `blockingAt(pos)` - Check if blocking at position
- `getBlockedPos()` - Set of all blocked positions
- `isVisitable()` - Returns true if object can be interacted with

**4. Object Management (CMap.h:82)**
```cpp
std::vector<std::shared_ptr<CGObjectInstance>> objects;
```
- Central list of all objects on map
- Position in vector = instance ID
- Separate tracking for heroes (`heroesOnMap`) and towns (`towns`)
- Objects reference tiles, tiles reference objects (bidirectional)

### Unity Translation Strategy

#### 1. GameMap (Pure C#)
```csharp
public class GameMap
{
    private MapTile[,] tiles;  // 2D array [x, y] (no underground for MVP)
    private List<MapObject> objects;
    private int width, height;

    public MapTile GetTile(Position pos);
    public bool IsInBounds(Position pos);
    public bool CanMoveBetween(Position from, Position to);
    public MapObject GetObjectAt(Position pos);
}
```

#### 2. MapTile (Struct)
```csharp
public struct MapTile
{
    public TerrainType Terrain;
    public int MovementCost;
    public bool IsPassable;
    public List<int> VisitableObjectIds;
    public List<int> BlockingObjectIds;
}
```

#### 3. MapObject (Abstract Base)
```csharp
public abstract class MapObject
{
    public int InstanceId;
    public MapObjectType ObjectType;
    public Position Position;
    public PlayerColor Owner;
    public bool BlocksMovement;
    public bool IsVisitable;

    public abstract HashSet<Position> GetBlockedPositions();
    public abstract void OnVisit(Hero hero);
}
```

#### 4. MapRenderer (MonoBehaviour)
- Uses Unity Tilemap for terrain rendering
- Instantiates prefabs for map objects
- Updates visual state when map changes
- Handles tile highlighting for movement range

#### 5. Separation of Concerns
- **Core.Map**: Pure C# logic (GameMap, MapTile, pathfinding)
- **Controllers**: MonoBehaviour wrappers (MapRenderer, object controllers)
- **Data**: ScriptableObjects (terrain definitions, object prefabs)

### Implementation Notes

1. **Simplified for MVP**: Start with 2D single-level maps (no underground)
2. **Object Storage**: Use Dictionary<int, MapObject> for fast lookup by ID
3. **Passability**: Store in MapTile for quick pathfinding queries
4. **Unity Integration**: MapRenderer subscribes to map events, updates visuals only
5. **Pathfinding**: Prepare integration point for A* Pathfinding Project

---

*Research complete. Ready for implementation.*
