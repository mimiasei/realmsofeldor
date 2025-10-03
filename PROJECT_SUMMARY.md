# Realms of Eldor - Project Summary

**Last Updated:** 2025-10-03
**Project Type:** Turn-based fantasy strategy game (HOMM3-inspired)
**Platform:** Unity 2022.3+ LTS with C#
**Architecture Reference:** VCMI (Heroes of Might and Magic III engine)

---

## Project Overview

Realms of Eldor is a Unity-based turn-based strategy game inspired by Heroes of Might and Magic III. The project leverages architectural patterns from the VCMI project (an open-source HOMM3 engine) to implement proven game systems while taking advantage of Unity's tooling and cross-platform capabilities.

**VCMI Repository Location:** `/tmp/vcmi-temp/` (reference for code patterns and architecture)

---

## Current Status

- Unity project initialized
- Comprehensive research completed on VCMI architecture and Unity translation strategies
- Two detailed research documents created:
  - **UNITY_MIGRATION_RESEARCH.md** - C++ to Unity/C# migration guide
  - **UNITY_RESEARCH.md** - VCMI architecture analysis and Unity implementation patterns

**Next Step:** Begin Phase 1 implementation (Foundation - Core architecture and data structures)

---

## Architecture Philosophy

### Three-Layer Separation (VCMI Pattern)

| Layer | Technology | Purpose |
|-------|------------|---------|
| **Core Logic** | Plain C# classes | Game state, rules, calculations (no Unity dependencies) |
| **Data** | ScriptableObjects | Static game data (creatures, heroes, spells, buildings) |
| **Presentation** | MonoBehaviours | Scene representation, UI, animations, rendering |

### Key Design Decisions

