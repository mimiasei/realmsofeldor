using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using RealmsOfEldor.Core;
using RealmsOfEldor.Core.Map;

namespace RealmsOfEldor.Controllers
{
    /// <summary>
    /// Renders the game map using Unity's Tilemap system.
    /// MonoBehaviour wrapper that visualizes the pure C# GameMap.
    /// Handles terrain rendering and map object placement.
    /// </summary>
    [RequireComponent(typeof(Grid))]
    public class MapRenderer : MonoBehaviour
    {
        [Header("Tilemaps")]
        [SerializeField] private Tilemap terrainTilemap;
        [SerializeField] private Tilemap objectsTilemap;

        [Header("Terrain Tiles")]
        [SerializeField] private TileBase grassTile;
        [SerializeField] private TileBase dirtTile;
        [SerializeField] private TileBase sandTile;
        [SerializeField] private TileBase snowTile;
        [SerializeField] private TileBase swampTile;
        [SerializeField] private TileBase roughTile;
        [SerializeField] private TileBase subterraneanTile;
        [SerializeField] private TileBase lavaTile;
        [SerializeField] private TileBase waterTile;
        [SerializeField] private TileBase rockTile;

        [Header("Object Prefabs")]
        [SerializeField] private GameObject resourcePrefab;
        [SerializeField] private GameObject minePrefab;
        [SerializeField] private GameObject dwellingPrefab;

        [Header("Rendering Settings")]
        [SerializeField] private bool showGrid = true;
        [SerializeField] private Color gridColor = new Color(1f, 1f, 1f, 0.1f);

        /// <summary>
        /// Reference to the game map being rendered.
        /// </summary>
        private GameMap gameMap;

        /// <summary>
        /// Grid component for tilemap positioning.
        /// </summary>
        private Grid grid;

        /// <summary>
        /// Dictionary of instantiated object GameObjects by object ID.
        /// </summary>
        private Dictionary<int, GameObject> objectInstances;

        private void Awake()
        {
            grid = GetComponent<Grid>();
            objectInstances = new Dictionary<int, GameObject>();

            // Create tilemaps if not assigned
            if (terrainTilemap == null)
            {
                var terrainObj = new GameObject("Terrain");
                terrainObj.transform.SetParent(transform);
                terrainTilemap = terrainObj.AddComponent<Tilemap>();
                terrainObj.AddComponent<TilemapRenderer>();
            }

            if (objectsTilemap == null)
            {
                var objectsObj = new GameObject("Objects");
                objectsObj.transform.SetParent(transform);
                objectsTilemap = objectsObj.AddComponent<Tilemap>();
                var renderer = objectsObj.AddComponent<TilemapRenderer>();
                renderer.sortingOrder = 1; // Render above terrain
            }
        }

        /// <summary>
        /// Sets the game map to render and performs initial rendering.
        /// </summary>
        public void SetMap(GameMap map)
        {
            if (map == null)
            {
                Debug.LogError("Cannot set null map.");
                return;
            }

            gameMap = map;
            RenderMap();
        }

        /// <summary>
        /// Renders the entire map (terrain and objects).
        /// </summary>
        public void RenderMap()
        {
            if (gameMap == null)
            {
                Debug.LogWarning("No map to render.");
                return;
            }

            RenderTerrain();
            RenderObjects();
        }

        /// <summary>
        /// Renders all terrain tiles.
        /// </summary>
        private void RenderTerrain()
        {
            terrainTilemap.ClearAllTiles();

            for (var x = 0; x < gameMap.Width; x++)
            {
                for (var y = 0; y < gameMap.Height; y++)
                {
                    var pos = new Position(x, y);
                    var tile = gameMap.GetTile(pos);
                    var tilePos = new Vector3Int(x, y, 0);

                    var tileBase = GetTileForTerrain(tile.Terrain);
                    if (tileBase != null)
                    {
                        terrainTilemap.SetTile(tilePos, tileBase);
                    }
                }
            }
        }

        /// <summary>
        /// Renders all map objects.
        /// </summary>
        private void RenderObjects()
        {
            // Clear existing object instances
            foreach (var obj in objectInstances.Values)
            {
                if (obj != null)
                    Destroy(obj);
            }
            objectInstances.Clear();

            // Get all objects from map
            var allObjects = gameMap.GetObjectsOfClass<MapObject>();
            foreach (var mapObj in allObjects)
            {
                RenderObject(mapObj);
            }
        }

