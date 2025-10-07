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

## Phase 5: Hero Panel UI Research (2025-10-05)

### VCMI Hero Panel Implementation

Based on analysis of VCMI source code in `/tmp/vcmi-temp/client/windows/` and `/tmp/vcmi-temp/client/adventureMap/`:

#### Key Files Analyzed:
- `CHeroWindow.h/cpp` - Full hero window (opened on double-click)
- `CInfoBar.h/cpp` - Adventure map hero info panel
- `MiscWidgets.h` - MoraleLuckBox, CHeroTooltip components
- `CGarrisonInt.h` - Army garrison display
- `CArtifactsOfHeroBase.h` - Artifact slots and display

### 1. Full Hero Window (CHeroWindow)

**Window Layout** (`CHeroWindow.h:50-115`):
```cpp
class CHeroWindow : public CStatusbarWindow
{
    // Header
    std::shared_ptr<CLabel> name;           // Hero name
    std::shared_ptr<CLabel> title;          // "Level X ClassName"
    std::shared_ptr<CAnimImage> banner;     // Player color crest
    std::shared_ptr<CAnimImage> portraitImage;

    // Primary Skills (Attack, Defense, Power, Knowledge)
    std::vector<std::shared_ptr<LRClickableAreaWTextComp>> primSkillAreas;
    std::vector<std::shared_ptr<CAnimImage>> primSkillImages;
    std::vector<std::shared_ptr<CLabel>> primSkillValues;

    // Hero Stats
    std::shared_ptr<CLabel> expValue;       // Experience points
    std::shared_ptr<CLabel> manaValue;      // Mana current/max

    // Specialty
    std::shared_ptr<CAnimImage> specImage;
    std::shared_ptr<CLabel> specName;

    // Morale & Luck
    std::shared_ptr<MoraleLuckBox> morale;
    std::shared_ptr<MoraleLuckBox> luck;

    // Secondary Skills (up to 8 visible, slider if more)
    std::vector<std::shared_ptr<CSecSkillPlace>> secSkills;
    std::vector<std::shared_ptr<CLabel>> secSkillNames;
    std::vector<std::shared_ptr<CLabel>> secSkillValues;
    std::shared_ptr<CSlider> secSkillSlider;

    // Army & Artifacts
    std::shared_ptr<CGarrisonInt> garr;     // 7 creature slots
    std::shared_ptr<CArtifactsOfHeroMain> arts;  // 19 equipment slots + backpack

    // Buttons
    std::shared_ptr<CButton> quitButton;
    std::shared_ptr<CButton> dismissButton;
    std::shared_ptr<CButton> questlogButton;
    std::shared_ptr<CButton> commanderButton;
    std::shared_ptr<CButton> backpackButton;
};
```

**Data Display** (`CHeroWindow.cpp:188-326`):

Primary Skills (lines 241-246):
```cpp
for(size_t g=0; g<primSkillAreas.size(); ++g) {
    int value = curHero->getPrimSkillLevel(static_cast<PrimarySkill>(g));
    primSkillAreas[g]->component.value = value;
    primSkillValues[g]->setText(std::to_string(value));
}
```

Secondary Skills (lines 248-267):
```cpp
for(size_t g=0; g < secSkills.size(); ++g) {
    int offset = secSkillSlider ? secSkillSlider->getValue() * 2 : 0;
    if(curHero->secSkills.size() < g + offset + 1) break;

    SecondarySkill skill = curHero->secSkills[g + offset].first;
    int level = curHero->getSecSkillLevel(skill);

    secSkillNames[g]->setText(skill.toEntity(LIBRARY)->getNameTranslated());
    secSkillValues[g]->setText(LIBRARY->generaltexth->levels[level-1]);
    secSkills[g]->setSkill(skill, level);
}
```

Experience & Mana (lines 269-287):
```cpp
expValue->setText(std::to_string(curHero->exp));
manaValue->setText(curHero->mana + "/" + curHero->manaLimit());

// Experience tooltip shows level-up progress
expArea->text = "Level %d | Next: %d XP | Current: %d XP";
spellPointsArea->text = "%s has %d/%d spell points";
```

