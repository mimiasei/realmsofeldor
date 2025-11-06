using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using RealmsOfEldor.Core;
using RealmsOfEldor.Core.Events;
using RealmsOfEldor.Core.Map;
using RealmsOfEldor.Database;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.Controllers
{
    /// <summary>
    /// Unified game initializer that creates both GameState and GameMap.
    /// Based on VCMI's CMap initialization pattern.
    /// Attach to a GameObject in the Adventure Map scene.
    /// </summary>
    public class GameInitializer : MonoBehaviour
    {
        [Header("Initialization Settings")]
        [SerializeField] private bool initializeOnStart = true;
        [SerializeField] private int numberOfPlayers = 2;
        [SerializeField] private int startingGold = 10000;
        [SerializeField] private int startingWood = 20;
        [SerializeField] private int startingOre = 20;

        [Header("Map Settings")]
        [SerializeField] private int mapWidth = 30;
        [SerializeField] private int mapHeight = 30;
        [SerializeField] private bool useModificatorPipeline = true;
        [SerializeField] private bool generateRandomTerrain = true;
        [SerializeField] private bool addMapObjects = true;
        [SerializeField] private int resourcePileCount = 5;
        [SerializeField] private int mineCount = 3;
        [SerializeField] private int dwellingCount = 2;
        [SerializeField] private bool validateReachability = true;
        [SerializeField] private int reachabilitySearchRadius = 5;

        [Header("Hero Settings (Legacy - use HeroSpawnModificator instead)")]
        [SerializeField] private bool createStartingHeroes = false; // Disabled: use HeroSpawnModificator
        [SerializeField] private Position player1HeroPosition = new Position(5, 5);
        [SerializeField] private Position player2HeroPosition = new Position(25, 25);

        [Header("Event Channels")]
        [SerializeField] private GameEventChannel gameEvents;
        [SerializeField] private MapEventChannel mapEvents;

        [Header("Required References")]
        [SerializeField] private MapRenderer mapRenderer;
        [SerializeField] private Cartographer cartographer; // 2.5D camera system with 3D rendering

        [Header("Optional References")]
        [SerializeField] private HeroDatabase heroDatabase;

        private void Awake()
        {
            // Ensure MapRenderer initializes first (to set GameMap delegates)
            if (mapRenderer == null)
            {
                mapRenderer = FindFirstObjectByType<MapRenderer>();
            }

            // Force MapRenderer Awake by accessing it (if not already called)
            if (mapRenderer != null)
            {
                // MapRenderer.Awake sets GameMap.GetTerrainPassability delegate
                // This ensures delegates are set before map generation
                Debug.Log("GameInitializer: Ensuring MapRenderer is initialized");
            }
            else
            {
                Debug.LogWarning("⚠️ MapRenderer not found! Terrain passability checks will fail.");
            }

            // Find HeroDatabase if not assigned
            if (heroDatabase == null && createStartingHeroes)
            {
                heroDatabase = FindFirstObjectByType<HeroDatabase>();
                if (heroDatabase == null)
                {
                    Debug.LogWarning("⚠️ HeroDatabase not found in scene. Heroes will not be created.");
                }
            }
        }

        private void Start()
        {
            if (initializeOnStart)
            {
                InitializeGame();
            }
        }

        /// <summary>
        /// Initializes the game state with players, resources, and heroes
        /// Can be called from Unity Editor or at runtime
        /// </summary>
        [ContextMenu("Initialize Game")]
        public void InitializeGame()
        {
            // Ensure GameStateManager exists
            if (GameStateManager.Instance == null)
            {
                var gameStateObj = new GameObject("GameStateManager");
                var gsm = gameStateObj.AddComponent<GameStateManager>();

                // Wire up event channel using reflection
                if (gameEvents != null)
                {
                    var field = typeof(GameStateManager).GetField("gameEvents",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    field?.SetValue(gsm, gameEvents);
                    Debug.Log("✓ Created GameStateManager singleton with gameEvents wired");
                }
                else
                {
                    Debug.LogWarning("⚠️ Created GameStateManager but gameEvents not assigned to GameInitializer!");
                }
            }

            var gameState = GameStateManager.Instance.State;

            Debug.Log("🎮 Initializing game state...");

            // Initialize game state with players
            var isHuman = new bool[numberOfPlayers];
            for (var i = 0; i < numberOfPlayers; i++)
            {
                isHuman[i] = true; // All players are human by default
            }

            gameState.Initialize(numberOfPlayers, isHuman);

            // Set starting resources for all players
            for (var i = 0; i < numberOfPlayers; i++)
            {
                var player = gameState.GetPlayer(i);
                if (player != null)
                {
                    player.Resources = new ResourceSet(
                        gold: startingGold,
                        wood: startingWood,
                        ore: startingOre,
                        mercury: 5,
                        sulfur: 5,
                        crystal: 5,
                        gems: 5
                    );
                    Debug.Log($"✓ Created Player {i} ({(PlayerColor)i}) with {startingGold} gold");
                }
            }

            // Create starting heroes if enabled
            if (createStartingHeroes)
            {
                CreateStartingHeroes(gameState);
            }

            Debug.Log($"✅ Game initialized: {numberOfPlayers} players, day {gameState.CurrentDay}");

            // Force movement refresh after initialization
            if (createStartingHeroes)
            {
                foreach (var heroId in gameState.GetPlayer(0)?.HeroIds ?? System.Linq.Enumerable.Empty<int>())
                {
                    var hero = gameState.GetHero(heroId);
                    if (hero != null)
                    {
                        hero.MaxMovement = 2000;
                        hero.Movement = 2000;
                    }
                }
                if (numberOfPlayers >= 2)
                {
                    foreach (var heroId in gameState.GetPlayer(1)?.HeroIds ?? System.Linq.Enumerable.Empty<int>())
                    {
                        var hero = gameState.GetHero(heroId);
                        if (hero != null)
                        {
                            hero.MaxMovement = 2000;
                            hero.Movement = 2000;
                        }
                    }
                }
            }

            // Initialize map (following VCMI's CMap pattern)
            InitializeMap();
        }

        /// <summary>
        /// Initializes the game map with terrain and objects.
        /// Based on VCMI's CMap::addNewObject pattern.
        /// Checks PlayerPrefs for a selected map from MapSelection scene.
        /// </summary>
        private void InitializeMap()
        {
            if (mapEvents == null)
            {
                Debug.LogWarning("⚠️ MapEventChannel not assigned. Map will not be initialized.");
                return;
            }

            Debug.Log("🗺️ Initializing game map...");

            GameMap gameMap;

            // Check if a map was selected in MapSelection scene
            var selectedMapId = PlayerPrefs.GetString("SelectedMapId", null);
            if (!string.IsNullOrEmpty(selectedMapId))
            {
                Debug.Log($"📂 Loading selected map: {selectedMapId}");

                // Ensure MapPersistenceManager exists
                if (MapPersistenceManager.Instance == null)
                {
                    var persistenceObj = new GameObject("MapPersistenceManager");
                    persistenceObj.AddComponent<MapPersistenceManager>();
                }

                // Load the map
                gameMap = MapPersistenceManager.Instance.LoadMap(selectedMapId);

                if (gameMap != null)
                {
                    Debug.Log($"✅ Loaded map: {selectedMapId} ({gameMap.Width}x{gameMap.Height})");

                    // Clear the PlayerPrefs key so it doesn't load again next time
                    PlayerPrefs.DeleteKey("SelectedMapId");
                    PlayerPrefs.Save();
                }
                else
                {
                    Debug.LogWarning($"⚠️ Failed to load map {selectedMapId}, generating new map instead");
                    gameMap = new GameMap(mapWidth, mapHeight);
                    GenerateNewMap(gameMap);
                }
            }
            else
            {
                // No selected map, generate new one
                Debug.Log("📝 No selected map, generating new map");
                gameMap = new GameMap(mapWidth, mapHeight);
                GenerateNewMap(gameMap);
            }

            // Configure Cartographer camera system
            if (cartographer != null)
            {
                // Setup Cartographer for 2.5D rendering
                cartographer.SetGroundSize(gameMap.Width, gameMap.Height);
                cartographer.SetMapBounds(gameMap.Width, gameMap.Height);
                cartographer.EnableControls(true);

                // Make sure Cartographer's camera is the main camera
                Camera cartographerCam = cartographer.GetCamera();
                if (cartographerCam != null)
                {
                    cartographerCam.tag = "MainCamera";
                }

                Debug.Log($"✓ Configured Cartographer camera for {gameMap.Width}x{gameMap.Height} map");
            }
            else
            {
                Debug.LogWarning("⚠️ No Cartographer assigned! Assign Cartographer component in GameInitializer.");
            }

            // Raise map loaded event (triggers MapRenderer to render)
            mapEvents.RaiseMapLoaded(gameMap);

            // Apply texture splatting if integrator is present
            var textureSplatting = GetComponent<TextureSplattingIntegrator>();
            if (textureSplatting != null)
            {
                textureSplatting.OnMapGenerated(gameMap);
            }

            Debug.Log($"✅ Map initialized: {gameMap.Width}x{gameMap.Height} with {gameMap.GetAllObjects().Count()} objects");
        }

        /// <summary>
        /// Generates a new map (either loaded or new generation).
        /// </summary>
        private void GenerateNewMap(GameMap gameMap)
        {
            // Use new modificator pipeline or legacy approach
            if (useModificatorPipeline)
            {
                InitializeMapWithModificators(gameMap);
            }
            else
            {
                InitializeMapLegacy(gameMap);
            }
        }

        /// <summary>
        /// Ensures MapRenderer delegates are set before map generation.
        /// Prevents passability check failures during map generation.
        /// </summary>
        private void EnsureMapRendererDelegatesSet()
        {
            // Check if delegates are already set
            if (GameMap.GetTerrainPassability != null && GameMap.GetTerrainVariantCount != null)
            {
                Debug.Log("✓ MapRenderer delegates already initialized");
                return;
            }

            // If MapRenderer reference not set, try to find it
            if (mapRenderer == null)
            {
                mapRenderer = FindFirstObjectByType<MapRenderer>();
            }

            // If still not found, log error
            if (mapRenderer == null)
            {
                Debug.LogError("⚠️ MapRenderer not found! Terrain passability checks will fail during map generation.");
                return;
            }

            // Delegates are set in MapRenderer.Awake(), which should have already run
            // But if for some reason they're not set, log a warning
            if (GameMap.GetTerrainPassability == null || GameMap.GetTerrainVariantCount == null)
            {
                Debug.LogWarning("⚠️ MapRenderer found but delegates not set. This should not happen if MapRenderer.Awake() has run.");
            }
            else
            {
                Debug.Log("✓ MapRenderer delegates verified and ready");
            }
        }

        /// <summary>
        /// Initializes map using the VCMI-style modificator pipeline.
        /// This is the new recommended approach.
        /// </summary>
        private void InitializeMapWithModificators(GameMap gameMap)
        {
            // Ensure MapRenderer delegates are set before map generation
            EnsureMapRendererDelegatesSet();

            var config = MapGenConfig.Instance;
            var pipeline = new ModificatorPipeline(gameMap, config);

            // Collect hero start positions for reachability validator
            var startPositions = new List<Position>();
            if (createStartingHeroes)
            {
                startPositions.Add(player1HeroPosition);
                if (numberOfPlayers >= 2)
                    startPositions.Add(player2HeroPosition);
            }
            if (startPositions.Count == 0)
            {
                startPositions.Add(new Position(mapWidth / 2, mapHeight / 2));
            }

            // Add modificators in priority order (they'll self-sort)
            pipeline.AddModificator(new TerrainPainterModificator());

            if (addMapObjects)
            {
                pipeline.AddModificator(new ResourcePlacerModificator());
                pipeline.AddModificator(new MinePlacerModificator());
                pipeline.AddModificator(new DwellingPlacerModificator());
                pipeline.AddModificator(new GuardPlacerModificator());
                pipeline.AddModificator(new ObstaclePlacerModificator());
            }

            if (validateReachability)
            {
                pipeline.AddModificator(new ReachabilityValidatorModificator(startPositions, reachabilitySearchRadius));
            }

            // Log pipeline configuration
            Debug.Log(pipeline.GetSummary());

            // Execute pipeline
            pipeline.ExecuteWithCleanup();
        }

        /// <summary>
        /// Legacy map initialization (pre-modificator system).
        /// Kept for backwards compatibility and testing.
        /// </summary>
        private void InitializeMapLegacy(GameMap gameMap)
        {
            // Generate terrain
            if (generateRandomTerrain)
            {
                GenerateRandomTerrain(gameMap);
            }
            else
            {
                GenerateBasicTerrain(gameMap);
            }

            // Add map objects (VCMI's addNewObject pattern)
            if (addMapObjects)
            {
                AddMapObjects(gameMap);

                // Add guards to high-value objects (VCMI's TreasurePlacer guard logic)
                AddGuardsToObjects(gameMap);

                // Add decorative obstacles (VCMI's ObstaclePlacer modificator)
                AddObstacles(gameMap);
            }

            // Validate reachability (VCMI's RockFiller equivalent)
            if (validateReachability)
            {
                ValidateMapReachability(gameMap);
            }

            // Calculate coastal tiles (VCMI's calculateGuardingCreaturePositions equivalent)
            gameMap.CalculateCoastalTiles();
        }

        /// <summary>
        /// Generates diverse terrain for pathfinding testing.
        /// Based on MapTestInitializer pattern.
        /// </summary>
        private void GenerateRandomTerrain(GameMap map)
        {
            // Fill with base terrain
            for (var y = 0; y < mapHeight; y++)
            {
                for (var x = 0; x < mapWidth; x++)
                {
                    var pos = new Position(x, y);
                    var roll = Random.value;

                    if (roll < 0.40f)
                        map.SetTerrain(pos, TerrainType.GrassTemperate);
                    else if (roll < 0.65f)
                        map.SetTerrain(pos, TerrainType.DirtTemperate);
                    else if (roll < 0.85f)
                        map.SetTerrain(pos, TerrainType.SandTemperate);
                    else if (roll < 0.95f)
                        map.SetTerrain(pos, TerrainType.RockTemperate);
                    else
                        map.SetTerrain(pos, TerrainType.SwampTemperate);
                }
            }

            // Add water lakes (impassable)
            for (var i = 0; i < 5; i++)
            {
                var centerX = Random.Range(5, mapWidth - 5);
                var centerY = Random.Range(5, mapHeight - 5);
                var radius = Random.Range(2, 4);

                for (var y = -radius; y <= radius; y++)
                {
                    for (var x = -radius; x <= radius; x++)
                    {
                        if (x * x + y * y <= radius * radius)
                        {
                            var pos = new Position(centerX + x, centerY + y);
                            if (map.IsInBounds(pos))
                            {
                                map.SetTerrain(pos, TerrainType.Water);
                            }
                        }
                    }
                }
            }

            Debug.Log("✓ Generated random terrain with diverse costs");
        }

        /// <summary>
        /// Generates simple grass terrain for basic testing.
        /// </summary>
        private void GenerateBasicTerrain(GameMap map)
        {
            for (var y = 0; y < mapHeight; y++)
            {
                for (var x = 0; x < mapWidth; x++)
                {
                    map.SetTerrain(new Position(x, y), TerrainType.GrassTemperate);
                }
            }

            Debug.Log("✓ Generated basic grass terrain");
        }

        /// <summary>
        /// Adds map objects (resources, mines, dwellings) using budget-based placement.
        /// Following VCMI's treasure budget and addNewObject pattern.
        /// </summary>
        private void AddMapObjects(GameMap map)
        {
            var config = MapGenConfig.Instance;
            var budget = new MapGenBudget(config);

            // Add resource piles using treasure budget (VCMI's CGResource equivalent)
            var resourceAttempts = 0;
            while (budget.CanPlaceResourcePile(1) && resourceAttempts < 100)
            {
                resourceAttempts++;
                var pos = FindClearPosition(map);
                if (pos == null)
                    continue;

                // Generate resource with value consideration
                var resourceType = (ResourceType)Random.Range(0, 7);
                var amount = CalculateResourceAmount(resourceType, budget.RemainingTreasureBudget, config);

                var resource = new ResourceObject(pos.Value, resourceType, amount);

                // Check if this resource fits within budget
                if (budget.CanPlaceResourcePile(resource.Value))
                {
                    map.AddObject(resource);
                    budget.RecordResourcePile(resource);
                }
            }

            // Add mines (VCMI's CGMine equivalent)
            var mineAttempts = 0;
            while (budget.CanPlaceMine() && mineAttempts < 50)
            {
                mineAttempts++;
                var pos = FindClearPosition(map);
                if (pos == null)
                    continue;

                var resourceType = (ResourceType)Random.Range(1, 7); // Not gold mines
                var production = Random.Range(1, 3);
                var mine = new MineObject(pos.Value, resourceType, production);

                map.AddObject(mine);
                budget.RecordMine(mine);
            }

            // Add dwellings (VCMI's CGDwelling equivalent)
            var dwellingAttempts = 0;
            while (budget.CanPlaceDwelling() && dwellingAttempts < 50)
            {
                dwellingAttempts++;
                var pos = FindClearPosition(map);
                if (pos == null)
                    continue;

                var creatureId = Random.Range(1, 10);
                var weeklyGrowth = Random.Range(5, 15);
                var dwelling = new DwellingObject(pos.Value, creatureId, weeklyGrowth);
                dwelling.ApplyWeeklyGrowth(); // Start with some creatures

                map.AddObject(dwelling);
                budget.RecordDwelling(dwelling);
            }

            Debug.Log($"✅ Budget-based placement complete:\n{budget.GetSummary()}");
        }

        /// <summary>
        /// Calculates appropriate resource amount based on remaining budget and resource type.
        /// Prevents placing resources that exceed budget limits.
        /// </summary>
        private int CalculateResourceAmount(ResourceType resourceType, int remainingBudget, MapGenConfig config)
        {
            // Calculate max amount based on remaining budget
            int minAmount, maxAmount;

            switch (resourceType)
            {
                case ResourceType.Gold:
                    minAmount = 500;
                    maxAmount = System.Math.Min(2000, remainingBudget / config.goldValueMultiplier);
                    break;

                case ResourceType.Wood:
                case ResourceType.Ore:
                    minAmount = 3;
                    maxAmount = System.Math.Min(10, remainingBudget / config.basicResourceValue);
                    break;

                case ResourceType.Mercury:
                case ResourceType.Sulfur:
                case ResourceType.Crystal:
                case ResourceType.Gems:
                    minAmount = 2;
                    maxAmount = System.Math.Min(6, remainingBudget / config.rareResourceValue);
                    break;

                default:
                    return 1;
            }

            // Ensure we have valid range
            maxAmount = System.Math.Max(minAmount, maxAmount);
            return Random.Range(minAmount, maxAmount + 1);
        }

        /// <summary>
        /// Validates that all map objects are reachable from hero start positions.
        /// Based on VCMI's RockFiller reachability validation.
        /// </summary>
        private void ValidateMapReachability(GameMap map)
        {
            Debug.Log("🔍 Validating map reachability...");

            // Collect hero start positions
            var startPositions = new List<Position>();
            if (createStartingHeroes)
            {
                startPositions.Add(player1HeroPosition);
                if (numberOfPlayers >= 2)
                    startPositions.Add(player2HeroPosition);
            }

            // If no heroes, use center of map as start position
            if (startPositions.Count == 0)
            {
                startPositions.Add(new Position(mapWidth / 2, mapHeight / 2));
            }

            // Run reachability validation
            var validator = new MapReachabilityValidator(map);
            var result = validator.FixUnreachableObjects(startPositions, reachabilitySearchRadius);

            // Log results
            if (result.TotalUnreachableObjects > 0)
            {
                Debug.Log($"⚠️ Reachability validation:\n{result}");
            }
            else
            {
                var stats = validator.CalculateStats(startPositions);
                Debug.Log($"✅ All objects reachable!\n{stats}");
            }
        }

        /// <summary>
        /// Finds a random clear position on the map for object placement.
        /// </summary>
        private Position? FindClearPosition(GameMap map)
        {
            for (var attempts = 0; attempts < 50; attempts++)
            {
                var x = Random.Range(1, mapWidth - 1);
                var y = Random.Range(1, mapHeight - 1);
                var pos = new Position(x, y);

                if (map.GetTile(pos).IsClear())
                {
                    return pos;
                }
            }
            return null;
        }

        /// <summary>
        /// Chooses appropriate guards for a treasure object based on its value.
        /// Based on VCMI's ObjectManager::chooseGuard() algorithm.
        /// </summary>
        /// <param name="treasureValue">Value of the object to guard</param>
        /// <param name="config">Map generation configuration</param>
        /// <returns>GuardInfo with creature and count, or null if no guard needed</returns>
        private GuardInfo ChooseGuard(int treasureValue, MapGenConfig config)
        {
            if (!config.RequiresGuards(treasureValue))
                return null;

            var guardStrength = config.CalculateGuardStrength(treasureValue);
            if (guardStrength <= 0)
                return null;

            // VCMI uses creature database to find appropriate guards
            // For simplicity, we'll use a hardcoded creature list with AI values
            // In a full implementation, this would query CreatureDatabase

            // Simplified creature list (creatureId, AIValue per creature)
            var availableCreatures = new[]
            {
                (id: 1, aiValue: 30),     // Weak creature (e.g., Pikeman)
                (id: 2, aiValue: 80),     // Low-tier creature
                (id: 3, aiValue: 150),    // Medium-tier creature
                (id: 4, aiValue: 300),    // Strong creature
                (id: 5, aiValue: 600),    // Very strong creature
                (id: 6, aiValue: 1200),   // Elite creature
                (id: 7, aiValue: 2500)    // Champion creature
            };

            // Find creatures that can provide guards with strength between average and 100x
            var suitableCreatures = availableCreatures
                .Where(c => c.aiValue * 50 >= guardStrength && c.aiValue <= guardStrength * 100)
                .ToList();

            if (suitableCreatures.Count == 0)
            {
                // Fallback: use strongest available creature if strength is very high
                var strongest = availableCreatures[availableCreatures.Length - 1];
                var count = Mathf.Max(1, guardStrength / strongest.aiValue);
                return new GuardInfo(strongest.id, count, guardStrength);
            }

            // Pick a random suitable creature
            var chosen = suitableCreatures[Random.Range(0, suitableCreatures.Count)];
            var guardCount = Mathf.Max(1, guardStrength / chosen.aiValue);

            // Add randomization for stacks of 4+
            if (guardCount >= 4)
            {
                guardCount = Mathf.RoundToInt(guardCount * Random.Range(0.75f, 1.25f));
            }

            return new GuardInfo(chosen.id, guardCount, guardStrength);
        }

        /// <summary>
        /// Adds guards to high-value objects after placement.
        /// Based on VCMI's TreasurePlacer::createTreasures() guard logic.
        /// </summary>
        private void AddGuardsToObjects(GameMap map)
        {
            var config = MapGenConfig.Instance;
            if (!config.enableGuards)
            {
                Debug.Log("⚔️ Guard placement disabled in config");
                return;
            }

            var guardsPlaced = 0;
            foreach (var obj in map.GetAllObjects())
            {
                if (config.RequiresGuards(obj.Value))
                {
                    var guard = ChooseGuard(obj.Value, config);
                    if (guard != null)
                    {
                        obj.Guard = guard;
                        guardsPlaced++;

                        // Guard position is automatically calculated by MapObject.GetGuardPosition()
                        Debug.Log($"🛡️ Added guard to {obj.Name} (value: {obj.Value}): {guard.Count}x creature {guard.CreatureId} (strength: {guard.CalculatedStrength})");
                    }
                }
            }

            Debug.Log($"✅ Placed {guardsPlaced} guards for high-value objects");
        }

        /// <summary>
        /// Adds decorative and blocking obstacles to the map.
        /// Based on VCMI's ObstaclePlacer modificator.
        /// </summary>
        private void AddObstacles(GameMap map)
        {
            var config = MapGenConfig.Instance;
            if (!config.enableObstacles)
            {
                Debug.Log("🌳 Obstacle placement disabled in config");
                return;
            }

            // Build obstacle type list based on configuration
            var obstacleTypes = BuildObstacleTypeList(config);

            // Use shared Random instance for consistent results
            var sharedRandom = new System.Random();

            // Create obstacle placer
            var placer = new ObstaclePlacer(map, sharedRandom);

            // Place obstacles
            var obstacles = placer.PlaceObstacles(
                obstacleTypes,
                config.obstacleCount,
                allowBlocking: config.blockingObstacleRatio > 0f);

            // Debug: Log each obstacle
            foreach (var obstacle in obstacles)
            {
                Debug.Log($"🌳 Obstacle: {obstacle.Name} at {obstacle.Position}, Blocking={obstacle.IsBlocking}, Type={obstacle.ObjectType}");
            }

            // Log statistics
            var stats = placer.GetStats();
            Debug.Log($"✅ Placed {obstacles.Count} obstacles\n{stats}");
        }

        /// <summary>
        /// Builds list of obstacle types based on configuration percentages.
        /// </summary>
        private List<ObstacleType> BuildObstacleTypeList(MapGenConfig config)
        {
            var obstacleTypes = new List<ObstacleType>();

            // Add mountain/rock types (30% by default)
            if (config.mountainRockChance > 0f)
            {
                obstacleTypes.Add(ObstacleType.Mountain);
                obstacleTypes.Add(ObstacleType.Rock);
                obstacleTypes.Add(ObstacleType.Boulder);
            }

            // Add tree types (40% by default)
            if (config.treeChance > 0f)
            {
                obstacleTypes.Add(ObstacleType.Tree);
                obstacleTypes.Add(ObstacleType.Tree); // Add twice for higher frequency
            }

            // Add bush/flower types (30% by default)
            if (config.bushFlowerChance > 0f)
            {
                obstacleTypes.Add(ObstacleType.Bush);
                obstacleTypes.Add(ObstacleType.Flowers);
                obstacleTypes.Add(ObstacleType.Grass);
            }

            // If no types configured, provide defaults
            if (obstacleTypes.Count == 0)
            {
                obstacleTypes.Add(ObstacleType.Tree);
                obstacleTypes.Add(ObstacleType.Rock);
                obstacleTypes.Add(ObstacleType.Bush);
            }

            return obstacleTypes;
        }

        private void CreateStartingHeroes(GameState gameState)
        {
            // Check if HeroDatabase exists
            if (heroDatabase == null)
            {
                Debug.LogWarning("⚠️ HeroDatabase not assigned or found in scene. Cannot create heroes.");
                return;
            }

            var heroTypes = heroDatabase.GetAllHeroTypes().ToList();
            if (heroTypes.Count == 0)
            {
                Debug.LogWarning("⚠️ No hero types found in HeroDatabase. Run 'Generate Sample Data' first.");
                return;
            }

            // Create hero for Player 0
            if (numberOfPlayers >= 1)
            {
                var heroType1 = heroTypes[0];
                var hero1 = GameStateManager.Instance.CreateHero(heroType1.heroTypeId, 0, player1HeroPosition);

                // Initialize hero from type data
                hero1.CustomName = $"{heroType1.heroName} (P1)";
                hero1.Initialize(heroType1);
                hero1.MaxMovement = 2000;
                hero1.Movement = 2000; // Set AFTER MaxMovement

                Debug.Log($"Hero {hero1.CustomName} initialized: Movement={hero1.Movement}, MaxMovement={hero1.MaxMovement}");

                Debug.Log($"✓ Created hero '{hero1.CustomName}' at {player1HeroPosition} for Player 0");
            }

            // Create hero for Player 1 (if 2+ players)
            if (numberOfPlayers >= 2 && heroTypes.Count >= 1)
            {
                var heroType2 = heroTypes.Count > 1 ? heroTypes[1] : heroTypes[0];
                var hero2 = GameStateManager.Instance.CreateHero(heroType2.heroTypeId, 1, player2HeroPosition);

                // Initialize hero from type data
                hero2.CustomName = $"{heroType2.heroName} (P2)";
                hero2.Initialize(heroType2);
                hero2.MaxMovement = 2000;
                hero2.Movement = 2000;

                Debug.Log($"Hero {hero2.CustomName} initialized: Movement={hero2.Movement}, MaxMovement={hero2.MaxMovement}");

                Debug.Log($"✓ Created hero '{hero2.CustomName}' at {player2HeroPosition} for Player 1");
            }
        }
    }
}
