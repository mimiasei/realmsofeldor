using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using RealmsOfEldor.Core;
using RealmsOfEldor.Controllers;

namespace RealmsOfEldor.UI
{
    /// <summary>
    /// Map selection screen UI controller.
    /// Displays available maps and allows generation of new maps.
    /// Based on VCMI's map selection pattern.
    /// </summary>
    public class MapSelectionUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Transform mapListContainer;
        [SerializeField] private GameObject mapItemPrefab;
        [SerializeField] private Button generateNewMapButton;
        [SerializeField] private Button backButton;
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Map Generation Settings")]
        [SerializeField] private int defaultMapWidth = 30;
        [SerializeField] private int defaultMapHeight = 30;
        [SerializeField] private int defaultPlayerCount = 2;

        [Header("Scene Settings")]
        [SerializeField] private string adventureMapSceneName = "AdventureMap";
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private List<MapMetadata> availableMaps;
        private string selectedMapId;

        void Start()
        {
            // Ensure MapPersistenceManager exists
            if (MapPersistenceManager.Instance == null)
            {
                var persistenceObj = new GameObject("MapPersistenceManager");
                persistenceObj.AddComponent<MapPersistenceManager>();
            }

            // Wire up buttons
            if (generateNewMapButton != null)
            {
                generateNewMapButton.onClick.AddListener(OnGenerateNewMapClicked);
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
            }

            // Set title
            if (titleText != null)
            {
                titleText.text = "Select Map";
            }

            // Load and display available maps
            RefreshMapList();
        }

        void OnDestroy()
        {
            if (generateNewMapButton != null)
            {
                generateNewMapButton.onClick.RemoveListener(OnGenerateNewMapClicked);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(OnBackClicked);
            }
        }

        /// <summary>
        /// Refreshes the list of available maps from MapPersistenceManager.
        /// </summary>
        private void RefreshMapList()
        {
            // Clear existing items
            if (mapListContainer != null)
            {
                foreach (Transform child in mapListContainer)
                {
                    Destroy(child.gameObject);
                }
            }

            // Get available maps
            availableMaps = MapPersistenceManager.Instance?.GetAllMapMetadata() ?? new List<MapMetadata>();

            Debug.Log($"Found {availableMaps.Count} available maps");

            // Create UI items for each map
            foreach (var metadata in availableMaps)
            {
                CreateMapListItem(metadata);
            }

            // If no maps, show helpful message
            if (availableMaps.Count == 0)
            {
                Debug.Log("No maps found. Generate a new map to get started!");
            }
        }

        /// <summary>
        /// Creates a UI list item for a map.
        /// </summary>
        private void CreateMapListItem(MapMetadata metadata)
        {
            if (mapItemPrefab == null || mapListContainer == null)
            {
                Debug.LogWarning("Map item prefab or container not assigned");
                return;
            }

            var item = Instantiate(mapItemPrefab, mapListContainer);

            // Find text component (assumes prefab has TextMeshProUGUI child)
            var textComponent = item.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = metadata.GetDisplayName();
            }