1. **Hybrid Approach:** Plain C# for game logic (testable, serializable) + MonoBehaviours for Unity features
2. **ScriptableObjects for Databases:** Replace VCMI's Handler pattern with Unity ScriptableObject assets
3. **Event Channels:** Decouple systems using ScriptableObject-based event channels (matches VCMI's observer pattern)
4. **Singleton GameState:** MonoBehaviour singleton wrapping pure C# GameState class

---

## Core Systems Overview

### 1. GameState Management
- **GameState.cs** (plain C#): Centralized game state, serializable for save/load
- **GameStateManager.cs** (MonoBehaviour): Unity lifecycle wrapper, singleton pattern
- Manages: Players, heroes, towns, resources, current day/turn

### 2. Hero System
- **HeroTypeData** (ScriptableObject): Static hero definitions (class, starting stats, specialty)
- **Hero** (plain C#): Runtime hero instance (position, level, experience, army, spells)
- **HeroController** (MonoBehaviour): Visual representation on map, movement animations

### 3. Battle System
- **BattleEngine** (plain C#): Turn-based combat logic, damage calculation
- **BattleUnit** (plain C#): Combat stack (creature type, count, health, position)
- **BattleController** (MonoBehaviour): Battle scene management, visual feedback
- Uses coroutines for animation pacing

### 4. Map System
- **GameMap** (plain C#): 2D tile array, map objects, pathfinding queries
- **MapTile** (struct): Terrain type, passability, movement cost
- **MapRenderer** (MonoBehaviour): Unity Tilemap rendering, object placement
- **MapObject** (abstract): Polymorphic objects (mines, resources, monsters)

### 5. Creature System
- **CreatureData** (ScriptableObject): Stats, abilities, costs, visuals
- **CreatureDatabase** (MonoBehaviour): Singleton lookup for creature data
- **Army** (plain C#): 7-slot army management (HOMM3 pattern)

### 6. Spell System
- **SpellData** (ScriptableObject): Spell definition, effects, costs
- **ISpellMechanics** (interface): Strategy pattern for spell behavior
- Separate mechanics for battle vs adventure map spells

---

## Technology Stack

### Essential Unity Packages

| Package | Purpose | Priority | Cost |
|---------|---------|----------|------|
| **A* Pathfinding Project Pro** | Grid-based pathfinding for heroes | High | $90 |
| **DOTween** | Animation tweening (movement, UI) | High | Free |
| **Newtonsoft Json** | Save/load (dictionaries, complex types) | High | Free |
| **Odin Inspector** | Enhanced Inspector, better serialization | Medium | $55 |
| **UniTask** | Modern async/await for Unity | Medium | Free |

### Unity Systems

- **UI Framework:** uGUI (recommended for HOMM3-style UI)
- **Tilemap:** Built-in Unity Tilemap for map rendering
- **Scene Management:** Additive scenes (Persistent, AdventureMap, Battle, Town)
- **Asset Management:** Addressables (optional, for larger projects)

---

## Project Structure

```
Assets/
├── Data/                          # ScriptableObject assets
│   ├── Creatures/
│   ├── Heroes/
│   ├── Spells/
│   ├── Buildings/
│   └── EventChannels/
├── Prefabs/
│   ├── Heroes/
│   ├── Creatures/
│   ├── MapObjects/
│   └── UI/
├── Scenes/
│   ├── Persistent.unity           # GameState (never unloads)
│   ├── MainMenu.unity
│   ├── AdventureMap.unity
│   ├── Battle.unity
│   └── Town.unity
├── Scripts/
│   ├── Core/                      # Plain C# game logic
│   │   ├── GameState.cs
│   │   ├── Hero.cs
│   │   ├── Army.cs
│   │   ├── Battle/
│   │   └── Map/
│   ├── Data/                      # ScriptableObject classes
│   │   ├── CreatureData.cs
│   │   ├── HeroTypeData.cs
│   │   └── EventChannels/
│   ├── Controllers/               # MonoBehaviours
│   │   ├── GameStateManager.cs
│   │   ├── HeroController.cs
│   │   └── MapRenderer.cs
│   ├── UI/                        # UI MonoBehaviours
│   └── Database/                  # Database managers
├── Sprites/
│   ├── Terrain/
│   ├── Creatures/
│   ├── Heroes/
│   └── UI/
└── Audio/
```

### Assembly Definitions (for fast compilation)
- **Core.asmdef** - Pure C# logic
- **Data.asmdef** - ScriptableObject classes
- **Controllers.asmdef** - MonoBehaviours
- **UI.asmdef** - UI components

---

## Implementation Roadmap (14-18 weeks)

### Phase 1: Foundation (2 weeks) COMPLETE!
- [✅] Set up Unity project structure and assembly definitions
- [✅] Create core data types (enums, structs)
- [✅] Implement GameState, Hero, Army, Creature classes
- [✅] Set up GameStateManager singleton
- [✅] Create event channel architecture
- [✅] Write unit tests for core logic

**Deliverable:** Testable game state with heroes, resources, turn management

### Phase 2: Data Layer (1-2 weeks)
- [ ] Create ScriptableObject classes (CreatureData, HeroTypeData, SpellData)
- [ ] Implement database manager singletons
- [ ] Create 5-10 sample creatures with placeholder stats
- [ ] Create 2-3 sample heroes
- [ ] Set up Addressables (optional)

**Deliverable:** Editable creature/hero database in Unity Inspector

### Phase 3: Map System (2 weeks)
- [ ] Implement GameMap class and MapTile structure
- [ ] Set up Unity Tilemap for terrain rendering
- [ ] Create/import terrain tiles (128x128)
- [ ] Implement MapRenderer MonoBehaviour
- [ ] Create camera controller (pan, zoom)
- [ ] Integrate A* Pathfinding Project

**Deliverable:** Scrollable, zoomable map with terrain variety

### Phase 4: Adventure Map UI (2 weeks)
- [ ] Create resource bar UI (uGUI)
- [ ] Create hero panel UI
- [ ] Implement hero selection and movement (mouse clicks)
- [ ] Create turn button and day counter
- [ ] Wire up event channels for UI updates
- [ ] Add keyboard shortcuts

**Deliverable:** Playable adventure map with hero movement and UI

### Phase 5: Battle System (3 weeks)
- [ ] Implement BattleEngine logic
- [ ] Create BattleUnit class
- [ ] Port damage calculation from VCMI
- [ ] Create battle UI scene
- [ ] Implement battlefield rendering
- [ ] Add battle animations (DOTween)
- [ ] Implement auto-battle AI
- [ ] Wire up victory/defeat handling

**Deliverable:** Functional auto-battle with visual feedback

### Phase 6: Town System (2-3 weeks)
- [ ] Create Town class and building system
- [ ] Create town window UI
- [ ] Add creature recruitment interface
- [ ] Implement daily creature growth
- [ ] Add resource costs for recruitment

**Deliverable:** Towns where heroes can recruit creatures

### Phase 7: Spell System (2 weeks)
- [ ] Create SpellData definitions
- [ ] Implement spell mechanics (Strategy pattern)
- [ ] Create spellbook UI
- [ ] Add spell casting in battle
- [ ] Add adventure map spells
- [ ] Create spell visual effects

**Deliverable:** Functional spell system in and out of combat

### Phase 8: Polish & Features (2-3 weeks)
- [ ] Save/load system (JSON serialization)
- [ ] Main menu with new game / load game
- [ ] Victory/defeat conditions
- [ ] Basic AI opponent
- [ ] Sound effects and background music
- [ ] Tutorial or first scenario
- [ ] Balance pass on creatures/spells

**Deliverable:** Complete, playable MVP

---

## Key Implementation Patterns

### MonoBehaviour vs Plain C# Decision Matrix

**Use MonoBehaviour when:**
- Need Unity lifecycle (Start, Update, OnEnable)
- Need to attach to GameObject
- Need Inspector serialization
- Need Coroutines
- Representing something in the scene

**Use Plain C# when:**
- Pure data storage (Hero, Creature, Army)
- Calculations and utilities
- Game logic independent of scene
- Manager classes (use singleton pattern)
- Static databases

### Event-Driven Architecture

```csharp
// ScriptableObject Event Channel
[CreateAssetMenu(menuName = "Events/Battle Event Channel")]
public class BattleEventChannel : ScriptableObject {
    public event Action<BattleUnit, BattleUnit, int> OnDamageDealt;

    public void RaiseDamageDealt(BattleUnit attacker, BattleUnit target, int damage) {
        OnDamageDealt?.Invoke(attacker, target, damage);
    }
}

// Logic broadcasts events
battleEvents.RaiseDamageDealt(attacker, target, damage);

// UI listens to events
void OnEnable() {
    battleEvents.OnDamageDealt += HandleDamageDealt;
}
```

### Static Data Management

```csharp
// ScriptableObject definition
[CreateAssetMenu(fileName = "Creature", menuName = "Game/Creature")]
public class CreatureData : ScriptableObject {
    public int creatureId;
    public int attack, defense, minDamage, maxDamage, hitPoints, speed;
    // ...
}

// Database manager
public class CreatureDatabase : MonoBehaviour {
    public static CreatureDatabase Instance { get; private set; }
    [SerializeField] private List<CreatureData> creatures;
    private Dictionary<int, CreatureData> lookup;

    public CreatureData GetCreature(int id) => lookup[id];
}
```

---

## Development Guidelines

### Code Reuse from VCMI
- **Always reference** `/tmp/vcmi-temp/` for proven patterns
- Key VCMI files to study:
  - `lib/gameState/CGameState.h` - Game state management
  - `lib/entities/hero/CHero.h` - Hero system
  - `lib/battle/BattleInfo.h` - Battle mechanics
  - `lib/mapping/CMap.h` - Map structure
  - `lib/CCreatureHandler.h` - Handler pattern

### Change Tracking
- Add brief summary of changes to CHANGES.md file after significant work
- Keep entries concise but informative

### Research Documentation
- After completing research tasks, summarize findings in RESEARCH.md
- Include code examples and architectural decisions

### Testing
- Write unit tests for core logic (Hero, Army, GameState, BattleEngine)
- Use Unity Test Framework (NUnit)
- Create test scenes for integration testing

---

## Performance Considerations

1. **Object Pooling:** Use for battle units, damage numbers, VFX
2. **Event-Driven UI:** Avoid Update() for static UI, use events
3. **Cache Components:** Store GetComponent references in Start/Awake
4. **Tilemap Optimization:** Unity Tilemap renders entire map in 1 draw call
5. **Assembly Definitions:** Enable for ~10x faster compile times

---

## Quick Start for New Session

1. **Review this summary** to understand project state
2. **Check current phase** in roadmap (currently: Phase 1 - Foundation)
3. **Reference VCMI code** at `/tmp/vcmi-temp/` for implementation patterns
4. **Consult detailed docs:**
   - UNITY_RESEARCH.md - Implementation details and code examples
   - UNITY_MIGRATION_RESEARCH.md - C++ to C# translation guide
5. **Follow development guidelines** in CLAUDE.md
6. **Update CHANGES.md** after significant work

---

## External Resources

### Documentation
- VCMI GitHub: https://github.com/vcmi/vcmi
- Unity Manual: https://docs.unity3d.com/Manual/
- Unity ScriptableObjects: https://unity.com/how-to/separate-game-data-logic-scriptable-objects

### Tutorials
- GameDev.tv Turn-Based Strategy: https://www.gamedev.tv/courses/unity-turn-based-strategy
- Code Monkey Unity Tutorials: https://www.youtube.com/@CodeMonkeyUnity

### Assets
- DOTween: https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676
- A* Pathfinding Project: https://arongranberg.com/astar/
- OpenGameArt: https://opengameart.org
- Kenney.nl: https://kenney.nl

---

## Success Criteria

**MVP Definition (Week 18):**
- ✅ Heroes can move on adventure map
- ✅ Battles execute with damage calculation and victory/defeat
- ✅ Towns allow creature recruitment
- ✅ Basic spells castable in battle
- ✅ Save/load functionality
- ✅ Win/loss conditions functional
- ✅ At least one playable scenario

**Post-MVP Goals:**
- Multiplayer support
- Advanced AI
- Map editor
- Mobile/web builds
- Mod support

---

*For detailed implementation guides, see UNITY_RESEARCH.md and UNITY_MIGRATION_RESEARCH.md*