Morale & Luck (lines 322-323):
```cpp
morale->set(curHero);  // Calculates from army + bonuses
luck->set(curHero);    // Calculates from artifacts + bonuses
```

### 2. Adventure Map Info Panel (CInfoBar)

**VisibleHeroInfo** (`CInfoBar.cpp:53-62`):
```cpp
CInfoBar::VisibleHeroInfo::VisibleHeroInfo(const CGHeroInstance * hero)
{
    background = std::make_shared<CPicture>(ImagePath::builtin("ADSTATHR"));

    // Two modes: Basic tooltip or Interactive with garrison
    if(settings["gameTweaks"]["infoBarCreatureManagement"].Bool())
        heroTooltip = std::make_shared<CInteractableHeroTooltip>(Point(0,0), hero);
    else
        heroTooltip = std::make_shared<CHeroTooltip>(Point(0,0), hero);
}
```

**CHeroTooltip** (`MiscWidgets.h:88-102`):
```cpp
class CHeroTooltip : public CArmyTooltip
{
    std::shared_ptr<CAnimImage> portrait;
    std::vector<std::shared_ptr<CLabel>> labels;      // Name, stats text
    std::shared_ptr<CAnimImage> morale;
    std::shared_ptr<CAnimImage> luck;

    // Shows: Portrait, Name, Primary skills, Morale, Luck, Army (from CArmyTooltip)
};
```

**CInteractableHeroTooltip** (`MiscWidgets.h:104-117`):
```cpp
class CInteractableHeroTooltip : public CIntObject
{
    std::shared_ptr<CLabel> title;
    std::shared_ptr<CAnimImage> portrait;
    std::vector<std::shared_ptr<CLabel>> labels;
    std::shared_ptr<CAnimImage> morale;
    std::shared_ptr<CAnimImage> luck;
    std::shared_ptr<CGarrisonInt> garrison;  // INTERACTIVE: Can click/drag creatures
};
```

### 3. Hero Data Structure (CGHeroInstance)

From `/tmp/vcmi-temp/lib/mapObjects/CGHeroInstance.h`:

**Core Properties** (lines 89-96):
```cpp
class CGHeroInstance : public CArmedInstance, public CArtifactSet
{
    TExpType exp;                           // Experience points
    ui32 level;                             // Current level
    si32 mana;                              // Current spell points
    std::vector<std::pair<SecondarySkill,ui8>> secSkills;  // (skill, level) pairs
    EHeroGender gender;
    std::set<SpellID> spells;              // Known spells
    ui32 movement;                          // Movement points remaining
};
```

**Key Methods**:
```cpp
int getPrimSkillLevel(PrimarySkill id) const;
ui8 getSecSkillLevel(const SecondarySkill & skill) const;
si32 manaLimit() const;
int getCurrentLuck(int stack=-1, bool town=false) const;
std::string getNameTranslated() const;
std::string getClassNameTranslated() const;
HeroTypeID getPortraitSource() const;
```

### 4. Artifact Display System

**Equipment Slots** (`CArtifactsOfHeroBase.h:63-72`):
```cpp
const std::vector<Point> slotPos = {
    Point(509,30),  // HEAD
    Point(568,242), // SHOULDERS
    Point(509,80),  // NECK
    Point(383,69),  // RIGHT_HAND
    Point(562,184), // LEFT_HAND
    Point(509,131), // TORSO
    Point(431,69),  // RIGHT_RING
    Point(610,184), // LEFT_RING
    Point(515,295), // FEET
    // ... 19 slots total + backpack scrolling
};
```

**Artifact Management**:
```cpp
class CArtifactsOfHeroBase
{
    ArtPlaceMap artWorn;                    // 19 worn slots
    std::vector<ArtPlacePtr> backpack;      // Unlimited scrollable backpack
    std::shared_ptr<CButton> leftBackpackRoll;
    std::shared_ptr<CButton> rightBackpackRoll;

    void updateWornSlots();
    void updateBackpackSlots();
    void scrollBackpack(bool left);
};
```

### 5. Army Garrison Display

