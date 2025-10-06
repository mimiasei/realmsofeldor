using System.Collections.Generic;

namespace RealmsOfEldor.Core
{
    /// <summary>
    /// Represents a single map tile with terrain and object references.
    /// Based on VCMI's TerrainTile structure.
    /// </summary>
    public struct MapTile
    {
        // Terrain properties
        public TerrainType Terrain { get; private set; }
        public int VisualVariant { get; private set; }
        public int MovementCost { get; private set; }

        // Object references
        private List<int> visitableObjectIds;
        private List<int> blockingObjectIds;

        // Tile flags
        public bool IsCoastal { get; private set; }
        public bool HasFavorableWinds { get; private set; }

        public MapTile(TerrainType terrain, int visualVariant = 0, int movementCost = 100)
        {
            Terrain = terrain;
            VisualVariant = visualVariant;
            MovementCost = movementCost;
            visitableObjectIds = new List<int>();
            blockingObjectIds = new List<int>();
            IsCoastal = false;
            HasFavorableWinds = false;
        }

        // Passability checks
        public bool IsPassable()
        {
            return Terrain != TerrainType.Rock;
        }

        public bool IsClear()
        {
            return IsPassable() && (blockingObjectIds == null || blockingObjectIds.Count == 0);
        }

        public bool IsBlocked()
        {
            return !IsPassable() || (blockingObjectIds != null && blockingObjectIds.Count > 0);
        }

        public bool IsWater()
        {
            return Terrain == TerrainType.Water;
        }

        // Object management
        public void AddVisitableObject(int objectId)
        {
            if (visitableObjectIds == null)
                visitableObjectIds = new List<int>();

            if (!visitableObjectIds.Contains(objectId))
                visitableObjectIds.Add(objectId);
        }

        public void AddBlockingObject(int objectId)
        {
            if (blockingObjectIds == null)
                blockingObjectIds = new List<int>();

            if (!blockingObjectIds.Contains(objectId))
                blockingObjectIds.Add(objectId);
        }

        public void RemoveVisitableObject(int objectId)
        {
            visitableObjectIds?.Remove(objectId);
        }

        public void RemoveBlockingObject(int objectId)
        {
            blockingObjectIds?.Remove(objectId);
        }

        public IReadOnlyList<int> GetVisitableObjects()
        {
            return visitableObjectIds ?? new List<int>();
        }

        public IReadOnlyList<int> GetBlockingObjects()
        {
            return blockingObjectIds ?? new List<int>();
        }

        public bool HasVisitableObject(int objectId)
        {
            return visitableObjectIds?.Contains(objectId) ?? false;
        }

        public bool HasBlockingObject(int objectId)
        {
            return blockingObjectIds?.Contains(objectId) ?? false;
        }

        // Flags
        public void SetCoastal(bool coastal)
        {
            IsCoastal = coastal;
        }

        public void SetFavorableWinds(bool favorableWinds)
        {
            HasFavorableWinds = favorableWinds;
        }

        // Terrain modification
        public void SetTerrain(TerrainType terrain, int visualVariant = -1, int movementCost = -1)
        {
            Terrain = terrain;
            if (visualVariant >= 0)
                VisualVariant = visualVariant;
            if (movementCost >= 0)
                MovementCost = movementCost;
        }
    }
}
