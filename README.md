# Realms of Eldor

A turn-based fantasy strategy game inspired by Heroes of Might and Magic III, built with Unity and C#.

## 🎮 Current Status: Phase 6F Complete

**Phase 6F: Modificator Architecture** ✅ **COMPLETE**
- VCMI-style modificator pipeline for extensible map generation
- Budget-based RMG with treasure limits and balanced placement
- Guard placement system for high-value objects
- Obstacle placement with path preservation
- Reachability validation ensuring all objects accessible
- Complete UI/UX flow (Main Menu → Map Selection → Adventure Map)

## 🕹️ Controls

### Camera Controls

| Input | Action |
|-------|--------|
| **W / ↑** | Pan camera up |
| **S / ↓** | Pan camera down |
| **A / ←** | Pan camera left |
| **D / →** | Pan camera right |
| **Mouse Wheel** | Zoom in/out (5-30 range) |
| **Middle Mouse + Drag** | Pan camera by dragging |
| **Move mouse to screen edges** | Edge pan (automatic) |

### Camera Behavior
- **Zoomed in**: Camera pans freely within map bounds
- **Zoomed out**: Camera locks to center when entire map is visible
- **Smooth movement**: All camera transitions are smooth and responsive

## 🗺️ Map Features

### Terrain Types (10 total)
- **Grass** - Green, standard movement cost (100)
- **Dirt** - Brown, slightly slower (125)
- **Sand** - Tan, desert terrain (150)
- **Snow** - White, cold regions (150)
- **Swamp** - Dark green, slow movement (175)
- **Rough** - Gray, rocky terrain (125)
- **Water** - Blue, impassable (requires special abilities)
- **Rock** - Dark gray, impassable mountains
- **Lava** - Red-orange, impassable volcanic terrain
- **Subterranean** - Dark, underground areas (100)

### Map Objects
- **Resources** - Pickable resource piles (gold, wood, ore, etc.)
- **Mines** - Capturable resource generators
- **Dwellings** - Creature recruitment buildings

## 🛠️ Development

### Technology Stack
- **Engine**: Unity 2022.3+ LTS
- **Language**: C# with .NET Standard 2.1
- **Packages**:
  - A* Pathfinding Project (grid-based pathfinding)
  - DOTween (animations)
  - UniTask (async/await)
  - Newtonsoft.Json (serialization)

### Architecture
- **Three-layer separation**: Core logic (pure C#) + Data (ScriptableObjects) + Presentation (MonoBehaviours)
- **Event-driven**: ScriptableObject event channels decouple systems
- **VCMI-inspired**: Based on proven HOMM3 engine architecture

### Testing the Map System

1. Open Unity project
2. Load scene: `Assets/Scenes/AdventureMap.unity`
3. Press **Play**
4. Use camera controls to explore the generated map
5. Press **Play** again to generate a new random map

### Project Structure
```
Assets/
├── Data/               # ScriptableObject assets
│   ├── Terrain/       # TerrainData (10 types)
│   ├── Tiles/         # Tile assets for Tilemap
│   └── EventChannels/ # Event system assets
├── Scenes/
│   └── AdventureMap.unity  # Main test scene
├── Scripts/
│   ├── Core/          # Pure C# game logic
│   ├── Controllers/   # MonoBehaviour controllers
│   ├── Data/          # ScriptableObject classes
│   ├── Editor/        # Unity Editor tools
│   └── Tests/         # Unit tests (65 tests)
└── Sprites/
    └── Terrain/       # Placeholder terrain tiles
```

## 📋 Implementation Roadmap

### ✅ Phase 1: Foundation (Complete)
- Core data types and game state
- Hero, Army, Resources, Player systems
- Event channel architecture
- 45 unit tests

### ✅ Phase 2: Data Layer (Complete)
- ScriptableObject classes for creatures, heroes, spells
- Database managers
- Sample data generation tools

### ✅ Phase 3: Map System (Complete)
- GameMap and MapTile structures
- Unity Tilemap rendering with MapRenderer
- CameraController with pan/zoom
- 10 terrain types with varied movement costs
- Map object system (resources, mines, dwellings)
- Random terrain generation
- A* pathfinding integration

### ✅ Phase 4: Adventure Map UI (Complete)
- Resource bar UI
- Hero panel with stats
- Hero selection and movement
- Turn button and day counter
- Keyboard shortcuts
- Path preview system
- Smooth hero movement

### ✅ Phase 5: Battle System (Complete)
- Turn-based combat engine
- Damage calculation (VCMI formulas)
- Turn queue with initiative ordering
- Special mechanics (double attack, status effects)
- Battle AI with action evaluation

### ✅ Phase 6A-6F: RMG Foundation (Complete)
- UI/UX flow (MainMenu → MapSelection → AdventureMap)
- Map persistence system with metadata
- Budget-based object placement (Phase 6B)
- Reachability validation (Phase 6C)
- Guard placement for high-value treasures (Phase 6D)
- Obstacle placement with density control (Phase 6E)
- **Modificator architecture refactor (Phase 6F)** ✨ NEW
  - 7 modificators with dependency chain
  - Extensible pipeline for adding new features
  - VCMI-accurate implementation

### ⏳ Phase 7: Zone-Based RMG (Future)
- Voronoi zone placement
- Per-zone budgets
- Zone connections and paths

### ⏳ Phase 8: Town System (Future)
- Town buildings
- Creature recruitment
- Resource costs and growth

## 🧪 Unit Tests

**Total: 180+ tests** with comprehensive coverage

Run tests in Unity:
- Window → General → Test Runner
- EditMode tab → Run All

Test Coverage:
- Core logic (GameState, Hero, Army, Resources)
- Map system (GameMap, MapTile, Pathfinding)
- Battle system (Combat, Turn Queue, Damage)
- RMG system (Budget, Reachability, Guards, Obstacles)
- Modificator pipeline (15 tests)

## 🎯 Next Steps

1. **Phase 7**: Zone-based RMG (Voronoi zones, balanced starting positions)
2. **Town System**: Buildings, creature recruitment, resource management
3. **Replace placeholder art** with proper sprites
4. **Battle visualization**: Hex grid rendering, unit animations

## 📝 Notes

- This is a work-in-progress educational project
- Architecture inspired by VCMI (open-source HOMM3 engine)
- Focus on clean code, testability, and proven patterns

---

**Current Build**: Phase 6F Complete - Modificator Architecture Refactor
**Last Updated**: 2025-10-15
