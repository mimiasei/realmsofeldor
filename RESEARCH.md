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

## Phase 4: Adventure Map UI Research (2025-10-04)

### VCMI Adventure Map UI Architecture

Based on analysis of VCMI source code in `/tmp/vcmi-temp/client/adventureMap/`:

#### Key Files Analyzed:
- `AdventureMapInterface.h/cpp` - Main controller coordinating all systems
- `CInfoBar.h/cpp` - Hero/town selection panel with state machine
- `CResDataBar.h/cpp` - Resource display bar with date
- `AdventureMapShortcuts.h/cpp` - Keyboard shortcut system

#### Core UI Components:

**1. CResDataBar - Resource Display**
- Shows all 7 resources: gold, wood, ore, mercury, sulfur, crystal, gems
- Displays current date: Month X, Week Y, Day Z
- Fixed positions for resource text overlays on background image
- Right-click popup for detailed resource breakdown
- Updates via direct text rendering in showAll()

Unity Translation:
```csharp
public class ResourceBarUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI[] resourceTexts;  // 7 resources
    [SerializeField] private TextMeshProUGUI dateText;
    [SerializeField] private GameEventChannel gameEvents;

    void OnEnable() => gameEvents.OnResourceChanged += UpdateResourceDisplay;
    void UpdateResourceDisplay(PlayerColor player, ResourceType type, int amount);
}
```

**2. CInfoBar - Context-Sensitive Info Panel**
Fixed size: 192x192 pixels

States (EState enum):
- EMPTY - No selection
- HERO - Selected hero (portrait, stats, artifacts)
- TOWN - Selected town (icon, buildings, garrison)
- DATE - Day/week transition animation
- GAME - Game status overview
- AITURN - Enemy turn indicator (hourglass)
- COMPONENT - Pickup notifications (timed, queued)

Key features:
- Polymorphic CVisibleInfo subclasses for each state
- Timer system for auto-dismissing components (3s default)
- Queue for multiple pickups
- Click to interact (open hero/town screen)

Unity Translation:
```csharp
public class InfoBarUI : MonoBehaviour
{
    public enum InfoBarState { Empty, Hero, Town, Date, EnemyTurn, Pickup }

    [SerializeField] private GameObject heroPanelPrefab;
    [SerializeField] private GameObject townPanelPrefab;
    [SerializeField] private GameObject datePanelPrefab;
    private Queue<PickupInfo> pickupQueue;

    public void ShowHeroInfo(Hero hero);
    public void ShowTownInfo(Town town);
    public void ShowDateAnimation();
    public async UniTask PushPickup(Component comp, string message, float duration);
}
```

**3. AdventureMapShortcuts - Keyboard Controls**
Centralized shortcut system with state-based availability:

Key shortcuts:
- Space - End turn
- E - Sleep/wake hero
- H - Next hero
- T - Next town
- V - Visit object
- M - Marketplace
- D - Dig for grail
- S - Spellbook
- Arrow keys - Direct hero movement (8-directional)
- Ctrl+S/L - Save/load

Availability conditions:
```cpp
bool optionHeroSelected();    // Hero must be selected
bool optionHeroCanMove();     // Hero has movement points
bool optionCanEndTurn();      // All heroes moved or asleep
bool optionCanVisitObject();  // Hero adjacent to visitable
```

Unity Translation:
```csharp
public class AdventureMapInput : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && CanEndTurn())
            EndTurn();
        if (Input.GetKeyDown(KeyCode.H) && HasHeroes())
            SelectNextHero();
        // ... etc
    }

    private bool CanEndTurn() => gameState.AllHeroesMoved();
    private bool HasHeroes() => gameState.GetCurrentPlayer().Heroes.Count > 0;
}
```

**4. AdventureMapInterface - Main Controller**
Central hub coordinating all systems:

