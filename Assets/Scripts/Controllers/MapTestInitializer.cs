using UnityEngine;
using RealmsOfEldor.Core;
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

            // Render the map
            mapRenderer.Initialize(gameMap, mapEvents);
            mapRenderer.RenderFullMap();

            // Raise map loaded event
            mapEvents.RaiseMapLoaded(gameMap);

            Debug.Log($"<color=green>✓ Test map initialized with {gameMap.Width}x{gameMap.Height} tiles</color>");
        }

        private void GenerateRandomTerrain()
        {
            // Create some water patches
            for (int i = 0; i < 5; i++)
            {
                var centerX = Random.Range(5, mapWidth - 5);
                var centerY = Random.Range(5, mapHeight - 5);
                var radius = Random.Range(2, 5);

                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
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

            // Add some rough terrain
            for (int i = 0; i < 10; i++)
            {
                var x = Random.Range(0, mapWidth);
                var y = Random.Range(0, mapHeight);
                var pos = new Position(x, y);

                if (!gameMap.GetTile(pos).IsWater())
                {
                    gameMap.SetTerrain(pos, TerrainType.Rough);
                }
            }

            // Add some dirt paths
            for (int i = 0; i < 3; i++)
            {
                var startX = Random.Range(0, mapWidth);
                var startY = Random.Range(0, mapHeight);
                var length = Random.Range(5, 15);
                var direction = Random.Range(0, 4); // 0=N, 1=E, 2=S, 3=W

                for (int j = 0; j < length; j++)
                {
                    var x = startX + (direction == 1 ? j : direction == 3 ? -j : 0);
                    var y = startY + (direction == 0 ? j : direction == 2 ? -j : 0);
                    var pos = new Position(x, y);

                    if (gameMap.IsInBounds(pos) && !gameMap.GetTile(pos).IsWater())
                    {
                        gameMap.SetTerrain(pos, TerrainType.Dirt);
                    }
                }
            }

            // Add some snow in top-right corner
            for (int y = mapHeight - 5; y < mapHeight; y++)
            {
                for (int x = mapWidth - 5; x < mapWidth; x++)
                {
                    var pos = new Position(x, y);
                    if (gameMap.IsInBounds(pos) && !gameMap.GetTile(pos).IsWater())
                    {
                        gameMap.SetTerrain(pos, TerrainType.Snow);
                    }
                }
            }

            // Add some swamp near water
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    var pos = new Position(x, y);
                    var tile = gameMap.GetTile(pos);

                    if (!tile.IsWater() && tile.IsCoastal && Random.value < 0.3f)
                    {
                        gameMap.SetTerrain(pos, TerrainType.Swamp);
                    }
                }
            }
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

            Debug.Log($"✓ Added {gameMap.GetAllObjects().Count} sample objects to map");
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
