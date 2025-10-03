# Unity/C# Implementation Research for Realms of Eldor
## Based on VCMI Architecture Analysis

**Research Date:** 2025-10-03
**VCMI Version:** Latest (analyzed from /tmp/vcmi-temp)
**Target Platform:** Unity 2022.3 LTS or newer with C#
**Project:** Realms of Eldor - HOMM3-inspired strategy game

---

## Executive Summary

This document provides comprehensive research on implementing Realms of Eldor in Unity/C# based on deep analysis of the VCMI (Heroes of Might and Magic III engine) codebase. VCMI provides a proven, production-quality architecture that has successfully recreated HOMM3's complex systems. This research maps VCMI's C++ patterns to Unity/C# equivalents while leveraging Unity's built-in features.

**Key Findings:**
- VCMI's three-layer architecture (lib/client/server) maps cleanly to Unity's data/logic/presentation separation
- Game state management uses patterns directly translatable to Unity singletons and ScriptableObjects
- Battle system architecture is well-suited for Unity's coroutine-based turn management
- Map system aligns perfectly with Unity's Tilemap and Grid components
- Handler pattern for game databases translates to ScriptableObject-based asset management

---

## Table of Contents

1. [VCMI Architecture Analysis](#1-vcmi-architecture-analysis)
2. [Core System Breakdown](#2-core-system-breakdown)
3. [Unity/C# Translation Strategies](#3-unityc-translation-strategies)
4. [Detailed System Implementations](#4-detailed-system-implementations)
5. [Recommended Unity Packages](#5-recommended-unity-packages)
6. [Project Structure](#6-project-structure)
7. [Implementation Roadmap](#7-implementation-roadmap)
8. [Performance Considerations](#8-performance-considerations)
9. [Testing Strategy](#9-testing-strategy)
10. [References](#10-references)

---

## 1. VCMI Architecture Analysis

### 1.1 Project Structure Overview

VCMI follows a clean separation of concerns across three main directories:

```
vcmi/
├── lib/              # Core game logic (platform-independent)
│   ├── gameState/    # Central game state management
│   ├── entities/     # Game entities (heroes, creatures, artifacts)
│   ├── battle/       # Battle system logic
│   ├── mapping/      # Map structures and operations
│   ├── spells/       # Spell system
│   ├── mapObjects/   # Map object types (towns, mines, etc.)
│   ├── bonuses/      # Bonus/buff system
│   ├── callback/     # Observer/callback interfaces
│   └── ...
├── client/           # UI, rendering, player interaction
│   ├── gui/          # Base GUI framework
│   ├── widgets/      # UI widgets
│   ├── windows/      # Game windows (hero, town, etc.)
│   ├── battle/       # Battle UI
│   ├── adventureMap/ # Adventure map UI
│   ├── render/       # Rendering abstractions
│   └── renderSDL/    # SDL2 implementations
├── server/           # Game server, networking, AI
└── AI/               # AI implementations
```

**Key Design Principle:** Complete separation between game logic (lib), presentation (client), and server logic. This enables:
- Headless server operation
- Multiple client implementations
- Easy testing of game logic
- Network multiplayer support

### 1.2 Core Classes and Relationships

**CGameState** - The Central Hub
```cpp
// lib/gameState/CGameState.h
class CGameState : public CNonConstInfoCallback {
    std::unique_ptr<CMap> map;                        // The game map
    std::map<PlayerColor, PlayerState> players;        // All players
    std::map<TeamID, TeamState> teams;                 // Team states
    std::vector<std::unique_ptr<BattleInfo>> currentBattles;
    std::unique_ptr<TavernHeroesPool> heroesPool;      // Available heroes
    ui32 day;                                          // Current day

    void apply(CPackForClient & pack);                 // Apply state changes
    BattleInfo* getBattle(const BattleID & battle);
    const CMap & getMap() const;
};
```

**Handler Pattern** - Static Databases
```cpp
// VCMI uses "Handler" classes for static game data
// Examples:
// - CCreatureHandler: All creature definitions
// - CHeroHandler: All hero type definitions
// - CSpellHandler: All spell definitions
// - CArtHandler: All artifact definitions
// - CTownHandler: All town/building definitions

// Pattern:
class CCreatureHandler : public IHandlerBase {
    std::vector<std::unique_ptr<CCreature>> objects; // All creatures

    const CCreature* getByIndex(CreatureID id) const;
    void loadObject(JsonNode & data);
};
```

**Entity Hierarchy**
```cpp
// Map objects use polymorphic hierarchy
CGObjectInstance                    // Base class
├── CGHeroInstance                  // Heroes on map
├── CGTownInstance                  // Towns
├── CGCreature                      // Monster groups
├── CGResource                      // Resource piles
├── CGMine                          // Mines (resource generation)
├── CGArtifact                      // Artifacts on map
└── ...other object types

// All inherit from IObjectInterface for callbacks
class IObjectInterface {
    virtual void onHeroVisit(const CGHeroInstance * h) = 0;
    virtual void onHeroLeave(const CGHeroInstance * h) = 0;
    virtual void battleFinished(...) = 0;
};
```

### 1.3 Observer/Callback Pattern

VCMI extensively uses interfaces for event communication:

```cpp
// lib/callback/CGameInterface.h
class CGameInterface {
    virtual void yourTurn() = 0;
    virtual void heroMoved(const TryMoveHero & details) = 0;
    virtual void battleStart(const BattleInfo * info) = 0;
    virtual void battleEnd(const BattleResult * br) = 0;
    virtual void artifactPut(const ArtifactLocation & al) = 0;
    // ...many more callbacks
};

// Client implements this interface
class CPlayerInterface : public CGameInterface {
    void yourTurn() override;
    void battleStart(const BattleInfo * info) override;
    // ...
};
```

This pattern enables:
- Decoupled communication between lib (logic) and client (UI)
- Multiple observers for same events
- Easy addition of AI players
- Network event synchronization

---

## 2. Core System Breakdown

### 2.1 Game State Management

**VCMI Implementation:**
```cpp
class CGameState {
    // Centralized state storage
    std::map<PlayerColor, PlayerState> players;
    std::unique_ptr<CMap> map;
    ui32 day;

    // State modification through "packs"
    void apply(CPackForClient & pack);

    // Querying
    const CGHeroInstance* getHero(ObjectInstanceID id) const;
    const CGTownInstance* getTown(ObjectInstanceID id) const;
};

// State changes packaged as objects
struct SetResources : public CPackForClient {
    PlayerColor player;
    TResources res;
    bool abs; // absolute or relative
};
```

**Key Patterns:**
- Single source of truth for all game data
- Immutable state modification (through packs/commands)
- Query interface for read operations
- Serializable for save/load

### 2.2 Hero System

**VCMI CHero vs CGHeroInstance:**
```cpp
// CHero - Static hero type definition (database entry)
class CHero {
    HeroTypeID ID;
    std::string identifier;
    const CHeroClass* heroClass;
    std::vector<InitialArmyStack> initialArmy;
    std::vector<std::pair<SecondarySkill, ui8>> secSkillsInit;
    BonusList specialty;
};

// CGHeroInstance - Runtime hero instance on map
class CGHeroInstance : public CArmedInstance {
    const CHero* type;              // Reference to static data

    // Runtime state
    std::string nameCustom;
    int32_t exp;
    int32_t level;
    int32_t movement;               // Current movement points
    int32_t mana;

    // Primary stats
    PrimarySkill primSkills;        // Attack, Defense, Power, Knowledge

    // Secondary skills
    std::map<SecondarySkill, MysteryHero::SScriptedHero::SecSkill> secSkills;

    // Inventory
    ArtSlotMap artifactsWorn;       // Equipped artifacts
    std::vector<ArtSlotInfo> artifactsInBackpack;

    // Army
    TSlots stacks;                  // 7 creature stacks

    // Spells
    std::set<SpellID> spells;
};
```

**Key Insight:** Clear separation between:
1. **Static Data** (CHero): Immutable definitions loaded once
2. **Runtime Instance** (CGHeroInstance): Mutable game state

### 2.3 Battle System

**VCMI Architecture:**
```cpp
// lib/battle/BattleInfo.h
class BattleInfo : public CBattleInfoCallback {
    BattleID battleID;
    si32 round;
    si32 activeStack;                            // Current unit's ID

    BattleSideArray<SideInBattle> sides;         // Attacker & Defender
    std::vector<std::unique_ptr<CStack>> stacks; // All battle units
    std::vector<std::shared_ptr<CObstacleInstance>> obstacles;

    BattleField battlefieldType;
    SiegeInfo si;                                // Siege-specific data

    // Combat queries
    TStacks getStacksIf(const TStackFilter & predicate) const;
    const CStack* getStack(int stackID) const;
};

// Battle unit (stack in combat)
class CStack : public CBonusSystemNode {
    ui32 ID;
    const CCreature* type;
    ui32 baseAmount;           // Original count
    ui32 count;                // Current alive count
    si32 firstHPleft;          // HP of first (damaged) creature

    ui8 position;              // Hex position on battlefield
    ui8 side;                  // 0=attacker, 1=defender

    // Combat stats (with bonuses applied)
    int getAttack() const;
    int getDefense() const;
    int getMinDamage() const;
    int getMaxDamage() const;
};
```

**Battle Flow:**
1. Create BattleInfo from two armies
2. Sort stacks by initiative (speed stat)
3. Each round: iterate through stacks
4. Stack performs action (move, attack, cast, wait, defend)
5. Apply damage, check for death
6. Repeat until one side eliminated
7. Award experience and loot

### 2.4 Map System

**VCMI CMap Structure:**
```cpp
class CMap {
    int32_t width, height, levels;   // Map dimensions (2 levels: surface/underground)

    // Terrain data (3D array)
    boost::multi_array<TerrainTile, 3> terrain; // [z][x][y]

    // Objects
    std::vector<std::shared_ptr<CGObjectInstance>> objects;

    // Precomputed indices
    std::vector<ObjectInstanceID> heroesOnMap;
    std::vector<ObjectInstanceID> towns;

    // Queries
    TerrainTile& getTile(const int3& tile);
    bool isInTheMap(const int3& pos) const;
    bool canMoveBetween(const int3& src, const int3& dst) const;
    int3 guardingCreaturePosition(int3 pos) const;
};

// Individual tile
struct TerrainTile {
    TerrainId terType;         // Grass, dirt, water, etc.
    TerrainId terView;         // Visual variation
    RiverId riverType;
    RoadId roadType;

    bool visitable;            // Has object that can be visited
    bool blocked;              // Impassable

    std::vector<ObjectInstanceID> visitableObjects;
    std::vector<ObjectInstanceID> blockingObjects;
};
```

**Pathfinding:**
- Dijkstra's algorithm with terrain costs
- Movement points based on hero stats and terrain
- Obstacles and blocked tiles considered
- Fog of war integration

### 2.5 Spell System

**VCMI Spell Architecture:**
```cpp
// Static spell definition
class CSpell {
    SpellID id;
    std::string identifier;
    si32 level;                         // 1-5 spell level

    SpellSchool school;                 // Air, Earth, Fire, Water

    std::map<ESpellCastProblem, std::string> castRequirements;

    // Effects
    std::vector<std::unique_ptr<ISpellMechanics>> mechanics;
};

// Spell mechanics (Strategy pattern)
class ISpellMechanics {
    virtual void applyBattle(BattleInfo* battle, const BattleSpellCast& packet) = 0;
    virtual void applyAdventure(CGameState* gs, const AdventureSpellCast& packet) = 0;
};

// Concrete implementations
class DamageSpellMechanics : public ISpellMechanics { ... };
class BuffSpellMechanics : public ISpellMechanics { ... };
class SummonSpellMechanics : public ISpellMechanics { ... };
```

### 2.6 Town System

**VCMI Town Structure:**
```cpp
class CGTownInstance : public CGDwelling {
    const CTown* town;                   // Town type (Castle, Rampart, etc.)
    std::string name;

    // Buildings
    std::set<BuildingID> builtBuildings;
    std::set<BuildingID> forbiddenBuildings;

    // Garrison
    CArmedInstance garrison;             // Town defenders

    // Economy
    std::vector<CGDwelling::CreatureID> creatures; // Available for recruit

    // Fortifications (for siege)
    SiegeInfo si;
};

// Town type definition
class CTown {
    FactionID faction;
    std::vector<std::unique_ptr<CBuilding>> buildings;
    std::vector<std::vector<CreatureID>> creatures; // Per level (1-7)

    // Visual
    AnimationPath clientInfo;
    std::string townBackground;
};
```

---

## 3. Unity/C# Translation Strategies

### 3.1 Overall Architecture Mapping

**VCMI Three-Layer → Unity Equivalent:**

| VCMI Layer | Purpose | Unity Equivalent |
|------------|---------|------------------|
| **lib/** | Core game logic | Plain C# classes in `Scripts/Core/` |
| **client/** | UI & rendering | MonoBehaviours + UI components |
| **server/** | Networking & AI | Unity Netcode + AI classes |

**Recommended Unity Architecture:**
```
Unity Project
├── Scripts/
│   ├── Core/                    # Game logic (plain C#)
│   │   ├── GameState.cs
│   │   ├── Hero.cs
│   │   ├── Creature.cs
│   │   ├── Army.cs
│   │   ├── Battle/
│   │   │   ├── BattleEngine.cs
│   │   │   └── BattleUnit.cs
│   │   └── Map/
│   │       ├── GameMap.cs
│   │       └── MapObject.cs
│   ├── Data/                    # ScriptableObjects
│   │   ├── CreatureData.cs
│   │   ├── HeroTypeData.cs
│   │   ├── SpellData.cs
│   │   └── BuildingData.cs
│   ├── Controllers/             # MonoBehaviours (scene logic)
│   │   ├── GameStateManager.cs
│   │   ├── HeroController.cs
│   │   ├── MapRenderer.cs
│   │   └── BattleController.cs
│   ├── UI/                      # UI MonoBehaviours
│   │   ├── ResourceBar.cs
│   │   ├── HeroPanel.cs
│   │   ├── BattleUI.cs
│   │   └── TownWindow.cs
│   └── Events/                  # Event system
│       ├── GameEvents.cs
│       └── BattleEvents.cs
└── Assets/
    └── Data/                    # ScriptableObject assets
        ├── Creatures/
        ├── Heroes/
        ├── Spells/
        └── Buildings/
```

### 3.2 Memory Management Translation

**VCMI (C++):**
```cpp
// Explicit ownership with smart pointers
std::unique_ptr<Hero> hero;              // Owns hero
std::map<HeroID, std::unique_ptr<Hero>> heroes;

Hero* getHero(HeroID id) {               // Non-owning reference
    return heroes[id].get();
}
```

**Unity (C#):**
```csharp
// Garbage-collected references
Hero hero;                                // Reference (GC manages)
Dictionary<int, Hero> heroes = new();

Hero GetHero(int id) {                    // Reference copy
    return heroes[id];
}
```

**Key Mindset Shift:** In Unity/C#, focus on object relationships rather than ownership. Let GC handle cleanup.

### 3.3 Static Data: Handler → ScriptableObject

**VCMI Handler Pattern:**
```cpp
// CCreatureHandler loads all creatures once
class CCreatureHandler {
    std::vector<std::unique_ptr<CCreature>> objects;

    const CCreature* getByIndex(CreatureID id) const {
        return objects[id.num].get();
    }
};
```

**Unity ScriptableObject Pattern:**
```csharp
// CreatureData.cs - Individual creature definition
[CreateAssetMenu(fileName = "Creature", menuName = "Game/Creature")]
public class CreatureData : ScriptableObject {
    public int creatureId;
    public string creatureName;
    public Faction faction;

    [Header("Combat Stats")]
    public int attack;
    public int defense;
    public int minDamage;
    public int maxDamage;
    public int hitPoints;
    public int speed;

    [Header("Economy")]
    public ResourceCost cost;
    public int aiValue;

    [Header("Abilities")]
    public List<CreatureAbility> abilities;

    [Header("Visuals")]
    public Sprite icon;
    public RuntimeAnimatorController battleAnimator;
}

// CreatureDatabase.cs - Database manager
public class CreatureDatabase : MonoBehaviour {
    public static CreatureDatabase Instance { get; private set; }

    [SerializeField] private List<CreatureData> creatures;
    private Dictionary<int, CreatureData> lookup;

    void Awake() {
        Instance = this;
        lookup = creatures.ToDictionary(c => c.creatureId, c => c);
    }

    public CreatureData GetCreature(int id) => lookup[id];
    public IEnumerable<CreatureData> GetByFaction(Faction f) =>
        creatures.Where(c => c.faction == f);
}
```

**Advantages:**
- Visual editing in Inspector (designers balance without code)
- Hot-reload during play mode
- Asset references (sprites, animations) built-in
- Serialization automatic
- No loading code needed

### 3.4 Observer Pattern → Unity Events

**VCMI Callback Interface:**
```cpp
class IBattleEventsReceiver {
    virtual void battleDamageDealt(const BattleDamage & damage) = 0;
    virtual void battleStackMoved(const CStack * stack, BattleHex dest) = 0;
    virtual void battleEnd(const BattleResult * br) = 0;
};

// Battle engine broadcasts to receiver
class BattleEngine {
    IBattleEventsReceiver* receiver;

    void dealDamage(...) {
        // ... damage calculation
        if (receiver) {
            receiver->battleDamageDealt(damageInfo);
        }
    }
};
```

**Unity ScriptableObject Event Channels:**
```csharp
// BattleEventChannel.cs
[CreateAssetMenu(menuName = "Events/Battle Event Channel")]
public class BattleEventChannel : ScriptableObject {
    // Events
    public event Action<BattleUnit, BattleUnit, int> OnDamageDealt;
    public event Action<BattleUnit, Vector2Int> OnUnitMoved;
    public event Action<BattleResult> OnBattleEnd;

    // Raise methods (called by game logic)
    public void RaiseDamageDealt(BattleUnit attacker, BattleUnit target, int damage) {
        OnDamageDealt?.Invoke(attacker, target, damage);
    }

    public void RaiseUnitMoved(BattleUnit unit, Vector2Int position) {
        OnUnitMoved?.Invoke(unit, position);
    }

    public void RaiseBattleEnd(BattleResult result) {
        OnBattleEnd?.Invoke(result);
    }
}

// BattleEngine.cs
public class BattleEngine {
    [SerializeField] private BattleEventChannel eventChannel;

    private void DealDamage(BattleUnit attacker, BattleUnit target) {
        int damage = CalculateDamage(attacker, target);
        target.CurrentHealth -= damage;

        // Broadcast event
        eventChannel.RaiseDamageDealt(attacker, target, damage);
    }
}

// BattleUI.cs (Listener)
public class BattleUI : MonoBehaviour {
    [SerializeField] private BattleEventChannel eventChannel;
    [SerializeField] private Text battleLog;

    void OnEnable() {
        eventChannel.OnDamageDealt += HandleDamageDealt;
        eventChannel.OnBattleEnd += HandleBattleEnd;
    }

    void OnDisable() {
        eventChannel.OnDamageDealt -= HandleDamageDealt;
        eventChannel.OnBattleEnd -= HandleBattleEnd;
    }

    void HandleDamageDealt(BattleUnit attacker, BattleUnit target, int damage) {
        battleLog.text += $"\n{attacker.Name} deals {damage} to {target.Name}";
    }
}
```

**Benefits:**
- Complete decoupling (battle logic doesn't know about UI)
- Multiple listeners supported
- Inspector-assignable event channels
- Easy to add new listeners without modifying senders

### 3.5 MonoBehaviour vs Plain C# Decision Matrix

| Use MonoBehaviour When | Use Plain C# Class When |
|------------------------|-------------------------|
| Need Unity lifecycle (Start, Update, OnEnable) | Pure data storage |
| Need to attach to GameObject | Calculations and utilities |
| Need Inspector serialization | Game logic that doesn't interact with scene |
| Need Coroutines | Manager classes (can use static) |
| Need physics callbacks (OnCollision, OnTrigger) | Data models (Hero, Creature, Army) |
| Representing something in the scene | Static databases |

**Examples:**
```csharp
// Plain C# - Game logic (like VCMI's lib/)
[System.Serializable]
public class Hero {
    public int Id { get; set; }
    public string Name { get; set; }
    public int Level { get; set; }
    public int Experience { get; set; }
    public Army Army { get; set; }

    public void GainExperience(int exp) {
        Experience += exp;
        while (CanLevelUp()) LevelUp();
    }

    private void LevelUp() { /* ... */ }
}

// MonoBehaviour - Scene representation (like VCMI's client/)
public class HeroController : MonoBehaviour {
    public Hero HeroData { get; set; }  // References plain C# data

    private Animator animator;
    private SpriteRenderer spriteRenderer;

    void Update() {
        // Sync visual representation with data
        transform.position = HeroData.Position.ToVector3();
        animator.SetBool("IsMoving", HeroData.IsMoving);
    }

    public IEnumerator MoveToPosition(Vector2Int target) {
        // Animation coroutine
        while (Vector2.Distance(transform.position, target) > 0.1f) {
            transform.position = Vector2.MoveTowards(transform.position, target, Time.deltaTime * 5f);
            yield return null;
        }
        HeroData.Position = target;
    }
}
```

---

## 4. Detailed System Implementations

### 4.1 GameState Management

**Hybrid Approach (Recommended):**
```csharp
// GameState.cs - Core logic (plain C#, serializable)
[System.Serializable]
public class GameState {
    public int CurrentDay { get; private set; } = 1;
    public Dictionary<int, Player> Players { get; private set; } = new();
    public GameMap Map { get; private set; }
    public Dictionary<int, Hero> Heroes { get; private set; } = new();
    public Dictionary<int, Town> Towns { get; private set; } = new();

    public void Initialize(int playerCount, GameMap map) {
        CurrentDay = 1;
        Map = map;
        // ... initialization
    }

    public void NextTurn() {
        CurrentDay++;
        // Process daily events
        foreach (var player in Players.Values) {
            player.AddResources(GetDailyIncome(player));
        }
        // Reset hero movement points
        foreach (var hero in Heroes.Values) {
            hero.RefreshMovement();
        }
    }

    public Hero GetHero(int id) => Heroes[id];
    public Player GetPlayer(int id) => Players[id];
}

// GameStateManager.cs - Unity lifecycle wrapper (MonoBehaviour)
public class GameStateManager : MonoBehaviour {
    public static GameStateManager Instance { get; private set; }

    [SerializeField] private GameEventChannel gameEvents;

    private GameState gameState = new();

    public GameState State => gameState;

    void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartNewGame(int playerCount) {
        GameMap map = GenerateMap();
        gameState.Initialize(playerCount, map);
        gameEvents.RaiseGameStarted(gameState);
    }

    public void AdvanceTurn() {
        gameState.NextTurn();
        gameEvents.RaiseTurnAdvanced(gameState.CurrentDay);
    }

    // Save/Load
    public void SaveGame(string filename) {
        string json = JsonConvert.SerializeObject(gameState, Formatting.Indented);
        File.WriteAllText(Application.persistentDataPath + "/" + filename, json);
    }

    public void LoadGame(string filename) {
        string json = File.ReadAllText(Application.persistentDataPath + "/" + filename);
        gameState = JsonConvert.DeserializeObject<GameState>(json);
        gameEvents.RaiseGameLoaded(gameState);
    }
}
```

**Why This Approach:**
- GameState is pure C# → easy to test, serialize, network
- GameStateManager handles Unity-specific lifecycle
- Clean separation like VCMI's lib/client split

### 4.2 Hero System Translation

**Data Layer (ScriptableObject - Static Definition):**
```csharp
// HeroTypeData.cs
[CreateAssetMenu(fileName = "HeroType", menuName = "Game/Hero Type")]
public class HeroTypeData : ScriptableObject {
    public int heroTypeId;
    public string heroName;
    public HeroClass heroClass;  // Knight, Wizard, etc.

    [Header("Starting Stats")]
    public int startAttack = 1;
    public int startDefense = 1;
    public int startPower = 1;
    public int startKnowledge = 1;

    [Header("Starting Army")]
    public List<CreatureStack> startingArmy;

    [Header("Starting Skills")]
    public List<SecondarySkill> startingSkills;

    [Header("Specialty")]
    public string specialtyName;
    public string specialtyDescription;
    public List<Bonus> specialtyBonuses;

    [Header("Visuals")]
    public Sprite portrait;
    public Sprite mapSprite;
    public RuntimeAnimatorController battleAnimator;
}

[System.Serializable]
public struct CreatureStack {
    public CreatureData creature;
    public int minCount;
    public int maxCount;
}
```

**Runtime Layer (Plain C# - Game Instance):**
```csharp
// Hero.cs
[System.Serializable]
public class Hero {
    public int Id { get; set; }
    public int TypeId { get; set; }  // Reference to HeroTypeData
    public string CustomName { get; set; }
    public int Owner { get; set; }

    // Position
    public Vector2Int Position { get; set; }
    public int Movement { get; set; }
    public int MaxMovement { get; set; } = 1500;  // HOMM3 default

    // Experience and Level
    public int Experience { get; private set; }
    public int Level { get; private set; } = 1;

    // Primary Stats
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int SpellPower { get; set; }
    public int Knowledge { get; set; }
    public int Mana { get; set; }
    public int MaxMana { get; set; }

    // Secondary Skills
    public Dictionary<SecondarySkillType, int> SecondarySkills { get; set; } = new();

    // Army (7 slots like HOMM3)
    public Army Army { get; set; } = new Army();

    // Spellbook
    public HashSet<int> KnownSpells { get; set; } = new();
    public bool HasSpellbook { get; set; }

    // Artifacts
    public Dictionary<ArtifactSlot, int> EquippedArtifacts { get; set; } = new();
    public List<int> Backpack { get; set; } = new();

    // Methods
    public void GainExperience(int exp) {
        Experience += exp;
        while (Experience >= GetExperienceForNextLevel()) {
            LevelUp();
        }
    }

    private int GetExperienceForNextLevel() {
        // HOMM3 formula: 1000 * level
        return 1000 * Level;
    }

    private void LevelUp() {
        Level++;

        // Primary stat increases (randomized like HOMM3)
        var heroType = HeroDatabase.Instance.GetHeroType(TypeId);
        IncreasePrimaryStats(heroType.heroClass);

        // Offer secondary skill choice (event)
        HeroEventChannel.Instance.RaiseLevelUp(this);
    }

    public void RefreshMovement() {
        Movement = MaxMovement;
    }

    public bool CanMove(int cost) {
        return Movement >= cost;
    }

    public void SpendMovement(int cost) {
        Movement = Math.Max(0, Movement - cost);
    }
}

// Army.cs - 7-slot army system
[System.Serializable]
public class Army {
    private const int MAX_SLOTS = 7;
    private CreatureStack[] slots = new CreatureStack[MAX_SLOTS];

    public CreatureStack GetSlot(int index) {
        if (index < 0 || index >= MAX_SLOTS) return null;
        return slots[index];
    }

    public bool AddCreatures(int creatureId, int count, int preferredSlot = -1) {
        // Try to merge with existing stack
        for (int i = 0; i < MAX_SLOTS; i++) {
            if (slots[i] != null && slots[i].CreatureId == creatureId) {
                slots[i].Count += count;
                return true;
            }
        }

        // Find empty slot
        int slot = preferredSlot >= 0 && preferredSlot < MAX_SLOTS ? preferredSlot : FindEmptySlot();
        if (slot >= 0) {
            slots[slot] = new CreatureStack { CreatureId = creatureId, Count = count };
            return true;
        }

        return false; // Army full
    }

    public int GetTotalStrength() {
        int total = 0;
        foreach (var stack in slots) {
            if (stack != null) {
                var creature = CreatureDatabase.Instance.GetCreature(stack.CreatureId);
                total += creature.aiValue * stack.Count;
            }
        }
        return total;
    }
}

[System.Serializable]
public class CreatureStack {
    public int CreatureId { get; set; }
    public int Count { get; set; }
}
```

**Scene Representation (MonoBehaviour - Visual):**
```csharp
// HeroController.cs
public class HeroController : MonoBehaviour {
    public int HeroId { get; set; }

    private Hero heroData;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    [SerializeField] private HeroEventChannel heroEvents;

    void Start() {
        heroData = GameStateManager.Instance.State.GetHero(HeroId);
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        // Load sprite from HeroTypeData
        var heroType = HeroDatabase.Instance.GetHeroType(heroData.TypeId);
        spriteRenderer.sprite = heroType.mapSprite;
    }

    void Update() {
        // Sync position with data
        Vector3 targetPos = new Vector3(heroData.Position.x, heroData.Position.y, 0);
        if (Vector3.Distance(transform.position, targetPos) > 0.1f) {
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 10f);
        }
    }

    public IEnumerator MoveAlongPath(List<Vector2Int> path) {
        foreach (var tile in path) {
            heroData.Position = tile;

            Vector3 targetPos = new Vector3(tile.x, tile.y, 0);
            float duration = 0.3f;
            float elapsed = 0;
            Vector3 startPos = transform.position;

            while (elapsed < duration) {
                transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = targetPos;
        }

        heroEvents.RaiseHeroMoved(heroData);
    }
}
```

### 4.3 Battle System Translation

**Battle Engine (Core Logic):**
```csharp
// BattleEngine.cs
public class BattleEngine {
    private BattleEventChannel eventChannel;

    public Hero AttackingHero { get; private set; }
    public Hero DefendingHero { get; private set; }
    public List<BattleUnit> AttackerUnits { get; private set; } = new();
    public List<BattleUnit> DefenderUnits { get; private set; } = new();

    public BattleEngine(BattleEventChannel eventChannel) {
        this.eventChannel = eventChannel;
    }

    public void InitializeBattle(Hero attacker, Hero defender) {
        AttackingHero = attacker;
        DefendingHero = defender;

        // Convert armies to battle units
        AttackerUnits = CreateBattleUnits(attacker.Army, BattleSide.Attacker);
        DefenderUnits = CreateBattleUnits(defender.Army, BattleSide.Defender);

        eventChannel.RaiseBattleStarted(this);
    }

    private List<BattleUnit> CreateBattleUnits(Army army, BattleSide side) {
        var units = new List<BattleUnit>();
        for (int i = 0; i < 7; i++) {
            var stack = army.GetSlot(i);
            if (stack != null && stack.Count > 0) {
                var creature = CreatureDatabase.Instance.GetCreature(stack.CreatureId);
                units.Add(new BattleUnit {
                    CreatureId = stack.CreatureId,
                    Count = stack.Count,
                    Side = side,
                    Position = CalculateStartPosition(i, side),
                    CurrentHealth = creature.hitPoints
                });
            }
        }
        return units;
    }

    public IEnumerator ExecuteAutoBattle() {
        eventChannel.RaiseBattleStarted(this);

        int round = 1;
        while (!IsBattleOver()) {
            eventChannel.RaiseRoundStarted(round);
            yield return ExecuteRound();
            round++;
        }

        BattleResult result = DetermineBattleResult();
        eventChannel.RaiseBattleEnded(result);
    }

    private IEnumerator ExecuteRound() {
        // Sort units by initiative (speed)
        var allUnits = AttackerUnits.Concat(DefenderUnits)
            .Where(u => u.IsAlive())
            .OrderByDescending(u => GetCreatureSpeed(u))
            .ToList();

        foreach (var unit in allUnits) {
            if (!unit.IsAlive()) continue;

            var target = SelectBestTarget(unit);
            if (target == null) continue;

            eventChannel.RaiseUnitAttacking(unit, target);
            yield return new WaitForSeconds(0.5f);  // Animation delay

            int damage = CalculateDamage(unit, target);
            ApplyDamage(target, damage);

            eventChannel.RaiseDamageDealt(unit, target, damage);
            yield return new WaitForSeconds(0.5f);

            if (!target.IsAlive()) {
                eventChannel.RaiseUnitDied(target);
            }
        }
    }

    private int CalculateDamage(BattleUnit attacker, BattleUnit target) {
        var attackerCreature = CreatureDatabase.Instance.GetCreature(attacker.CreatureId);
        var targetCreature = CreatureDatabase.Instance.GetCreature(target.CreatureId);

        // HOMM3 damage formula
        int baseDamage = UnityEngine.Random.Range(
            attackerCreature.minDamage,
            attackerCreature.maxDamage + 1
        );

        // Apply attack/defense modifier
        int attackDiff = attackerCreature.attack - targetCreature.defense;
        float modifier = 1.0f;
        if (attackDiff > 0) {
            modifier = 1.0f + (attackDiff * 0.05f);  // +5% per point
        } else if (attackDiff < 0) {
            modifier = 1.0f / (1.0f + Math.Abs(attackDiff) * 0.05f);
        }

        int totalDamage = Mathf.RoundToInt(baseDamage * attacker.Count * modifier);
        return totalDamage;
    }

    private void ApplyDamage(BattleUnit target, int damage) {
        var creature = CreatureDatabase.Instance.GetCreature(target.CreatureId);

        // Damage first creature
        target.CurrentHealth -= damage;

        // Kill creatures
        while (target.CurrentHealth <= 0 && target.Count > 0) {
            target.Count--;
            target.CurrentHealth += creature.hitPoints;
        }

        // Last creature might survive with low HP
        if (target.Count == 0) {
            target.CurrentHealth = 0;
        }
    }

    private bool IsBattleOver() {
        bool attackersAlive = AttackerUnits.Any(u => u.IsAlive());
        bool defendersAlive = DefenderUnits.Any(u => u.IsAlive());
        return !(attackersAlive && defendersAlive);
    }

    private BattleResult DetermineBattleResult() {
        bool attackerWon = AttackerUnits.Any(u => u.IsAlive());

        // Calculate casualties and experience
        int defenderLosses = DefenderUnits.Sum(u => GetCreatureValue(u.CreatureId) * u.InitialCount);
        int attackerLosses = AttackerUnits.Sum(u => GetCreatureValue(u.CreatureId) * (u.InitialCount - u.Count));

        return new BattleResult {
            AttackerWon = attackerWon,
            ExperienceGained = defenderLosses / 10,  // HOMM3: 10% of enemy value
            SurvivingUnits = attackerWon ? AttackerUnits : DefenderUnits
        };
    }
}

// BattleUnit.cs
[System.Serializable]
public class BattleUnit {
    public int CreatureId { get; set; }
    public int Count { get; set; }
    public int InitialCount { get; set; }
    public int CurrentHealth { get; set; }  // HP of first (damaged) creature
    public BattleSide Side { get; set; }
    public Vector2Int Position { get; set; }

    public bool IsAlive() => Count > 0;
}

public enum BattleSide { Attacker, Defender }

[System.Serializable]
public struct BattleResult {
    public bool AttackerWon { get; set; }
    public int ExperienceGained { get; set; }
    public List<BattleUnit> SurvivingUnits { get; set; }
}
```

**Battle Controller (MonoBehaviour):**
```csharp
// BattleController.cs
public class BattleController : MonoBehaviour {
    [SerializeField] private BattleEventChannel battleEvents;
    [SerializeField] private GameObject battleUnitPrefab;
    [SerializeField] private Transform attackerContainer;
    [SerializeField] private Transform defenderContainer;

    private BattleEngine battleEngine;
    private Dictionary<BattleUnit, GameObject> unitVisuals = new();

    public void StartBattle(Hero attacker, Hero defender) {
        battleEngine = new BattleEngine(battleEvents);
        battleEngine.InitializeBattle(attacker, defender);

        SpawnUnitVisuals();
        StartCoroutine(battleEngine.ExecuteAutoBattle());
    }

    private void SpawnUnitVisuals() {
        // Spawn attacker units
        foreach (var unit in battleEngine.AttackerUnits) {
            var go = Instantiate(battleUnitPrefab, attackerContainer);
            go.GetComponent<BattleUnitController>().Initialize(unit);
            unitVisuals[unit] = go;
        }

        // Spawn defender units
        foreach (var unit in battleEngine.DefenderUnits) {
            var go = Instantiate(battleUnitPrefab, defenderContainer);
            go.GetComponent<BattleUnitController>().Initialize(unit);
            unitVisuals[unit] = go;
        }
    }

    void OnEnable() {
        battleEvents.OnDamageDealt += HandleDamageDealt;
        battleEvents.OnBattleEnded += HandleBattleEnded;
    }

    void OnDisable() {
        battleEvents.OnDamageDealt -= HandleDamageDealt;
        battleEvents.OnBattleEnded -= HandleBattleEnded;
    }

    private void HandleDamageDealt(BattleUnit attacker, BattleUnit target, int damage) {
        // Trigger attack animation
        if (unitVisuals.TryGetValue(attacker, out var attackerGO)) {
            attackerGO.GetComponent<Animator>().SetTrigger("Attack");
        }

        // Show damage number
        if (unitVisuals.TryGetValue(target, out var targetGO)) {
            DamageNumbers.Show(targetGO.transform.position, damage);
        }
    }

    private void HandleBattleEnded(BattleResult result) {
        // Apply results to game state
        if (result.AttackerWon) {
            var attacker = battleEngine.AttackingHero;
            attacker.GainExperience(result.ExperienceGained);

            // Update army with survivors
            UpdateArmyFromBattle(attacker, result.SurvivingUnits);
        }

        // Close battle window
        StartCoroutine(CloseBattleAfterDelay(2f));
    }

    private IEnumerator CloseBattleAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);
        SceneManager.UnloadSceneAsync("Battle");
    }
}
```

### 4.4 Map System with Unity Tilemap

**GameMap (Core Logic):**
```csharp
// GameMap.cs
[System.Serializable]
public class GameMap {
    public int Width { get; private set; }
    public int Height { get; private set; }

    private MapTile[,] tiles;
    public List<MapObject> Objects { get; private set; } = new();

    public GameMap(int width, int height) {
        Width = width;
        Height = height;
        tiles = new MapTile[width, height];
    }

    public MapTile GetTile(Vector2Int pos) {
        if (!IsInBounds(pos)) return null;
        return tiles[pos.x, pos.y];
    }

    public void SetTile(Vector2Int pos, MapTile tile) {
        if (IsInBounds(pos)) {
            tiles[pos.x, pos.y] = tile;
        }
    }

    public bool IsInBounds(Vector2Int pos) {
        return pos.x >= 0 && pos.x < Width && pos.y >= 0 && pos.y < Height;
    }

    public bool IsPassable(Vector2Int pos) {
        var tile = GetTile(pos);
        if (tile == null || !tile.Passable) return false;

        // Check for blocking objects
        return !Objects.Any(obj => obj.Position == pos && obj.Blocks);
    }

    public MapObject GetObjectAt(Vector2Int pos) {
        return Objects.FirstOrDefault(obj => obj.Position == pos);
    }
}

[System.Serializable]
public class MapTile {
    public TerrainType Terrain { get; set; }
    public bool Passable { get; set; } = true;
    public int MovementCost { get; set; } = 100;  // HOMM3: base 100
}

public enum TerrainType {
    Grass, Dirt, Sand, Snow, Swamp, Rough, Lava, Water
}

[System.Serializable]
public abstract class MapObject {
    public int Id { get; set; }
    public MapObjectType Type { get; set; }
    public Vector2Int Position { get; set; }
    public bool Blocks { get; set; }
    public bool Visitable { get; set; }

    public abstract void OnHeroVisit(Hero hero);
}
```

**MapRenderer (Unity Tilemap):**
```csharp
// MapRenderer.cs
public class MapRenderer : MonoBehaviour {
    [SerializeField] private Tilemap terrainTilemap;
    [SerializeField] private Tilemap objectsTilemap;
    [SerializeField] private GameObject heroPrefab;
    [SerializeField] private TileBase[] terrainTiles;  // Indexed by TerrainType

    private GameMap map;
    private Dictionary<int, GameObject> heroObjects = new();
    private Dictionary<int, GameObject> mapObjectVisuals = new();

    public void RenderMap(GameMap gameMap) {
        map = gameMap;

        ClearMap();
        RenderTerrain();
        RenderObjects();
        RenderHeroes();
    }

    private void RenderTerrain() {
        for (int x = 0; x < map.Width; x++) {
            for (int y = 0; y < map.Height; y++) {
                var tile = map.GetTile(new Vector2Int(x, y));
                var tilePos = new Vector3Int(x, y, 0);
                terrainTilemap.SetTile(tilePos, terrainTiles[(int)tile.Terrain]);
            }
        }
    }

    private void RenderObjects() {
        foreach (var obj in map.Objects) {
            // Instantiate object prefab based on type
            GameObject prefab = GetPrefabForObjectType(obj.Type);
            GameObject instance = Instantiate(prefab, new Vector3(obj.Position.x, obj.Position.y, 0), Quaternion.identity);
            instance.transform.SetParent(transform);
            mapObjectVisuals[obj.Id] = instance;
        }
    }

    private void RenderHeroes() {
        var heroes = GameStateManager.Instance.State.Heroes.Values;
        foreach (var hero in heroes) {
            GameObject heroGO = Instantiate(heroPrefab, new Vector3(hero.Position.x, hero.Position.y, 0), Quaternion.identity);
            heroGO.GetComponent<HeroController>().HeroId = hero.Id;
            heroObjects[hero.Id] = heroGO;
        }
    }
}
```

### 4.5 Spell System

**Spell Data (ScriptableObject):**
```csharp
// SpellData.cs
[CreateAssetMenu(fileName = "Spell", menuName = "Game/Spell")]
public class SpellData : ScriptableObject {
    public int spellId;
    public string spellName;
    public int level;  // 1-5
    public SpellSchool school;  // Air, Earth, Fire, Water

    [Header("Costs")]
    public int manaCost;

    [Header("Target")]
    public SpellTargetType targetType;

    [Header("Effects")]
    public List<SpellEffect> effects;

    [Header("Visuals")]
    public Sprite icon;
    public GameObject effectPrefab;
    public AudioClip castSound;
}

public enum SpellSchool { Air, Earth, Fire, Water }
public enum SpellTargetType { SingleEnemy, AllEnemies, SingleAlly, AllAllies, Battlefield }

[System.Serializable]
public class SpellEffect {
    public SpellEffectType type;
    public int basePower;
    public float scaling;  // Scales with spell power
}

public enum SpellEffectType {
    Damage,
    Heal,
    Buff,
    Debuff,
    Summon
}
```

**Spell Mechanics (Strategy Pattern):**
```csharp
// ISpellMechanics.cs
public interface ISpellMechanics {
    void ApplyToBattle(BattleEngine battle, BattleUnit caster, BattleUnit target, int spellPower);
    void ApplyToAdventure(GameState gameState, Hero caster);
}

// DamageSpellMechanics.cs
public class DamageSpellMechanics : ISpellMechanics {
    private SpellData spellData;

    public DamageSpellMechanics(SpellData data) {
        spellData = data;
    }

    public void ApplyToBattle(BattleEngine battle, BattleUnit caster, BattleUnit target, int spellPower) {
        var effect = spellData.effects[0];
        int damage = effect.basePower + Mathf.RoundToInt(spellPower * effect.scaling);

        // Apply damage
        target.CurrentHealth -= damage;

        // Broadcast event
        BattleEventChannel.Instance.RaiseSpellCast(spellData, caster, target, damage);
    }

    public void ApplyToAdventure(GameState gameState, Hero caster) {
        // Adventure map spells (town portal, summon boat, etc.)
    }
}
```

---

## 5. Recommended Unity Packages

### 5.1 Essential Packages

| Package | Purpose | Why Essential | Cost |
|---------|---------|---------------|------|
| **DOTween** | Animation tweening | Smooth unit movement, UI transitions, visual polish | Free (Pro $75) |
| **A* Pathfinding Project Pro** | Grid pathfinding | Industry-standard pathfinding for strategy games | $90 |
| **Odin Inspector** | Enhanced Inspector | Better serialization, dictionaries in Inspector, validation | $55 |
| **UniTask** | Async/await for Unity | Modern async patterns, better than coroutines for complex logic | Free |
| **Newtonsoft Json** | JSON serialization | Save/load (Unity JsonUtility doesn't support dictionaries) | Free |

### 5.2 UI Framework Decision

**Option 1: uGUI (Recommended for HOMM3-style UI)**

**Pros:**
- Mature, production-proven
- Excellent tutorial coverage
- World-space UI support (unit health bars)
- Canvas animation integration
- Event system well-documented

**Cons:**
- Older technology
- Performance can degrade with very complex UIs
- Less modern workflow

**Best For:** Traditional strategy game UIs with panels, buttons, hero windows, town screens

**Option 2: UI Toolkit (Future-focused)**

**Pros:**
- HTML/CSS-like workflow (UXML + USS)
- Better performance for complex UIs
- Hot-reload UI changes
- Same system for Editor and Runtime

**Cons:**
- Still maturing (some features missing)
- Fewer tutorials/examples
- No world-space rendering yet
- Steeper learning curve

**Best For:** Modern UI designs, games targeting newer Unity versions

**Recommendation:** Start with **uGUI** for proven reliability and better HOMM3-style UI patterns.

### 5.3 Asset Pipeline

**Addressables System (Recommended):**
```csharp
// Load assets asynchronously
public class AssetLoader : MonoBehaviour {
    public async Task<CreatureData> LoadCreature(int creatureId) {
        var handle = Addressables.LoadAssetAsync<CreatureData>($"Creature_{creatureId}");
        return await handle.Task;
    }

    public async Task<Sprite> LoadSprite(string address) {
        var handle = Addressables.LoadAssetAsync<Sprite>(address);
        return await handle.Task;
    }
}
```

**Benefits over Resources:**
- Automatic memory management
- Remote asset delivery (DLC, patches)
- Smaller initial build size
- Better profiling tools
- Dependency tracking

---

## 6. Project Structure

### 6.1 Recommended Folder Organization

```
Assets/
├── Data/                          # ScriptableObject definitions
│   ├── Creatures/
│   │   ├── Castle/
│   │   │   ├── Peasant.asset
│   │   │   ├── Archer.asset
│   │   │   └── ...
│   │   ├── Rampart/
│   │   └── ...
│   ├── Heroes/
│   │   ├── Knight/
│   │   ├── Wizard/
│   │   └── ...
│   ├── Spells/
│   ├── Buildings/
│   ├── Artifacts/
│   └── EventChannels/          # Event ScriptableObjects
│       ├── BattleEvents.asset
│       ├── GameEvents.asset
│       └── HeroEvents.asset
├── Prefabs/
│   ├── Heroes/
│   ├── Creatures/
│   ├── MapObjects/
│   └── UI/
├── Scenes/
│   ├── Persistent.unity         # GameState (DontDestroyOnLoad)
│   ├── MainMenu.unity
│   ├── AdventureMap.unity
│   ├── Battle.unity
│   └── Town.unity
├── Scripts/
│   ├── Core/                    # Plain C# game logic
│   │   ├── GameState.cs
│   │   ├── Player.cs
│   │   ├── Hero.cs
│   │   ├── Army.cs
│   │   ├── Battle/
│   │   │   ├── BattleEngine.cs
│   │   │   ├── BattleUnit.cs
│   │   │   └── DamageCalculator.cs
│   │   ├── Map/
│   │   │   ├── GameMap.cs
│   │   │   ├── MapTile.cs
│   │   │   └── MapObject.cs
│   │   └── Spells/
│   │       ├── ISpellMechanics.cs
│   │       └── SpellMechanicsImpl.cs
│   ├── Data/                    # ScriptableObject classes
│   │   ├── CreatureData.cs
│   │   ├── HeroTypeData.cs
│   │   ├── SpellData.cs
│   │   ├── BuildingData.cs
│   │   └── EventChannels/
│   │       ├── BattleEventChannel.cs
│   │       └── GameEventChannel.cs
│   ├── Controllers/             # MonoBehaviours
│   │   ├── GameStateManager.cs
│   │   ├── HeroController.cs
│   │   ├── MapRenderer.cs
│   │   ├── BattleController.cs
│   │   └── TownController.cs
│   ├── UI/                      # UI MonoBehaviours
│   │   ├── ResourceBar.cs
│   │   ├── HeroPanel.cs
│   │   ├── BattleUI.cs
│   │   ├── TownWindow.cs
│   │   └── SpellbookWindow.cs
│   ├── Database/                # Database managers
│   │   ├── CreatureDatabase.cs
│   │   ├── HeroDatabase.cs
│   │   └── SpellDatabase.cs
│   └── Utilities/
│       ├── Pathfinding.cs
│       ├── DamageCalculator.cs
│       └── ResourceManager.cs
├── Sprites/
│   ├── Terrain/
│   ├── Creatures/
│   ├── Heroes/
│   ├── UI/
│   └── Effects/
└── Audio/
    ├── Music/
    ├── SFX/
    └── Ambient/
```

### 6.2 Assembly Definitions (Fast Compilation)

```
Scripts/
├── Core.asmdef
│   └── Dependencies: None (pure C# logic)
├── Data.asmdef
│   └── Dependencies: Core
├── Controllers.asmdef
│   └── Dependencies: Core, Data
├── UI.asmdef
│   └── Dependencies: Core, Data, Controllers
└── Editor/
    └── Editor.asmdef (editor-only tools)
```

**Benefits:**
- Only changed assemblies recompile
- Clear dependency graph enforced at compile-time
- Editor scripts don't bloat runtime builds
- ~10x faster iteration times

### 6.3 Namespace Organization

```csharp
namespace RealmsOfEldor.Core {
    public class GameState { }
    public class Hero { }
    public class Army { }
}

namespace RealmsOfEldor.Core.Battle {
    public class BattleEngine { }
    public class BattleUnit { }
}

namespace RealmsOfEldor.Data {
    public class CreatureData : ScriptableObject { }
    public class HeroTypeData : ScriptableObject { }
}

namespace RealmsOfEldor.Controllers {
    public class GameStateManager : MonoBehaviour { }
    public class HeroController : MonoBehaviour { }
}

namespace RealmsOfEldor.UI {
    public class ResourceBar : MonoBehaviour { }
    public class BattleUI : MonoBehaviour { }
}

namespace RealmsOfEldor.Events {
    public class BattleEventChannel : ScriptableObject { }
}
```

---

## 7. Implementation Roadmap

### Phase 1: Foundation (2 weeks)

**Goals:** Core architecture, data structures, basic scene setup

**Tasks:**
- [ ] Create Unity project (2022.3 LTS)
- [ ] Set up folder structure and assembly definitions
- [ ] Create core data types (enums, structs)
- [ ] Implement GameState class (plain C#)
- [ ] Create Hero, Creature, Army classes
- [ ] Set up GameStateManager singleton
- [ ] Create event channel architecture
- [ ] Write unit tests for core logic

**Deliverable:** Testable game state that can create heroes, manage resources, advance turns

### Phase 2: Data Layer (1-2 weeks)

**Goals:** ScriptableObject database, content pipeline

**Tasks:**
- [ ] Create CreatureData ScriptableObject
- [ ] Create HeroTypeData ScriptableObject
- [ ] Create SpellData ScriptableObject
- [ ] Implement database manager singletons
- [ ] Create 5-10 sample creatures (placeholder stats)
- [ ] Create 2-3 sample heroes
- [ ] Set up Addressables (optional but recommended)
- [ ] Create placeholder sprites (64x64 icons)

**Deliverable:** Editable creature/hero database in Unity Inspector

### Phase 3: Map System (2 weeks)

**Goals:** Map rendering, tile system, camera

**Tasks:**
- [ ] Implement GameMap class (plain C#)
- [ ] Create MapTile structure
- [ ] Set up Unity Tilemap for terrain
- [ ] Create/import terrain tiles (128x128)
- [ ] Implement MapRenderer MonoBehaviour
- [ ] Create camera controller (pan, zoom)
- [ ] Implement map object placement
- [ ] Add pathfinding integration (A* Pathfinding Project)

**Deliverable:** Scrollable, zoomable map with terrain variety

### Phase 4: Adventure Map UI (2 weeks)

**Goals:** Functional adventure map interface

**Tasks:**
- [ ] Create resource bar UI (uGUI)
- [ ] Create hero panel UI
- [ ] Implement hero selection system
- [ ] Add hero movement via mouse clicks
- [ ] Create turn button and day counter
- [ ] Wire up event channels for UI updates
- [ ] Add keyboard shortcuts (arrows, space, tab)
- [ ] Implement fog of war (optional)

**Deliverable:** Playable adventure map with hero movement and UI

### Phase 5: Battle System (3 weeks)

**Goals:** Complete battle mechanics and UI

**Tasks:**
- [ ] Implement BattleEngine logic
- [ ] Create BattleUnit class
- [ ] Port damage calculation from C++ version
- [ ] Create battle UI scene
- [ ] Implement battlefield rendering
- [ ] Add battle unit visuals
- [ ] Create battle animations (DOTween)
- [ ] Add battle log display
- [ ] Implement auto-battle AI
- [ ] Wire up victory/defeat handling
- [ ] Update hero armies after battle

**Deliverable:** Functional auto-battle with visual feedback

### Phase 6: Town System (2-3 weeks)

**Goals:** Basic town management

**Tasks:**
- [ ] Create Town class
- [ ] Implement building system
- [ ] Create town window UI
- [ ] Add creature recruitment interface
- [ ] Implement daily creature growth
- [ ] Add resource costs for recruitment
- [ ] Create placeholder town visuals
- [ ] Integrate towns with adventure map

**Deliverable:** Towns where heroes can recruit creatures

### Phase 7: Spell System (2 weeks)

**Goals:** Spellbook and magic mechanics

**Tasks:**
- [ ] Create SpellData definitions
- [ ] Implement spell mechanics (Strategy pattern)
- [ ] Create spellbook UI
- [ ] Add spell casting in battle
- [ ] Add adventure map spells
- [ ] Create spell visual effects
- [ ] Wire up mana costs and restrictions

**Deliverable:** Functional spell system in and out of combat

### Phase 8: Polish & Features (2-3 weeks)

**Goals:** Game feel and completeness

**Tasks:**
- [ ] Save/load system (JSON serialization)
- [ ] Main menu with new game / load game
- [ ] Victory/defeat conditions
- [ ] Basic AI opponent
- [ ] Sound effects integration
- [ ] Background music
- [ ] Particle effects for spells/combat
- [ ] Tutorial or first scenario
- [ ] Balance pass on creatures/spells

**Deliverable:** Complete, playable game

### Total Timeline: 14-18 weeks (3.5-4.5 months)

**Milestones:**
- **Week 4:** Core systems tested and working
- **Week 8:** Adventure map playable
- **Week 11:** Battles functional
- **Week 14:** Towns implemented
- **Week 16:** Spells working
- **Week 18:** MVP complete

---

## 8. Performance Considerations

### 8.1 Common Unity Performance Patterns

**Object Pooling (for battle units, damage numbers):**
```csharp
public class ObjectPool<T> where T : MonoBehaviour {
    private T prefab;
    private Queue<T> pool = new Queue<T>();

    public ObjectPool(T prefab, int initialSize) {
        this.prefab = prefab;
        for (int i = 0; i < initialSize; i++) {
            T obj = GameObject.Instantiate(prefab);
            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    public T Get() {
        if (pool.Count > 0) {
            T obj = pool.Dequeue();
            obj.gameObject.SetActive(true);
            return obj;
        }
        return GameObject.Instantiate(prefab);
    }

    public void Return(T obj) {
        obj.gameObject.SetActive(false);
        pool.Enqueue(obj);
    }
}
```

**Avoid Update() for static objects:**
```csharp
// Bad: Update() called every frame even if nothing changes
public class StaticUI : MonoBehaviour {
    void Update() {
        resourceText.text = gameState.Gold.ToString();
    }
}

// Good: Event-driven updates
public class StaticUI : MonoBehaviour {
    void OnEnable() {
        GameEvents.Instance.OnResourcesChanged += UpdateResourceDisplay;
    }

    void UpdateResourceDisplay(Player player) {
        resourceText.text = player.Gold.ToString();
    }
}
```

**Cache Component references:**
```csharp
// Bad: GetComponent every frame
void Update() {
    GetComponent<Animator>().SetBool("Moving", isMoving);
}

// Good: Cache in Start/Awake
private Animator animator;
void Start() {
    animator = GetComponent<Animator>();
}
void Update() {
    animator.SetBool("Moving", isMoving);
}
```

### 8.2 Tilemap Optimization

Unity's Tilemap is highly optimized:
- Entire tilemap rendered in 1 draw call
- Automatic chunking for large maps
- Built-in frustum culling
- Use TilemapRenderer's "Chunk Mode" for best performance

### 8.3 Recommended Unity Settings

**Player Settings:**
- Scripting Backend: IL2CPP (production builds)
- API Compatibility: .NET Standard 2.1
- Managed Stripping Level: Medium
- Enable "Script Compilation" → "Assembly Definitions"

**Quality Settings:**
- VSync: On (for consistent 60 FPS)
- Anti-Aliasing: 2x or 4x (for clean sprite edges)
- Shadow Quality: Medium (or disable for 2D)

**Project Settings:**
- Color Space: Linear (better blending)
- Physics 2D: Disable if not using (saves CPU)
- Input System: New Input System (better for multiple control schemes)

---

## 9. Testing Strategy

### 9.1 Unit Testing Core Logic

Unity's Test Framework (NUnit):
```csharp
using NUnit.Framework;

[TestFixture]
public class HeroTests {
    [Test]
    public void Hero_GainExperience_LevelsUp() {
        var hero = new Hero {
            Experience = 0,
            Level = 1
        };

        hero.GainExperience(1000);

        Assert.AreEqual(2, hero.Level);
        Assert.GreaterOrEqual(hero.Attack, 1);
    }

    [Test]
    public void Army_AddCreatures_FillsSlots() {
        var army = new Army();
        bool success = army.AddCreatures(creatureId: 1, count: 10);

        Assert.IsTrue(success);
        Assert.AreEqual(10, army.GetSlot(0).Count);
    }

    [Test]
    public void GameMap_IsPassable_RetectsBlockedTiles() {
        var map = new GameMap(10, 10);
        map.GetTile(new Vector2Int(5, 5)).Passable = false;

        Assert.IsFalse(map.IsPassable(new Vector2Int(5, 5)));
        Assert.IsTrue(map.IsPassable(new Vector2Int(4, 5)));
    }
}
```

### 9.2 Integration Testing

```csharp
using UnityEngine.TestTools;

[UnityTest]
public IEnumerator Battle_PlayerWins_AwardsExperience() {
    // Arrange
    var hero = CreateTestHero();
    var weakEnemy = CreateWeakEnemy();
    int startExp = hero.Experience;

    var battleEngine = new BattleEngine(eventChannel);
    battleEngine.InitializeBattle(hero, weakEnemy);

    // Act
    yield return battleEngine.ExecuteAutoBattle();

    // Assert
    Assert.Greater(hero.Experience, startExp);
}
```

### 9.3 Play Mode Testing

Create test scenes:
- `Test_HeroMovement.unity` - Hero pathfinding and movement
- `Test_Battle.unity` - Battle system with preset armies
- `Test_TownRecruitment.unity` - Creature recruitment flow

---

## 10. References

### 10.1 VCMI Resources

- **VCMI GitHub:** https://github.com/vcmi/vcmi
- **VCMI Wiki:** https://wiki.vcmi.eu/
- **Key files analyzed:**
  - `lib/gameState/CGameState.h` - Game state management
  - `lib/entities/hero/CHero.h` & `lib/mapObjects/CGHeroInstance.h` - Hero system
  - `lib/battle/BattleInfo.h` - Battle mechanics
  - `lib/mapping/CMap.h` - Map structure
  - `lib/CCreatureHandler.h` - Handler pattern example
  - `client/CPlayerInterface.h` - Observer pattern example

### 10.2 Unity Documentation

- **Unity Manual:** https://docs.unity3d.com/Manual/
- **ScriptableObjects Guide:** https://unity.com/how-to/separate-game-data-logic-scriptable-objects
- **Tilemap:** https://docs.unity3d.com/Manual/class-Tilemap.html
- **Event System:** https://docs.unity3d.com/Manual/EventSystem.html
- **Addressables:** https://docs.unity3d.com/Packages/com.unity.addressables@latest

### 10.3 Unity Asset Store

- **DOTween:** https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676
- **A* Pathfinding Project:** https://arongranberg.com/astar/
- **Odin Inspector:** https://assetstore.unity.com/packages/tools/utilities/odin-inspector-and-serializer-89041

### 10.4 Turn-Based Strategy Resources

- **GameDev.tv Turn-Based Strategy Course:** https://www.gamedev.tv/courses/unity-turn-based-strategy
- **Code Monkey Unity Tutorials:** https://www.youtube.com/@CodeMonkeyUnity
- **Sebastian Lague Coding Adventures:** https://www.youtube.com/@SebastianLague

### 10.5 Free Asset Resources

- **OpenGameArt:** https://opengameart.org
- **Kenney.nl:** https://kenney.nl (UI, sprites, sounds)
- **itch.io Free Assets:** https://itch.io/game-assets/free

---

## Conclusion

This research demonstrates that VCMI's proven architecture translates exceptionally well to Unity/C#. The key principles are:

1. **Maintain VCMI's separation of concerns:** Game logic (plain C#) separate from presentation (MonoBehaviours)
2. **Leverage Unity's strengths:** ScriptableObjects for data, Tilemap for maps, event channels for decoupling
3. **Use hybrid architecture:** Plain C# for logic testability, MonoBehaviours where Unity features needed
4. **Follow proven patterns:** VCMI's 15+ years of HOMM3 recreation provides battle-tested design decisions

**Estimated development time:** 14-18 weeks for feature parity with the C++ prototype, after which Unity's tooling will accelerate further development.

The C++ prototype validated the game design. Unity implementation will provide:
- 3-5x faster feature development
- Cross-platform deployment (mobile, web, console)
- Better asset pipeline and tooling
- Larger developer community and resources
- Professional-quality UI systems out of the box

**Recommendation:** Proceed with Unity implementation using the architecture outlined in this document.