Responsibilities:
- Input routing (clicks, hovers, shortcuts)
- State management (normal, spell casting, world view)
- Turn management (player/AI/hotseat coordination)
- Object interaction (hero-object encounters)
- Camera control (center on hero/town/events)
- UI updates (triggers refreshes on state changes)

Key methods:
```cpp
void onTileLeftClicked(const int3 & targetPosition);
void onTileRightClicked(const int3 & mapPos);
void onSelectionChanged(const CArmedInstance *sel);
void onHeroMovementStarted(const CGHeroInstance * hero);
void centerOnTile(int3 on);
void enterCastingMode(const CSpell * sp);
```

Unity Translation:
```csharp
public class AdventureMapController : MonoBehaviour
{
    public void OnTileClicked(Position pos);
    public void OnHeroSelected(Hero hero);
    public void OnEndTurnClicked();
    private void MoveSelectedHero(Position target);
    private bool ValidateMovement(Hero hero, Position target);
}
```

### UI Layout (HOMM3 Pattern)
```
┌────────────────────────────────────────────────┐
│  [Gold][Wood][Ore]... [Month: X, Week: Y]     │
├────────────────────────────────────────────────┤
│                                                │
│         Main Map View (Scrollable)             │
│                                                │
├──────────┬─────────────────────────────────────┤
│ InfoBar  │  Buttons: Sleep | Spellbook | Hero │
│ 192x192  │           EndTurn | Menu            │
└──────────┴─────────────────────────────────────┘
```

### Event-Driven Architecture

VCMI Pattern:
```cpp
// Logic broadcasts
adventureInt->onHeroChanged(hero);

// UI listens
void AdventureMapInterface::onHeroChanged(const CGHeroInstance * hero)
{
    infoBar->showHeroSelection(hero);
    heroList->updateHero(hero);
}
```

Unity Translation:
```csharp
// GameStateManager broadcasts
gameEventChannel.RaiseHeroMoved(hero, oldPos, newPos);

// UI subscribes
void OnEnable() => gameEvents.OnHeroMoved += HandleHeroMoved;
void HandleHeroMoved(Hero hero, Position oldPos, Position newPos) { UpdateUI(); }
```

### Input Handling Flow

**Click on Map Tile**:
1. MapRenderer detects click → converts screen to map position
2. Raises OnTileClicked event (MapEventChannel)
3. AdventureMapController receives event
4. Validates: Is tile visible? Hero selected? Tile reachable?
5. If valid: Calculate path → Send move command
6. GameStateManager updates hero position → Raises OnHeroMoved
7. HeroController animates movement (DOTween)
8. MapRenderer updates tile states
9. InfoBarUI updates hero stats (movement points)

Movement validation:
```csharp
bool CanMoveHeroToTile(Hero hero, Position target)
{
    if (!gameMap.IsInBounds(target)) return false;
    if (hero.MovementPoints <= 0) return false;
    if (!gameMap.GetTile(target).IsPassable()) return false;

    var path = pathfinder.FindPath(hero.Position, target);
    return path != null && CalculatePathCost(path) <= hero.MovementPoints;
}
```

### Key Takeaways for Unity

1. **Separation**: Core logic (C#) ↔ UI (MonoBehaviours) ↔ Events
2. **Event-Driven UI**: No Update() for static elements, subscribe to changes
3. **State-Based Input**: Check conditions before executing shortcuts
4. **Modular Panels**: InfoBar switches between hero/town/date/pickup states
5. **Input Validation**: Always validate on GameState, not UI state

### Implementation Order

1. ✅ Research complete
2. Create UIEventChannel (hero selection, button clicks)
3. Implement ResourceBarUI (subscribe to OnResourceChanged)
4. Implement InfoBarUI (state machine for hero/town/date)
5. Implement DayCounterUI + EndTurnButton
6. Create AdventureMapInputController (tile clicks → movement)
7. Add keyboard shortcuts (Unity Input System)
8. Wire all UI to event channels
9. Write unit tests

---

*Research complete. Ready for implementation.*