**CGarrisonSlot** (`CGarrisonInt.h:33-74`):
```cpp
class CGarrisonSlot : public CIntObject
{
    SlotID ID;                              // 0-6 slot index
    const CStackInstance * myStack;         // nullptr if empty
    const CCreature * creature;

    std::shared_ptr<CAnimImage> creatureImage;
    std::shared_ptr<CAnimImage> selectionImage;
    std::shared_ptr<CLabel> stackCount;     // "42"

    void clickPressed() override;           // Select/swap/split
    void showPopupWindow() override;        // Right-click creature info
    bool split();                           // Split stack into two
};
```

**CGarrisonInt** (`CGarrisonInt.h:83-100`):
```cpp
class CGarrisonInt : public CIntObject
{
    std::vector<std::shared_ptr<CGarrisonSlot>> availableSlots;  // 7 slots
    CGarrisonSlot * highlighted;            // Currently selected slot
    bool inSplittingMode;

    void splitClick();                      // Toggle split mode
    void createSlots();
    void recreateSlots();                   // Update after changes
};
```

### Unity Translation

**1. HeroPanelUI** (Adventure Map Info Panel):
```csharp
public class HeroPanelUI : MonoBehaviour
{
    [Header("Hero Info")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private TextMeshProUGUI heroNameText;
    [SerializeField] private TextMeshProUGUI levelClassText;

    [Header("Primary Skills")]
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI defenseText;
    [SerializeField] private TextMeshProUGUI powerText;
    [SerializeField] private TextMeshProUGUI knowledgeText;

    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI manaText;           // "50/100"
    [SerializeField] private Image moraleIcon;                   // Morale face icon
    [SerializeField] private Image luckIcon;                     // Luck clover icon

    [Header("Army")]
    [SerializeField] private GarrisonSlotUI[] armySlots;         // 7 slots

    public void ShowHero(Hero hero)
    {
        heroNameText.text = hero.Name;
        levelClassText.text = $"Level {hero.Level} {hero.HeroClass.Name}";
        attackText.text = hero.GetPrimarySkill(PrimarySkill.Attack).ToString();
        defenseText.text = hero.GetPrimarySkill(PrimarySkill.Defense).ToString();
        powerText.text = hero.GetPrimarySkill(PrimarySkill.Power).ToString();
        knowledgeText.text = hero.GetPrimarySkill(PrimarySkill.Knowledge).ToString();
        manaText.text = $"{hero.Mana}/{hero.MaxMana}";

        UpdateMoraleIcon(hero.CalculateMorale());
        UpdateLuckIcon(hero.CalculateLuck());
        UpdateArmyDisplay(hero.Army);
    }
}
```

**2. HeroWindowUI** (Full Hero Screen):
```csharp
public class HeroWindowUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HeroPanelUI heroPanel;
    [SerializeField] private SecondarySkillsUI secondarySkills;
    [SerializeField] private ArtifactSlotsUI artifactSlots;
    [SerializeField] private GarrisonUI garrison;

    [Header("Buttons")]
    [SerializeField] private Button dismissButton;
    [SerializeField] private Button questLogButton;
    [SerializeField] private Button spellbookButton;
    [SerializeField] private Button backpackButton;

    public void Open(Hero hero)
    {
        heroPanel.ShowHero(hero);
        secondarySkills.ShowSkills(hero.SecondarySkills);
        artifactSlots.ShowEquipment(hero.Equipment);
        garrison.ShowArmy(hero.Army);

        dismissButton.interactable = CanDismissHero(hero);
    }

    private bool CanDismissHero(Hero hero)
    {
        // Can't dismiss if: Last hero + no towns, or mission critical
        return !hero.IsMissionCritical &&
               (gameState.GetTownCount() > 0 || gameState.GetHeroCount() > 1);
    }
}
```

**3. GarrisonSlotUI**:
```csharp
public class GarrisonSlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image creatureIcon;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Image selectionBorder;

    private CreatureStack stack;

    public void SetStack(CreatureStack stack)
    {
        this.stack = stack;

        if (stack == null || stack.Count == 0)
        {
            creatureIcon.gameObject.SetActive(false);
            countText.gameObject.SetActive(false);
        }
        else
        {
            creatureIcon.gameObject.SetActive(true);
            creatureIcon.sprite = stack.Creature.Icon;
            countText.text = stack.Count.ToString();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            garrison.OnSlotClicked(this);
        else if (eventData.button == PointerEventData.InputButton.Right)
            ShowCreatureTooltip();
    }
}
```

