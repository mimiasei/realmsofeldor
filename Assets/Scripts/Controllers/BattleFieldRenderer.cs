using UnityEngine;
using RealmsOfEldor.Core;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.Controllers
{
    /// <summary>
    /// Renders the battle field background and optional hex grid overlay.
    /// Based on VCMI's BattleFieldController.
    /// Camera is now managed by BattleCameraController (not this component).
    /// </summary>
    public class BattleFieldRenderer : MonoBehaviour
    {
        [Header("Camera Reference")]
        [Tooltip("Reference to the battle camera (managed by BattleCameraController)")]
        [SerializeField] private Camera battleCamera;

        [Header("Background")]
        [SerializeField] private SpriteRenderer backgroundRenderer;
        [SerializeField] private Sprite grasslandBackground;
        [SerializeField] private Sprite dirtBackground;
        [SerializeField] private Sprite sandBackground;
        [SerializeField] private Sprite swampBackground;
        [SerializeField] private Sprite roughBackground;
        [SerializeField] private Sprite snowBackground;

        [Header("Hex Grid")]
        [SerializeField] private GameObject hexGridContainer;
        [SerializeField] private bool showHexGrid = false;
        [SerializeField] private Color hexLineColor = new Color(0f, 1f, 0f, 0.3f);
        [SerializeField] private float hexLineWidth = 0.01f; // Very thin for new world scale (1 pixel equivalent)

        private GameObject[,] hexOverlays;

        void Awake()
        {
            Debug.Log("BattleFieldRenderer.Awake() called - starting initialization");

            // Find battle camera if not assigned (managed by BattleCameraController)
            if (battleCamera == null)
            {
                battleCamera = Camera.main;
                if (battleCamera == null)
                {
                    Debug.LogError("BattleFieldRenderer: No camera found! Make sure BattleCameraController is set up.");
                    return;
                }
                Debug.Log($"BattleFieldRenderer: Using camera: {battleCamera.name}");
            }

            // Background renderer is now managed manually in scene (or use BattleGroundPlane instead)
            // Don't auto-create if missing
            if (backgroundRenderer != null)
            {
                Debug.Log($"BattleFieldRenderer: backgroundRenderer exists at {backgroundRenderer.transform.position}, enabled: {backgroundRenderer.enabled}");
            }
            else
            {
                Debug.Log("BattleFieldRenderer: No backgroundRenderer assigned (using BattleGroundPlane instead)");
            }


            // Create hex grid container if not assigned
            if (hexGridContainer == null)
            {
                hexGridContainer = new GameObject("HexGridContainer");
                hexGridContainer.transform.SetParent(transform);
                hexGridContainer.transform.localPosition = Vector3.zero; // Ensure it's at (0,0,0) in world space
            }
            else
            {
                // Reset position if it exists
                hexGridContainer.transform.localPosition = Vector3.zero;
            }

            hexOverlays = new GameObject[BattleHexGrid.BATTLE_WIDTH, BattleHexGrid.BATTLE_HEIGHT];
        }

        void Start()
        {
            // Camera is managed by BattleCameraController, just use it for rendering

            // Log camera state
            if (battleCamera != null)
            {
                Debug.Log("=== BATTLE CAMERA STATE ===");
                Debug.Log($"Camera Position: {battleCamera.transform.position}");
                Debug.Log($"Camera Rotation (Euler): {battleCamera.transform.rotation.eulerAngles}");
                Debug.Log($"Camera Projection: {(battleCamera.orthographic ? "Orthographic" : "Perspective")}");
                if (!battleCamera.orthographic)
                {
                    Debug.Log($"Camera FOV: {battleCamera.fieldOfView}");
                }
                Debug.Log("===========================");
            }

            // Setup background
            SetupBackground();

            // Initialize hex grid (hidden by default)
            InitializeHexGrid();
        }


        /// <summary>
        /// Sets the battlefield background based on terrain type.
        /// </summary>
        public void SetBackground(TerrainType terrain)
        {
            if (backgroundRenderer == null)
            {
                Debug.LogWarning("BattleFieldRenderer: Background renderer not assigned!");
                return;
            }

            Sprite background = terrain.Biome switch
            {
                BiomeType.Grass => grasslandBackground,
                BiomeType.Dirt => dirtBackground,
                BiomeType.Sand => sandBackground,
                BiomeType.Swamp => swampBackground,
                BiomeType.Rock => roughBackground,
                BiomeType.Snow => snowBackground,
                _ => grasslandBackground
            };

            if (background != null)
            {
                backgroundRenderer.sprite = background;
                Debug.Log($"BattleFieldRenderer: Set background to {terrain}");
            }
            else
            {
                Debug.LogWarning($"BattleFieldRenderer: No background sprite for {terrain}!");
            }
        }


        /// <summary>
        /// Sets up the background sprite to fill the camera view.
        /// Must be called after camera is positioned.
        /// </summary>
        private void SetupBackground()
        {
            if (backgroundRenderer == null)
            {
                Debug.LogWarning("BattleFieldRenderer: No backgroundRenderer to setup!");
                return;
            }

            // Get or create background sprite
            if (grasslandBackground == null && backgroundRenderer.sprite == null)
            {
                Debug.Log("BattleFieldRenderer: No background sprite found, creating placeholder");
                grasslandBackground = CreatePlaceholderBackground();
            }
            else if (grasslandBackground == null && backgroundRenderer.sprite != null)
            {
                // Use the existing sprite on the renderer
                Debug.Log($"BattleFieldRenderer: Using existing sprite from renderer: {backgroundRenderer.sprite.name}");
                grasslandBackground = backgroundRenderer.sprite;
            }

            // Set sprite (but don't force enable - respect scene settings)
            if (grasslandBackground != null)
            {
                backgroundRenderer.sprite = grasslandBackground;
            }
            // Don't force enable - let scene settings control this
            // backgroundRenderer.enabled = true;
            backgroundRenderer.sortingOrder = -100;

            // Configure shadow receiving for 2.5D rendering
            backgroundRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // Ground doesn't cast shadows
            backgroundRenderer.receiveShadows = true; // Ground receives shadows from units

            // Position background as horizontal ground plane
            var topLeft = BattleHexGrid.HexToWorld(0, 0);
            var bottomRight = BattleHexGrid.HexToWorld(BattleHexGrid.BATTLE_WIDTH - 1, BattleHexGrid.BATTLE_HEIGHT - 1);
            var centerX = (topLeft.x + bottomRight.x) * 0.5f;
            var centerZ = (topLeft.z + bottomRight.z) * 0.5f;

            // Background is a horizontal plane at ground level (Y=0)
            // Rotated 90° on X axis to lie flat on X,Z plane
            backgroundRenderer.transform.position = new Vector3(centerX, 0f, centerZ);
            backgroundRenderer.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // Rotate to be horizontal

            Debug.Log($"BattleFieldRenderer: Background positioned as horizontal ground plane at ({centerX}, 0, {centerZ}), rotated 90° on X");

            // Add BoxCollider2D for click detection on the battlefield
            var bgCollider = backgroundRenderer.GetComponent<BoxCollider2D>();
            if (bgCollider == null)
            {
                bgCollider = backgroundRenderer.gameObject.AddComponent<BoxCollider2D>();
            }

            // Get sprite bounds (this accounts for PPU automatically)
            var sprite = backgroundRenderer.sprite;
            var spriteBounds = sprite.bounds.size;
            var spriteTexture = sprite.texture;

            // Calculate camera's view size
            var cameraViewHeight = battleCamera.orthographicSize * 2f; // Full height
            var cameraViewWidth = cameraViewHeight * battleCamera.aspect;   // Full width

            Debug.Log($"BattleFieldRenderer: Camera orthographic size: {battleCamera.orthographicSize}, aspect: {battleCamera.aspect}");
            Debug.Log($"BattleFieldRenderer: Camera view size: {cameraViewWidth}x{cameraViewHeight}");
            Debug.Log($"BattleFieldRenderer: Sprite texture size: {spriteTexture.width}x{spriteTexture.height} pixels");
            Debug.Log($"BattleFieldRenderer: Sprite PPU: {sprite.pixelsPerUnit}");
            Debug.Log($"BattleFieldRenderer: Sprite bounds (world units): {spriteBounds.x}x{spriteBounds.y}");

            // The grass sprite is ONE large image (1184x864 pixels with PPU=1)
            // that includes the battlefield AND decorative borders (like HOMM3)
            // Now that hex dimensions are properly set (HEX_WIDTH=1.0, HEX_HEIGHT=1.0),
            // the battlefield is ~17x11 world units, and we can scale the background to fit the camera

            // Scale to fit camera view (camera shows entire battlefield + some extra space)
            var scaleToFitWidth = cameraViewWidth / spriteBounds.x;
            var scaleToFitHeight = cameraViewHeight / spriteBounds.y;

            // Use the smaller scale to ensure entire sprite fits in view
            var targetScale = Mathf.Min(scaleToFitWidth, scaleToFitHeight);

            backgroundRenderer.transform.localScale = Vector3.one * targetScale;

            Debug.Log($"BattleFieldRenderer: Camera view size: {cameraViewWidth:F2}x{cameraViewHeight:F2}");
            Debug.Log($"BattleFieldRenderer: Sprite size: {spriteBounds.x}x{spriteBounds.y}");
            Debug.Log($"BattleFieldRenderer: Scaling background to fit camera - scale: {targetScale:F4}");
            Debug.Log($"BattleFieldRenderer: Final sprite display size: {spriteBounds.x * targetScale:F2}x{spriteBounds.y * targetScale:F2}");

            Debug.Log($"BattleFieldRenderer: Background positioned at {backgroundRenderer.transform.position}, scale: {backgroundRenderer.transform.localScale}");
        }

        /// <summary>
        /// Adjust background scale at runtime (for tuning in play mode).
        /// Enable this checkbox to override the automatic scaling.
        /// </summary>
        [Header("Runtime Tuning")]
        [SerializeField] private bool manualScaleOverride = false;
        [SerializeField] [Range(0.01f, 2f)] private float manualBackgroundScale = 0.3f;

        private float lastBackgroundScale = -1f;

        void Update()
        {
            // Allow runtime adjustment of background scale (only if manual override enabled)
            if (manualScaleOverride && backgroundRenderer != null && Application.isPlaying)
            {
                if (Mathf.Abs(manualBackgroundScale - lastBackgroundScale) > 0.001f)
                {
                    backgroundRenderer.transform.localScale = Vector3.one * manualBackgroundScale;
                    lastBackgroundScale = manualBackgroundScale;
                }
            }
        }

        /// <summary>
        /// Toggles hex grid overlay visibility.
        /// </summary>
        public void ToggleHexGrid()
        {
            showHexGrid = !showHexGrid;
            hexGridContainer.SetActive(showHexGrid);
            Debug.Log($"BattleFieldRenderer: Hex grid {(showHexGrid ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Shows or hides the hex grid overlay.
        /// </summary>
        public void SetHexGridVisible(bool visible)
        {
            showHexGrid = visible;
            hexGridContainer.SetActive(visible);
        }

        /// <summary>
        /// Initializes hex grid overlay for all battlefield hexes.
        /// </summary>
        private void InitializeHexGrid()
        {
            // Clear any existing hex overlays (in case we're reinitializing)
            if (hexGridContainer != null)
            {
                // Destroy all children
                for (var i = hexGridContainer.transform.childCount - 1; i >= 0; i--)
                {
                    Destroy(hexGridContainer.transform.GetChild(i).gameObject);
                }
            }

            // Sample a few hex positions for debugging
            var hex00 = BattleHexGrid.HexToWorld(0, 0);
            var hex10 = BattleHexGrid.HexToWorld(1, 0);
            var hex01 = BattleHexGrid.HexToWorld(0, 1);
            Debug.Log($"BattleFieldRenderer: Sample hex positions - (0,0): {hex00}, (1,0): {hex10}, (0,1): {hex01}");

            for (var y = 0; y < BattleHexGrid.BATTLE_HEIGHT; y++)
            {
                for (var x = 0; x < BattleHexGrid.BATTLE_WIDTH; x++)
                {
                    var hexOverlay = CreateHexOverlay(x, y);
                    hexOverlay.transform.SetParent(hexGridContainer.transform);
                    hexOverlays[x, y] = hexOverlay;
                }
            }

            // Hide hex grid by default
            hexGridContainer.SetActive(showHexGrid);

            Debug.Log($"BattleFieldRenderer: HexGridContainer position: {hexGridContainer.transform.position}, scale: {hexGridContainer.transform.localScale}");
            Debug.Log($"BattleFieldRenderer: Initialized {BattleHexGrid.BATTLE_WIDTH}x{BattleHexGrid.BATTLE_HEIGHT} hex grid (lineWidth: {hexLineWidth})");
        }

        /// <summary>
        /// Creates a visual hex overlay at the specified hex coordinates.
        /// </summary>
        private GameObject CreateHexOverlay(int hexX, int hexY)
        {
            var hexObj = new GameObject($"Hex_{hexX}_{hexY}");
            var worldPos = BattleHexGrid.HexToWorld(hexX, hexY);
            // Position on ground plane (X,Z at Y=0), slightly above to be visible
            hexObj.transform.position = new Vector3(worldPos.x, 0.01f, worldPos.z);

            // Rotate hex to lie flat on X,Z plane
            hexObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            // Add LineRenderer for hex border
            var lineRenderer = hexObj.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = hexLineColor;
            lineRenderer.endColor = hexLineColor;
            lineRenderer.startWidth = hexLineWidth;
            lineRenderer.endWidth = hexLineWidth;
            lineRenderer.loop = true;
            lineRenderer.useWorldSpace = false; // Use local space (relative to hexObj position)
            lineRenderer.sortingOrder = 100; // Render above everything

            // Create hex shape (6 vertices) - these are in local space, will be rotated by transform
            var hexPoints = GetHexVertices(BattleHexGrid.HEX_WIDTH, BattleHexGrid.HEX_HEIGHT);
            lineRenderer.positionCount = 6;
            lineRenderer.SetPositions(hexPoints);

            // Debug several hexes to verify positioning
            if ((hexX == 0 && hexY == 0) || (hexX == 5 && hexY == 5) || (hexX == 16 && hexY == 10))
            {
                Debug.Log($"BattleFieldRenderer: Hex ({hexX},{hexY}) created at world position {hexObj.transform.position}");
                if (hexX == 0 && hexY == 0)
                {
                    Debug.Log($"BattleFieldRenderer: Hex vertices: {string.Join(", ", hexPoints)}");
                    Debug.Log($"BattleFieldRenderer: Line width: {hexLineWidth}");
                }
            }

            return hexObj;
        }

        /// <summary>
        /// Calculates the 6 vertices of a hexagon for rendering.
        /// </summary>
        private Vector3[] GetHexVertices(float width, float height)
        {
            var halfWidth = width * 0.5f;
            var halfHeight = height * 0.5f;
            var quarterHeight = height * 0.25f;

            // Pointy-top hexagon vertices (clockwise from top)
            return new Vector3[]
            {
                new Vector3(0f, halfHeight, 0f),              // Top
                new Vector3(halfWidth, quarterHeight, 0f),    // Top-right
                new Vector3(halfWidth, -quarterHeight, 0f),   // Bottom-right
                new Vector3(0f, -halfHeight, 0f),             // Bottom
                new Vector3(-halfWidth, -quarterHeight, 0f),  // Bottom-left
                new Vector3(-halfWidth, quarterHeight, 0f)    // Top-left
            };
        }

        /// <summary>
        /// Highlights a specific hex (for targeting, movement range, etc.).
        /// </summary>
        public void HighlightHex(int hexX, int hexY, Color color)
        {
            if (!BattleHexGrid.IsValidHex(hexX, hexY))
                return;

            var hexOverlay = hexOverlays[hexX, hexY];
            if (hexOverlay != null)
            {
                var lineRenderer = hexOverlay.GetComponent<LineRenderer>();
                lineRenderer.startColor = color;
                lineRenderer.endColor = color;
                lineRenderer.startWidth = hexLineWidth * 2f; // Thicker line
                lineRenderer.endWidth = hexLineWidth * 2f;
            }
        }

        /// <summary>
        /// Clears all hex highlights.
        /// </summary>
        public void ClearHighlights()
        {
            for (var y = 0; y < BattleHexGrid.BATTLE_HEIGHT; y++)
            {
                for (var x = 0; x < BattleHexGrid.BATTLE_WIDTH; x++)
                {
                    var hexOverlay = hexOverlays[x, y];
                    if (hexOverlay != null)
                    {
                        var lineRenderer = hexOverlay.GetComponent<LineRenderer>();
                        lineRenderer.startColor = hexLineColor;
                        lineRenderer.endColor = hexLineColor;
                        lineRenderer.startWidth = hexLineWidth;
                        lineRenderer.endWidth = hexLineWidth;
                    }
                }
            }
        }

        /// <summary>
        /// Highlights multiple hexes (for movement range, AoE, etc.).
        /// </summary>
        public void HighlightHexes(System.Collections.Generic.List<Vector2Int> hexes, Color color)
        {
            foreach (var hex in hexes)
            {
                HighlightHex(hex.x, hex.y, color);
            }
        }

        /// <summary>
        /// Creates a placeholder background sprite (bright green so it's visible).
        /// </summary>
        private Sprite CreatePlaceholderBackground()
        {
            var size = 1024;
            var texture = new Texture2D(size, size);
            var pixels = new Color[size * size];

            // Create a bright green background for visibility
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(0.2f, 0.8f, 0.2f, 1f); // Bright green
            }

            texture.SetPixels(pixels);
            texture.Apply();

            // Create sprite that covers entire battlefield
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
