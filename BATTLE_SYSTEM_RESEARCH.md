# VCMI Battle System Research - Phase 5 Implementation Guide

**Research Date**: 2025-10-05
**Source**: /tmp/vcmi-temp (VCMI GitHub repository)
**Purpose**: Complete battle system implementation for Realms of Eldor

---

## Table of Contents

1. [Battle System Architecture](#1-battle-system-architecture)
2. [Battlefield Layout & Positioning](#2-battlefield-layout--positioning)
3. [Battle Mechanics](#3-battle-mechanics)
4. [Battle Actions & Flow](#4-battle-actions--flow)
5. [Battle AI](#5-battle-ai)
6. [Unity C# Translation](#6-unity-c-translation)
7. [Implementation Roadmap](#7-implementation-roadmap)

---

## 1. Battle System Architecture

### Core Classes Hierarchy

```
BattleInfo (Main Battle State)
├── CBattleInfoCallback (Query Interface)
├── IBattleState (State Modification Interface)
├── CBonusSystemNode (Bonus/Effect System)
└── GameCallbackHolder (Game Integration)
```

### Key Files Analyzed

- **`/tmp/vcmi-temp/lib/battle/BattleInfo.h`** - Main battle state container
- **`/tmp/vcmi-temp/lib/battle/BattleAction.h`** - Action definitions
- **`/tmp/vcmi-temp/lib/battle/Unit.h`** - Unit interface
- **`/tmp/vcmi-temp/lib/battle/CUnitState.h`** - Unit state implementation
- **`/tmp/vcmi-temp/lib/battle/DamageCalculator.h/cpp`** - Damage calculation
- **`/tmp/vcmi-temp/server/battles/BattleFlowProcessor.h`** - Battle flow control
- **`/tmp/vcmi-temp/server/battles/BattleActionProcessor.h`** - Action execution

### BattleInfo - Main Battle State

**Location**: `/tmp/vcmi-temp/lib/battle/BattleInfo.h:30-173`

```cpp
class BattleInfo : public CBonusSystemNode, public CBattleInfoCallback,
                   public IBattleState, public GameCallbackHolder
{
    BattleSideArray<SideInBattle> sides;  // sides[0]=attacker, sides[1]=defender
    std::unique_ptr<BattleLayout> layout;

    BattleID battleID;
    si32 round;                           // Current round number
    si32 activeStack;                     // ID of currently active unit
    ObjectInstanceID townID;              // For siege battles
    int3 tile;                            // Map position (for terrain bonuses)

    std::vector<std::unique_ptr<CStack>> stacks;  // All battle units
    std::vector<std::shared_ptr<CObstacleInstance>> obstacles;
    SiegeInfo si;                         // Siege-specific data (walls, towers)

    BattleField battlefieldType;          // Terrain battlefield type
    TerrainId terrainType;                // For terrain-based bonuses

    BattleSide tacticsSide;               // Which side is in tactics phase
    ui8 tacticDistance;                   // How many hexes can move forward in tactics
};
```

**Key Responsibilities**:
- State container for entire battle
- Unit management (add/remove/update)
- Obstacle management
- Round/turn tracking
- Siege state (if applicable)

### SideInBattle - Per-Side Data

**Location**: `/tmp/vcmi-temp/lib/battle/SideInBattle.h`

```cpp
class SideInBattle
{
    PlayerColor color;
    const CGHeroInstance * hero;
    const CArmedInstance * armyObject;
    ui8 castSpellsCount;      // Spells cast this round
    si32 enchanterCounter;    // For enchanter ability
    std::set<SpellID> usedSpells;
};
```

### Unit Representation

**battle::Unit** (Interface) - `/tmp/vcmi-temp/lib/battle/Unit.h:65-167`

```cpp
class Unit : public IUnitInfo, public spells::Caster, public virtual IBonusBearer
{
    // Identity
    virtual CreatureID creatureId() const = 0;
    virtual int32_t creatureLevel() const = 0;
    virtual bool doubleWide() const = 0;

    // State queries
    virtual bool alive() const = 0;
    virtual bool isGhost() const = 0;
    virtual bool ableToRetaliate() const = 0;
    virtual bool canShoot() const = 0;
    virtual bool canCast() const = 0;

    // Stats
    virtual int32_t getCount() const = 0;           // Number of creatures
    virtual int32_t getFirstHPleft() const = 0;     // HP of first creature
    virtual int64_t getAvailableHealth() const = 0; // Total remaining HP
    virtual int getTotalAttacks(bool ranged) const = 0;

    // Position & Movement
    virtual BattleHex getPosition() const = 0;
    virtual bool canMove(int turn = 0) const = 0;
    virtual bool moved(int turn = 0) const = 0;
    virtual bool waited(int turn = 0) const = 0;
    virtual bool defended(int turn = 0) const = 0;

    // Combat
    virtual int getMinDamage(bool ranged) const = 0;
    virtual int getMaxDamage(bool ranged) const = 0;
    virtual int getAttack(bool ranged) const = 0;
    virtual int getDefense(bool ranged) const = 0;
    virtual int32_t getInitiative(int turn = 0) const = 0;
};
```

**CUnitState** (Implementation) - `/tmp/vcmi-temp/lib/battle/CUnitState.h:128-273`

```cpp
class CUnitState : public Unit
{
    // State flags
    bool cloned;
    bool defending;
    bool ghost;
    bool hadMorale;
    bool movedThisRound;
    bool summoned;
    bool waiting;
    bool waitedThisTurn;

    // Resources
    CCasts casts;               // Spell casts available
    CRetaliations counterAttacks; // Retaliations remaining
    CHealth health;             // HP management
    CShots shots;               // Ammunition for shooters

    si32 cloneID;               // ID of clone if this unit has one
    BattleHex position;         // Current hex
};
```

---

## 2. Battlefield Layout & Positioning

### Hex Grid System

**Dimensions**: `/tmp/vcmi-temp/lib/battle/BattleHex.h:18-23`

```cpp
namespace GameConstants
{
    const int BFIELD_WIDTH = 17;   // 17 columns (0-16)
    const int BFIELD_HEIGHT = 11;  // 11 rows (0-10)
    const int BFIELD_SIZE = 187;   // Total hexes (17 * 11)
}
```

**Hex Coordinate System**:
- Single integer index: `hex = x + y * BFIELD_WIDTH`
- Range: 0-186
- Invalid hexes: -1, -2 (castle towers), etc.
- Edge columns (0, 16) are inaccessible for units

### BattleHex Class

**Location**: `/tmp/vcmi-temp/lib/battle/BattleHex.h:33-294`

```cpp
class BattleHex
{
    si16 hex;  // Internal storage

    // Special constants
    static constexpr si16 INVALID = -1;
    static constexpr si16 CASTLE_CENTRAL_TOWER = -2;
    static constexpr si16 CASTLE_BOTTOM_TOWER = -3;
    static constexpr si16 CASTLE_UPPER_TOWER = -4;
    static constexpr si16 HERO_ATTACKER = 0;
    static constexpr si16 HERO_DEFENDER = 16;

    // Directions for hex navigation
    enum EDir {
        NONE = -1,
        TOP_LEFT, TOP_RIGHT, RIGHT,
        BOTTOM_RIGHT, BOTTOM_LEFT, LEFT
    };

    // Coordinate conversion
    si16 getX() const { return hex % BFIELD_WIDTH; }
    si16 getY() const { return hex / BFIELD_WIDTH; }
    void setXY(si16 x, si16 y);

    // Navigation
    BattleHex cloneInDirection(EDir dir) const;
    const BattleHexArray& getNeighbouringTiles() const;
    const BattleHexArray& getNeighbouringTilesDoubleWide(BattleSide side) const;

    // Distance calculation (hexagonal)
    static uint8_t getDistance(const BattleHex& hex1, const BattleHex& hex2);
};
```

### Distance Calculation (Hexagonal Grid)

**Location**: `/tmp/vcmi-temp/lib/battle/BattleHex.h:187-202`

```cpp
static uint8_t getDistance(const BattleHex& hex1, const BattleHex& hex2)
{
    // Convert to axial coordinates
    int y1 = hex1.getY();
    int y2 = hex2.getY();
    int x1 = hex1.getX() + y1 / 2;  // Offset for odd rows
    int x2 = hex2.getX() + y2 / 2;

    int xDst = x2 - x1;
    int yDst = y2 - y1;

    // Hexagonal distance formula
    if ((xDst >= 0 && yDst >= 0) || (xDst < 0 && yDst < 0))
        return std::max(std::abs(xDst), std::abs(yDst));

    return std::abs(xDst) + std::abs(yDst);
}
```

### Hex Navigation Example

```cpp
// Moving in directions (handles odd/even row offsets automatically)
BattleHex current(50);  // Some hex
BattleHex topRight = current.cloneInDirection(BattleHex::TOP_RIGHT);
BattleHex right = current.cloneInDirection(BattleHex::RIGHT);

// Get all 6 neighbors
const BattleHexArray& neighbors = current.getNeighbouringTiles();

// For double-wide units (e.g., Dragons)
const BattleHexArray& neighbors2hex = current.getNeighbouringTilesDoubleWide(BattleSide::ATTACKER);
```

### Accessibility System

**Location**: `/tmp/vcmi-temp/lib/battle/AccessibilityInfo.h:22-46`

```cpp
enum class EAccessibility
{
    ACCESSIBLE,           // Can move here
    ALIVE_STACK,          // Occupied by unit
    OBSTACLE,             // Blocked by obstacle
    DESTRUCTIBLE_WALL,    // Siege wall (can be destroyed)
    GATE,                 // Gate (defender can pass)
    UNAVAILABLE,          // Permanently blocked
    SIDE_COLUMN          // Edge columns (war machines only)
};

using TAccessibilityArray = std::array<EAccessibility, BFIELD_SIZE>;

struct AccessibilityInfo : TAccessibilityArray
{
    bool accessible(const BattleHex& tile, const battle::Unit* stack) const;
    bool accessible(const BattleHex& tile, bool doubleWide, BattleSide side) const;
};
```

### Reachability System (BFS Pathfinding)

**Location**: `/tmp/vcmi-temp/lib/battle/ReachabilityInfo.h:21-65`

```cpp
struct ReachabilityInfo
{
    using TDistances = std::array<uint32_t, BFIELD_SIZE>;
    using TPredecessors = std::array<BattleHex, BFIELD_SIZE>;

    static constexpr int INFINITE_DIST = 1000000;

    struct Parameters {
        BattleSide side;
        bool doubleWide;
        bool flying;
        bool ignoreKnownAccessible;
        BattleHex startPosition;
        BattleSide perspective;  // For hidden obstacles
    };

    AccessibilityInfo accessibility;
    TDistances distances;       // Distance from start to each hex
    TPredecessors predecessors; // For path reconstruction

    bool isReachable(const BattleHex& hex) const {
        return distances[hex.toInt()] < INFINITE_DIST;
    }
};
```

### Battlefield Layout Documentation

**Source**: `/tmp/vcmi-temp/docs/developers/Battlefield.md`

Hex types:
- **Gray (0, 16, 17...)**: Inaccessible (war machine positions)
- **Green (1, 15, 35...)**: Starting positions for units
- **Yellow (18, 32, 52...)**: War machine starting positions
- **Dark Red**: Non-destructible walls (siege)
- **Light Red (29, 78, 130, 182)**: Destructible walls (siege)
- **Blue (11, 28, 44...)**: Moat positions (siege)
- **Pink (94)**: Drawbridge hex
- **Purple (95, 96)**: Gatehouse hexes (defender access only)

---

## 3. Battle Mechanics

### Turn Order System (Initiative)

**Location**: `/tmp/vcmi-temp/lib/battle/BattleInfo.cpp:971-1008`

```cpp
// CMP_stack - Comparator for turn queue
bool CMP_stack::operator()(const battle::Unit* a, const battle::Unit* b) const
{
    switch(phase)
    {
    case 0:  // SIEGE phase (catapult after turrets)
        return a->creatureIndex() > b->creatureIndex();

    case 1:  // NORMAL phase
    case 2:  // WAIT_MORALE phase
    case 3:  // WAIT phase
    {
        // Primary sort: Initiative (speed)
        int as = a->getInitiative(turn);
        int bs = b->getInitiative(turn);
        if (as != bs)
            return as > bs;  // Higher initiative goes first

        // Tie-breaker 1: Same side → slot order
        if (a->unitSide() == b->unitSide())
            return a->unitSlot() < b->unitSlot();

        // Tie-breaker 2: Different sides → side that started turn
        return (a->unitSide() == side || b->unitSide() == side)
            ? a->unitSide() != side
            : a->unitSide() < b->unitSide();
    }
    }
}
```

**Battle Phases** (from `/tmp/vcmi-temp/lib/battle/Unit.h:32-42`):
```cpp
namespace BattlePhases
{
    enum Type {
        SIEGE,       // Turrets/catapult
        NORMAL,      // Normal units
        WAIT_MORALE, // Units that waited and got good morale
        WAIT,        // Units that waited (without morale)
        NUMBER_OF_PHASES
    };
}
```

**Initiative Calculation** (`/tmp/vcmi-temp/lib/battle/CUnitState.cpp:590-593`):
```cpp
int32_t CUnitState::getInitiative(int turn) const
{
    return stackSpeedPerTurn.getValue(turn);
    // Base speed + bonuses - penalties (morale affects this)
}
```

### Damage Calculation System

**Location**: `/tmp/vcmi-temp/lib/battle/DamageCalculator.cpp`

#### Core Damage Formula

```cpp
DamageEstimation DamageCalculator::calculateDmgRange() const
{
    // 1. Base damage = creature damage * stack size
    DamageRange damageBase = getBaseDamageStack();

    // 2. Attack factors (multiplicative bonuses)
    auto attackFactors = getAttackFactors();  // Returns vector of multipliers
    auto defenseFactors = getDefenseFactors();

    double attackFactorTotal = 1.0;
    for (auto& factor : attackFactors)
        attackFactorTotal += factor;  // Additive combination

    // 3. Defense factors (multiplicative penalties)
    double defenseFactorTotal = 1.0;
    for (auto& factor : defenseFactors)
        defenseFactorTotal *= (1 - std::min(1.0, factor));

    // 4. Final damage = base * attack * defense
    double resultingFactor = attackFactorTotal * defenseFactorTotal;

    DamageRange damageDealt {
        std::max<int64_t>(1, std::floor(damageBase.min * resultingFactor)),
        std::max<int64_t>(1, std::floor(damageBase.max * resultingFactor))
    };

    // 5. Calculate casualties
    DamageRange killsDealt = getCasualties(damageDealt);

    return DamageEstimation{damageDealt, killsDealt};
}
```

#### Base Damage Calculation

```cpp
DamageRange getBaseDamageSingle() const
{
    int64_t minDmg = info.attacker->getMinDamage(info.shooting);
    int64_t maxDmg = info.attacker->getMaxDamage(info.shooting);

    // Special handling for towers, ballista, etc.
    // ...

    return {minDmg, maxDmg};
}

DamageRange getBaseDamageBlessCurse() const
{
    auto baseDamage = getBaseDamageSingle();

    // Curse: Always minimum damage
    if (hasCurse)
        return {baseDamage.min, baseDamage.min};

    // Bless: Always maximum damage
    if (hasBless)
        return {baseDamage.max, baseDamage.max};

    return baseDamage;
}

DamageRange getBaseDamageStack() const
{
    auto stackSize = info.attacker->getCount();
    auto baseDamage = getBaseDamageBlessCurse();
    return {
        baseDamage.min * stackSize,
        baseDamage.max * stackSize
    };
}
```

#### Attack Factors (Bonuses)

**Location**: `/tmp/vcmi-temp/lib/battle/DamageCalculator.cpp:467-480`

```cpp
std::vector<double> getAttackFactors() const
{
    return {
        getAttackSkillFactor(),        // Attack vs Defense
        getAttackOffenseArcheryFactor(), // Offense/Archery skill
        getAttackBlessFactor(),        // Bless/Curse spell
        getAttackLuckFactor(),         // Lucky strike (+100%)
        getAttackJoustingFactor(),     // Jousting (distance * bonus)
        getAttackFromBackFactor(),     // Attacking from behind
        getAttackDeathBlowFactor(),    // Death blow (+100%)
        getAttackDoubleDamageFactor(), // Specialty bonus
        getAttackHateFactor(),         // Hatred bonus
        getAttackRevengeFactor()       // Revenge ability (Haspid)
    };
}
```

**Attack Skill Factor** (Attack vs Defense):
```cpp
double getAttackSkillFactor() const
{
    int attackAdvantage = getActorAttackEffective() - getTargetDefenseEffective();

    if (attackAdvantage > 0)
    {
        const double attackMultiplier = 0.05;  // 5% per point
        const double attackMultiplierCap = 3.0; // Max 300% (60 point advantage)
        return std::min(attackMultiplier * attackAdvantage, attackMultiplierCap);
    }
    return 0.0;
}
```

**Luck Factor**:
```cpp
double getAttackLuckFactor() const
{
    if (info.luckyStrike)
        return 1.0;  // +100% damage
    return 0.0;
}
```

#### Defense Factors (Penalties)

**Location**: `/tmp/vcmi-temp/lib/battle/DamageCalculator.cpp:483-497`

```cpp
std::vector<double> getDefenseFactors() const
{
    return {
        getDefenseSkillFactor(),           // Defense vs Attack
        getDefenseArmorerFactor(),         // Armorer skill
        getDefenseMagicShieldFactor(),     // Shield/Air Shield spell
        getDefenseRangePenaltiesFactor(),  // Range/melee penalties
        getDefenseObstacleFactor(),        // Shooting through walls
        getDefenseBlindParalysisFactor(),  // Blind/Paralyze effect
        getDefenseUnluckyFactor(),         // Unlucky strike (-50%)
        getDefenseForgetfulnessFactor(),   // Forgetfulness spell
        getDefensePetrificationFactor(),   // Petrify effect (-50%)
        getDefenseMagicFactor(),           // Magic resistance
        getDefenseMindFactor()             // Mind immunity
    };
}
```

**Defense Skill Factor**:
```cpp
double getDefenseSkillFactor() const
{
    int defenseAdvantage = getTargetDefenseEffective() - getActorAttackEffective();

    if (defenseAdvantage > 0)
    {
        const double defenseMultiplier = 0.025; // 2.5% per point
        const double defenseMultiplierCap = 0.7; // Max 70% reduction
        return std::min(defenseMultiplier * defenseAdvantage, defenseMultiplierCap);
    }
    return 0.0;
}
```

**Range Penalty Factor**:
```cpp
double getDefenseRangePenaltiesFactor() const
{
    if (info.shooting)
    {
        bool distPenalty = battleHasDistancePenalty(attacker, attackerPos, defenderPos);
        if (distPenalty)
            return 0.5;  // -50% damage at long range
    }
    else  // Melee
    {
        if (info.attacker->isShooter() && !hasNoMeleePenalty)
            return 0.5;  // Shooters deal -50% in melee
    }
    return 0.0;
}
```

#### Casualty Calculation

```cpp
int64_t getCasualties(int64_t damageDealt) const
{
    // If damage doesn't kill first creature
    if (damageDealt < info.defender->getFirstHPleft())
        return 0;

    // Calculate how many creatures die
    int64_t damageLeft = damageDealt - info.defender->getFirstHPleft();
    int64_t killsLeft = damageLeft / info.defender->getMaxHealth();

    return std::min<int32_t>(1 + killsLeft, info.defender->getCount());
}
```

### Special Mechanics

#### Morale System

**Good Morale** (from `/tmp/vcmi-temp/server/battles/BattleFlowProcessor.cpp`):
- Triggers at start of unit's turn
- Chance based on morale level (-3 to +3)
- Effect: Unit gets an extra action immediately
- Cannot trigger twice in a row (tracked via `hadMorale` flag)

**Bad Morale**:
- Triggers at start of turn
- Chance based on negative morale
- Effect: Unit skips its turn entirely
- Action type: `EActionType::BAD_MORALE`

#### Retaliation System

**Location**: `/tmp/vcmi-temp/lib/battle/CUnitState.h:74-89`

```cpp
class CRetaliations : public CAmmo
{
    int32_t total() const override;
    void reset() override;  // Resets to max at start of turn

    // Unlimited retaliation: Some creatures can counter-attack infinitely
    // Limited retaliation: Most units have 1 retaliation per turn
    // No retaliation: Some effects disable counter-attacks
};
```

**Retaliation Flow**:
1. Attacker deals melee damage to defender
2. Check if defender `ableToRetaliate()`
3. If yes: Defender immediately attacks back
4. Decrement defender's retaliation counter
5. Attacker can also retaliate to the retaliation (if able)

#### Wait Mechanism

Units can **wait** during their turn:
- Unit is moved to a later phase (`WAIT` or `WAIT_MORALE`)
- Acts after all normal-phase units
- If unit gets good morale while waiting → `WAIT_MORALE` phase (before regular `WAIT`)
- Can only wait once per round

---

## 4. Battle Actions & Flow

### Action Types

**Location**: `/tmp/vcmi-temp/lib/constants/Enumerations.h:138-157`

```cpp
enum class EActionType : int8_t
{
    NO_ACTION,

    END_TACTIC_PHASE,  // End tactics deployment
    RETREAT,           // Flee from battle
    SURRENDER,         // Surrender to opponent

    HERO_SPELL,        // Hero casts spell

    WALK,              // Move without attacking
    WAIT,              // Delay action to later phase
    DEFEND,            // +defense, skip turn
    WALK_AND_ATTACK,   // Move + melee attack
    SHOOT,             // Ranged attack
    CATAPULT,          // Catapult attack (siege)
    MONSTER_SPELL,     // Creature casts spell
    BAD_MORALE,        // Skip turn due to bad morale
    STACK_HEAL,        // Healing tent action
};
```

### BattleAction Structure

**Location**: `/tmp/vcmi-temp/lib/battle/BattleAction.h:24-81`

```cpp
class BattleAction
{
    BattleSide side;           // Who made this action
    ui32 stackNumber;          // Stack ID (-1=left hero, -2=right hero)
    EActionType actionType;
    SpellID spell;             // If casting spell

    std::vector<DestinationInfo> target;  // Can have multiple targets

    // Factory methods
    static BattleAction makeDefend(const battle::Unit* stack);
    static BattleAction makeWait(const battle::Unit* stack);
    static BattleAction makeMeleeAttack(const battle::Unit* stack,
                                        const BattleHex& destination,
                                        const BattleHex& attackFrom,
                                        bool returnAfterAttack = true);
    static BattleAction makeShotAttack(const battle::Unit* shooter,
                                       const battle::Unit* target);
    static BattleAction makeMove(const battle::Unit* stack,
                                 const BattleHex& dest);
};
```

### Battle Flow System

**Location**: `/tmp/vcmi-temp/server/battles/BattleFlowProcessor.h:32-69`

**Flow Controller Responsibilities**:
1. **Battle Start**: Initialize sides, place obstacles, summon guardians
2. **Tactics Phase**: Allow hero to reposition units
3. **Round Start**: Reset unit states, check for new round effects
4. **Stack Activation**: Trigger morale, determine next active unit
5. **Action Processing**: Execute player/AI action
6. **Turn End**: Update cooldowns, check win condition
7. **Battle End**: Calculate rewards, apply casualties

**Key Methods**:

```cpp
class BattleFlowProcessor
{
    // Initialization
    void onBattleStarted(const CBattleInfoCallback& battle);
    void onTacticsEnded(const CBattleInfoCallback& battle);

    // Turn management
    void activateNextStack(const CBattleInfoCallback& battle);
    void startNextRound(const CBattleInfoCallback& battle, bool isFirstRound);

    // Automatic actions
    bool tryMakeAutomaticAction(const CBattleInfoCallback& battle, const CStack* stack);
    bool tryActivateMoralePenalty(const CBattleInfoCallback& battle, const CStack* stack);
    bool rollGoodMorale(const CBattleInfoCallback& battle, const CStack* stack);

    // Siege-specific
    void tryPlaceMoats(const CBattleInfoCallback& battle);
    void castOpeningSpells(const CBattleInfoCallback& battle);
    void trySummonGuardians(const CBattleInfoCallback& battle, const CStack* stack);

    // Response to actions
    void onActionMade(const CBattleInfoCallback& battle, const BattleAction& ba);
};
```

### Action Processing

**Location**: `/tmp/vcmi-temp/server/battles/BattleActionProcessor.h:37-99`

```cpp
class BattleActionProcessor
{
    // Main dispatcher
    bool makeBattleActionImpl(const CBattleInfoCallback& battle, const BattleAction& ba);
    bool dispatchBattleAction(const CBattleInfoCallback& battle, const BattleAction& ba);

    // Action handlers
    bool doWalkAction(const CBattleInfoCallback& battle, const BattleAction& ba);
    bool doWaitAction(const CBattleInfoCallback& battle, const BattleAction& ba);
    bool doDefendAction(const CBattleInfoCallback& battle, const BattleAction& ba);
    bool doAttackAction(const CBattleInfoCallback& battle, const BattleAction& ba);
    bool doShootAction(const CBattleInfoCallback& battle, const BattleAction& ba);
    bool doHeroSpellAction(const CBattleInfoCallback& battle, const BattleAction& ba);
    bool doRetreatAction(const CBattleInfoCallback& battle, const BattleAction& ba);
    bool doSurrenderAction(const CBattleInfoCallback& battle, const BattleAction& ba);

    // Attack execution
    void makeAttack(const CBattleInfoCallback& battle,
                    const CStack* attacker,
                    const CStack* defender,
                    int distance,
                    const BattleHex& targetHex,
                    bool first,      // First attack in sequence
                    bool ranged,
                    bool counter);   // Is this a counter-attack?

    // Movement
    MovementResult moveStack(const CBattleInfoCallback& battle, int stack, BattleHex dest);
};
```

### Attack Sequence Example

```cpp
// Player clicks enemy unit to attack
1. BattleAction action = BattleAction::makeMeleeAttack(attacker, dest, attackFrom);
2. BattleActionProcessor::doAttackAction(action)
   a. Move attacker to attackFrom hex
   b. makeAttack(attacker, defender, distance, targetHex, first=true, ranged=false, counter=false)
      - Calculate damage using DamageCalculator
      - Apply damage to defender
      - Trigger on-attack effects (poison, paralysis, etc.)
      - If defender alive and can retaliate:
        * makeAttack(defender, attacker, 0, attackerHex, first=false, ranged=false, counter=true)
      - If attacker survives and has double-attack:
        * makeAttack(attacker, defender, ..., first=false, ...)
   c. Move attacker back to original hex (if returnAfterAttack)
3. BattleFlowProcessor::onActionMade()
   a. Check if defender died → trigger death effects
   b. Update UI
   c. Activate next stack
```

### Turn Queue Management

**Turn Queue Construction** (from `/tmp/vcmi-temp/lib/battle/CBattleInfoCallback.cpp`):

```cpp
void battleGetTurnOrder(std::vector<battle::Units>& out,
                        const size_t maxUnits,
                        const int maxTurns,
                        const int turn = 0,
                        BattleSide lastMoved = BattleSide::NONE) const
{
    // Simulate future turns to show turn queue UI
    for (int turnNr = 0; turnNr < maxTurns; ++turnNr)
    {
        battle::Units turnQueue;

        for (auto* unit : battleAliveUnits())
        {
            if (unit->willMove(turnNr))
                turnQueue.push_back(unit);
        }

        // Sort by initiative
        std::sort(turnQueue.begin(), turnQueue.end(),
                  CMP_stack(BattlePhases::NORMAL, turnNr, lastMoved));

        out.push_back(turnQueue);
        if (out.size() >= maxUnits)
            break;
    }
}
```

---

## 5. Battle AI

### AI Architecture

**Location**: `/tmp/vcmi-temp/AI/BattleAI/`

**Core Classes**:
- **`BattleAI.h`** - Main AI controller, receives events
- **`BattleEvaluator.h`** - Evaluates and selects actions
- **`AttackPossibility.h`** - Evaluates attack options
- **`BattleExchangeVariant.h`** - Simulates combat outcomes
- **`PotentialTargets.h`** - Finds valid attack targets
- **`ThreatMap.h`** - Evaluates battlefield threats

### BattleEvaluator - Action Selection

**Location**: `/tmp/vcmi-temp/AI/BattleAI/BattleEvaluator.h:34-83`

```cpp
class BattleEvaluator
{
    std::unique_ptr<PotentialTargets> targets;
    std::shared_ptr<HypotheticBattle> hb;    // Simulation environment
    BattleExchangeEvaluator scoreEvaluator;
    DamageCache damageCache;
    float strengthRatio;                      // Our army strength / enemy strength

    // Main decision method
    BattleAction selectStackAction(const CStack* stack)
    {
        // 1. Try casting spell
        if (attemptCastingSpell(stack))
            return makeSpellAction(...);

        // 2. Evaluate all attack possibilities
        auto attackOptions = evaluateAttackPossibilities(stack);

        // 3. Score each option
        for (auto& option : attackOptions)
            option.score = scoreEvaluator.evaluate(option);

        // 4. Pick best action
        auto best = std::max_element(attackOptions.begin(), attackOptions.end(),
                                      [](auto& a, auto& b) { return a.score < b.score; });

        if (best->score > MIN_THRESHOLD)
            return makeAttackAction(best);

        // 5. Fallback: Move closer or wait
        return goTowardsNearest(stack, hexes, targets);
    }
};
```

### AttackPossibility Evaluation

**Location**: `/tmp/vcmi-temp/AI/BattleAI/AttackPossibility.h:40-82`

```cpp
class AttackPossibility
{
    BattleHex from;        // Attack from this hex
    BattleHex dest;        // Target hex
    BattleAttackInfo attack;

    std::shared_ptr<battle::CUnitState> attackerState;
    std::vector<std::shared_ptr<battle::CUnitState>> affectedUnits;

    float defenderDamageReduce;     // HP removed from defender
    float attackerDamageReduce;     // HP lost to retaliation
    float collateralDamageReduce;   // Friendly fire damage
    int64_t shootersBlockedDmg;     // Damage blocked by obstacle
    bool defenderDead;

    // Scoring
    float attackValue() const
    {
        return defenderDamageReduce - attackerDamageReduce - collateralDamageReduce;
    }

    // Simulate attack outcome
    static AttackPossibility evaluate(
        const BattleAttackInfo& attackInfo,
        BattleHex hex,
        DamageCache& damageCache,
        std::shared_ptr<CBattleInfoCallback> state)
    {
        // Calculate damage to defender
        // Calculate retaliation damage
        // Calculate collateral damage (breath, etc.)
        // Return scored possibility
    }
};
```

### AI Decision Logic

**Simple AI Flow**:
```
1. Is unit a war machine?
   → Yes: Execute automatic action (catapult, ballista, healing tent)

2. Can cast beneficial spell?
   → Yes: Cast spell on best target

3. Evaluate all attack options:
   For each reachable hex:
     For each enemy in range from that hex:
       Calculate: damage dealt - damage taken - friendly fire
       Score option

4. Best attack score > threshold?
   → Yes: Execute attack
   → No: Move towards nearest enemy OR wait

5. No movement possible?
   → Defend (increases defense for this turn)
```

### DamageCache System

**Location**: `/tmp/vcmi-temp/AI/BattleAI/AttackPossibility.h:16-34`

```cpp
class DamageCache
{
    // Cache damage calculations to avoid recomputing
    std::unordered_map<uint32_t, std::unordered_map<uint32_t, float>> damageCache;

    // Precompute all damage values for current turn
    void buildDamageCache(std::shared_ptr<HypotheticBattle> hb, BattleSide side)
    {
        for (auto* attacker : hb->battleGetUnitsIf([&](auto* u) { return u->unitSide() == side; }))
        {
            for (auto* defender : hb->battleGetUnitsIf([&](auto* u) { return u->unitSide() != side; }))
            {
                cacheDamage(attacker, defender, hb);
            }
        }
    }

    int64_t getDamage(const battle::Unit* attacker, const battle::Unit* defender, ...)
    {
        // Check cache, calculate if missing
    }
};
```

---

## 6. Unity C# Translation

### Core Architecture

```
BattleSystem/
├── Core/                  # Pure C# logic
│   ├── BattleState.cs
│   ├── BattleUnit.cs
│   ├── BattleHex.cs
│   ├── DamageCalculator.cs
│   ├── TurnQueue.cs
│   └── BattleAction.cs
├── Controllers/           # MonoBehaviour controllers
│   ├── BattleController.cs
│   ├── BattleUnitController.cs
│   ├── BattleFieldRenderer.cs
│   └── BattleInputHandler.cs
├── AI/                    # AI logic
│   ├── BattleAI.cs
│   ├── ActionEvaluator.cs
│   └── AttackPossibility.cs
└── UI/                    # UI components
    ├── TurnQueueUI.cs
    ├── UnitStatsUI.cs
    └── BattleControlsUI.cs
```

### 1. BattleHex (C# Translation)

```csharp
using UnityEngine;
using System;

public struct BattleHex : IEquatable<BattleHex>
{
    public const int FIELD_WIDTH = 17;
    public const int FIELD_HEIGHT = 11;
    public const int FIELD_SIZE = 187;

    public const short INVALID = -1;
    public const short HERO_ATTACKER = 0;
    public const short HERO_DEFENDER = 16;

    public enum Direction
    {
        None = -1,
        TopLeft, TopRight, Right,
        BottomRight, BottomLeft, Left
    }

    private short _hex;

    public BattleHex(short hex) => _hex = hex;
    public BattleHex(int x, int y) => SetXY(x, y);

    public int X => _hex % FIELD_WIDTH;
    public int Y => _hex / FIELD_WIDTH;
    public short Value => _hex;

    public bool IsValid => _hex >= 0 && _hex < FIELD_SIZE;
    public bool IsAvailable => IsValid && X > 0 && X < FIELD_WIDTH - 1;

    public void SetXY(int x, int y)
    {
        if (x < 0 || x >= FIELD_WIDTH || y < 0 || y >= FIELD_HEIGHT)
            throw new ArgumentOutOfRangeException($"Invalid hex coords: ({x}, {y})");
        _hex = (short)(x + y * FIELD_WIDTH);
    }

    public BattleHex GetNeighbor(Direction dir)
    {
        var result = new BattleHex(_hex);
        int x = X, y = Y;
        bool oddRow = (y % 2) == 1;

        switch (dir)
        {
            case Direction.TopLeft:
                result.SetXY(oddRow ? x - 1 : x, y - 1);
                break;
            case Direction.TopRight:
                result.SetXY(oddRow ? x : x + 1, y - 1);
                break;
            case Direction.Right:
                result.SetXY(x + 1, y);
                break;
            case Direction.BottomRight:
                result.SetXY(oddRow ? x : x + 1, y + 1);
                break;
            case Direction.BottomLeft:
                result.SetXY(oddRow ? x - 1 : x, y + 1);
                break;
            case Direction.Left:
                result.SetXY(x - 1, y);
                break;
        }
        return result;
    }

    public static int GetDistance(BattleHex a, BattleHex b)
    {
        // Hexagonal distance using axial coordinates
        int y1 = a.Y, y2 = b.Y;
        int x1 = a.X + y1 / 2;
        int x2 = b.X + y2 / 2;

        int xDst = x2 - x1;
        int yDst = y2 - y1;

        if ((xDst >= 0 && yDst >= 0) || (xDst < 0 && yDst < 0))
            return Mathf.Max(Mathf.Abs(xDst), Mathf.Abs(yDst));

        return Mathf.Abs(xDst) + Mathf.Abs(yDst);
    }

    public static readonly Direction[] AllDirections = new[]
    {
        Direction.TopLeft, Direction.TopRight, Direction.Right,
        Direction.BottomRight, Direction.BottomLeft, Direction.Left
    };

    public bool Equals(BattleHex other) => _hex == other._hex;
    public override bool Equals(object obj) => obj is BattleHex hex && Equals(hex);
    public override int GetHashCode() => _hex.GetHashCode();

    public static bool operator ==(BattleHex a, BattleHex b) => a._hex == b._hex;
    public static bool operator !=(BattleHex a, BattleHex b) => a._hex != b._hex;
}
```

### 2. BattleUnit (C# Translation)

```csharp
using System.Collections.Generic;

public class BattleUnit
{
    // Identity
    public int UnitId { get; set; }
    public CreatureType Creature { get; private set; }
    public BattleSide Side { get; private set; }
    public int SlotIndex { get; private set; }

    // Position
    public BattleHex Position { get; set; }
    public bool IsDoubleWide => Creature.IsDoubleWide;

    // Health
    public int Count { get; private set; }           // Number of creatures
    public int FirstUnitHP { get; private set; }     // HP of first creature
    public int MaxHealth => Creature.MaxHealth;
    public int TotalHealth => (Count - 1) * MaxHealth + FirstUnitHP;

    // Stats
    public int Attack => GetEffectiveStat(Creature.Attack);
    public int Defense => GetEffectiveStat(Creature.Defense);
    public int MinDamage => Creature.MinDamage;
    public int MaxDamage => Creature.MaxDamage;
    public int Speed => GetEffectiveStat(Creature.Speed);
    public int Initiative => Speed; // Base initiative is speed

    // Combat State
    public bool HasMoved { get; set; }
    public bool HasRetaliatedThisTurn { get; set; }
    public bool IsDefending { get; set; }
    public bool HasWaited { get; set; }
    public bool HadMoraleThisTurn { get; set; }

    // Abilities
    public bool CanShoot => Creature.IsRanged && Shots > 0;
    public bool CanRetaliate => !HasRetaliatedThisTurn && Creature.Retaliations > 0;
    public int Shots { get; private set; }
    public int Retaliations { get; private set; }

    // Buffs/Debuffs
    private List<StatusEffect> _activeEffects = new();

    public void TakeDamage(int damage)
    {
        var remaining = damage;

        // Damage first creature
        if (FirstUnitHP <= remaining)
        {
            remaining -= FirstUnitHP;
            Count--;
            FirstUnitHP = MaxHealth;
        }
        else
        {
            FirstUnitHP -= remaining;
            return;
        }

        // Damage remaining creatures
        while (remaining >= MaxHealth && Count > 0)
        {
            remaining -= MaxHealth;
            Count--;
        }

        // Final partial damage
        if (Count > 0 && remaining > 0)
        {
            FirstUnitHP -= remaining;
            if (FirstUnitHP <= 0)
            {
                Count--;
                FirstUnitHP = MaxHealth;
            }
        }
    }

    public void ResetTurn()
    {
        HasMoved = false;
        HasRetaliatedThisTurn = false;
        IsDefending = false;
        HadMoraleThisTurn = false;
        Retaliations = Creature.Retaliations;
    }

    public int GetEffectiveStat(int baseStat)
    {
        var total = baseStat;
        foreach (var effect in _activeEffects)
            total += effect.GetStatModifier();
        return total;
    }

    public bool IsAlive => Count > 0;
}
```

### 3. DamageCalculator (C# Translation)

```csharp
using System;
using System.Linq;
using UnityEngine;

public class DamageCalculator
{
    private readonly BattleAttackInfo _info;

    public DamageCalculator(BattleAttackInfo info)
    {
        _info = info;
    }

    public struct DamageRange
    {
        public int Min;
        public int Max;
    }

    public struct DamageEstimation
    {
        public DamageRange Damage;
        public DamageRange Kills;
    }

    public DamageEstimation CalculateDamage()
    {
        // 1. Base damage
        var baseDamage = GetBaseDamage();

        // 2. Attack factors (additive)
        var attackFactors = GetAttackFactors();
        float attackMultiplier = 1.0f + attackFactors.Sum();

        // 3. Defense factors (multiplicative penalties)
        var defenseFactors = GetDefenseFactors();
        float defenseMultiplier = 1.0f;
        foreach (var factor in defenseFactors)
            defenseMultiplier *= (1.0f - Mathf.Min(1.0f, factor));

        // 4. Final damage
        float totalMultiplier = attackMultiplier * defenseMultiplier;
        var damageDealt = new DamageRange
        {
            Min = Mathf.Max(1, Mathf.FloorToInt(baseDamage.Min * totalMultiplier)),
            Max = Mathf.Max(1, Mathf.FloorToInt(baseDamage.Max * totalMultiplier))
        };

        // 5. Calculate kills
        var kills = new DamageRange
        {
            Min = CalculateCasualties(damageDealt.Min),
            Max = CalculateCasualties(damageDealt.Max)
        };

        return new DamageEstimation { Damage = damageDealt, Kills = kills };
    }

    private DamageRange GetBaseDamage()
    {
        var unit = _info.Attacker;
        var baseDmg = new DamageRange
        {
            Min = unit.MinDamage,
            Max = unit.MaxDamage
        };

        // Apply bless/curse
        if (_info.IsCursed)
            baseDmg.Max = baseDmg.Min;
        else if (_info.IsBlessed)
            baseDmg.Min = baseDmg.Max;

        // Multiply by stack size
        baseDmg.Min *= unit.Count;
        baseDmg.Max *= unit.Count;

        return baseDmg;
    }

    private float[] GetAttackFactors()
    {
        return new[]
        {
            GetAttackSkillFactor(),
            GetLuckFactor(),
            GetSpecialtyFactor(),
            // ... more factors
        };
    }

    private float GetAttackSkillFactor()
    {
        int attackDiff = _info.Attacker.Attack - _info.Defender.Defense;
        if (attackDiff <= 0)
            return 0f;

        const float attackMult = 0.05f;  // 5% per point
        const float maxBonus = 3.0f;     // 300% max
        return Mathf.Min(attackDiff * attackMult, maxBonus);
    }

    private float GetLuckFactor()
    {
        return _info.IsLuckyStrike ? 1.0f : 0f;  // +100% on lucky strike
    }

    private float[] GetDefenseFactors()
    {
        return new[]
        {
            GetDefenseSkillFactor(),
            GetRangePenaltyFactor(),
            GetUnluckyFactor(),
            // ... more factors
        };
    }

    private float GetDefenseSkillFactor()
    {
        int defenseDiff = _info.Defender.Defense - _info.Attacker.Attack;
        if (defenseDiff <= 0)
            return 0f;

        const float defenseMult = 0.025f;  // 2.5% per point
        const float maxReduction = 0.7f;   // 70% max
        return Mathf.Min(defenseDiff * defenseMult, maxReduction);
    }

    private float GetRangePenaltyFactor()
    {
        if (_info.IsRangedAttack)
        {
            // Check distance penalty
            int distance = BattleHex.GetDistance(_info.AttackerPosition, _info.DefenderPosition);
            if (distance > 10)  // Example threshold
                return 0.5f;
        }
        else if (_info.Attacker.Creature.IsRanged)
        {
            // Ranged unit in melee
            return 0.5f;
        }
        return 0f;
    }

    private float GetUnluckyFactor()
    {
        return _info.IsUnluckyStrike ? 0.5f : 0f;  // -50% on unlucky strike
    }

    private int CalculateCasualties(int damage)
    {
        if (damage < _info.Defender.FirstUnitHP)
            return 0;

        int remaining = damage - _info.Defender.FirstUnitHP;
        int kills = 1 + remaining / _info.Defender.MaxHealth;
        return Mathf.Min(kills, _info.Defender.Count);
    }
}

public struct BattleAttackInfo
{
    public BattleUnit Attacker;
    public BattleUnit Defender;
    public BattleHex AttackerPosition;
    public BattleHex DefenderPosition;
    public bool IsRangedAttack;
    public int ChargeDistance;
    public bool IsLuckyStrike;
    public bool IsUnluckyStrike;
    public bool IsBlessed;
    public bool IsCursed;
}
```

### 4. TurnQueue (C# Translation)

```csharp
using System.Collections.Generic;
using System.Linq;

public class TurnQueue
{
    public enum Phase
    {
        Siege = 0,       // Turrets, catapult
        Normal = 1,      // Regular units
        WaitMorale = 2,  // Waited units with morale
        Wait = 3         // Waited units
    }

    private List<BattleUnit> _units = new();
    private int _currentTurn = 0;
    private BattleSide _lastActedSide;

    public void Initialize(IEnumerable<BattleUnit> units)
    {
        _units = units.Where(u => u.IsAlive).ToList();
        RebuildQueue();
    }

    public BattleUnit GetNextUnit()
    {
        // Get all units that can act this turn
        var candidates = _units.Where(u => u.IsAlive && !u.HasMoved).ToList();

        if (candidates.Count == 0)
        {
            // New round
            _currentTurn++;
            foreach (var unit in _units)
                unit.ResetTurn();
            candidates = _units.Where(u => u.IsAlive).ToList();
        }

        // Sort by initiative, then tiebreakers
        candidates.Sort((a, b) => CompareUnits(a, b, Phase.Normal, _currentTurn, _lastActedSide));

        var next = candidates.First();
        _lastActedSide = next.Side;
        return next;
    }

    private int CompareUnits(BattleUnit a, BattleUnit b, Phase phase, int turn, BattleSide lastSide)
    {
        // Primary: Initiative (higher is better)
        if (a.Initiative != b.Initiative)
            return b.Initiative.CompareTo(a.Initiative);

        // Tiebreaker 1: Same side → slot order
        if (a.Side == b.Side)
            return a.SlotIndex.CompareTo(b.SlotIndex);

        // Tiebreaker 2: Different sides → prioritize side that didn't go last
        if (a.Side == lastSide)
            return 1;
        if (b.Side == lastSide)
            return -1;

        return a.Side.CompareTo(b.Side);
    }

    public List<BattleUnit> GetUpcomingTurns(int count)
    {
        // Simulate future turns for UI display
        var result = new List<BattleUnit>();
        var simulatedUnits = _units.Select(u => CloneUnit(u)).ToList();
        var simulatedTurn = _currentTurn;
        var simulatedSide = _lastActedSide;

        while (result.Count < count)
        {
            var candidates = simulatedUnits.Where(u => u.IsAlive && !u.HasMoved).ToList();

            if (candidates.Count == 0)
            {
                simulatedTurn++;
                foreach (var unit in simulatedUnits)
                    unit.ResetTurn();
                continue;
            }

            candidates.Sort((a, b) => CompareUnits(a, b, Phase.Normal, simulatedTurn, simulatedSide));
            var next = candidates.First();
            next.HasMoved = true;
            result.Add(_units.First(u => u.UnitId == next.UnitId));
            simulatedSide = next.Side;
        }

        return result;
    }

    private BattleUnit CloneUnit(BattleUnit original)
    {
        // Shallow clone for simulation
        return new BattleUnit
        {
            UnitId = original.UnitId,
            Side = original.Side,
            SlotIndex = original.SlotIndex,
            HasMoved = original.HasMoved,
            // ... copy other fields
        };
    }

    private void RebuildQueue()
    {
        // Initial queue construction
        foreach (var unit in _units)
            unit.ResetTurn();
    }
}
```

### 5. BattleController (MonoBehaviour)

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BattleController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BattleFieldRenderer _fieldRenderer;
    [SerializeField] private BattleInputHandler _inputHandler;
    [SerializeField] private TurnQueueUI _turnQueueUI;

    // Core battle state
    private BattleState _battleState;
    private TurnQueue _turnQueue;
    private BattleUnit _activeUnit;

    // Configuration
    [SerializeField] private BattleSide _playerSide = BattleSide.Attacker;

    public void InitializeBattle(Army attackers, Army defenders)
    {
        // 1. Create battle state
        _battleState = new BattleState();
        _battleState.Initialize(attackers, defenders);

        // 2. Initialize turn queue
        _turnQueue = new TurnQueue();
        _turnQueue.Initialize(_battleState.GetAllUnits());

        // 3. Setup battlefield rendering
        _fieldRenderer.Initialize(_battleState);

        // 4. Start first turn
        ActivateNextUnit();
    }

    private void ActivateNextUnit()
    {
        _activeUnit = _turnQueue.GetNextUnit();

        // Update UI
        _turnQueueUI.UpdateQueue(_turnQueue.GetUpcomingTurns(10));
        _fieldRenderer.HighlightUnit(_activeUnit);

        // Check if AI or player
        if (_activeUnit.Side == _playerSide)
        {
            // Player turn - enable input
            _inputHandler.EnableInput(_activeUnit);
        }
        else
        {
            // AI turn
            var aiAction = BattleAI.SelectAction(_battleState, _activeUnit);
            ExecuteAction(aiAction);
        }
    }

    public void OnPlayerAction(BattleAction action)
    {
        ExecuteAction(action);
    }

    private void ExecuteAction(BattleAction action)
    {
        switch (action.Type)
        {
            case ActionType.Walk:
                ExecuteWalk(action);
                break;
            case ActionType.MeleeAttack:
                ExecuteMeleeAttack(action);
                break;
            case ActionType.RangedAttack:
                ExecuteRangedAttack(action);
                break;
            case ActionType.Wait:
                ExecuteWait(action);
                break;
            case ActionType.Defend:
                ExecuteDefend(action);
                break;
        }

        // Mark unit as moved
        _activeUnit.HasMoved = true;

        // Check for battle end
        if (_battleState.IsBattleFinished(out var winner))
        {
            OnBattleEnd(winner);
            return;
        }

        // Next turn
        ActivateNextUnit();
    }

    private void ExecuteMeleeAttack(BattleAction action)
    {
        var attacker = action.Unit;
        var defender = action.Target;

        // 1. Move to attack position (if needed)
        if (action.AttackFrom != attacker.Position)
            _fieldRenderer.AnimateMovement(attacker, action.AttackFrom);

        // 2. Calculate damage
        var attackInfo = new BattleAttackInfo
        {
            Attacker = attacker,
            Defender = defender,
            AttackerPosition = action.AttackFrom,
            DefenderPosition = defender.Position,
            IsRangedAttack = false,
            ChargeDistance = CalculateChargeDistance(attacker.Position, action.AttackFrom),
            IsLuckyStrike = RollLuck(attacker),
            IsUnluckyStrike = RollUnluck(defender)
        };

        var calculator = new DamageCalculator(attackInfo);
        var result = calculator.CalculateDamage();

        // 3. Animate attack
        _fieldRenderer.AnimateAttack(attacker, defender);

        // 4. Apply damage
        defender.TakeDamage(result.Damage.Max);  // Or random between min/max
        _fieldRenderer.ShowDamageNumber(defender, result.Damage.Max);

        // 5. Check for retaliation
        if (defender.IsAlive && defender.CanRetaliate)
        {
            ExecuteRetaliation(defender, attacker);
        }

        // 6. Move back (if returnAfterAttack)
        if (action.ReturnAfterAttack && attacker.IsAlive)
            _fieldRenderer.AnimateMovement(attacker, action.OriginalPosition);
    }

    private void ExecuteRetaliation(BattleUnit retaliator, BattleUnit target)
    {
        var attackInfo = new BattleAttackInfo
        {
            Attacker = retaliator,
            Defender = target,
            IsRangedAttack = false,
            // ... setup attack info
        };

        var calculator = new DamageCalculator(attackInfo);
        var result = calculator.CalculateDamage();

        _fieldRenderer.AnimateAttack(retaliator, target);
        target.TakeDamage(result.Damage.Max);
        _fieldRenderer.ShowDamageNumber(target, result.Damage.Max);

        retaliator.HasRetaliatedThisTurn = true;
    }

    private void OnBattleEnd(BattleSide winner)
    {
        Debug.Log($"Battle ended! Winner: {winner}");
        // Show battle result screen, calculate rewards, etc.
    }

    private bool RollLuck(BattleUnit unit)
    {
        // Simplified luck calculation
        int luckLevel = unit.GetLuck();
        float chance = luckLevel * 0.1f;  // 10% per luck point
        return Random.value < chance;
    }

    private bool RollUnluck(BattleUnit unit)
    {
        int luckLevel = unit.GetLuck();
        if (luckLevel >= 0)
            return false;
        float chance = Mathf.Abs(luckLevel) * 0.1f;
        return Random.value < chance;
    }

    private int CalculateChargeDistance(BattleHex from, BattleHex to)
    {
        return BattleHex.GetDistance(from, to);
    }
}
```

---

## 7. Implementation Roadmap

### Phase 5A: Core Battle Foundation (Week 1-2)

**Goal**: Basic battle initialization and hex grid

**Tasks**:
1. Create BattleHex struct with coordinate system
2. Implement hex distance and navigation
3. Create BattleUnit class with stats
4. Implement BattleState container
5. Create BattleFieldRenderer for hex visualization
6. Test: Initialize battle with 2 units, display on hex grid

**Deliverables**:
- `BattleHex.cs` - Hex coordinate system
- `BattleUnit.cs` - Unit state and stats
- `BattleState.cs` - Battle container
- `BattleFieldRenderer.cs` - Visual representation
- Unit tests for hex navigation

### Phase 5B: Turn Order & Actions (Week 3-4)

**Goal**: Implement turn queue and basic actions

**Tasks**:
1. Implement TurnQueue with initiative sorting
2. Create BattleAction structure
3. Implement WALK action (movement)
4. Implement WAIT action
5. Implement DEFEND action
6. Create BattleController to orchestrate turns
7. Test: Units take turns based on initiative

**Deliverables**:
- `TurnQueue.cs` - Initiative-based turn order
- `BattleAction.cs` - Action definitions
- `BattleController.cs` - Main battle controller
- `TurnQueueUI.cs` - Visual turn queue display
- Unit tests for turn order

### Phase 5C: Combat System (Week 5-6)

**Goal**: Implement damage calculation and combat

**Tasks**:
1. Implement DamageCalculator with attack/defense factors
2. Implement melee attack action
3. Implement retaliation system
4. Implement ranged attack action
5. Add combat animations
6. Test: Units attack and deal/receive damage

**Deliverables**:
- `DamageCalculator.cs` - Complete damage formulas
- Attack animations
- Damage popup UI
- Unit tests for damage calculation

### Phase 5D: Special Mechanics (Week 7-8)

**Goal**: Add morale, luck, and special abilities

**Tasks**:
1. Implement morale system (good/bad morale)
2. Implement luck system (lucky/unlucky strikes)
3. Implement unit abilities (double attack, etc.)
4. Implement status effects (buffs/debuffs)
5. Add visual feedback for special events
6. Test: Special mechanics trigger correctly

**Deliverables**:
- `MoraleSystem.cs` - Morale calculation
- `StatusEffect.cs` - Buff/debuff system
- VFX for special abilities
- Unit tests for mechanics

### Phase 5E: Battle AI (Week 9-10)

**Goal**: Basic AI for enemy units

**Tasks**:
1. Implement AttackPossibility evaluation
2. Implement DamageCache for performance
3. Create BattleAI decision logic
4. Implement target selection
5. Add AI difficulty levels
6. Test: AI makes reasonable decisions

**Deliverables**:
- `BattleAI.cs` - AI controller
- `ActionEvaluator.cs` - Action scoring
- `AttackPossibility.cs` - Attack evaluation
- Unit tests for AI logic

### Phase 5F: UI & Polish (Week 11-12)

**Goal**: Complete battle UI and polish

**Tasks**:
1. Create unit stat display panel
2. Implement battle controls (retreat, auto-battle)
3. Add combat log
4. Add sound effects
5. Add particle effects
6. Polish animations and transitions
7. Test: Complete battle experience

**Deliverables**:
- `BattleUI.cs` - Complete UI system
- Sound integration
- VFX polish
- End-to-end battle tests

### Phase 5G: Advanced Features (Week 13-14)

**Goal**: Obstacles, terrain, and siege mechanics

**Tasks**:
1. Implement obstacle system
2. Add terrain effects
3. Implement siege mechanics (walls, towers)
4. Add spell casting
5. Test: Full feature integration

**Deliverables**:
- `ObstacleSystem.cs` - Battlefield obstacles
- `SiegeSystem.cs` - Siege battles
- Complete battle system
- Integration tests

---

## Key Implementation Notes

### Critical Design Decisions

1. **Hex Grid**: Use offset coordinates (x + y * width) for storage, convert to axial for distance
2. **Damage Calculation**: Attack factors are additive, defense factors are multiplicative
3. **Turn Order**: Initiative (speed) → slot order → side tiebreaker
4. **Retaliation**: Immediate counter-attack, limited to once per turn
5. **Unit State**: Separate `BattleUnit` from visual `BattleUnitController`

### Performance Considerations

1. **Damage Cache**: Pre-calculate all damage values at turn start for AI
2. **Pathfinding**: Use A* Pathfinding Project for reachability
3. **Animation**: Use DOTween for smooth unit movement
4. **UI Updates**: Event-driven updates, avoid polling in Update()

### Testing Strategy

1. **Unit Tests**: Core logic (damage calc, turn order, hex navigation)
2. **Integration Tests**: Full battle flow scenarios
3. **AI Tests**: Validate AI decision quality
4. **Performance Tests**: 20v20 battles run smoothly

---

## References

### VCMI Source Files Analyzed

- `/tmp/vcmi-temp/lib/battle/BattleInfo.h` - Main battle state
- `/tmp/vcmi-temp/lib/battle/BattleAction.h` - Actions
- `/tmp/vcmi-temp/lib/battle/BattleHex.h` - Hex system
- `/tmp/vcmi-temp/lib/battle/Unit.h` - Unit interface
- `/tmp/vcmi-temp/lib/battle/CUnitState.h` - Unit state
- `/tmp/vcmi-temp/lib/battle/DamageCalculator.cpp` - Damage formulas
- `/tmp/vcmi-temp/lib/battle/CBattleInfoCallback.cpp` - Battle queries
- `/tmp/vcmi-temp/server/battles/BattleFlowProcessor.cpp` - Battle flow
- `/tmp/vcmi-temp/server/battles/BattleActionProcessor.cpp` - Action execution
- `/tmp/vcmi-temp/AI/BattleAI/BattleEvaluator.cpp` - AI logic
- `/tmp/vcmi-temp/AI/BattleAI/AttackPossibility.cpp` - Attack evaluation
- `/tmp/vcmi-temp/docs/developers/Battlefield.md` - Battlefield documentation

### Unity Resources

- **A* Pathfinding Project**: Hex-based pathfinding
- **DOTween**: Animation system
- **TextMeshPro**: UI text rendering
- **Cinemachine**: Camera control for battle view

---

*Research complete. All systems analyzed and ready for implementation.*