            // Find button component and wire up click
            var button = item.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => OnMapSelected(metadata.Id));
            }

            Debug.Log($"Created map list item: {metadata.GetDisplayName()}");
        }

        /// <summary>
        /// Called when a map is selected from the list.
        /// </summary>
        private void OnMapSelected(string mapId)
        {
            selectedMapId = mapId;
            Debug.Log($"Map selected: {mapId}");

            // Load the selected map and start game
            LoadMapAndStartGame(mapId);
        }

        /// <summary>
        /// Called when Generate New Map button is clicked.
        /// </summary>
        private void OnGenerateNewMapClicked()
        {
            Debug.Log("Generating new map...");

            // Generate random map name
            var mapName = $"Random Map {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}";

            // Create metadata
            var metadata = new MapMetadata(
                name: mapName,
                width: defaultMapWidth,
                height: defaultMapHeight,
                playerCount: defaultPlayerCount,
                isGenerated: true
            );
            metadata.Description = "Randomly generated map";

            // Generate the map (using same pattern as GameInitializer)
            var gameMap = GenerateRandomMap(metadata.Width, metadata.Height);

            // Save the map
            if (MapPersistenceManager.Instance.SaveMap(gameMap, metadata))
            {
                Debug.Log($"✅ Map generated and saved: {mapName}");

                // Refresh list to show new map
                RefreshMapList();

                // Optionally, auto-select and load the new map
                // LoadMapAndStartGame(metadata.Id);
            }
            else
            {
                Debug.LogError("Failed to save generated map");
            }
        }

        /// <summary>
        /// Generates a random map with diverse terrain and objects.
        /// Based on GameInitializer's GenerateRandomTerrain pattern.
        /// </summary>
        private GameMap GenerateRandomMap(int width, int height)
        {
            var map = new GameMap(width, height);

            // Generate diverse terrain
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var pos = new Position(x, y);
                    var roll = Random.value;

                    if (roll < 0.40f)
                        map.SetTerrain(pos, Data.TerrainType.Grass);
                    else if (roll < 0.65f)
                        map.SetTerrain(pos, Data.TerrainType.Dirt);
                    else if (roll < 0.85f)
                        map.SetTerrain(pos, Data.TerrainType.Sand);
                    else if (roll < 0.95f)
                        map.SetTerrain(pos, Data.TerrainType.Rough);
                    else
                        map.SetTerrain(pos, Data.TerrainType.Swamp);
                }
            }

            // Add water lakes (impassable)
            for (var i = 0; i < 5; i++)
            {
                var centerX = Random.Range(5, width - 5);
                var centerY = Random.Range(5, height - 5);
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
                                map.SetTerrain(pos, Data.TerrainType.Water);
                            }
                        }
                    }
                }
            }

            // Add map objects
            AddMapObjects(map, 5, 3, 2); // 5 resources, 3 mines, 2 dwellings

            // Calculate coastal tiles
            map.CalculateCoastalTiles();

            Debug.Log($"✓ Generated random map: {width}x{height}");
            return map;
        }

        /// <summary>
        /// Adds map objects (resources, mines, dwellings) to the map.
        /// Based on GameInitializer's AddMapObjects pattern.
        /// </summary>
        private void AddMapObjects(GameMap map, int resourceCount, int mineCount, int dwellingCount)
        {
            // Add resource piles
            for (var i = 0; i < resourceCount; i++)
            {
                var pos = FindClearPosition(map);
                if (pos != null)
                {
                    var resourceType = (ResourceType)Random.Range(0, 7);
                    var amount = Random.Range(5, 20);
                    var resource = new ResourceObject(pos.Value, resourceType, amount);
                    map.AddObject(resource);
                }
            }

            // Add mines
            for (var i = 0; i < mineCount; i++)
            {
                var pos = FindClearPosition(map);
                if (pos != null)
                {
                    var resourceType = (ResourceType)Random.Range(1, 7); // Not gold
                    var production = Random.Range(1, 3);
                    var mine = new MineObject(pos.Value, resourceType, production);
                    map.AddObject(mine);
                }
            }

            // Add dwellings
            for (var i = 0; i < dwellingCount; i++)
            {
                var pos = FindClearPosition(map);
                if (pos != null)
                {
                    var creatureId = Random.Range(1, 10);
                    var weeklyGrowth = Random.Range(5, 15);
                    var dwelling = new DwellingObject(pos.Value, creatureId, weeklyGrowth);
                    dwelling.ApplyWeeklyGrowth(); // Start with some creatures
                    map.AddObject(dwelling);
                }
            }
        }

        /// <summary>
        /// Finds a random clear position on the map for object placement.
        /// </summary>
        private Position? FindClearPosition(GameMap map)
        {
            for (var attempts = 0; attempts < 50; attempts++)
            {
                var x = Random.Range(1, map.Width - 1);
                var y = Random.Range(1, map.Height - 1);
                var pos = new Position(x, y);

                if (map.GetTile(pos).IsClear())
                {
                    return pos;
                }
            }
            return null;
        }

        /// <summary>
        /// Loads a map and transitions to the adventure map scene.
        /// </summary>
        private void LoadMapAndStartGame(string mapId)
        {
            Debug.Log($"Loading map {mapId} and starting game...");

            // Load the map
            var gameMap = MapPersistenceManager.Instance.LoadMap(mapId);
            if (gameMap == null)
            {
                Debug.LogError($"Failed to load map: {mapId}");
                return;
            }

            // Store map ID in PlayerPrefs so AdventureMap scene can load it
            PlayerPrefs.SetString("SelectedMapId", mapId);
            PlayerPrefs.Save();

            // Load adventure map scene
            SceneManager.LoadScene(adventureMapSceneName);
        }

        /// <summary>
        /// Called when Back button is clicked. Returns to main menu.
        /// </summary>
        private void OnBackClicked()
        {
            Debug.Log("Returning to main menu");
            SceneManager.LoadScene(mainMenuSceneName);
        }

#if UNITY_EDITOR
        [ContextMenu("Refresh Map List")]
        private void EditorRefreshMapList()
        {
            RefreshMapList();
        }

        [ContextMenu("Generate Test Map")]
        private void EditorGenerateTestMap()
        {
            OnGenerateNewMapClicked();
        }
#endif
    }
}
