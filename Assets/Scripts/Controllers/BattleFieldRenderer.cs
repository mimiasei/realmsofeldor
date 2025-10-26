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

        [Header("Camera Mode - Olden Era Style")]
        [SerializeField] private bool usePerspectiveCamera = true; // 2.5D billboard mode (true for Olden Era style)
        [SerializeField] private float perspectiveFOV = 40f; // Field of view
        [SerializeField] private float cameraHeight = 12f; // Height above ground (Y axis)
        [SerializeField] private float cameraZOffset = -10f; // Distance back from center (negative Z)
        [SerializeField] private float cameraTiltAngle = 50f; // Tilt angle looking down at ground (Olden Era ~50°)

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

            // Setup camera mode (perspective for 2.5D billboards or orthographic for classic 2D)
            if (usePerspectiveCamera)
            {
                battleCamera.orthographic = false;
                battleCamera.fieldOfView = perspectiveFOV;
                Debug.Log($"BattleFieldRenderer: Using perspective camera (FOV: {perspectiveFOV})");
            }
            else
            {
                battleCamera.orthographic = true;
                Debug.Log("BattleFieldRenderer: Using orthographic camera");
            }

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
            // Disable CameraController if it exists (it's for adventure map, not battle)
            var cameraController = GetComponent<CameraController>();
            if (cameraController != null)
            {
                Debug.LogWarning("BattleFieldRenderer: Disabling CameraController - it's for adventure map, not battle scene!");
                cameraController.enabled = false;
            }

            // Center camera on battlefield
            CenterCamera();

            // Log final camera state after setup
            Debug.Log("=== CAMERA STATE AFTER CENTERING ===");
            Debug.Log($"Camera Position: {battleCamera.transform.position}");
            Debug.Log($"Camera Rotation (Euler): {battleCamera.transform.rotation.eulerAngles}");
            Debug.Log($"Camera Rotation (Quaternion): {battleCamera.transform.rotation}");
            Debug.Log($"Camera Projection: {(battleCamera.orthographic ? "Orthographic" : "Perspective")}");
            if (!battleCamera.orthographic)
            {
                Debug.Log($"Camera FOV: {battleCamera.fieldOfView}");
            }
            Debug.Log("====================================");

            // Setup background (must be after camera is positioned)
            SetupBackground();

            // Initialize hex grid (hidden by default)
            InitializeHexGrid();

            // Setup lighting for 2.5D billboard rendering (if using perspective camera)
            if (usePerspectiveCamera)
            {
                SetupLighting();
            }
        }

        /// <summary>
        /// Sets up lighting and shadows for 2.5D billboard rendering.
        /// </summary>
        private void SetupLighting()
        {
            // Check if lighting setup already exists
            var existingSetup = GetComponent<BattleLightingSetup>();
            if (existingSetup == null)
            {
                // Add lighting setup component
                var lightingSetup = gameObject.AddComponent<BattleLightingSetup>();
                Debug.Log("BattleFieldRenderer: Added BattleLightingSetup component for 2.5D rendering");
            }
            else
            {
                Debug.Log("BattleFieldRenderer: BattleLightingSetup already exists");
            }
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
        /// Supports both orthographic (classic 2D) and perspective (2.5D billboard) modes.
        /// </summary>
        private void CenterCamera()
        {
            // Calculate the bounds of the entire battlefield on X,Z ground plane
            var topLeft = BattleHexGrid.HexToWorld(0, 0);
            var bottomRight = BattleHexGrid.HexToWorld(BattleHexGrid.BATTLE_WIDTH - 1, BattleHexGrid.BATTLE_HEIGHT - 1);

            // Calculate center on X,Z plane (Y is height)
            var centerX = (topLeft.x + bottomRight.x) * 0.5f;
            var centerZ = (topLeft.z + bottomRight.z) * 0.5f;

            if (usePerspectiveCamera)
            {
                // Position camera for 3D perspective view (Olden Era style)
                // Camera is above (Y+) and behind (Z-) the battlefield center
                // Looking down at the X,Z ground plane
                battleCamera.transform.position = new Vector3(centerX, cameraHeight, centerZ + cameraZOffset);
                battleCamera.transform.rotation = Quaternion.Euler(cameraTiltAngle, 0, 0);

                Debug.Log($"BattleFieldRenderer: Perspective camera at {battleCamera.transform.position}, rotation: {battleCamera.transform.rotation.eulerAngles}, FOV: {perspectiveFOV}°");
                Debug.Log($"BattleFieldRenderer: Battlefield center at ({centerX}, 0, {centerZ}) on X,Z ground plane");
            }
            else
            {
                // Orthographic camera - top down view
                battleCamera.transform.position = new Vector3(centerX, 20f, centerZ);
                battleCamera.transform.rotation = Quaternion.Euler(90f, 0, 0); // Looking straight down

                // Calculate orthographic size to fit entire battlefield
                var battlefieldWidth = bottomRight.x - topLeft.x + BattleHexGrid.HEX_WIDTH;
                var battlefieldDepth = bottomRight.z - topLeft.z + BattleHexGrid.HEX_HEIGHT;

                var cameraAspect = battleCamera.aspect;
                var battlefieldAspect = battlefieldWidth / battlefieldDepth;

                if (battlefieldAspect > cameraAspect)
                {
                    battleCamera.orthographicSize = (battlefieldWidth / cameraAspect) * 0.5f;
                }
                else
                {
                    battleCamera.orthographicSize = battlefieldDepth * 0.5f;
                }

                Debug.Log($"BattleFieldRenderer: Orthographic top-down camera at {battleCamera.transform.position}, size: {battleCamera.orthographicSize}");
            }
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

        [Header("Runtime Camera Adjustment")]
        [Tooltip("Enable to manually adjust camera during play mode")]
        [SerializeField] private bool manualCameraAdjustment = false;
        [SerializeField] [Range(1f, 30f)] private float runtimeCameraHeight = 12f;
        [SerializeField] [Range(-20f, 5f)] private float runtimeCameraZOffset = -10f;
        [SerializeField] [Range(20f, 70f)] private float runtimeCameraTilt = 50f;
        [SerializeField] [Range(15f, 60f)] private float runtimeCameraFOV = 40f;

        private float lastBackgroundScale = -1f;
        private Vector3 lastCameraPos = Vector3.zero;
        private Quaternion lastCameraRot = Quaternion.identity;
        private float lastFOV = 0f;

        void Update()
        {
            // Allow runtime adjustment of camera
            if (manualCameraAdjustment && battleCamera != null && Application.isPlaying)
            {
                var topLeft = BattleHexGrid.HexToWorld(0, 0);
                var bottomRight = BattleHexGrid.HexToWorld(BattleHexGrid.BATTLE_WIDTH - 1, BattleHexGrid.BATTLE_HEIGHT - 1);
                var centerX = (topLeft.x + bottomRight.x) * 0.5f;
                var centerZ = (topLeft.z + bottomRight.z) * 0.5f;

                var newCameraPos = new Vector3(centerX, runtimeCameraHeight, centerZ + runtimeCameraZOffset);
                var newCameraRot = Quaternion.Euler(runtimeCameraTilt, 0, 0);

                if (Vector3.Distance(newCameraPos, lastCameraPos) > 0.01f ||
                    Quaternion.Angle(newCameraRot, lastCameraRot) > 0.1f ||
                    Mathf.Abs(runtimeCameraFOV - lastFOV) > 0.1f)
                {
                    battleCamera.transform.position = newCameraPos;
                    battleCamera.transform.rotation = newCameraRot;
                    battleCamera.fieldOfView = runtimeCameraFOV;

                    lastCameraPos = newCameraPos;
                    lastCameraRot = newCameraRot;
                    lastFOV = runtimeCameraFOV;
                }
            }

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
