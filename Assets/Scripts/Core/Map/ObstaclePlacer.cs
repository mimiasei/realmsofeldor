using System;
using System.Collections.Generic;
using System.Linq;

namespace RealmsOfEldor.Core
{
    /// <summary>
    /// Places decorative and blocking obstacles on the map.
    /// Based on VCMI's ObstaclePlacer modificator pattern.
    /// Ensures obstacles don't block critical paths or cluster with other objects.
    /// </summary>
    public class ObstaclePlacer
    {
        private readonly GameMap map;
        private readonly Random random;
        private readonly HashSet<Position> prohibitedArea;
        private readonly HashSet<Position> blockedArea;

        public ObstaclePlacer(GameMap map, Random random = null)
        {
            this.map = map ?? throw new ArgumentNullException(nameof(map));
            this.random = random ?? new Random();
            this.prohibitedArea = new HashSet<Position>();
            this.blockedArea = new HashSet<Position>();
        }

        /// <summary>
        /// Places obstacles on the map using density-based placement.
        /// VCMI Reference: ObstaclePlacer::process() and ObstacleProxy::createObstacles()
        /// </summary>
        /// <param name="obstacleTypes">Types of obstacles to place (e.g., Tree, Rock, Bush)</param>
        /// <param name="targetDensity">Target obstacle count (NOT tiles to fill, but number of obstacles)</param>
        /// <param name="allowBlocking">If true, places blocking obstacles; if false, only decorative</param>
        /// <returns>List of placed obstacle objects</returns>
        public List<MapObject> PlaceObstacles(
            List<ObstacleType> obstacleTypes,
            int targetDensity,
            bool allowBlocking = true)
        {
            if (obstacleTypes == null || obstacleTypes.Count == 0)
                throw new ArgumentException("Must provide at least one obstacle type", nameof(obstacleTypes));

            var placedObstacles = new List<MapObject>();

            // Build prohibited area from existing objects + paths
            BuildProhibitedArea();

            // Build candidate tiles for obstacle placement
            var candidateTiles = BuildCandidateTiles();

            if (candidateTiles.Count == 0)
                return placedObstacles; // No space for obstacles

            // Place obstacles until target density reached
            int obstaclesPlaced = 0;
            int maxAttempts = targetDensity * 10; // Prevent infinite loops
            int attempts = 0;

            while (obstaclesPlaced < targetDensity && attempts < maxAttempts)
            {
                attempts++;

                // Pick random tile from candidates
                if (candidateTiles.Count == 0)
                    break; // No more space

                var tileIndex = random.Next(candidateTiles.Count);
                var tile = candidateTiles[tileIndex];

                // Pick random obstacle type
                var obstacleType = obstacleTypes[random.Next(obstacleTypes.Count)];

                // Decide if this obstacle should block based on configuration
                bool isBlocking = allowBlocking && ShouldBeBlocking(obstacleType);

                // Create obstacle
                var obstacle = CreateObstacle(tile, obstacleType, isBlocking);

                // Verify placement is valid
                if (IsValidPlacement(obstacle, tile))
                {
                    // Add to map
                    map.AddObject(obstacle);
                    placedObstacles.Add(obstacle);

                    // Mark tile as occupied (and neighbors if blocking)
                    blockedArea.Add(tile);
                    if (isBlocking)
                    {
                        // Remove this tile and adjacent tiles from candidates
                        RemoveAdjacentTiles(candidateTiles, tile);
                    }
                    else
                    {
                        // Just remove this tile
                        candidateTiles.Remove(tile);
                    }

                    obstaclesPlaced++;
                }
                else
                {
                    // Invalid placement, remove from candidates
                    candidateTiles.Remove(tile);
                }
            }

            return placedObstacles;
        }

        /// <summary>
        /// Builds the prohibited area where obstacles cannot be placed.
        /// Includes: existing objects, visitable areas, and paths.
        /// VCMI Reference: ObstaclePlacer::process() lines 69-72
        /// </summary>
        private void BuildProhibitedArea()
        {
            prohibitedArea.Clear();

            // Add all existing object positions
            var objects = map.GetAllObjects();
            foreach (var obj in objects)
            {
                // Block object's position
                prohibitedArea.Add(obj.Position);

                // Block all positions blocked by object
                foreach (var blockedPos in obj.GetBlockedPositions())
                    prohibitedArea.Add(blockedPos);

                // Block visitable positions (give objects breathing room)
                foreach (var visitablePos in obj.GetVisitablePositions())
                    prohibitedArea.Add(visitablePos);

                // Extra buffer for high-value objects (1-tile margin)
                if (obj.Value > 1000 || obj.IsGuarded())
                {
                    AddBufferAround(obj.Position, 1);
                }
            }
        }