**4. SecondarySkillsUI**:
```csharp
public class SecondarySkillsUI : MonoBehaviour
{
    [SerializeField] private SkillSlotUI[] skillSlots;     // 8 visible slots
    [SerializeField] private Scrollbar scrollbar;           // If hero has >8 skills

    public void ShowSkills(List<SecondarySkill> skills)
    {
        scrollbar.gameObject.SetActive(skills.Count > 8);

        for (int i = 0; i < skillSlots.Length; i++)
        {
            if (i < skills.Count)
                skillSlots[i].SetSkill(skills[i]);
            else
                skillSlots[i].Clear();
        }
    }
}

public class SkillSlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;   // "Basic" / "Advanced" / "Expert"

    public void SetSkill(SecondarySkill skill)
    {
        icon.sprite = skill.Icon;
        nameText.text = skill.Name;
        levelText.text = skill.Level.ToString();  // "Basic", "Advanced", "Expert"
    }
}
```

**5. ArtifactSlotsUI**:
```csharp
public class ArtifactSlotsUI : MonoBehaviour
{
    [SerializeField] private ArtifactSlotUI[] wornSlots;   // 19 equipment slots
    [SerializeField] private ArtifactSlotUI[] backpackSlots; // 5 visible + scroll
    [SerializeField] private Button scrollLeftButton;
    [SerializeField] private Button scrollRightButton;

    private int backpackScrollOffset = 0;

    public void ShowEquipment(HeroEquipment equipment)
    {
        // Update worn slots (head, shoulders, neck, etc.)
        for (int i = 0; i < 19; i++)
        {
            var artifact = equipment.GetSlot((ArtifactSlot)i);
            wornSlots[i].SetArtifact(artifact);
        }

        // Update backpack with scroll
        UpdateBackpackDisplay(equipment.Backpack);
    }

    private void UpdateBackpackDisplay(List<Artifact> backpack)
    {
        for (int i = 0; i < backpackSlots.Length; i++)
        {
            int index = backpackScrollOffset + i;
            var artifact = (index < backpack.Count) ? backpack[index] : null;
            backpackSlots[i].SetArtifact(artifact);
        }

        scrollLeftButton.interactable = backpackScrollOffset > 0;
        scrollRightButton.interactable = backpackScrollOffset + 5 < backpack.Count;
    }
}
```

### Key Data Flow

**1. Hero Selection on Adventure Map**:
```
User clicks hero
  → AdventureMapController.OnHeroSelected(hero)
  → gameEventChannel.RaiseHeroSelected(hero)
  → HeroPanelUI.OnHeroSelected(hero) subscribes
  → HeroPanelUI.ShowHero(hero) updates display
```

**2. Opening Full Hero Window**:
```
User double-clicks hero OR clicks portrait in info panel
  → UIManager.OpenHeroWindow(hero)
  → Instantiate HeroWindowUI prefab
  → HeroWindowUI.Open(hero) populates all sections
```

**3. Hero Stat Updates** (e.g., after level up):
```
Hero gains level
  → Hero.LevelUp() updates exp, level, skills
  → gameEventChannel.RaiseHeroStatsChanged(hero)
  → HeroPanelUI updates if hero is selected
  → HeroWindowUI updates if window is open
```

**4. Army Slot Interaction** (drag & drop):
```
User clicks garrison slot
  → GarrisonSlotUI.OnPointerClick()
  → GarrisonUI.SelectSlot(slot)
  → Highlight selected slot
User clicks another slot
  → GarrisonUI.SwapSlots(fromSlot, toSlot)
  → hero.Army.SwapStacks(fromIndex, toIndex)
  → gameEventChannel.RaiseArmyChanged(hero)
  → Refresh garrison display
```

### Visual Layout Patterns

**Info Panel (192x192px)** - Compact view:
```
┌────────────────────┐
│ [Portrait] [Name]  │
│            [Class] │
│ ATT DEF POW KNW    │
│  12  10   8   6    │
│ [Morale] [Luck]    │
│ Mana: 50/100       │
│ [Army slots 1-7]   │
└────────────────────┘
```

