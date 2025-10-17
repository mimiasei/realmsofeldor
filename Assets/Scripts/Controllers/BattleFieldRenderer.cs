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
        [SerializeField] private float hexLineWidth = 0.1f;

        private Camera battleCamera;
        private GameObject[,] hexOverlays;

        void Awake()
        {
            battleCamera = GetComponent<Camera>();

            // Create hex grid container if not assigned
            if (hexGridContainer == null)
            {
                hexGridContainer = new GameObject("HexGridContainer");
                hexGridContainer.transform.SetParent(transform);
            }

            hexOverlays = new GameObject[BattleHexGrid.BATTLE_WIDTH, BattleHexGrid.BATTLE_HEIGHT];
        }

        void Start()
        {
            // Center camera on battlefield
            CenterCamera();

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
        /// Centers the camera on the battlefield.
        /// </summary>
        private void CenterCamera()
        {
            var center = BattleHexGrid.GetBattlefieldCenter();
            battleCamera.transform.position = new Vector3(center.x, center.y, -10f);

            // Adjust orthographic size to fit battlefield
            var battlefieldHeight = BattleHexGrid.BATTLE_HEIGHT * BattleHexGrid.HEX_HEIGHT;
            battleCamera.orthographicSize = battlefieldHeight * 0.6f; // Add some padding

            Debug.Log($"BattleFieldRenderer: Camera centered at {center}");
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

            Debug.Log($"BattleFieldRenderer: Initialized {BattleHexGrid.BATTLE_WIDTH}x{BattleHexGrid.BATTLE_HEIGHT} hex grid");
        }

        /// <summary>
        /// Creates a visual hex overlay at the specified hex coordinates.
        /// </summary>
        private GameObject CreateHexOverlay(int hexX, int hexY)
        {
            var hexObj = new GameObject($"Hex_{hexX}_{hexY}");
            var worldPos = BattleHexGrid.HexToWorld(hexX, hexY);
            hexObj.transform.position = worldPos;

            // Add LineRenderer for hex border
            var lineRenderer = hexObj.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = hexLineColor;
            lineRenderer.endColor = hexLineColor;
            lineRenderer.startWidth = hexLineWidth;
            lineRenderer.endWidth = hexLineWidth;
            lineRenderer.loop = true;
            lineRenderer.sortingOrder = 100; // Render above everything

            // Create hex shape (6 vertices)
            var hexPoints = GetHexVertices(BattleHexGrid.HEX_WIDTH, BattleHexGrid.HEX_HEIGHT);
            lineRenderer.positionCount = 6;
            lineRenderer.SetPositions(hexPoints);

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
    }
}
