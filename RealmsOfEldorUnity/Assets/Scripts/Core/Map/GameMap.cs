using System;
using System.Collections.Generic;
using System.Linq;

namespace RealmsOfEldor.Core.Map
{
    /// <summary>
    /// The main game map containing terrain tiles and objects.
    /// Based on VCMI's CMap class (CMap.h).
    /// Pure C# class with no Unity dependencies.
    /// </summary>
    [Serializable]
    public class GameMap
    {
        /// <summary>
        /// Width of the map in tiles.
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// Height of the map in tiles.
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// 2D array of map tiles indexed as [x, y].
        /// Based on VCMI's terrain array, simplified to 2D (no underground for MVP).
        /// </summary>
        private MapTile[,] tiles;

        /// <summary>
        /// All map objects indexed by their instance ID.
        /// Based on VCMI's objects vector.
        /// </summary>
        private readonly Dictionary<int, MapObject> objects;

        /// <summary>
        /// Counter for assigning unique object IDs.
        /// Based on VCMI's uidCounter.
        /// </summary>
        private int nextObjectId;

        /// <summary>
        /// Map name/title.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Map description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Creates a new game map with the specified dimensions.
        /// </summary>
        public GameMap(int width, int height, string name = "Untitled Map")
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Map dimensions must be positive.");

            Width = width;
            Height = height;
            Name = name;
            Description = string.Empty;

            tiles = new MapTile[width, height];
            objects = new Dictionary<int, MapObject>();
            nextObjectId = 0;

            InitializeTerrain();
        }