        /// <summary>
        /// Renders a single map object.
        /// </summary>
        private void RenderObject(MapObject mapObj)
        {
            if (mapObj == null)
                return;

            GameObject prefab = mapObj.ObjectType switch
            {
                MapObjectType.Resource => resourcePrefab,
                MapObjectType.Mine => minePrefab,
                MapObjectType.Dwelling => dwellingPrefab,
                _ => null
            };

            if (prefab == null)
            {
                Debug.LogWarning($"No prefab assigned for object type: {mapObj.ObjectType}");
                return;
            }

            var worldPos = grid.CellToWorld(new Vector3Int(mapObj.Position.X, mapObj.Position.Y, 0));
            worldPos += grid.cellSize / 2; // Center in cell

            var instance = Instantiate(prefab, worldPos, Quaternion.identity, transform);
            instance.name = $"{mapObj.ObjectType}_{mapObj.InstanceId}";

            // Store reference
            objectInstances[mapObj.InstanceId] = instance;

            // Attach map object data component if needed
            var objData = instance.GetComponent<MapObjectView>();
            if (objData == null)
                objData = instance.AddComponent<MapObjectView>();

            objData.Initialize(mapObj);
        }

        /// <summary>
        /// Updates the rendering of a specific tile.
        /// </summary>
        public void UpdateTile(Position pos)
        {
            if (gameMap == null || !gameMap.IsInBounds(pos))
                return;

            var tile = gameMap.GetTile(pos);
            var tilePos = new Vector3Int(pos.X, pos.Y, 0);
            var tileBase = GetTileForTerrain(tile.Terrain);

            if (tileBase != null)
            {
                terrainTilemap.SetTile(tilePos, tileBase);
            }
        }

        /// <summary>
        /// Adds rendering for a new object.
        /// </summary>
        public void AddObjectRendering(MapObject mapObj)
        {
            if (mapObj == null || objectInstances.ContainsKey(mapObj.InstanceId))
                return;

            RenderObject(mapObj);
        }

        /// <summary>
        /// Removes rendering for an object.
        /// </summary>
        public void RemoveObjectRendering(int objectId)
        {
            if (objectInstances.TryGetValue(objectId, out var obj))
            {
                if (obj != null)
                    Destroy(obj);
                objectInstances.Remove(objectId);
            }
        }

        /// <summary>
        /// Highlights a tile (for movement range, selection, etc.)
        /// </summary>
        public void HighlightTile(Position pos, Color color)
        {
            if (!gameMap.IsInBounds(pos))
                return;

            var tilePos = new Vector3Int(pos.X, pos.Y, 0);
            objectsTilemap.SetTileFlags(tilePos, TileFlags.None);
            objectsTilemap.SetColor(tilePos, color);
        }

        /// <summary>
        /// Clears all tile highlights.
        /// </summary>
        public void ClearHighlights()
        {
            objectsTilemap.ClearAllTiles();
        }

        /// <summary>
        /// Converts world position to map position.
        /// </summary>
        public Position WorldToMapPosition(Vector3 worldPos)
        {
            var cellPos = grid.WorldToCell(worldPos);
            return new Position(cellPos.x, cellPos.y);
        }

        /// <summary>
        /// Converts map position to world position (cell center).
        /// </summary>
        public Vector3 MapToWorldPosition(Position mapPos)
        {
            var worldPos = grid.CellToWorld(new Vector3Int(mapPos.X, mapPos.Y, 0));
            return worldPos + grid.cellSize / 2;
        }

        /// <summary>
        /// Gets the appropriate TileBase for a terrain type.
        /// </summary>
        private TileBase GetTileForTerrain(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Grass => grassTile,
                TerrainType.Dirt => dirtTile,
                TerrainType.Sand => sandTile,
                TerrainType.Snow => snowTile,
                TerrainType.Swamp => swampTile,
                TerrainType.Rough => roughTile,
                TerrainType.Subterranean => subterraneanTile,
                TerrainType.Lava => lavaTile,
                TerrainType.Water => waterTile,
                TerrainType.Rock => rockTile,
                _ => null
            };
        }

        private void OnDrawGizmos()
        {
            if (!showGrid || gameMap == null)
                return;

            Gizmos.color = gridColor;

            // Draw vertical lines
            for (var x = 0; x <= gameMap.Width; x++)
            {
                var start = grid.CellToWorld(new Vector3Int(x, 0, 0));
                var end = grid.CellToWorld(new Vector3Int(x, gameMap.Height, 0));
                Gizmos.DrawLine(start, end);
            }

            // Draw horizontal lines
            for (var y = 0; y <= gameMap.Height; y++)
            {
                var start = grid.CellToWorld(new Vector3Int(0, y, 0));
                var end = grid.CellToWorld(new Vector3Int(gameMap.Width, y, 0));
                Gizmos.DrawLine(start, end);
            }
        }
    }

    /// <summary>
    /// Component attached to map object instances to link them to their data.
    /// </summary>
    public class MapObjectView : MonoBehaviour
    {
        public MapObject MapObjectData { get; private set; }

        public void Initialize(MapObject mapObj)
        {
            MapObjectData = mapObj;
        }
    }
}