        /// <summary>
        /// Adds a buffer of N tiles around a position to prohibited area.
        /// </summary>
        private void AddBufferAround(Position center, int radius)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    var pos = new Position(center.X + dx, center.Y + dy);
                    if (map.IsInBounds(pos))
                        prohibitedArea.Add(pos);
                }
            }
        }

        /// <summary>
        /// Builds list of candidate tiles for obstacle placement.
        /// Filters out prohibited areas and impassable terrain.
        /// </summary>
        private List<Position> BuildCandidateTiles()
        {
            var candidates = new List<Position>();

            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    var pos = new Position(x, y);

                    // Skip if in prohibited area
                    if (prohibitedArea.Contains(pos))
                        continue;

                    // Skip if already has objects
                    if (map.GetObjectsAt(pos) != null)
                        continue;

                    // Skip if impassable terrain (water, rock)
                    var tile = map.GetTile(pos);
                    if (!tile.IsPassable())
                        continue;

                    candidates.Add(pos);
                }
            }

            return candidates;
        }

        /// <summary>
        /// Determines if an obstacle type should be blocking based on its nature.
        /// Mountains, rocks, large obstacles = blocking
        /// Trees, bushes, flowers = decorative (non-blocking)
        /// </summary>
        private bool ShouldBeBlocking(ObstacleType obstacleType)
        {
            switch (obstacleType)
            {
                case ObstacleType.Mountain:
                case ObstacleType.Rock:
                case ObstacleType.Boulder:
                    return random.NextDouble() > 0.3; // 70% blocking

                case ObstacleType.Tree:
                    return random.NextDouble() > 0.7; // 30% blocking (dense forests)

                case ObstacleType.Bush:
                case ObstacleType.Flowers:
                case ObstacleType.Grass:
                    return false; // Always non-blocking

                default:
                    return random.NextDouble() > 0.5; // 50/50 for others
            }
        }

        /// <summary>
        /// Creates an obstacle MapObject of the specified type.
        /// </summary>
        private MapObject CreateObstacle(Position position, ObstacleType obstacleType, bool isBlocking)
        {
            var mapObjectType = ObstacleTypeToMapObjectType(obstacleType);
            var obstacle = new MapObject(mapObjectType, position)
            {
                Name = $"{obstacleType}",
                IsBlocking = isBlocking,
                IsVisitable = false,
                IsRemovable = true // Obstacles can be removed by map editor
            };

            return obstacle;
        }

        /// <summary>
        /// Maps ObstacleType enum to MapObjectType.
        /// All obstacles use Decorative type, differentiated by name.
        /// </summary>
        private MapObjectType ObstacleTypeToMapObjectType(ObstacleType obstacleType)
        {
            // For now, all obstacles are "Decorative" type
            // Later we could have specific types for Mountains, Trees, etc.
            return MapObjectType.Decorative;
        }

        /// <summary>
        /// Validates that an obstacle can be placed at the given position.
        /// Checks for blocking paths and connectivity.
        /// VCMI Reference: ObstacleProxy::isProhibited() and verifyCoverage()
        /// </summary>
        private bool IsValidPlacement(MapObject obstacle, Position position)
        {
            // Already checked in candidate building, but double-check
            if (prohibitedArea.Contains(position))
                return false;

            // If obstacle is blocking, ensure it doesn't seal gaps
            if (obstacle.IsBlocking)
            {
                // Check if placing this obstacle would create unreachable areas
                // Simple heuristic: count passable neighbors
                int passableNeighbors = CountPassableNeighbors(position);

                // If tile has only 1-2 passable neighbors, it might create a chokepoint
                // VCMI uses more sophisticated connected-components analysis
                if (passableNeighbors <= 2)
                    return false; // Would likely block path
            }

            return true;
        }

        /// <summary>
        /// Counts how many of the 8 neighbors are passable.
        /// </summary>
        private int CountPassableNeighbors(Position position)
        {
            int count = 0;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    var neighbor = new Position(position.X + dx, position.Y + dy);
                    if (!map.IsInBounds(neighbor))
                        continue;

                    if (map.IsClear(neighbor))
                        count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Removes a tile and all adjacent tiles from the candidate list.
        /// Used when placing blocking obstacles to create spacing.
        /// </summary>
        private void RemoveAdjacentTiles(List<Position> candidates, Position center)
        {
            candidates.Remove(center);

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    var neighbor = new Position(center.X + dx, center.Y + dy);
                    candidates.Remove(neighbor);
                }
            }
        }

        /// <summary>
        /// Gets statistics about obstacle placement for debugging.
        /// </summary>
        public PlacementStats GetStats()
        {
            return new PlacementStats
            {
                ProhibitedTiles = prohibitedArea.Count,
                BlockedTiles = blockedArea.Count,
                TotalMapTiles = map.Width * map.Height
            };
        }
    }

    /// <summary>
    /// Types of obstacles that can be placed.
    /// Based on VCMI's obstacle classification system.
    /// </summary>
    public enum ObstacleType
    {
        // Blocking obstacles (usually impassable)
        Mountain,
        Rock,
        Boulder,

        // Natural decorations
        Tree,
        Bush,
        Flowers,
        Grass,
        Mushrooms,

        // Structures
        Statue,
        Ruins,
        Fence,
        Well,

        // Water features (if on land)
        Lake,
        Pond
    }

    /// <summary>
    /// Statistics about obstacle placement for debugging.
    /// </summary>
    public class PlacementStats
    {
        public int ProhibitedTiles { get; set; }
        public int BlockedTiles { get; set; }
        public int TotalMapTiles { get; set; }

        public float ProhibitedPercent => (float)ProhibitedTiles / TotalMapTiles * 100f;
        public float BlockedPercent => (float)BlockedTiles / TotalMapTiles * 100f;

        public override string ToString()
        {
            return $"Obstacle Placement Stats:\n" +
                   $"  Total Tiles: {TotalMapTiles}\n" +
                   $"  Prohibited: {ProhibitedTiles} ({ProhibitedPercent:F1}%)\n" +
                   $"  Blocked by Obstacles: {BlockedTiles} ({BlockedPercent:F1}%)";
        }
    }
}
