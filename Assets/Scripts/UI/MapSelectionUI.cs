using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using RealmsOfEldor.Core;
using RealmsOfEldor.Core.Map;
using RealmsOfEldor.Controllers;
using RealmsOfEldor.Data;

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

            // CRITICAL FIX: Ensure Content has VerticalLayoutGroup and ContentSizeFitter
            if (mapListContainer != null)
            {
                FixContentLayoutComponents();
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

        /// <summary>
        /// Runtime fix: Ensures Content GameObject has VerticalLayoutGroup and ContentSizeFitter.
        /// This is a failsafe in case the scene wasn't saved properly.
        /// </summary>
        private void FixContentLayoutComponents()
        {
            Debug.Log($"[MapSelectionUI] mapListContainer is: {GetFullPath(mapListContainer)}");

            var rectTransform = mapListContainer.GetComponent<RectTransform>();

            // Fix anchors if incorrect
            if (rectTransform.anchorMin != new Vector2(0f, 1f) || rectTransform.anchorMax != new Vector2(1f, 1f))
            {
                Debug.LogWarning($"[MapSelectionUI] Wrong anchors: {rectTransform.anchorMin} to {rectTransform.anchorMax}. Fixing...");
                rectTransform.anchorMin = new Vector2(0f, 1f);
                rectTransform.anchorMax = new Vector2(1f, 1f);
                rectTransform.pivot = new Vector2(0.5f, 1f);
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.sizeDelta = new Vector2(0, 0);
            }

            // Ensure VerticalLayoutGroup exists
            var verticalLayout = mapListContainer.GetComponent<VerticalLayoutGroup>();
            if (verticalLayout == null)
            {
                Debug.LogWarning("[MapSelectionUI] VerticalLayoutGroup missing! Adding...");
                verticalLayout = mapListContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            verticalLayout.childAlignment = TextAnchor.UpperCenter;
            verticalLayout.childControlWidth = true;
            verticalLayout.childControlHeight = false;
            verticalLayout.childForceExpandWidth = true;
            verticalLayout.childForceExpandHeight = false;
            verticalLayout.spacing = 10;

            // Ensure ContentSizeFitter exists
            var contentSizeFitter = mapListContainer.GetComponent<ContentSizeFitter>();
            if (contentSizeFitter == null)
            {
                Debug.LogWarning("[MapSelectionUI] ContentSizeFitter missing! Adding...");
                contentSizeFitter = mapListContainer.gameObject.AddComponent<ContentSizeFitter>();
            }

            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
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
            if (MapPersistenceManager.Instance == null)
            {
                Debug.LogError("MapPersistenceManager.Instance is NULL!");
                return;
            }

            if (mapListContainer == null)
            {
                Debug.LogError("mapListContainer is NULL! Assign it in Inspector!");
                return;
            }

            // Clear existing items
            foreach (Transform child in mapListContainer)
            {
                Destroy(child.gameObject);
            }

            // Get available maps
            availableMaps = MapPersistenceManager.Instance.GetAllMapMetadata();
            Debug.Log($"[MapSelectionUI] Found {availableMaps.Count} available maps");

            // Create UI items for each map
            foreach (var metadata in availableMaps)
            {
                CreateMapListItem(metadata);
            }

            // Force layout rebuild
            Canvas.ForceUpdateCanvases();
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(mapListContainer.GetComponent<RectTransform>());
        }

        /// <summary>
        /// Creates a UI list item for a map.
        /// </summary>
        private void CreateMapListItem(MapMetadata metadata)
        {
            if (mapItemPrefab == null || mapListContainer == null)
                return;

            var item = Instantiate(mapItemPrefab, mapListContainer);
            item.name = $"MapItem_{metadata.Name}";

            // Set text
            var textComponent = item.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = metadata.GetDisplayName();
            }

            // Wire up button click
            var button = item.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => OnMapSelected(metadata.Id));
            }
        }

        /// <summary>
        /// Called when a map is selected from the list.
        /// </summary>
        private void OnMapSelected(string mapId)
        {
            selectedMapId = mapId;
            LoadMapAndStartGame(mapId);
        }

        /// <summary>
        /// Called when Generate New Map button is clicked.
        /// </summary>
        private void OnGenerateNewMapClicked()
        {
            var mapName = $"Random Map {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}";

            var metadata = new MapMetadata(
                name: mapName,
                width: defaultMapWidth,
                height: defaultMapHeight,
                playerCount: defaultPlayerCount,
                isGenerated: true
            );
            metadata.Description = "Randomly generated map";

            var gameMap = GenerateRandomMap(metadata.Width, metadata.Height);

            if (MapPersistenceManager.Instance.SaveMap(gameMap, metadata))
            {
                Debug.Log($"[MapSelectionUI] Generated new map: {mapName}");
                RefreshMapList();
            }
            else
            {
                Debug.LogError("Failed to save generated map");
            }
        }

        /// <summary>
        /// Generates a random map using the Phase 6F modificator pipeline.
        /// This replaces the legacy inline generation code.
        /// Note: Hero spawning is now handled by HeroSpawnModificator (end of pipeline).
        /// </summary>
        private GameMap GenerateRandomMap(int width, int height)
        {
            var map = new GameMap(width, height);
            var config = MapGenConfig.Instance;

            // Set up modificator pipeline
            var pipeline = new ModificatorPipeline(map, config);

            // Define starting position for hero spawn (center of map)
            var heroSpawnPos = new Position(width / 2, height / 2);
            var startPositions = new List<Position> { heroSpawnPos };

            // Add all modificators
            pipeline.AddModificator(new TerrainPainterModificator());
            pipeline.AddModificator(new ResourcePlacerModificator());
            pipeline.AddModificator(new MinePlacerModificator());
            pipeline.AddModificator(new DwellingPlacerModificator());
            pipeline.AddModificator(new GuardPlacerModificator());
            pipeline.AddModificator(new ObstaclePlacerModificator());
            pipeline.AddModificator(new ReachabilityValidatorModificator(startPositions, 5));

            // Add hero spawn modificator (runs AFTER terrain generation - Priority 90)
            // This ensures hero spawns on passable terrain
            var heroSpawnMod = new HeroSpawnModificator();
            pipeline.AddModificator(heroSpawnMod);

            // Execute pipeline
            pipeline.ExecuteWithCleanup();
            map.CalculateCoastalTiles();

            // Create hero at the spawn position found by modificator
            if (heroSpawnMod.SpawnPosition.HasValue && GameStateManager.Instance != null)
            {
                var hero = GameStateManager.Instance.CreateHero(1, 0, heroSpawnMod.SpawnPosition.Value);
                hero.CustomName = "Starting Hero";
                hero.MaxMovement = 2000;
                hero.Movement = 2000;
                Debug.Log($"✓ Created hero at {heroSpawnMod.SpawnPosition.Value}");
            }

            return map;
        }

        /// <summary>
        /// Loads a map and transitions to the adventure map scene.
        /// </summary>
        private void LoadMapAndStartGame(string mapId)
        {
            var gameMap = MapPersistenceManager.Instance.LoadMap(mapId);
            if (gameMap == null)
            {
                Debug.LogError($"Failed to load map: {mapId}");
                return;
            }

            // Store map ID in PlayerPrefs so AdventureMap scene can load it
            PlayerPrefs.SetString("SelectedMapId", mapId);
            PlayerPrefs.Save();

            SceneManager.LoadScene(adventureMapSceneName);
        }

        /// <summary>
        /// Called when Back button is clicked. Returns to main menu.
        /// </summary>
        private void OnBackClicked()
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }

        /// <summary>
        /// Helper to get full hierarchy path of a Transform.
        /// </summary>
        private string GetFullPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
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
