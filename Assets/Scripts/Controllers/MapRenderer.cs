using System.Collections.Generic;
using UnityEngine;
using RealmsOfEldor.Core;
using RealmsOfEldor.Core.Events;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.Controllers
{
    /// <summary>
    /// Renders the game map using 3D billboards on Cartographer ground plane.
    /// Subscribes to MapEventChannel for updates.
    /// All terrain and objects are rendered as billboards with CartographerBillboard shader.
    /// </summary>
    public class MapRenderer : MonoBehaviour
    {
        [Header("Terrain Configuration")]
        [SerializeField] private Data.TerrainData[] terrainDataArray;
        [SerializeField] private bool autoLoadTerrainData = true;
        [SerializeField] private string terrainDataPath = "Assets/Data/Terrain";

        [Header("Object Prefabs")]
        [SerializeField] private GameObject resourcePrefab;
        [SerializeField] private GameObject minePrefab;
        [SerializeField] private GameObject dwellingPrefab;
        [SerializeField] private GameObject obstaclePrefab;
        [SerializeField] private MapObjectPrefabGenerator prefabGenerator;

        [Header("Highlight Settings")]
        [SerializeField] private Color selectionColor = new Color(1f, 1f, 0f, 0.5f); // Yellow
        [SerializeField] private Color reachableColor = new Color(0f, 1f, 0f, 0.3f); // Green

        [Header("Event Channel")]
        [SerializeField] private MapEventChannel mapEvents;

        private GameMap currentMap;
        private Dictionary<TerrainType, Data.TerrainData> terrainLookup;
        private Dictionary<int, GameObject> objectInstances; // Map objects (resources, mines, etc.)
        private Dictionary<int, GameObject> terrainInstances; // Terrain tiles as billboards
        private Dictionary<Position, GameObject> highlightInstances; // Selection/reachable highlights
        private Camera mainCamera;

        // Public accessor for MapEvents
        public MapEventChannel MapEvents => mapEvents;

        private void Awake()
        {
            terrainLookup = new Dictionary<TerrainType, Data.TerrainData>();
            objectInstances = new Dictionary<int, GameObject>();
            terrainInstances = new Dictionary<int, GameObject>();
            highlightInstances = new Dictionary<Position, GameObject>();

            // Auto-load terrain data from Resources if enabled (SSOT: find all TerrainData assets)
            #if UNITY_EDITOR
            if (autoLoadTerrainData && (terrainDataArray == null || terrainDataArray.Length == 0))
            {
                LoadTerrainDataFromAssets();
            }
            #endif

            // Build terrain lookup
            if (terrainDataArray != null)
            {
                foreach (var terrainData in terrainDataArray)
                {
                    if (terrainData != null)
                        terrainLookup[terrainData.terrainType] = terrainData;
                }
            }

            // Inject delegates into GameMap (SSOT: TerrainData knows properties)
            GameMap.GetTerrainVariantCount = GetVariantCountForTerrain;
            GameMap.GetTerrainPassability = GetPassabilityForTerrain;

            // Auto-find prefab generator if not assigned
            if (prefabGenerator == null)
            {
                prefabGenerator = FindFirstObjectByType<MapObjectPrefabGenerator>();
                if (prefabGenerator == null)
                {
                    Debug.LogWarning("⚠️ MapObjectPrefabGenerator not found. Creating one...");
                    var generatorObj = new GameObject("MapObjectPrefabGenerator");
                    prefabGenerator = generatorObj.AddComponent<MapObjectPrefabGenerator>();
                }
            }

            // Use generated prefabs if manual prefabs not assigned
            if (resourcePrefab == null && prefabGenerator != null)
                resourcePrefab = prefabGenerator.ResourcePrefab;
            if (minePrefab == null && prefabGenerator != null)
                minePrefab = prefabGenerator.MinePrefab;
            if (dwellingPrefab == null && prefabGenerator != null)
                dwellingPrefab = prefabGenerator.DwellingPrefab;
            if (obstaclePrefab == null && prefabGenerator != null)
                obstaclePrefab = prefabGenerator.ObstaclePrefab;

            // Cache main camera
            mainCamera = Camera.main;
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Auto-loads all TerrainData assets from the specified path.
        /// DRY/SSOT: Automatically finds TerrainData instead of manual assignment.
        /// </summary>
        private void LoadTerrainDataFromAssets()
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("t:TerrainData", new[] { terrainDataPath });
            var terrainDataList = new System.Collections.Generic.List<Data.TerrainData>();

            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var terrainData = UnityEditor.AssetDatabase.LoadAssetAtPath<Data.TerrainData>(path);
                if (terrainData != null)
                {
                    terrainDataList.Add(terrainData);
                }
            }

            terrainDataArray = terrainDataList.ToArray();
            Debug.Log($"MapRenderer: Auto-loaded {terrainDataArray.Length} TerrainData assets from {terrainDataPath}");
        }
        #endif

        /// <summary>
        /// Gets the number of sprite variants available for a terrain type.
        /// Used by GameMap to generate random variants for billboard rendering.
        /// </summary>
        private int GetVariantCountForTerrain(TerrainType terrain)
        {
            if (terrainLookup.TryGetValue(terrain, out var terrainData))
            {
                // Use sprite variants for 3D billboard rendering
                return terrainData.spriteVariants?.Length ?? 1;
            }
            return 1; // Default: 1 variant if terrain data not found
        }

        /// <summary>
        /// Gets passability for a terrain type from TerrainData.
        /// </summary>
        private bool GetPassabilityForTerrain(TerrainType terrain)
        {
            if (terrainLookup.TryGetValue(terrain, out var terrainData))
            {
                return terrainData.isPassable;
            }
            return false; // Default: impassable if terrain data not found
        }

        private void OnEnable()
        {
            if (mapEvents == null) return;

            mapEvents.OnMapLoaded += HandleMapLoaded;
            mapEvents.OnMapUnloaded += HandleMapUnloaded;
            mapEvents.OnTerrainChanged += HandleTerrainChanged;
            mapEvents.OnTileUpdated += HandleTileUpdated;
            mapEvents.OnObjectAdded += HandleObjectAdded;
            mapEvents.OnObjectRemoved += HandleObjectRemoved;
            mapEvents.OnTileSelected += HandleTileSelected;
            mapEvents.OnTilesHighlighted += HandleTilesHighlighted;
            mapEvents.OnSelectionCleared += HandleSelectionCleared;
        }

        private void OnDisable()
        {
            if (mapEvents == null) return;

            mapEvents.OnMapLoaded -= HandleMapLoaded;
            mapEvents.OnMapUnloaded -= HandleMapUnloaded;
            mapEvents.OnTerrainChanged -= HandleTerrainChanged;
            mapEvents.OnTileUpdated -= HandleTileUpdated;
            mapEvents.OnObjectAdded -= HandleObjectAdded;
            mapEvents.OnObjectRemoved -= HandleObjectRemoved;
            mapEvents.OnTileSelected -= HandleTileSelected;
            mapEvents.OnTilesHighlighted -= HandleTilesHighlighted;
            mapEvents.OnSelectionCleared -= HandleSelectionCleared;
        }

        private void Update()
        {
            HandleMouseInput();
        }

        /// <summary>
        /// Handles mouse input for tile selection.
        /// Uses 3D ground plane raycasting (Y=0 plane) for Cartographer system.
        /// </summary>
        private void HandleMouseInput()
        {
            if (currentMap == null)
            {
                Debug.LogWarning("MapRenderer: currentMap is null, cannot handle input");
                return;
            }

            if (mapEvents == null)
            {
                Debug.LogWarning("MapRenderer: mapEvents is null, cannot handle input");
                return;
            }

            // Check for left mouse button click
            if (Input.GetMouseButtonDown(0))
            {
                if (mainCamera == null)
                    mainCamera = Camera.main;

                if (mainCamera == null)
                {
                    Debug.LogWarning("MapRenderer: Main camera not found!");
                    return;
                }

                // Convert mouse position to world position using 3D ground plane raycast
                var mousePos = Input.mousePosition;

                // Safety check: ensure mouse is within screen bounds before raycasting
                if (mousePos.x < 0 || mousePos.x > Screen.width ||
                    mousePos.y < 0 || mousePos.y > Screen.height)
                {
                    return; // Mouse outside window, skip input handling
                }
                Vector3 worldPos;

                if (CoordinateConverter.ScreenToWorld3D(mainCamera, mousePos, out worldPos))
                {
                    Debug.Log($"MapRenderer: Mouse clicked at screen {mousePos}, world {worldPos}");

                    // Convert world position to map position
                    var mapPos = WorldToMapPosition(worldPos);

                    Debug.Log($"MapRenderer: Converted to map position {mapPos}");

                    // Check if position is valid
                    if (currentMap.IsInBounds(mapPos))
                    {
                        Debug.Log($"MapRenderer: Position is in bounds, raising OnTileSelected for {mapPos}");
                        mapEvents.RaiseTileSelected(mapPos);
                    }
                    else
                    {
                        Debug.Log($"MapRenderer: Position {mapPos} is OUT OF BOUNDS (map size: {currentMap.Width}x{currentMap.Height})");
                    }
                }
                else
                {
                    Debug.LogWarning("MapRenderer: Failed to raycast mouse position to ground plane");
                }
            }
        }

        // Event handlers
        private void HandleMapLoaded(GameMap map)
        {
            Debug.Log($"MapRenderer: HandleMapLoaded called - map size {map.Width}x{map.Height}");
            currentMap = map;
            RenderFullMap();
        }

        private void HandleMapUnloaded()
        {
            ClearMap();
            currentMap = null;
        }

        private void HandleTerrainChanged(Position pos, TerrainType newTerrain)
        {
            UpdateTile(pos);
        }

        private void HandleTileUpdated(Position pos)
        {
            UpdateTile(pos);
        }

        private void HandleObjectAdded(MapObject obj)
        {
            AddObjectRendering(obj);
        }

        private void HandleObjectRemoved(int objectId)
        {
            RemoveObjectRendering(objectId);
        }

        private void HandleTileSelected(Position pos)
        {
            // Clear all previous highlights
            ClearHighlights();

            // Create selection highlight billboard at position
            CreateHighlight(pos, selectionColor, "Selection");
        }

        private void HandleTilesHighlighted(List<Position> positions)
        {
            // Clear all previous highlights
            ClearHighlights();

            // Create reachable highlight billboards
            foreach (var pos in positions)
            {
                CreateHighlight(pos, reachableColor, "Reachable");
            }
        }

        private void HandleSelectionCleared()
        {
            ClearHighlights();
        }

        // Rendering methods
        private void RenderFullMap()
        {
            if (currentMap == null)
            {
                Debug.LogError("MapRenderer: RenderFullMap called but currentMap is null!");
                return;
            }

            Debug.Log($"MapRenderer: RenderFullMap starting - {currentMap.Width}x{currentMap.Height} tiles");
            Debug.Log($"MapRenderer: terrainLookup has {terrainLookup.Count} entries");

            ClearMap();

            // Render all terrain tiles as billboards
            var tilesRendered = 0;
            for (var x = 0; x < currentMap.Width; x++)
            {
                for (var y = 0; y < currentMap.Height; y++)
                {
                    var pos = new Position(x, y);
                    UpdateTile(pos);
                    tilesRendered++;
                }
            }

            Debug.Log($"MapRenderer: Rendered {tilesRendered} terrain billboards");

            // Render all objects
            foreach (var obj in currentMap.GetAllObjects())
            {
                AddObjectRendering(obj);
            }

            Debug.Log($"<color=green>MapRenderer: RenderFullMap complete!</color>");
        }

        /// <summary>
        /// Updates a single terrain tile, rendering it as a billboard.
        /// </summary>
        private void UpdateTile(Position pos)
        {
            if (currentMap == null || !currentMap.IsInBounds(pos)) return;

            var tile = currentMap.GetTile(pos);
            var worldPos = MapToWorldPosition(pos);

            // Generate unique ID for this terrain tile
            var tileId = pos.X + pos.Y * 10000; // Simple unique ID

            // Remove existing terrain billboard if it exists
            if (terrainInstances.TryGetValue(tileId, out var existingInstance))
            {
                if (existingInstance != null)
                    Destroy(existingInstance);
                terrainInstances.Remove(tileId);
            }

            // Get terrain data and sprite
            if (terrainLookup.TryGetValue(tile.Terrain, out var terrainData))
            {
                var sprite = terrainData.GetSpriteVariant(tile.VisualVariant);
                if (sprite != null)
                {
                    // Create billboard for terrain tile
                    var terrainBillboard = new GameObject($"Terrain_{pos.X}_{pos.Y}");
                    terrainBillboard.transform.SetParent(transform);
                    terrainBillboard.transform.position = worldPos;

                    // Add CartographerBillboard component
                    var billboard = terrainBillboard.AddComponent<CartographerBillboard>();
                    billboard.SetSprite(sprite);
                    billboard.SetHeightOffset(0f); // Terrain sits on ground
                    billboard.SetCastShadows(false); // Terrain doesn't cast shadows
                    billboard.SetTint(Color.white);

                    terrainInstances[tileId] = terrainBillboard;
                }
            }
        }

        private void AddObjectRendering(MapObject obj)
        {
            if (obj == null) return;

            GameObject prefab = null;

            if (obj is ResourceObject)
                prefab = resourcePrefab;
            else if (obj is MineObject)
                prefab = minePrefab;
            else if (obj is DwellingObject)
                prefab = dwellingPrefab;
            else if (obj.ObjectType == MapObjectType.Decorative)
                prefab = obstaclePrefab;

            Debug.Log($"MapRenderer.AddObjectRendering: {obj.Name} (Type={obj.ObjectType}), prefab={(prefab != null ? "FOUND" : "NULL")}");

            if (prefab != null)
            {
                var instance = Instantiate(prefab, transform);
                instance.transform.position = MapToWorldPosition(obj.Position);
                instance.name = obj.Name;
                instance.SetActive(true); // Ensure it's active

                Debug.Log($"MapRenderer: Instantiated {obj.Name} at world position {instance.transform.position}");

                // Add CartographerBillboard component for 2.5D rendering
                var billboard = instance.GetComponent<CartographerBillboard>();
                if (billboard == null)
                {
                    // Check if object has a SpriteRenderer with a sprite
                    var spriteRenderer = instance.GetComponent<SpriteRenderer>();
                    if (spriteRenderer != null && spriteRenderer.sprite != null)
                    {
                        // Convert SpriteRenderer to CartographerBillboard
                        var sprite = spriteRenderer.sprite;
                        var tint = spriteRenderer.color;

                        // Remove old SpriteRenderer
                        Destroy(spriteRenderer);

                        // Add billboard component
                        billboard = instance.AddComponent<CartographerBillboard>();
                        billboard.SetSprite(sprite);
                        billboard.SetTint(tint);
                        billboard.SetHeightOffset(0.5f); // Slight offset from ground
                        billboard.SetCastShadows(true); // Enable shadows

                        Debug.Log($"MapRenderer: Converted {obj.Name} to CartographerBillboard");
                    }
                }

                // Add MapObjectView component to link instance to data
                var view = instance.GetComponent<MapObjectView>();
                if (view == null)
                    view = instance.AddComponent<MapObjectView>();
                view.ObjectId = obj.InstanceId;
                view.SetObjectReference(obj); // Cache object reference for click detection

                // Add guard visualization if object has guards
                if (obj.IsGuarded())
                {
                    AddGuardVisualization(instance, obj);
                }

                objectInstances[obj.InstanceId] = instance;
            }
            else
            {
                Debug.LogWarning($"MapRenderer: No prefab found for object {obj.Name} (Type={obj.ObjectType})");
            }
        }

        private void RemoveObjectRendering(int objectId)
        {
            if (objectInstances.TryGetValue(objectId, out var instance))
            {
                Destroy(instance);
                objectInstances.Remove(objectId);
            }
        }

        /// <summary>
        /// Adds a visual indicator for guarded objects.
        /// Creates a small billboard showing guard icon or count.
        /// </summary>
        private void AddGuardVisualization(GameObject objectInstance, MapObject obj)
        {
            if (!obj.IsGuarded())
                return;

            // Create child GameObject for guard indicator
            var guardIndicator = new GameObject("GuardIndicator");
            guardIndicator.transform.SetParent(objectInstance.transform);

            // Position indicator slightly above and to the right of object
            guardIndicator.transform.localPosition = new Vector3(0.3f, 0.5f, 0f); // Above object in 3D
            guardIndicator.transform.localScale = Vector3.one * 0.4f; // Smaller than object

            // Add CartographerBillboard for guard icon
            var billboard = guardIndicator.AddComponent<CartographerBillboard>();
            billboard.SetSprite(CreateGuardSprite());
            billboard.SetTint(new Color(1f, 0.2f, 0.2f, 0.8f)); // Red with transparency
            billboard.SetHeightOffset(0f);
            billboard.SetCastShadows(false);

            // Add TextMesh for guard count (TextMesh is already 3D, so keep it)
            var textObj = new GameObject("GuardCount");
            textObj.transform.SetParent(guardIndicator.transform);
            textObj.transform.localPosition = Vector3.zero;
            textObj.transform.localScale = Vector3.one * 2f; // Scale up text

            var textMesh = textObj.AddComponent<TextMesh>();
            textMesh.text = obj.Guard.Count.ToString();
            textMesh.fontSize = 20;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.color = Color.white;

            Debug.Log($"✓ Added guard visualization to {obj.Name}: {obj.Guard.Count}x creature {obj.Guard.CreatureId}");
        }

        /// <summary>
        /// Creates a simple sprite for guard indicator (small square).
        /// </summary>
        private Sprite CreateGuardSprite()
        {
            // Create a 16x16 texture with a filled square
            var texture = new Texture2D(16, 16);
            var pixels = new Color[16 * 16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white; // Fill with white (will be tinted red by SpriteRenderer)
            }
            texture.SetPixels(pixels);
            texture.Apply();

            // Create sprite from texture
            return Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
        }

        /// <summary>
        /// Creates a highlight billboard at the specified position.
        /// </summary>
        private void CreateHighlight(Position pos, Color color, string type)
        {
            if (!currentMap.IsInBounds(pos)) return;

            var worldPos = MapToWorldPosition(pos);

            // Create highlight GameObject
            var highlight = GameObject.CreatePrimitive(PrimitiveType.Quad);
            highlight.name = $"{type}Highlight_{pos.X}_{pos.Y}";
            highlight.transform.SetParent(transform);
            highlight.transform.position = worldPos + Vector3.up * 0.05f; // Slightly above ground
            highlight.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // Lie flat on ground

            // Remove default collider
            var collider = highlight.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            // Create unlit material with color
            var meshRenderer = highlight.GetComponent<MeshRenderer>();
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.color = color;
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.renderQueue = 3000; // Transparent queue
            meshRenderer.material = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            highlightInstances[pos] = highlight;
        }

        /// <summary>
        /// Clears all highlight billboards.
        /// </summary>
        private void ClearHighlights()
        {
            foreach (var highlight in highlightInstances.Values)
            {
                if (highlight != null)
                    Destroy(highlight);
            }
            highlightInstances.Clear();
        }

        private void ClearMap()
        {
            // Clear terrain billboards
            foreach (var instance in terrainInstances.Values)
            {
                if (instance != null)
                    Destroy(instance);
            }
            terrainInstances.Clear();

            // Clear object billboards
            foreach (var instance in objectInstances.Values)
            {
                if (instance != null)
                    Destroy(instance);
            }
            objectInstances.Clear();

            // Clear highlights
            ClearHighlights();
        }

        // Position conversion for 3D Cartographer system
        /// <summary>
        /// Converts game logic position to 3D world position.
        /// Returns position on X,Z ground plane (Y=0) for Cartographer system.
        /// </summary>
        public Vector3 MapToWorldPosition(Position pos)
        {
            return CoordinateConverter.PositionToWorld3D(pos, 0f);
        }

        /// <summary>
        /// Converts 3D world position to game logic position.
        /// Expects position on X,Z ground plane.
        /// </summary>
        public Position WorldToMapPosition(Vector3 worldPos)
        {
            return CoordinateConverter.WorldToPosition3D(worldPos);
        }

        // Debug visualization
        private void OnDrawGizmos()
        {
            if (currentMap == null) return;

            Gizmos.color = Color.cyan;
            var cellSize = Vector3.one; // 1x1 tiles

            for (var x = 0; x < currentMap.Width; x++)
            {
                for (var y = 0; y < currentMap.Height; y++)
                {
                    var pos = new Position(x, y);
                    var worldPos = MapToWorldPosition(pos);
                    Gizmos.DrawWireCube(worldPos, cellSize);
                }
            }
        }
    }

    /// <summary>
    /// Component attached to map object instances to link them to their data.
    /// Handles click detection for map objects using 3D colliders.
    /// </summary>
    public class MapObjectView : MonoBehaviour
    {
        public int ObjectId { get; set; }

        [SerializeField] private MapEventChannel mapEvents;
        private MapObject cachedObject;

        void Awake()
        {
            // Auto-find mapEvents if not assigned
            if (mapEvents == null)
            {
                var mapRenderer = FindFirstObjectByType<MapRenderer>();
                if (mapRenderer != null)
                {
                    mapEvents = mapRenderer.GetComponent<MapRenderer>().MapEvents;
                }
            }

            // Ensure we have a 3D collider for click detection
            var collider = GetComponent<Collider>();
            if (collider == null)
            {
                var boxCollider = gameObject.AddComponent<BoxCollider>();
                boxCollider.size = new Vector3(1f, 1f, 0.1f); // Match tile size on XZ plane
            }
        }

        // Note: OnMouseDown removed to prevent Unity's internal SendMouseEvents
        // from triggering raycasts outside screen bounds.
        // Object click detection now handled by AdventureMapInputController
        // using manual raycasting with proper bounds checking.

        public void SetObjectReference(MapObject obj)
        {
            cachedObject = obj;
        }

        public MapObject GetObject() => cachedObject;
    }
}
