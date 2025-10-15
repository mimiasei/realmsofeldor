using System.Linq;
using UnityEngine;
using RealmsOfEldor.Core;
using RealmsOfEldor.Core.Events;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.Controllers
{
    /// <summary>
    /// Test script to initialize a sample map with various terrain types and objects.
    /// Attach this to a GameObject in the scene and assign the MapRenderer and MapEventChannel.
    /// </summary>
    public class MapTestInitializer : MonoBehaviour
    {
        [Header("Required References")]
        [SerializeField] private MapRenderer mapRenderer;
        [SerializeField] private MapEventChannel mapEvents;
        [SerializeField] private CameraController cameraController;

        [Header("Map Settings")]
        [SerializeField] private int mapWidth = 30;
        [SerializeField] private int mapHeight = 30;

        [Header("Test Options")]
        [SerializeField] private bool generateRandomTerrain = true;
        [SerializeField] private bool addSampleObjects = true;

        private GameMap gameMap;

        private void Start()
        {
            if (mapRenderer == null)
            {
                Debug.LogError("MapTestInitializer: MapRenderer reference not set!");
                return;
            }

            if (mapEvents == null)
            {
                Debug.LogError("MapTestInitializer: MapEventChannel reference not set!");
                return;
            }

            InitializeTestMap();
        }

        private void InitializeTestMap()
        {
            Debug.Log($"Initializing test map ({mapWidth}x{mapHeight})...");

            // Create game map
            gameMap = new GameMap(mapWidth, mapHeight);

            // Generate terrain
            if (generateRandomTerrain)
            {
                GenerateRandomTerrain();
            }
            else
            {
                GenerateCheckerboardPattern();
            }

            // Add sample objects
            if (addSampleObjects)
            {
                AddSampleObjects();
            }

            // Calculate coastal tiles
            gameMap.CalculateCoastalTiles();

            // Configure camera bounds if camera controller is assigned
            if (cameraController != null)
            {
                cameraController.SetMapBounds(mapWidth, mapHeight);

                // Set perspective camera angle
                var cam = cameraController.GetComponent<Camera>();
                if (cam != null)
                {
                    cam.orthographic = false;
                    cam.fieldOfView = 20f;
                    cam.transform.rotation = Quaternion.Euler(-30f, 0f, 0f);
                    var pos = cam.transform.position;
                    // Y offset compensates for 30° tilt: -50 * tan(30°) ≈ -29
                    cam.transform.position = new Vector3(pos.x, pos.y - 29f, -50f);
                }
            }

            // Raise map loaded event - this triggers MapRenderer to render via event subscription
            mapEvents.RaiseMapLoaded(gameMap);

            Debug.Log($"<color=green>✓ Test map initialized with {gameMap.Width}x{gameMap.Height} tiles</color>");
        }

        private void GenerateRandomTerrain()
        {
            // PATHFINDING TEST MAP GENERATOR
            // Creates diverse terrain to test A* pathfinding:
            // - Passable: Grass (100), Dirt (110), Sand (125), Rough (150), Swamp (175)
            // - Impassable: Water, Snow, Lava, Rock
            // - Varied costs encourage pathfinder to find optimal routes

            // Step 1: Fill map with varied passable terrain (strategic distribution)
            for (var y = 0; y < mapHeight; y++)
            {
                for (var x = 0; x < mapWidth; x++)
                {
                    var pos = new Position(x, y);
                    var roll = Random.value;

                    // 40% Grass (fast), 25% Dirt (medium), 20% Sand (medium-slow), 10% Rough (slow), 5% Swamp (very slow)
                    if (roll < 0.40f)
                        gameMap.SetTerrain(pos, TerrainType.Grass);
                    else if (roll < 0.65f)
                        gameMap.SetTerrain(pos, TerrainType.Dirt);
                    else if (roll < 0.85f)
                        gameMap.SetTerrain(pos, TerrainType.Sand);
                    else if (roll < 0.95f)
                        gameMap.SetTerrain(pos, TerrainType.Rough);
                    else
                        gameMap.SetTerrain(pos, TerrainType.Swamp);
                }
            }

            // Step 2: Create water lakes (impassable obstacles)
            for (var i = 0; i < 8; i++)
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
                            if (gameMap.IsInBounds(pos))
                            {
                                gameMap.SetTerrain(pos, TerrainType.Water);
                            }
                        }
                    }
                }
            }

            // Step 3: Add snow patches (impassable) in corners
            // Top-right corner
            for (var y = mapHeight - 6; y < mapHeight; y++)
            {
                for (var x = mapWidth - 6; x < mapWidth; x++)
                {
                    var pos = new Position(x, y);
                    if (gameMap.IsInBounds(pos))
                    {
                        gameMap.SetTerrain(pos, TerrainType.Snow);
                    }
                }
            }

            // Bottom-left corner (smaller)
            for (var y = 0; y < 4; y++)
            {
                for (var x = 0; x < 4; x++)
                {
                    var pos = new Position(x, y);
                    if (gameMap.IsInBounds(pos))
                    {
                        gameMap.SetTerrain(pos, TerrainType.Snow);
                    }
                }
            }

            // Step 4: Add lava rivers (impassable barriers)
            for (var i = 0; i < 3; i++)
            {
                var startX = Random.Range(0, mapWidth);
                var startY = Random.Range(0, mapHeight);
                var length = Random.Range(10, 20);
                var direction = Random.Range(0, 4); // 0=N, 1=E, 2=S, 3=W

                for (var j = 0; j < length; j++)
                {
                    var x = startX + (direction == 1 ? j : direction == 3 ? -j : 0);
                    var y = startY + (direction == 0 ? j : direction == 2 ? -j : 0);
                    var pos = new Position(x, y);

                    if (gameMap.IsInBounds(pos))
                    {
                        gameMap.SetTerrain(pos, TerrainType.Lava);
                    }
                }
            }

            // Step 5: Create dirt roads (fast paths) - pathfinder should prefer these
            for (var i = 0; i < 5; i++)
            {
                var startX = Random.Range(0, mapWidth);
                var startY = Random.Range(0, mapHeight);
                var length = Random.Range(8, 20);
                var direction = Random.Range(0, 4);

                for (var j = 0; j < length; j++)
                {
                    var x = startX + (direction == 1 ? j : direction == 3 ? -j : 0);
                    var y = startY + (direction == 0 ? j : direction == 2 ? -j : 0);
                    var pos = new Position(x, y);

                    if (gameMap.IsInBounds(pos))
                    {
                        var tile = gameMap.GetTile(pos);
                        // Don't overwrite impassable terrain
                        if (!tile.IsWater() && tile.Terrain != TerrainType.Snow && tile.Terrain != TerrainType.Lava)
                        {
                            gameMap.SetTerrain(pos, TerrainType.Dirt);
                        }
                    }
                }
            }

            // Step 6: Add rough terrain patches (slow terrain - pathfinder should avoid)
            for (var i = 0; i < 15; i++)
            {
                var centerX = Random.Range(2, mapWidth - 2);
                var centerY = Random.Range(2, mapHeight - 2);

                for (var dy = -1; dy <= 1; dy++)
                {
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        var pos = new Position(centerX + dx, centerY + dy);
                        if (gameMap.IsInBounds(pos) && Random.value < 0.6f)
                        {
                            var tile = gameMap.GetTile(pos);
                            // Don't overwrite impassable or already good terrain
                            if (!tile.IsWater() && tile.Terrain != TerrainType.Snow &&
                                tile.Terrain != TerrainType.Lava && tile.Terrain != TerrainType.Dirt)
                            {
                                gameMap.SetTerrain(pos, TerrainType.Rough);
                            }
                        }
                    }
                }
            }

            // Step 7: Add swamp near water edges (very slow - strong avoidance)
            for (var y = 0; y < mapHeight; y++)
            {
                for (var x = 0; x < mapWidth; x++)
                {
                    var pos = new Position(x, y);
                    var tile = gameMap.GetTile(pos);

                    if (!tile.IsWater() && tile.IsCoastal && Random.value < 0.4f)
                    {
                        gameMap.SetTerrain(pos, TerrainType.Swamp);
                    }
                }
            }

            Debug.Log("✓ Generated pathfinding test map with diverse terrain costs:");
            Debug.Log("  - Passable: Grass(100), Dirt(110), Sand(125), Rough(150), Swamp(175)");
            Debug.Log("  - Impassable: Water, Snow, Lava");
            Debug.Log("  - Pathfinder should prefer: Grass > Dirt > Sand, avoid Rough/Swamp");
        }

        private void GenerateCheckerboardPattern()
        {
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    var pos = new Position(x, y);
                    var terrain = (x + y) % 2 == 0 ? TerrainType.Grass : TerrainType.Dirt;
                    gameMap.SetTerrain(pos, terrain);
                }
            }
        }

        private void AddSampleObjects()
        {
            // Add some resource piles
            for (int i = 0; i < 5; i++)
            {
                var x = Random.Range(1, mapWidth - 1);
                var y = Random.Range(1, mapHeight - 1);
                var pos = new Position(x, y);

                if (gameMap.GetTile(pos).IsClear())
                {
                    var resourceType = (ResourceType)Random.Range(0, 7);
                    var amount = Random.Range(5, 20);
                    var resource = new ResourceObject(pos, resourceType, amount);
                    gameMap.AddObject(resource);
                }
            }

            // Add some mines
            for (int i = 0; i < 3; i++)
            {
                var x = Random.Range(2, mapWidth - 2);
                var y = Random.Range(2, mapHeight - 2);
                var pos = new Position(x, y);

                if (gameMap.GetTile(pos).IsClear())
                {
                    var resourceType = (ResourceType)Random.Range(1, 7); // Not gold (gold mine needs special handling)
                    var production = Random.Range(1, 3);
                    var mine = new MineObject(pos, resourceType, production);
                    gameMap.AddObject(mine);
                }
            }

            // Add some dwellings
            for (int i = 0; i < 2; i++)
            {
                var x = Random.Range(3, mapWidth - 3);
                var y = Random.Range(3, mapHeight - 3);
                var pos = new Position(x, y);

                if (gameMap.GetTile(pos).IsClear())
                {
                    var creatureId = Random.Range(1, 10); // Assume creature IDs 1-10 exist
                    var weeklyGrowth = Random.Range(5, 15);
                    var dwelling = new DwellingObject(pos, creatureId, weeklyGrowth);
                    dwelling.ApplyWeeklyGrowth(); // Start with some available creatures
                    gameMap.AddObject(dwelling);
                }
            }

            Debug.Log($"✓ Added {gameMap.GetAllObjects().Count()} sample objects to map");
        }

        // Editor helper to regenerate map
        [ContextMenu("Regenerate Map")]
        private void RegenerateMap()
        {
            if (Application.isPlaying)
            {
                InitializeTestMap();
            }
            else
            {
                Debug.LogWarning("Map regeneration only works in Play mode");
            }
        }
    }
}
