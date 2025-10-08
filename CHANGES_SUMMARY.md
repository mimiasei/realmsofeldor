# Realms of Eldor - Changes Summary

**Quick reference for getting up to speed in new sessions**

---

## Current Project Status (2025-10-08)

**Current Phase:** Phase 5E (Battle AI & Action Selection) - ✅ COMPLETE

**Next Phase:** Phase 6 - Town System (or Battle Visualization)

**Total Project Files:** ~90 files
**Total Project LOC:** ~18,100+ lines

---

## Architecture Overview

**Three-Layer Separation (VCMI Pattern):**
- **Data**: ScriptableObjects & foundational types - static game data (creatures, heroes, spells, terrain)
- **Core Logic**: Game systems & logic - game state, rules, calculations, event channels
- **Presentation**: MonoBehaviours - scene representation, UI, animations, rendering

**Key Principles:**
- ✅ 100% SSOT (Single Source of Truth)
- ✅ 100% DRY (Don't Repeat Yourself)
- ✅ Event-driven architecture via ScriptableObject event channels
- ✅ Layered dependencies: Data (foundation) ← Core ← Controllers/UI

**Assembly Definitions (for fast compilation):**
- `RealmsOfEldor.Data` - Foundation layer (no dependencies)
- `RealmsOfEldor.Core` - Game logic (references Data)
- `RealmsOfEldor.Controllers` - MonoBehaviours (references Core, Data)
- `RealmsOfEldor.UI` - UI components (references Core, Data)
- `RealmsOfEldor.Database` - Database managers
- `RealmsOfEldor.Tests` - Unit tests

**Assembly Dependency Rules (CRITICAL):**
```
Data Assembly (Foundation Layer)
  ├─ CreatureData, HeroTypeData, SpellData, TerrainData
  ├─ Faction, CreatureTier, ResourceCost (moved from Core)
  ├─ NO DEPENDENCIES - cannot reference Core or any other assembly
  └─ Contains: Pure data definitions, enums, simple structs

Core Assembly (Logic Layer)
  ├─ Hero, GameMap, Position, MapObject, GameState
  ├─ BattleUnit, BattleState, DamageCalculator, TurnQueue
  ├─ MapEventChannel (event channels live here, not in Data!)
  ├─ References: Data assembly only
  └─ Contains: Game logic, systems, event coordination

Controllers/UI Assemblies (Presentation Layer)
  ├─ References: Core, Data
  └─ Contains: MonoBehaviours, scene management, visual feedback
```

**Why This Matters:**
- ✅ Data cannot use `using RealmsOfEldor.Core;` (would create circular dependency)
- ✅ Core CAN use `using RealmsOfEldor.Data;` (proper layering)
- ✅ Event channels that use Core types (Hero, GameMap, etc.) must live in Core
- ✅ ScriptableObjects can live in either Data (pure data) or Core (systems/events)

---

## Phase Completion Status

### Phase 1: Foundation ✅ COMPLETE
- Core data types (GameTypes, Resources, Position)
- Game logic classes (GameState, Hero, Army, Player)
- GameStateManager singleton
- Event channel architecture (GameEventChannel, BattleEventChannel)
- 53 unit tests for core logic

### Phase 2: Data Layer ✅ COMPLETE
- ScriptableObject classes (CreatureData, HeroTypeData, SpellData)
- Database managers (CreatureDatabase, HeroDatabase, SpellDatabase)
- Sample data: 10 creatures, 2 heroes, 5 spells
- SampleDataGenerator editor tool

### Phase 3: Map System ✅ COMPLETE
- Core map logic (GameMap, MapTile, MapObject)
- MapRenderer with dual Tilemap system
- CameraController (pan, zoom, edge detection)
- TerrainData ScriptableObjects
- MapEventChannel for map events
- 56 unit tests for map system

### Phase 4: Adventure Map UI ✅ COMPLETE
- ResourceBarUI, InfoBarUI, TurnControlUI, HeroPanelUI
- Hero selection and movement (mouse + keyboard)
- AdventureMapInputController with pathfinding
- BasicPathfinder implementation (28 tests)
- GameInitializer for game state setup
- Phase4UISetup editor tool for automated scene setup
- Keyboard shortcuts (Space, H, E, S, C, arrows)

### Phase 5A: Battle System Core ✅ COMPLETE
- BattleHex (17×11 hexagonal coordinate system)
- BattleUnit (creature stacks with health, stats, abilities)
- BattleAction (walk, wait, defend, attack, shoot, spell)
- BattleState (battle container with sides, units, obstacles)
- BattleController (MonoBehaviour orchestrator)
- BATTLE_SYSTEM_RESEARCH.md (15,000+ lines analyzing VCMI)

### Phase 5B: Turn Order System ✅ COMPLETE
- TurnQueue (initiative-based turn order with VCMI's CMP_stack comparator)
- Three-phase turn system (NORMAL, WAIT_MORALE, WAIT)
- Tiebreaker rules (initiative → slot order → side priority)
- Wait action mechanics (delay turn to later in round)
- Bonus turn support (good morale framework)
- BattleState integration with turn queue
- BattleController.ProcessRound() orchestration
- 20 unit tests (100% coverage)

### Phase 5C: Combat System ✅ COMPLETE
- DamageCalculator with VCMI formula (attack/defense skill factors, luck, range penalties)
- Attack factors: +5% per attack point (capped at +300%), luck (+100%), placeholders for specials
- Defense factors: -2.5% per defense point (capped at -70%), range penalty (-50%), unlucky (-50%)
- Casualties calculation: converts damage to kills using VCMI formula
- ExecuteAttack(): melee attack with automatic retaliation
- ExecuteShoot(): ranged attack (no retaliation)
- ExecuteRetaliation(): counter-attack system (once per turn)
- AttackResult class for combat result tracking
- 45 unit tests (25 damage + 20 combat actions)

### Phase 5D: Special Mechanics ✅ COMPLETE
- **Double Attack**: Creatures with `isDoubleAttack` attack twice per turn (if defender survives)
- **No Retaliation**: Creatures with `noMeleeRetal` prevent defender retaliation
- **Flying**: `isFlying` flag added (movement integration deferred to pathfinding phase)
- **Status Effects Framework**: Complete buff/debuff system
  - StatusEffect class with duration tracking, stat modifiers (attack/defense/speed)
  - BattleUnit.AddStatusEffect(), RemoveStatusEffect(), ClearStatusEffects()
  - Auto-expiration via UpdateStatusEffects() called each round
  - Multiple effects stack additively
- **Architecture Cleanup**:
  - Moved TerrainType, SpellSchool from Core to Data (proper layering)
  - Moved Hero.Initialize() from Data/HeroTypeData to Core/Hero
  - Removed duplicate BattleSide, CreatureStack, StatusEffect definitions
  - Fixed Hero constructor (now uses GameState.AddHero() factory pattern)
  - Added SpellData.isDoubleWide to CreatureData
- **Testing**: 13 new unit tests for double attack, no-retaliation, flying, status effects
- **Deferred**: Morale system, Luck system (moved to later phase)

### Phase 5E: Battle AI & Action Selection ✅ COMPLETE
- **BattleAI Class**: Main AI controller with action selection logic
  - SelectAction(): Evaluates all possible actions and picks best
  - Simple decision flow: evaluate attacks → score → execute best or wait
  - Based on VCMI's BattleEvaluator pattern
- **AttackPossibility Class**: Attack option evaluation and scoring
  - Calculates expected damage to defender and retaliation damage
  - Score formula: `DamageToDefender - RetaliationDamage + Bonuses`
  - Bonus for killing defender (+100), penalty for getting killed (-1000)
  - Handles both melee and ranged attacks
- **BattleController Integration**:
  - AI automatically selects actions for units
  - ExecuteAction() fully implemented (attack, shoot, wait, defend)
  - ProcessUnitTurn() calls AI and executes selected actions
  - Toggle AI on/off via inspector
- **Action Evaluation**:
  - Ranged units prefer shooting (no retaliation)
  - Melee units attack if adjacent, otherwise wait
  - AI picks highest scoring target among multiple enemies
- **Testing**: 10 new unit tests for AI logic, attack evaluation, target selection
- **Deferred**: Pathfinding integration (move towards enemy), spell casting AI

---

## Key Systems Implemented

### Map System
- **GameMap.cs** (~360 lines): 2D tile array, object management, movement validation
- **MapTile.cs** (~180 lines): Terrain type, visual variants, blocking/visitable objects
- **MapObject.cs** (~260 lines): Base class for map objects (ResourceObject, MineObject, DwellingObject)
- **MapRenderer.cs** (~340 lines): Dual Tilemap rendering, terrain + objects layer
- **CameraController.cs** (~240 lines): WASD/edge pan, scroll zoom, camera bounds
- **TerrainData.cs** (~90 lines): Terrain definitions with movement cost, variants, passability
- Random terrain variants: Grass has 3 variants, auto-selected on map generation

### UI System
- **ResourceBarUI**: Top bar showing 7 resources + date
- **InfoBarUI**: Bottom-left info panel (HOMM3 style)
- **TurnControlUI**: Bottom-right with day counter + End Turn button
- **HeroPanelUI**: Left panel showing selected hero stats + 7 garrison slots
- Event-driven updates via UIEventChannel and GameEventChannel

### Input & Movement
- **AdventureMapInputController**: Hero selection, pathfinding, movement
- **BasicPathfinder**: A* pathfinding with movement cost calculation
- **Keyboard shortcuts**: Space (end turn), H (next hero), E (sleep), S (spellbook), C (center), arrows (move)

### Battle System
- **BattleHex.cs** (~180 lines): Hexagonal grid navigation, distance calculation, neighbor traversal
- **BattleUnit.cs** (~350 lines): Health tracking, combat stats, ranged/retaliation system, special abilities
- **BattleAction.cs** (~220 lines): Action types with factory methods (VCMI pattern)
- **BattleState.cs** (~540 lines): Battle container, unit management, round tracking, TurnQueue integration, combat execution
- **BattleController.cs** (~280 lines): Battle initialization, ProcessRound() orchestration, turn order display
- **TurnQueue.cs** (~220 lines): Initiative-based turn order, three-phase system, tiebreaker rules, wait mechanics
- **DamageCalculator.cs** (~450 lines): VCMI damage formula, attack/defense factors, casualties calculation
- **AttackResult.cs** (~30 lines): Combat result tracking with retaliation support

---

## Technology Stack

### Installed Packages
- ✅ **Newtonsoft.Json** (3.2.1) - Advanced serialization for save/load
- ✅ **UniTask** (Git) - Zero-allocation async/await for performance
- ✅ **DOTween** (Asset Store) - Animation tweening
- ✅ **Cinemachine** (3.1.4) - Advanced camera control
- ✅ **Universal Render Pipeline (URP)** (17.2.0) - Activated with 2D Renderer
- ✅ **2D Tilemap Extras** (5.0.1) - AnimatedTile support

### Rendering
- **URP Activated**: GraphicsSettings uses UniversalRP.asset with 2D Renderer
- **SRP Batcher**: Enabled for ~50% CPU overhead reduction
- **Tilemap System**: Dual tilemaps (terrain + objects layer)
- **Animated Tiles**: Water terrain uses 21-frame AnimatedTile

---

## Editor Tools

### Scene Setup Tools
- **Phase4UISetup.cs**: One-click UI setup (ResourceBar, InfoBar, TurnControl, HeroPanel, Databases)
- **Phase4SetupWindow.cs**: Interactive setup wizard with step-by-step instructions
- **URPActivationTool.cs**: Activate/deactivate URP, verify setup
- **SampleDataGenerator.cs**: Generate sample creatures, heroes, spells

### Automated Features
- **Auto-load TerrainData**: MapRenderer auto-discovers TerrainData assets in Assets/Data/Terrain
- **Auto-wire event channels**: Phase4UISetup uses reflection to wire event channels to UI components
- **Auto-initialize databases**: Phase4UISetup creates singleton databases and populates data

---

## Serialization & Performance

### Newtonsoft.Json Integration
- Custom converters for Position and ResourceSet structs
- Polymorphic support with `TypeNameHandling.Auto`
- Dictionary serialization (JsonUtility doesn't support this)
- Human-readable JSON save files
- 9 serialization tests

### UniTask Integration
- **CameraController**: `MoveToAsync()` for zero-allocation camera movement
- **GameStateManager**: `SaveGameAsync()` / `LoadGameAsync()` with background thread serialization
- **AsyncHelpers.cs**: Utility class for common async patterns (delay, wait, fade, retry, timeout)
- Zero GC allocation compared to coroutines

---

## Research Documents

### BATTLE_SYSTEM_RESEARCH.md (15,000+ lines)
Comprehensive analysis of VCMI battle system:
- Hex grid system (17×11 battlefield)
- Turn order algorithm (initiative-based with tiebreakers)
- Damage calculation formula (attack/defense factors, luck, morale)
- Special mechanics (morale, luck, retaliation, wait, defend)
- Battle AI (action evaluation, target selection)
- Unity translation architecture with code examples

### URP_RESEARCH.md (~400 lines)
- URP benefits for 2D strategy games
- Project status analysis (URP installed but not activated)
- Migration checklist and testing strategy
- Performance optimization recommendations

### ANIMATED_TILES_GUIDE.md (~500 lines)
- Complete setup guide for 21-frame animated water
- Step-by-step Unity workflow (no code changes needed!)
- Performance considerations
- Advanced techniques for other animated terrain

### RESEARCH.md
- VCMI map system analysis (CMap.h, TerrainTile.h, CGObjectInstance.h)
- Core patterns: 3D terrain storage, tile structure, object management
- Unity translation strategy for map system

---

## Key Code Patterns

### Event-Driven Architecture
```csharp
// ScriptableObject Event Channel
[CreateAssetMenu(menuName = "Events/Game Event Channel")]
public class GameEventChannel : ScriptableObject {
    public event Action<Hero> OnHeroCreated;
    public void RaiseHeroCreated(Hero hero) => OnHeroCreated?.Invoke(hero);
}

// UI subscribes to events
void OnEnable() {
    gameEvents.OnHeroCreated += HandleHeroCreated;
}
```

### Static Data Management
```csharp
// ScriptableObject definition
[CreateAssetMenu(fileName = "Creature", menuName = "Game/Creature")]
public class CreatureData : ScriptableObject {
    public int creatureId;
    public int attack, defense, minDamage, maxDamage, hitPoints, speed;
}

// Database manager (singleton)
public class CreatureDatabase : MonoBehaviour {
    public static CreatureDatabase Instance { get; private set; }
    [SerializeField] private List<CreatureData> creatures;
    private Dictionary<int, CreatureData> lookup;
    public CreatureData GetCreature(int id) => lookup[id];
}
```

### DRY/SSOT Compliance Example
```csharp
// Auto-load TerrainData from folder (SSOT)
void LoadTerrainDataFromAssets() {
    var guids = AssetDatabase.FindAssets("t:TerrainData", new[] { terrainDataPath });
    terrainDataArray = new TerrainData[guids.Length];
    for (int i = 0; i < guids.Length; i++) {
        var path = AssetDatabase.GUIDToAssetPath(guids[i]);
        terrainDataArray[i] = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
    }
}
// No manual Inspector assignment needed! ✅
```

---

## Important Code Standards

**From CLAUDE.md:**
- Always reference VCMI repo at `/tmp/vcmi-temp` for proven patterns
- Use `var` for type-inferred variables
- Maintain 100% SSOT and 100% DRY
- Use `FindFirstObjectByType` instead of deprecated `FindObjectOfType`
- Update CHANGES.md after work (keep it brief)
- Document research findings in RESEARCH.md
- Copy and reuse as much VCMI code as possible - don't reinvent the wheel!

---

## Bug Fixes & Important Changes

### Map Rendering
- Fixed NullReferenceExceptions in MapTile (null checks for blockingObjectIds)
- GameInitializer auto-creates GameStateManager if missing
- Fixed random terrain variants on map initialization (was hardcoded to variant 0)
- Added shared Random instance for proper distribution
- Optional seed parameter for deterministic map generation

### Compilation Fixes (Phase 5C Post-Implementation)
- **Assembly Dependency Restructuring** (Critical Fix):
  - **Problem**: Circular dependency between Core and Data assemblies
  - **Root Cause**: Data files used `using RealmsOfEldor.Core;` but Core referenced Data
  - **Solution**: Reversed dependency - Data is now foundation, Core references Data
  - **Actions Taken**:
    1. Moved `Faction`, `CreatureTier`, `ResourceCost` from Core to Data (new file: `GameDataTypes.cs`)
    2. Updated Core assembly: Added Data reference, set `noEngineReferences: false`
    3. Updated Data assembly: Removed Core reference (Data has NO dependencies now)
    4. Moved ALL event channels from `Data/EventChannels/` to `Core/Events/`:
       - MapEventChannel (uses Position, GameMap, Hero, MapObject, TerrainType)
       - BattleEventChannel (uses Position)
       - GameEventChannel (uses Position, Hero)
       - UIEventChannel (uses Hero)
    5. Updated event channel namespaces: `RealmsOfEldor.Data` → `RealmsOfEldor.Core.Events`
    6. Removed `using RealmsOfEldor.Core;` from all Data files
    7. Added `using RealmsOfEldor.Data;` to Core files (GameTypes.cs, Resources.cs, BattleState.cs)
  - **Key Learning**: Event channels that reference game logic types (Hero, GameMap) belong in Core, not Data
- BattleHex.cs: Removed `UnityEngine` using and `[SerializeField]` attribute (pure C# struct)
- TurnQueue.cs: Removed duplicate `BattleSide` enum (already defined in BattleUnit.cs)
- Position.cs: Removed Unity dependencies from Core assembly (pure C# now)
- PositionExtensions.cs: Created Utilities assembly for Unity conversions
- GameInitializer: Fixed to use GameState API correctly
- Phase4UISetup: Auto-initializes database singletons with reflection

### DRY/SSOT Fixes
- MapRenderer auto-loads TerrainData from Assets/Data/Terrain folder
- Removed manual Inspector assignment requirement
- Phase4UISetup auto-configures MapRenderer using reflection

---

## Next Steps

### Phase 5E: Battle AI & Action Selection - NEXT (or skip to Phase 6)
- AI action evaluation (VCMI's BattleEvaluator pattern)
- Target selection logic
- Action priority system
- Simple AI for testing battles

### Phase 6: Town System (Weeks 9-11)
- Town class and building system
- Town window UI
- Creature recruitment interface
- Daily creature growth
- Resource costs for recruitment

### Deferred Mechanics (Future Phases)
- Morale system (extra turn / skip turn)
- Luck system (+100% damage / -50% damage)
- Flying movement (pathfinding integration)

---

## Common Commands

### Unity Editor
- **Menu: Realms of Eldor/Setup/Setup Phase 4 UI Components** - One-click UI setup
- **Menu: Realms of Eldor/Setup/Activate URP (Recommended)** - Activate URP rendering
- **Menu: Realms of Eldor/Setup/Verify URP Setup** - Verify URP configuration
- **Menu: Realms of Eldor/Generate Sample Data** - Generate creatures, heroes, spells

### Testing
- **Unity Test Runner**: Run all unit tests (Window > General > Test Runner)
- **Edit Mode Tests**: 151 tests across GameState, Hero, Army, Resources, Map, Battle, Damage, Combat Actions
- **Play Mode Tests**: None yet (waiting for integration tests)

---

## File Locations

### Core Logic (Pure C#)
- `Assets/Scripts/Core/` - GameState, Hero, Army, Player, Resources, Position, GameTypes (minus types moved to Data)
- `Assets/Scripts/Core/Map/` - GameMap, MapTile, MapObject
- `Assets/Scripts/Core/Battle/` - BattleHex, BattleUnit, BattleAction, BattleState, TurnQueue, DamageCalculator, AttackResult
- `Assets/Scripts/Core/Events/` - ALL event channels (MapEventChannel, BattleEventChannel, GameEventChannel, UIEventChannel)

### Data (ScriptableObjects & Foundation Types)
- `Assets/Scripts/Data/` - CreatureData, HeroTypeData, SpellData, TerrainData, GameDataTypes (Faction, CreatureTier, ResourceCost)
- `Assets/Scripts/Data/EventChannels/` - REMOVED (all event channels moved to Core/Events)

### Controllers (MonoBehaviours)
- `Assets/Scripts/Controllers/` - GameStateManager, MapRenderer, CameraController, GameInitializer
- `Assets/Scripts/Controllers/Battle/` - BattleController

### UI
- `Assets/Scripts/UI/` - ResourceBarUI, InfoBarUI, TurnControlUI, HeroPanelUI

### Databases
- `Assets/Scripts/Database/` - CreatureDatabase, HeroDatabase, SpellDatabase

### Editor Tools
- `Assets/Scripts/Editor/SceneSetup/` - Phase4UISetup, Phase4SetupWindow, URPActivationTool
- `Assets/Scripts/Editor/DataGeneration/` - SampleDataGenerator

### Tests
- `Assets/Scripts/Tests/EditMode/` - All unit tests (GameStateTests, HeroTests, MapTests, BattleHexTests, TurnQueueTests, DamageCalculatorTests, CombatActionsTests, etc.)

---

## VCMI Reference

**Location:** `/tmp/vcmi-temp/` (entire VCMI repo downloaded locally)

**Key Files to Reference:**
- `lib/gameState/CGameState.h` - Game state management
- `lib/entities/hero/CHero.h` - Hero system
- `lib/battle/BattleInfo.h` - Battle mechanics
- `lib/battle/BattleHex.h` - Hex grid system
- `lib/battle/BattleInfo.cpp` - CMP_stack comparator (turn order)
- `lib/battle/DamageCalculator.cpp` - Damage formulas
- `lib/mapping/CMap.h` - Map structure
- `lib/CCreatureHandler.h` - Handler pattern
- `server/battles/BattleFlowProcessor.cpp` - Battle flow control
- `AI/BattleAI/BattleEvaluator.cpp` - AI logic

---

## Quick Start Checklist for New Session

1. ✅ Read PROJECT_SUMMARY.md for project overview
2. ✅ Read this file (CHANGES_SUMMARY.md) for recent changes
3. ✅ Check current phase status (currently Phase 5C complete, starting Phase 5D)
4. ✅ Reference VCMI code at `/tmp/vcmi-temp/` for implementation patterns
5. ✅ Review BATTLE_SYSTEM_RESEARCH.md for battle system details (especially damage calculation)
6. ✅ Update CHANGES.md after significant work (keep it brief)
7. ✅ Update this file (CHANGES_SUMMARY.md) with brief summary after completing phase

## Troubleshooting Assembly Dependencies

**If you see**: `error CS0234: The type or namespace name 'X' does not exist in the namespace 'Y'`

**Common Causes**:
1. **Circular dependency attempt**: Trying to add `using RealmsOfEldor.Core;` in a Data file
2. **Wrong layer**: Type is in wrong assembly (e.g., event channel using Core types but living in Data)
3. **Missing assembly reference**: Assembly definition doesn't list required assembly

**Quick Fixes**:
- ✅ **Data files**: Can NEVER use `using RealmsOfEldor.Core;` - move type to Data or move file to Core
- ✅ **Core files**: CAN use `using RealmsOfEldor.Data;` - this is correct layering
- ✅ **Event channels**: If they reference Hero/GameMap/Position, they belong in `Core/Events/`, not `Data/EventChannels/`
- ✅ **Foundation types**: Enums/structs used by Data belong in Data (Faction, CreatureTier, ResourceCost)

**Assembly Reference Chain**:
```
Data (no deps) ← Core (refs Data) ← Controllers (refs Core, Data) ← UI (refs Core, Data)
```

---

**Recent Completions (2025-10-08)**:
- ✅ Phase 5B: Turn order system with TurnQueue, initiative sorting, wait mechanics (4 files, ~730 lines)
- ✅ Phase 5C: Combat system with DamageCalculator, attack/shoot actions, retaliation (6 files, ~2,330 lines, 45 tests)
- ✅ Phase 5D: Special mechanics - double attack, status effects framework, architecture cleanup (2 files updated, 1 new test file, 13 tests)
- ✅ Phase 5E: Battle AI - BattleAI, AttackPossibility, action selection, BattleController integration (3 new files, ~800 lines, 10 tests)
- Next: Phase 6 - Town System

*Last Updated: 2025-10-08*
