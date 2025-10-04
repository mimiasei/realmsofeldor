using System;
using System.Collections.Generic;
using System.Linq;

namespace RealmsOfEldor.Core
{
    /// <summary>
    /// Main game map class managing tiles and map objects.
    /// Based on VCMI's CMap.
    /// </summary>
    public class GameMap
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        private MapTile[,] tiles;
        private Dictionary<int, MapObject> objects;
        private int nextObjectId;

        public GameMap(int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Map dimensions must be positive");

            Width = width;
            Height = height;
            tiles = new MapTile[width, height];
            objects = new Dictionary<int, MapObject>();
            nextObjectId = 1;

            // Initialize with default grass terrain
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    tiles[x, y] = new MapTile(TerrainType.Grass, 0, 100);
                }
            }
        }

        // Bounds checking
        public bool IsInBounds(Position pos)
        {
            return pos.X >= 0 && pos.X < Width && pos.Y >= 0 && pos.Y < Height;
        }

        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        // Tile access
        public MapTile GetTile(Position pos)
        {
            if (!IsInBounds(pos))
                throw new ArgumentOutOfRangeException(nameof(pos), "Position is out of map bounds");

            return tiles[pos.X, pos.Y];
        }

        public MapTile GetTile(int x, int y)
        {
            return GetTile(new Position(x, y));
        }

        public void SetTile(Position pos, MapTile tile)
        {
            if (!IsInBounds(pos))
                throw new ArgumentOutOfRangeException(nameof(pos), "Position is out of map bounds");

            tiles[pos.X, pos.Y] = tile;
        }

        // Terrain modification
        public void SetTerrain(Position pos, TerrainType terrain, int visualVariant = 0, int movementCost = 100)
        {
            if (!IsInBounds(pos))
                throw new ArgumentOutOfRangeException(nameof(pos), "Position is out of map bounds");

            var tile = tiles[pos.X, pos.Y];
            tile.SetTerrain(terrain, visualVariant, movementCost);
            tiles[pos.X, pos.Y] = tile;
        }

        // Object management
        public int AddObject(MapObject obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            obj.InstanceId = nextObjectId++;
            objects[obj.InstanceId] = obj;

            // Update tiles with object references
            var blockedPositions = obj.GetBlockedPositions();
            var visitablePositions = obj.GetVisitablePositions();

            foreach (var pos in blockedPositions)
            {
                if (IsInBounds(pos))
                {
                    var tile = tiles[pos.X, pos.Y];
                    tile.AddBlockingObject(obj.InstanceId);
                    if (obj.IsVisitable)
                        tile.AddVisitableObject(obj.InstanceId);
                    tiles[pos.X, pos.Y] = tile;
                }
            }

            // For non-blocking visitable objects (like resource piles)
            if (!obj.IsBlocking && obj.IsVisitable)
            {
                if (IsInBounds(obj.Position))
                {
                    var tile = tiles[obj.Position.X, obj.Position.Y];
                    tile.AddVisitableObject(obj.InstanceId);
                    tiles[obj.Position.X, obj.Position.Y] = tile;
                }
            }

            return obj.InstanceId;
        }

        public void RemoveObject(int objectId)
        {
            if (!objects.TryGetValue(objectId, out var obj))
                return;

            // Remove from tiles
            var blockedPositions = obj.GetBlockedPositions();
            foreach (var pos in blockedPositions)
            {
                if (IsInBounds(pos))
                {
                    var tile = tiles[pos.X, pos.Y];
                    tile.RemoveBlockingObject(objectId);
                    tile.RemoveVisitableObject(objectId);
                    tiles[pos.X, pos.Y] = tile;
                }
            }

            // For non-blocking visitable objects
            if (!obj.IsBlocking && obj.IsVisitable)
            {
                if (IsInBounds(obj.Position))
                {
                    var tile = tiles[obj.Position.X, obj.Position.Y];
                    tile.RemoveVisitableObject(objectId);
                    tiles[obj.Position.X, obj.Position.Y] = tile;
                }
            }

            objects.Remove(objectId);
        }

        public MapObject GetObject(int objectId)
        {
            return objects.TryGetValue(objectId, out var obj) ? obj : null;
        }

        public List<MapObject> GetObjectsAt(Position pos)
        {
            if (!IsInBounds(pos))
                return new List<MapObject>();

            var tile = GetTile(pos);
            var result = new List<MapObject>();

            // Get all visitable and blocking objects at this position
            var allObjectIds = new HashSet<int>();
            foreach (var id in tile.GetVisitableObjects())
                allObjectIds.Add(id);
            foreach (var id in tile.GetBlockingObjects())
                allObjectIds.Add(id);

            foreach (var id in allObjectIds)
            {
                if (objects.TryGetValue(id, out var obj))
                    result.Add(obj);
            }

            return result;
        }

        public List<MapObject> GetObjectsByType(MapObjectType type)
        {
            return objects.Values.Where(obj => obj.ObjectType == type).ToList();
        }

        public List<T> GetObjectsOfClass<T>() where T : MapObject
        {
            return objects.Values.OfType<T>().ToList();
        }

        public IEnumerable<MapObject> GetAllObjects()
        {
            return objects.Values;
        }

        // Movement validation
        public bool CanMoveBetween(Position from, Position to)
        {
            if (!IsInBounds(from) || !IsInBounds(to))
                return false;

            var tile = GetTile(to);
            return tile.IsClear();
        }

        public int GetMovementCost(Position from, Position to)
        {
            if (!IsInBounds(from) || !IsInBounds(to))
                return int.MaxValue;

            var tile = GetTile(to);
            if (!tile.IsPassable())
                return int.MaxValue;

            return tile.MovementCost;
        }

        // Utility methods
        public void CalculateCoastalTiles()
        {
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    var pos = new Position(x, y);
                    var tile = tiles[x, y];

                    // Land tile adjacent to water = coastal
                    if (!tile.IsWater())
                    {
                        var isCoastal = false;
                        foreach (var adjacentPos in GetAdjacentPositions(pos))
                        {
                            if (GetTile(adjacentPos).IsWater())
                            {
                                isCoastal = true;
                                break;
                            }
                        }
                        tile.SetCoastal(isCoastal);
                        tiles[x, y] = tile;
                    }
                }
            }
        }

        public List<Position> GetAdjacentPositions(Position pos)
        {
            var adjacent = new List<Position>();

            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    var adjacentPos = new Position(pos.X + dx, pos.Y + dy);
                    if (IsInBounds(adjacentPos))
                        adjacent.Add(adjacentPos);
                }
            }

            return adjacent;
        }

        public void ApplyWeeklyGrowth()
        {
            foreach (var dwelling in GetObjectsOfClass<DwellingObject>())
            {
                dwelling.ApplyWeeklyGrowth();
            }
        }
    }
}