**Full Hero Window** - Detailed view:
```
┌─────────────────────────────────────────┐
│ [Banner] Hero Name - Level X ClassName  │
├──────────────┬──────────────────────────┤
│ [Portrait]   │ Primary Skills:          │
│              │ ATT [12] DEF [10]        │
│ [Specialty]  │ POW [8]  KNW [6]         │
│ Description  │                          │
│              │ EXP: 5000                │
│ [Morale][Luck] Mana: 50/100            │
├──────────────┴──────────────────────────┤
│ Secondary Skills:                       │
│ [Wisdom    ] Advanced                   │
│ [Logistics ] Basic                      │
│ ... (up to 8, scroll if more)           │
├─────────────────────────────────────────┤
│       Artifact Slots (19 worn)          │
│  [Head] [Neck] [Shoulders] ...          │
│  [Backpack: <  [][][][][] > ]          │
├─────────────────────────────────────────┤
│       Army Garrison (7 slots)           │
│  [Creature][Count] ...                  │
├─────────────────────────────────────────┤
│ [Dismiss] [Quest] [Spellbook] [Close]   │
└─────────────────────────────────────────┘
```

### Implementation Checklist

**Phase 5A: Basic Hero Panel (Info Bar)**
- [ ] Create HeroPanelUI prefab (192x192)
- [ ] Add portrait, name, level display
- [ ] Add primary skill text fields (4)
- [ ] Add morale/luck icons
- [ ] Add mana text display
- [ ] Create 7 GarrisonSlotUI elements
- [ ] Wire up OnHeroSelected event subscription
- [ ] Test with sample hero data

**Phase 5B: Full Hero Window**
- [ ] Create HeroWindowUI prefab (full screen)
- [ ] Reuse HeroPanelUI component for header
- [ ] Create SecondarySkillsUI (8 slots + scrollbar)
- [ ] Create ArtifactSlotsUI (19 worn + 5 backpack visible)
- [ ] Create full GarrisonUI with drag & drop
- [ ] Add buttons: Dismiss, Quest Log, Spellbook, Close
- [ ] Implement dismiss validation
- [ ] Wire up double-click to open window
- [ ] Add unit tests

**Phase 5C: Interactive Features**
- [ ] Garrison slot drag & drop (swap stacks)
- [ ] Garrison split mode (divide stack)
- [ ] Artifact drag & drop (equip/unequip)
- [ ] Right-click tooltips (skills, artifacts, creatures)
- [ ] Hover descriptions
- [ ] Keyboard shortcuts (D=dismiss, Q=quest, S=spellbook)

### Key Takeaways

1. **Two-Tier Display**: Info panel (compact) + full window (detailed)
2. **Data Binding**: Hero object → UI components via events
3. **Modular Components**: Garrison, artifacts, skills are reusable
4. **Interactive vs Static**: Info panel can be static or interactive (garrison)
5. **Validation Logic**: Dismiss checks, slot interactions validated on game state
6. **Scroll Support**: Secondary skills (>8) and backpack (unlimited) need scrolling

---

*Research complete. Ready for hero panel implementation.*

---

## Hero ID Management Research (2025-10-07)

### Problem Statement

The Hero class has a constructor that takes `HeroTypeData` as a parameter:
```csharp
public Hero(HeroTypeData template, int playerId, Position startPosition)
{
    Id = template.heroId;  // ⚠️ Problem: Using template ID as instance ID
    TypeId = template.heroTypeId;
    // ...
}
```

**Question**: Should hero instances use the template's `heroId`, or should they get unique runtime instance IDs separate from template IDs?

### VCMI Analysis

#### Hero Type vs Hero Instance in VCMI

**CHero (Static Template)** - `/tmp/vcmi-temp/lib/entities/hero/CHero.h`:
- Represents hero TYPE definition (like "Catherine the Knight")
- Has `HeroTypeID` - identifies the hero type in the game data
- Static data: name, hero class, starting skills, specialty
- Stored in `CHeroHandler` - the hero type database

