using UnityEngine;
using RealmsOfEldor.Core;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.Controllers
{
    /// <summary>
    /// Renders the battle field background and optional hex grid overlay.
    /// Based on VCMI's BattleFieldController.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class BattleFieldRenderer : MonoBehaviour
    {
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

        private Camera battleCamera;
        private GameObject[,] hexOverlays;

        void Awake()
        {
            Debug.Log("BattleFieldRenderer.Awake() called - starting initialization");
            battleCamera = GetComponent<Camera>();

            // Set camera to orthographic mode for 2D battle view
            battleCamera.orthographic = true;

            // Set camera to render sprites properly (not skybox)
            battleCamera.clearFlags = CameraClearFlags.SolidColor;
            battleCamera.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f); // Dark gray

            // Create background renderer if not assigned or if gameobject is null
            if (backgroundRenderer == null || backgroundRenderer.gameObject == null)
            {
                Debug.Log("BattleFieldRenderer: backgroundRenderer is null or destroyed, creating new one");
                var bgObj = new GameObject("BattleBackground");
                bgObj.transform.SetParent(transform);
                bgObj.transform.position = new Vector3(374f, 210f, 10f); // Behind everything (positive Z = behind camera)
                backgroundRenderer = bgObj.AddComponent<SpriteRenderer>();
                backgroundRenderer.sortingOrder = -100; // Behind everything

                Debug.Log($"BattleFieldRenderer: Created background renderer at position {bgObj.transform.position}");
            }
            else
            {
                Debug.Log($"BattleFieldRenderer: backgroundRenderer already exists at {backgroundRenderer.transform.position}, enabled: {backgroundRenderer.enabled}, sprite: {backgroundRenderer.sprite}");
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
            // Center camera on battlefield
            CenterCamera();

            // Setup background (must be after camera is positioned)
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

            Sprite background = terrain switch
            {
                TerrainType.Grass => grasslandBackground,
                TerrainType.Dirt => dirtBackground,
                TerrainType.Sand => sandBackground,
                TerrainType.Swamp => swampBackground,
                TerrainType.Rough => roughBackground,
                TerrainType.Snow => snowBackground,
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
        /// Centers the camera on the battlefield to show all hexes.
        /// Fixed camera position like HoMM3 - shows entire battlefield.
        /// </summary>
        private void CenterCamera()
        {
            // Calculate the bounds of the entire battlefield
            // Top-left corner: hex (0, 0)
            var topLeft = BattleHexGrid.HexToWorld(0, 0);
            // Bottom-right corner: hex (BATTLE_WIDTH-1, BATTLE_HEIGHT-1)
            var bottomRight = BattleHexGrid.HexToWorld(BattleHexGrid.BATTLE_WIDTH - 1, BattleHexGrid.BATTLE_HEIGHT - 1);

            // Calculate center between these bounds
            var centerX = (topLeft.x + bottomRight.x) * 0.5f;
            var centerY = (topLeft.y + bottomRight.y) * 0.5f;

            battleCamera.transform.position = new Vector3(centerX, centerY, -10f);

            // Calculate orthographic size to fit entire battlefield
            var battlefieldWidth = bottomRight.x - topLeft.x + BattleHexGrid.HEX_WIDTH;
            var battlefieldHeight = bottomRight.y - topLeft.y + BattleHexGrid.HEX_HEIGHT;

            Debug.Log($"BattleFieldRenderer: Calculated battlefield size: {battlefieldWidth}x{battlefieldHeight}");

            // Orthographic size is half of the camera's view height
            // To fit the battlefield, we need: orthographicSize * 2 >= battlefieldHeight
            var cameraAspect = battleCamera.aspect;
            var battlefieldAspect = battlefieldWidth / battlefieldHeight;

            Debug.Log($"BattleFieldRenderer: Camera aspect: {cameraAspect}, Battlefield aspect: {battlefieldAspect}");

            if (battlefieldAspect > cameraAspect)
            {
                // Battlefield is wider than camera - fit to width
                // Camera view width = orthographicSize * 2 * aspect
                // We need: orthographicSize * 2 * aspect >= battlefieldWidth
                battleCamera.orthographicSize = (battlefieldWidth / cameraAspect) * 0.5f;
                Debug.Log($"BattleFieldRenderer: Fitting to WIDTH - ortho size set to {battleCamera.orthographicSize}");
            }
            else
            {
                // Battlefield is taller than camera - fit to height
                battleCamera.orthographicSize = battlefieldHeight * 0.5f;
                Debug.Log($"BattleFieldRenderer: Fitting to HEIGHT - ortho size set to {battleCamera.orthographicSize}");
            }

            Debug.Log($"BattleFieldRenderer: Camera positioned at ({centerX:F2}, {centerY:F2}, -10) with orthographic size {battleCamera.orthographicSize:F2}");
            Debug.Log($"BattleFieldRenderer: Battlefield bounds - TopLeft: {topLeft}, BottomRight: {bottomRight}");
            Debug.Log($"BattleFieldRenderer: Camera view will show from ({centerX - battleCamera.orthographicSize * battleCamera.aspect:F2}, {centerY - battleCamera.orthographicSize:F2}) to ({centerX + battleCamera.orthographicSize * battleCamera.aspect:F2}, {centerY + battleCamera.orthographicSize:F2})");
        }

        /// <summary>
        /// Public method to recenter camera (useful for debugging).
        /// </summary>
        [ContextMenu("Recenter Camera")]
        public void RecenterCamera()
        {
            CenterCamera();
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

            // Set sprite and enable renderer
            if (grasslandBackground != null)
            {
                backgroundRenderer.sprite = grasslandBackground;
            }
            backgroundRenderer.enabled = true;
            backgroundRenderer.sortingOrder = -100;

            // Position background at camera position (same X,Y, but in front at Z=5)
            var camPos = battleCamera.transform.position;
            backgroundRenderer.transform.position = new Vector3(camPos.x, camPos.y, 5f);

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
            var cameraHeight = battleCamera.orthographicSize * 2f; // Full height
            var cameraWidth = cameraHeight * battleCamera.aspect;   // Full width

            Debug.Log($"BattleFieldRenderer: Camera orthographic size: {battleCamera.orthographicSize}, aspect: {battleCamera.aspect}");
            Debug.Log($"BattleFieldRenderer: Camera view size: {cameraWidth}x{cameraHeight}");
            Debug.Log($"BattleFieldRenderer: Sprite texture size: {spriteTexture.width}x{spriteTexture.height} pixels");
            Debug.Log($"BattleFieldRenderer: Sprite PPU: {sprite.pixelsPerUnit}");
            Debug.Log($"BattleFieldRenderer: Sprite bounds (world units): {spriteBounds.x}x{spriteBounds.y}");

            // The grass sprite is ONE large image (1184x864 pixels with PPU=1)
            // that includes the battlefield AND decorative borders (like HOMM3)
            // Now that hex dimensions are properly set (HEX_WIDTH=1.0, HEX_HEIGHT=1.0),
            // the battlefield is ~17x11 world units, and we can scale the background to fit the camera

            // Scale to fit camera view (camera shows entire battlefield + some extra space)
            var scaleToFitWidth = cameraWidth / spriteBounds.x;
            var scaleToFitHeight = cameraHeight / spriteBounds.y;

            // Use the smaller scale to ensure entire sprite fits in view
            var targetScale = Mathf.Min(scaleToFitWidth, scaleToFitHeight);

            backgroundRenderer.transform.localScale = Vector3.one * targetScale;

            Debug.Log($"BattleFieldRenderer: Camera view size: {cameraWidth:F2}x{cameraHeight:F2}");
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
        private bool hasLoggedAutoScale = false;

        void Update()
        {
            // Allow runtime adjustment of background scale (only if manual override enabled)
            if (manualScaleOverride && backgroundRenderer != null && Application.isPlaying)
            {
                if (Mathf.Abs(manualBackgroundScale - lastBackgroundScale) > 0.001f)
                {
                    backgroundRenderer.transform.localScale = Vector3.one * manualBackgroundScale;
                    lastBackgroundScale = manualBackgroundScale;
                    Debug.Log($"BattleFieldRenderer: Manual background scale adjusted to {manualBackgroundScale}");
                }
            }
            else if (!hasLoggedAutoScale && backgroundRenderer != null)
            {
                // Log once that we're using automatic scaling
                Debug.Log($"BattleFieldRenderer: Using automatic scale (manualScaleOverride is disabled) - current scale: {backgroundRenderer.transform.localScale}");
                hasLoggedAutoScale = true;
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
            // Set Z to -1 to be in front of background (background is at Z=5, camera at Z=-10)
            // So Z=-1 puts hexes in front of camera
            hexObj.transform.position = new Vector3(worldPos.x, worldPos.y, -1f);

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

            // Create hex shape (6 vertices)
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
