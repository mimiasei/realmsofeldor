using System;
using System.Collections.Generic;

namespace RealmsOfEldor.Core.Map
{
    /// <summary>
    /// Represents a single tile on the game map.
    /// Based on VCMI's TerrainTile structure (TerrainTile.h).
    /// Pure C# struct with no Unity dependencies.
    /// </summary>
    [Serializable]
    public struct MapTile
    {
        /// <summary>
        /// The terrain type of this tile (grass, dirt, water, etc.)
        /// </summary>
        public TerrainType Terrain;

        /// <summary>
        /// Visual variant index for texture variety (0-based)
        /// </summary>
        public byte VisualVariant;

        /// <summary>
        /// Movement cost to enter this tile (10 = normal, 15 = rough, etc.)
        /// Calculated based on terrain type and roads.
        /// </summary>
        public int MovementCost;

        /// <summary>
        /// IDs of objects that can be visited/interacted with on this tile.
        /// References MapObject instances in GameMap.
        /// </summary>
        public List<int> VisitableObjectIds;

        /// <summary>
        /// IDs of objects that block movement through this tile.
        /// References MapObject instances in GameMap.
        /// </summary>
        public List<int> BlockingObjectIds;

        /// <summary>
        /// Bit flags for tile properties (coastal, favorable winds, etc.)
        /// Based on VCMI's extTileFlags.
        /// </summary>
        public TileFlags Flags;

        /// <summary>
        /// Creates a new map tile with the specified terrain type.
        /// </summary>
        public MapTile(TerrainType terrain)
        {
            Terrain = terrain;
            VisualVariant = 0;
            MovementCost = GetBaseMovementCost(terrain);
            VisitableObjectIds = new List<int>();
            BlockingObjectIds = new List<int>();
            Flags = TileFlags.None;
        }

        /// <summary>
        /// Checks if the tile is passable (not rock/impassable terrain).
        /// Based on VCMI's TerrainTile::entrableTerrain.
        /// </summary>
        public bool IsPassable => Terrain != TerrainType.Rock && Terrain != TerrainType.Border;

        /// <summary>
        /// Checks if the tile is water terrain.
        /// </summary>
        public bool IsWater => Terrain == TerrainType.Water;

        /// <summary>
        /// Checks if the tile is land terrain (not water).
        /// </summary>
        public bool IsLand => IsPassable && !IsWater;

        /// <summary>
        /// Checks if the tile has any blocking objects.
        /// Based on VCMI's TerrainTile::blocked.
        /// </summary>
        public bool IsBlocked => BlockingObjectIds != null && BlockingObjectIds.Count > 0;

        /// <summary>
        /// Checks if the tile has any visitable objects.
        /// Based on VCMI's TerrainTile::visitable.
        /// </summary>
        public bool IsVisitable => VisitableObjectIds != null && VisitableObjectIds.Count > 0;

        /// <summary>
        /// Checks if the tile is clear for movement (passable and not blocked).
        /// Based on VCMI's TerrainTile::isClear.
        /// </summary>
        public bool IsClear => IsPassable && !IsBlocked;

        /// <summary>
        /// Checks if the tile is coastal (adjacent to water).
        /// </summary>
        public bool IsCoastal
        {
            get => (Flags & TileFlags.Coastal) != 0;
            set
            {
                if (value)
                    Flags |= TileFlags.Coastal;
                else
                    Flags &= ~TileFlags.Coastal;
            }
        }

        /// <summary>
        /// Gets the ID of the top visitable object on this tile, or -1 if none.
        /// Based on VCMI's TerrainTile::topVisitableObj.
        /// </summary>
        public int TopVisitableObjectId
        {
            get
            {
                if (VisitableObjectIds == null || VisitableObjectIds.Count == 0)
                    return -1;
                return VisitableObjectIds[^1]; // Last object is on top
            }
        }

        /// <summary>
        /// Adds a visitable object to this tile.
        /// </summary>
        public void AddVisitableObject(int objectId)
        {
            VisitableObjectIds ??= new List<int>();
            if (!VisitableObjectIds.Contains(objectId))
                VisitableObjectIds.Add(objectId);
        }

        /// <summary>
        /// Adds a blocking object to this tile.
        /// </summary>
        public void AddBlockingObject(int objectId)
        {
            BlockingObjectIds ??= new List<int>();
            if (!BlockingObjectIds.Contains(objectId))
                BlockingObjectIds.Add(objectId);
        }

        /// <summary>
        /// Removes a visitable object from this tile.
        /// </summary>
        public void RemoveVisitableObject(int objectId)
        {
            VisitableObjectIds?.Remove(objectId);
        }

        /// <summary>
        /// Removes a blocking object from this tile.
        /// </summary>
        public void RemoveBlockingObject(int objectId)
        {
            BlockingObjectIds?.Remove(objectId);
        }

        /// <summary>
        /// Gets base movement cost for a terrain type.
        /// Based on HOMM3 movement costs.
        /// </summary>
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
                TerrainType.Rock => int.MaxValue, // Impassable
                TerrainType.Border => int.MaxValue, // Impassable
                _ => 100
            };
        }

        public override string ToString()
        {
            return $"Tile({Terrain}, Cost:{MovementCost}, Blocked:{IsBlocked}, Visitable:{IsVisitable})";
        }
    }

    /// <summary>
    /// Bit flags for tile properties.
    /// Based on VCMI's TerrainTile::extTileFlags.
    /// </summary>
    [Flags]
    public enum TileFlags : byte
    {
        None = 0,
        Coastal = 1 << 0,           // Tile is adjacent to water (allows ship landing)
        FavorableWinds = 1 << 1,    // Spell effect active on tile
        // Additional flags can be added as needed
    }
}