**CGHeroInstance (Runtime Instance)** - `/tmp/vcmi-temp/lib/mapObjects/CGHeroInstance.h`:
```cpp
class CGHeroInstance : public CArmedInstance
{
    ObjectInstanceID id;           // Unique runtime instance ID
    std::optional<HeroTypeID> heroType;  // Reference to CHero template

    // Runtime state
    TExpType exp;
    ui32 level;
    si32 mana;
    ui32 movement;
    // ...
};
```

**Key Findings**:
1. **Template ID ≠ Instance ID**: Hero instances have `ObjectInstanceID` (unique per instance), separate from `HeroTypeID` (identifies template)
2. **Multiple instances possible**: The same hero type can have multiple instances (e.g., two "Catherine" heroes in different games)
3. **ID assignment**: Object instance IDs are assigned by the map or game state at creation time

#### Hero Creation in VCMI

From `/tmp/vcmi-temp/lib/mapObjects/CGHeroInstance.cpp`:
```cpp
CGHeroInstance::CGHeroInstance(IGameInfoCallback * cb)
    : CArmedInstance(cb, BonusNodeType::HERO, false),
    level(1),
    exp(UNINITIALIZED_EXPERIENCE),
    // ...
{
    // Constructor does NOT assign instance ID
    // ID is assigned when object is added to map/game state
}
```

From VCMI game state management:
- Instance IDs are managed centrally by `CGameState`
- When a hero is created, the game state assigns a unique `ObjectInstanceID`
- The hero's `heroType` field references the template `HeroTypeID`

### Realms of Eldor Current Implementation

#### Current Pattern

**GameState.cs** (lines 100-140):
```csharp
public class GameState
{
    private Dictionary<int, Hero> heroes = new Dictionary<int, Hero>();
    private int nextHeroId = 1;  // ID generator

    public Hero AddHero(int typeId, int owner, Position position)
    {
        var hero = new Hero
        {
            Id = nextHeroId++,        // ✅ Generates unique runtime ID
            TypeId = typeId,          // ✅ References template type
            Owner = owner,
            Position = position,
            // ...
        };
        heroes[hero.Id] = hero;
        return hero;
    }
}
```

**Hero.cs** constructor (lines 59-73):
```csharp
public Hero(HeroTypeData template, int playerId, Position startPosition)
{
    Id = template.heroId;         // ❌ WRONG: Uses template ID
    TypeId = template.heroTypeId; // ✅ Correct: References template type
    // ...
}
```

**HeroTypeData.cs**:
```csharp
public class HeroTypeData : ScriptableObject
{
    public int heroTypeId;  // Template identifier (e.g., 1 = "Sir Roland")
    public string heroName;
    public HeroClass heroClass;
    // ...
}
```

#### Current Usage Patterns

**Tests use object initializer** (no constructor):
```csharp
// HeroTests.cs (line 13, 26, etc.)
var hero = new Hero { Level = 1, Experience = 0 };
```

**BattleController uses simple constructor**:
```csharp
// BattleController.cs (line 82, 88)
testAttacker = new Hero(1, 0);  // ❌ Constructor doesn't exist!
testDefender = new Hero(2, 1);
```

**CombatActionsTests uses non-existent constructor**:
```csharp
// CombatActionsTests.cs (line 18-19)
attackerHero = new Hero(1, "Attacker", HeroClass.KNIGHT);  // ❌ Doesn't exist!
defenderHero = new Hero(2, "Defender", HeroClass.KNIGHT);
```

**GameInitializer uses GameState.AddHero correctly**:
```csharp
// GameInitializer.cs (line 126, 139)
var hero1 = gameState.AddHero(heroType1.heroTypeId, owner: 0, player1HeroPosition);
hero1.CustomName = $"{heroType1.heroName} (P1)";
heroType1.InitializeHero(hero1);  // Populates stats, skills, army
```

### Analysis and Recommendations

#### Problems Identified

1. **Constructor confusion**: Hero class has:
   - A constructor taking `HeroTypeData` that incorrectly uses `template.heroId`
   - Tests/code expecting constructors that don't exist

2. **ID management inconsistency**:
   - GameState correctly generates unique IDs via `nextHeroId++`
   - Hero constructor tries to assign ID from template (wrong pattern)

