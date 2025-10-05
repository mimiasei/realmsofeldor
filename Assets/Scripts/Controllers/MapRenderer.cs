using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using RealmsOfEldor.Core;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.Controllers
{
    /// <summary>
    /// Renders the game map using Unity Tilemaps.
    /// Subscribes to MapEventChannel for updates.
    /// </summary>
    public class MapRenderer : MonoBehaviour
    {
        [Header("Tilemap References")]
        [SerializeField] private Tilemap terrainTilemap;
        [SerializeField] private Tilemap objectsTilemap;
        [SerializeField] private Tilemap highlightTilemap;

        [Header("Terrain Configuration")]
        [SerializeField] private Data.TerrainData[] terrainDataArray;

        [Header("Object Prefabs")]
        [SerializeField] private GameObject resourcePrefab;
        [SerializeField] private GameObject minePrefab;
        [SerializeField] private GameObject dwellingPrefab;

        [Header("Highlight Tiles")]
        [SerializeField] private TileBase selectionTile;
        [SerializeField] private TileBase reachableTile;

        [Header("Event Channel")]
        [SerializeField] private MapEventChannel mapEvents;

        private GameMap currentMap;
        private Dictionary<TerrainType, Data.TerrainData> terrainLookup;
        private Dictionary<int, GameObject> objectInstances;

        private void Awake()
        {
            terrainLookup = new Dictionary<TerrainType, Data.TerrainData>();
            objectInstances = new Dictionary<int, GameObject>();

            // Build terrain lookup
            if (terrainDataArray != null)
            {
                foreach (var terrainData in terrainDataArray)
                {
                    if (terrainData != null)
                        terrainLookup[terrainData.terrainType] = terrainData;
                }
            }
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
            if (highlightTilemap != null && selectionTile != null)
            {
                highlightTilemap.ClearAllTiles();
                var tilePos = PositionToTilePosition(pos);
                highlightTilemap.SetTile(tilePos, selectionTile);
            }
        }

        private void HandleTilesHighlighted(List<Position> positions)
        {
            if (highlightTilemap != null && reachableTile != null)
            {
                highlightTilemap.ClearAllTiles();
                foreach (var pos in positions)
                {
                    var tilePos = PositionToTilePosition(pos);
                    highlightTilemap.SetTile(tilePos, reachableTile);
                }
            }
        }

        private void HandleSelectionCleared()
        {
            if (highlightTilemap != null)
                highlightTilemap.ClearAllTiles();
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
            Debug.Log($"MapRenderer: terrainTilemap = {(terrainTilemap != null ? "OK" : "NULL")}");
            Debug.Log($"MapRenderer: terrainLookup has {terrainLookup.Count} entries");

            ClearMap();

            // Render all terrain tiles
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

            Debug.Log($"MapRenderer: Rendered {tilesRendered} tiles");

            // Render all objects
            foreach (var obj in currentMap.GetAllObjects())
            {
                AddObjectRendering(obj);
            }

            Debug.Log($"<color=green>MapRenderer: RenderFullMap complete!</color>");
        }

        private void UpdateTile(Position pos)
        {
            if (currentMap == null || !currentMap.IsInBounds(pos)) return;
            if (terrainTilemap == null) return;

            var tile = currentMap.GetTile(pos);
            var tilePos = PositionToTilePosition(pos);

            // Get terrain data
            if (terrainLookup.TryGetValue(tile.Terrain, out var terrainData))
            {
                var unityTile = terrainData.GetTileVariant(tile.VisualVariant);
                terrainTilemap.SetTile(tilePos, unityTile);
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

            if (prefab != null)
            {
                var instance = Instantiate(prefab, transform);
                instance.transform.position = MapToWorldPosition(obj.Position);
                instance.name = obj.Name;

                // Add MapObjectView component to link instance to data
                var view = instance.GetComponent<MapObjectView>();
                if (view == null)
                    view = instance.AddComponent<MapObjectView>();
                view.ObjectId = obj.InstanceId;

                objectInstances[obj.InstanceId] = instance;
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

        private void ClearMap()
        {
            if (terrainTilemap != null)
                terrainTilemap.ClearAllTiles();
            if (objectsTilemap != null)
                objectsTilemap.ClearAllTiles();
            if (highlightTilemap != null)
                highlightTilemap.ClearAllTiles();

            foreach (var instance in objectInstances.Values)
            {
                if (instance != null)
                    Destroy(instance);
            }
            objectInstances.Clear();
        }

        // Position conversion
        public Vector3Int PositionToTilePosition(Position pos)
        {
            return new Vector3Int(pos.X, pos.Y, 0);
        }

        public Position TilePositionToPosition(Vector3Int tilePos)
        {
            return new Position(tilePos.x, tilePos.y);
        }

        public Vector3 MapToWorldPosition(Position pos)
        {
            if (terrainTilemap != null)
            {
                var tilePos = PositionToTilePosition(pos);
                return terrainTilemap.GetCellCenterWorld(tilePos);
            }
            return new Vector3(pos.X, pos.Y, 0);
        }

        public Position WorldToMapPosition(Vector3 worldPos)
        {
            if (terrainTilemap != null)
            {
                var tilePos = terrainTilemap.WorldToCell(worldPos);
                return TilePositionToPosition(tilePos);
            }
            return new Position(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y));
        }

        // Debug visualization
        private void OnDrawGizmos()
        {
            if (currentMap == null || terrainTilemap == null) return;

            Gizmos.color = Color.cyan;
            var cellSize = terrainTilemap.cellSize;

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
    /// </summary>
    public class MapObjectView : MonoBehaviour
    {
        public int ObjectId { get; set; }
    }
}