        /// <summary>
        /// Initializes all tiles with default terrain (grass).
        /// Based on VCMI's CMap::initTerrain.
        /// </summary>
        private void InitializeTerrain()
        {
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    tiles[x, y] = new MapTile(TerrainType.Grass);
                }
            }
        }

        /// <summary>
        /// Checks if a position is within map bounds.
        /// Based on VCMI's CMap::isInTheMap.
        /// </summary>
        public bool IsInBounds(Position pos)
        {
            return pos.X >= 0 && pos.X < Width && pos.Y >= 0 && pos.Y < Height;
        }

        /// <summary>
        /// Gets the tile at the specified position.
        /// Based on VCMI's CMap::getTile.
        /// </summary>
        public MapTile GetTile(Position pos)
        {
            if (!IsInBounds(pos))
                throw new ArgumentOutOfRangeException(nameof(pos), "Position is outside map bounds.");

            return tiles[pos.X, pos.Y];
        }

        /// <summary>
        /// Sets the tile at the specified position.
        /// </summary>
        public void SetTile(Position pos, MapTile tile)
        {
            if (!IsInBounds(pos))
                throw new ArgumentOutOfRangeException(nameof(pos), "Position is outside map bounds.");

            tiles[pos.X, pos.Y] = tile;
        }

        /// <summary>
        /// Sets the terrain type for a tile.
        /// </summary>
        public void SetTerrain(Position pos, TerrainType terrain)
        {
            if (!IsInBounds(pos))
                throw new ArgumentOutOfRangeException(nameof(pos), "Position is outside map bounds.");

            tiles[pos.X, pos.Y].Terrain = terrain;
            tiles[pos.X, pos.Y].MovementCost = GetBaseMovementCost(terrain);
        }

        /// <summary>
        /// Adds a map object to the map.
        /// Based on VCMI's CMap::addNewObject.
        /// </summary>
        public void AddObject(MapObject obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            if (!IsInBounds(obj.Position))
                throw new ArgumentException("Object position is outside map bounds.");

            // Assign unique ID
            obj.InstanceId = nextObjectId++;
            objects[obj.InstanceId] = obj;

            // Add to tiles
            foreach (var pos in obj.GetBlockedPositions())
            {
                if (IsInBounds(pos))
                {
                    tiles[pos.X, pos.Y].AddBlockingObject(obj.InstanceId);
                }
            }

            if (obj.IsVisitable)
            {
                foreach (var pos in obj.GetVisitablePositions())
                {
                    if (IsInBounds(pos))
                    {
                        tiles[pos.X, pos.Y].AddVisitableObject(obj.InstanceId);
                    }
                }
            }
        }

        /// <summary>
        /// Removes a map object from the map.
        /// Based on VCMI's CMap::removeObject.
        /// </summary>
        public bool RemoveObject(int objectId)
        {
            if (!objects.TryGetValue(objectId, out var obj))
                return false;

            // Remove from tiles
            foreach (var pos in obj.GetBlockedPositions())
            {
                if (IsInBounds(pos))
                {
                    tiles[pos.X, pos.Y].RemoveBlockingObject(objectId);
                }
            }

            if (obj.IsVisitable)
            {
                foreach (var pos in obj.GetVisitablePositions())
                {
                    if (IsInBounds(pos))
                    {
                        tiles[pos.X, pos.Y].RemoveVisitableObject(objectId);
                    }
                }
            }

            objects.Remove(objectId);
            return true;
        }

        /// <summary>
        /// Gets a map object by its instance ID.
        /// Based on VCMI's CMap::getObject.
        /// </summary>
        public MapObject GetObject(int objectId)
        {
            return objects.TryGetValue(objectId, out var obj) ? obj : null;
        }

        /// <summary>
        /// Gets all objects at a specific position.
        /// </summary>
        public List<MapObject> GetObjectsAt(Position pos)
        {
            if (!IsInBounds(pos))
                return new List<MapObject>();

            var tile = GetTile(pos);
            var result = new List<MapObject>();

            if (tile.VisitableObjectIds != null)
            {
                foreach (var id in tile.VisitableObjectIds)
                {
                    var obj = GetObject(id);
                    if (obj != null)
                        result.Add(obj);
                }
            }

            if (tile.BlockingObjectIds != null)
            {
                foreach (var id in tile.BlockingObjectIds)
                {
                    var obj = GetObject(id);
                    if (obj != null && !result.Contains(obj))
                        result.Add(obj);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets all objects of a specific type.
        /// Based on VCMI's CMap::getObjects template.
        /// </summary>
        public List<MapObject> GetObjectsByType(MapObjectType type)
        {
            return objects.Values.Where(obj => obj.ObjectType == type).ToList();
        }

        /// <summary>
        /// Gets all objects of a specific class (using type parameter).
        /// </summary>
        public List<T> GetObjectsOfClass<T>() where T : MapObject
        {
            return objects.Values.OfType<T>().ToList();
        }

        /// <summary>
        /// Checks if movement is possible between two positions.
        /// Based on VCMI's CMap::canMoveBetween.
        /// </summary>
        public bool CanMoveBetween(Position from, Position to)
        {
            if (!IsInBounds(from) || !IsInBounds(to))
                return false;

            // Must be adjacent positions (including diagonals)
            var dx = Math.Abs(to.X - from.X);
            var dy = Math.Abs(to.Y - from.Y);
            if (dx > 1 || dy > 1)
                return false;

            var fromTile = GetTile(from);
            var toTile = GetTile(to);

            // Target must be passable
            if (!toTile.IsPassable)
                return false;

            // Check water/land compatibility
            if (fromTile.IsWater != toTile.IsWater)
            {
                // Can only transition water<->land on coastal tiles
                if (!fromTile.IsCoastal && !toTile.IsCoastal)
                    return false;
            }

            // Target must not be blocked
            return !toTile.IsBlocked;
        }

        /// <summary>
        /// Calculates movement cost between adjacent positions.
        /// Returns int.MaxValue if movement is not possible.
        /// </summary>
        public int GetMovementCost(Position from, Position to)
        {
            if (!CanMoveBetween(from, to))
                return int.MaxValue;

            var toTile = GetTile(to);
            return toTile.MovementCost;
        }

        /// <summary>
        /// Marks coastal tiles (adjacent to water).
        /// Should be called after terrain generation.
        /// Based on VCMI's CMap::isCoastalTile.
        /// </summary>
        public void CalculateCoastalTiles()
        {
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    var pos = new Position(x, y);
                    var tile = GetTile(pos);

                    // Skip if already water
                    if (tile.IsWater)
                        continue;

                    // Check adjacent tiles for water
                    var isCoastal = false;
                    for (var dx = -1; dx <= 1 && !isCoastal; dx++)
                    {
                        for (var dy = -1; dy <= 1 && !isCoastal; dy++)
                        {
                            if (dx == 0 && dy == 0)
                                continue;

                            var adjacentPos = new Position(x + dx, y + dy);
                            if (IsInBounds(adjacentPos) && GetTile(adjacentPos).IsWater)
                            {
                                isCoastal = true;
                            }
                        }
                    }

                    tiles[x, y].IsCoastal = isCoastal;
                }
            }
        }

        /// <summary>
        /// Gets adjacent positions (8-directional).
        /// </summary>
        public List<Position> GetAdjacentPositions(Position pos)
        {
            var adjacent = new List<Position>();
            var offsets = new[]
            {
                new Position(-1, -1), new Position(0, -1), new Position(1, -1),
                new Position(-1, 0), new Position(1, 0),
                new Position(-1, 1), new Position(0, 1), new Position(1, 1)
            };

            foreach (var offset in offsets)
            {
                var adjacentPos = new Position(pos.X + offset.X, pos.Y + offset.Y);
                if (IsInBounds(adjacentPos))
                    adjacent.Add(adjacentPos);
            }

            return adjacent;
        }

        private static int GetBaseMovementCost(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Dirt => 100,
                TerrainType.Sand => 150,
                TerrainType.Grass => 100,
                TerrainType.Snow => 150,
                TerrainType.Swamp => 175,
                TerrainType.Rough => 125,
                TerrainType.Subterranean => 100,
                TerrainType.Lava => 100,
                TerrainType.Water => 100,
                TerrainType.Rock => int.MaxValue,
                TerrainType.Border => int.MaxValue,
                _ => 100
            };
        }

        public override string ToString()
        {
            return $"Map '{Name}' ({Width}x{Height}, {objects.Count} objects)";
        }
    }
}