3. **Two ID fields confusion**:
   - `Hero.Id` - Should be unique runtime instance ID
   - `Hero.TypeId` - Should reference the template type ID
   - Current constructor conflates these

#### Correct Pattern (VCMI-based)

**Template (HeroTypeData)**:
- `heroTypeId` - Identifies the hero type (1 = "Sir Roland", 2 = "Lord Haart")
- Used for: Database lookup, initialization data reference

**Instance (Hero)**:
- `Id` - Unique runtime instance ID (auto-generated by GameState)
- `TypeId` - References the template `heroTypeId`
- Created via: `GameState.AddHero(typeId, owner, position)`

#### Recommended Solution

**Option 1: Remove problematic constructor, use object initializer + factory pattern** (CURRENT PATTERN):
```csharp
public class Hero
{
    // NO public constructor - use GameState.AddHero() factory

    public int Id { get; set; }      // Runtime instance ID (set by GameState)
    public int TypeId { get; set; }  // Template reference
    // ...
}

// GameState.cs
public Hero AddHero(int typeId, int owner, Position position)
{
    var hero = new Hero
    {
        Id = nextHeroId++,  // Unique runtime ID
        TypeId = typeId,    // Template reference
        Owner = owner,
        Position = position,
    };
    heroes[hero.Id] = hero;
    return hero;
}

// Usage (GameInitializer.cs)
var hero = gameState.AddHero(heroType.heroTypeId, 0, position);
heroType.InitializeHero(hero);  // Populate from template
```

**Option 2: Add internal constructor for GameState** (NOT RECOMMENDED - too complex):
```csharp
public class Hero
{
    // Internal constructor - only GameState can create heroes
    internal Hero(int runtimeId, int typeId, int owner, Position position)
    {
        Id = runtimeId;
        TypeId = typeId;
        Owner = owner;
        Position = position;
    }
}
```

### Conclusion: Correct Architecture

#### Hero IDs Should Work Like This:

1. **HeroTypeData.heroTypeId**: Template identifier (1, 2, 3, etc.)
   - Static, defined in ScriptableObject asset
   - Shared by all instances of that hero type
   - Used for: Database lookup, initialization

2. **Hero.Id**: Runtime instance identifier (1, 2, 3, etc.)
   - Dynamic, auto-generated by GameState
   - Unique per hero instance
   - Used for: Dictionary lookup, serialization, references

3. **Hero.TypeId**: Reference to template
   - Copy of `HeroTypeData.heroTypeId`
   - Allows looking up template data later

#### Correct Constructor Signature:

**Remove the HeroTypeData constructor entirely**. Hero creation should ONLY happen via `GameState.AddHero()`:

```csharp
public class Hero
{
    // Use object initializer pattern (current approach is correct)
    public int Id { get; set; }
    public int TypeId { get; set; }
    public string CustomName { get; set; }
    // ...
}
```

**Factory pattern in GameState** (already implemented correctly):
```csharp
public Hero AddHero(int typeId, int owner, Position position)
{
    var hero = new Hero
    {
        Id = nextHeroId++,  // ✅ Unique runtime ID
        TypeId = typeId,    // ✅ Template reference
        // ...
    };
    heroes[hero.Id] = hero;
    return hero;
}
```

**Initialization from template** (also correct):
```csharp
var hero = gameState.AddHero(heroTypeData.heroTypeId, playerId, position);
heroTypeData.InitializeHero(hero);  // Populates stats, skills, army from template
```

#### What to Fix:

1. **Remove the problematic constructor** from Hero.cs (line 59-73)
2. **Fix test files** that expect non-existent constructors:
   - BattleController.cs (lines 82, 88)
   - CombatActionsTests.cs (lines 18-19)
3. **Keep using GameState.AddHero()** as the single source of hero creation

### ID Generation Patterns in Codebase

**Current ID Generators** (from GameState.cs):
```csharp
private int nextHeroId = 1;     // Hero instance IDs
private int nextPlayerId = 0;   // Player IDs
```

**Consistent with VCMI**: Central ID management in GameState, not in individual classes.

---

*Research complete: Hero instances need unique runtime IDs (managed by GameState), separate from template IDs (in HeroTypeData). Remove problematic Hero constructor.*
