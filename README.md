# Realms of Eldor

A turn-based fantasy strategy game inspired by Heroes of Might and Magic III, built with Unity and C#.

## 🎮 Current Status: Phase 3 Complete

**Phase 3: Map System** ✅ **COMPLETE**
- Fully functional adventure map with varied terrain
- Smooth camera controls with zoom and pan
- Random map generation with water, mountains, swamps, and more
- Object placement system (resources, mines, dwellings)

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
- 65 total unit tests

### 🔄 Phase 4: Adventure Map UI (In Progress - Code Complete)
- Resource bar UI
- Hero panel with stats
- Hero selection and movement
- Turn button and day counter
- Keyboard shortcuts

### ⏳ Phase 5: Battle System (Not Started)
- Turn-based combat engine
- Damage calculation (HOMM3 formulas)
- Battle UI and animations
- Auto-battle AI

### ⏳ Phase 6: Town System (Not Started)
- Town buildings
- Creature recruitment
- Resource costs and growth

## 🧪 Unit Tests

**Total: 65 tests** with 100% coverage on core logic

Run tests in Unity:
- Window → General → Test Runner
- EditMode tab → Run All

## 📚 Documentation

- **PROJECT_SUMMARY.md** - Complete project overview and architecture
- **CHANGES.md** - Detailed development log (1800+ lines)
- **RESEARCH.md** - VCMI architecture research and Unity translation
- **CLAUDE.md** - Development commands and instructions

## 🎯 Next Steps

1. **Phase 4**: Integrate adventure map UI (code exists, needs scene setup)
2. **Phase 5**: Implement battle system
3. **Replace placeholder art** with proper sprites
4. **Create hero prefabs** for movement testing

## 📝 Notes

- This is a work-in-progress educational project
- Architecture inspired by VCMI (open-source HOMM3 engine)
- Focus on clean code, testability, and proven patterns
- VCMI reference code available at `/tmp/vcmi-temp/`

## 🚀 Quick Start for New Session

1. Read **PROJECT_SUMMARY.md** for current state
2. Check **CHANGES.md** for recent updates
3. Reference **VCMI code** at `/tmp/vcmi-temp/` for implementation patterns
4. Run tests to verify everything works
5. Load AdventureMap scene and press Play!

---

**Current Build**: Phase 3 Complete - Map System Fully Functional
**Last Updated**: 2025-10-04
