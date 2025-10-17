using System.Collections.Generic;
using System.Linq;

namespace RealmsOfEldor.Core
{
    /// <summary>
    /// Validates that all map objects are reachable via pathfinding.
    /// Based on VCMI's RockFiller reachability validation.
    /// Uses flood-fill algorithm to identify unreachable areas.
    /// </summary>
    public class MapReachabilityValidator
    {
        private readonly GameMap _map;
        private HashSet<Position> _reachableTiles;

        public MapReachabilityValidator(GameMap map)
        {
            _map = map;
            _reachableTiles = new HashSet<Position>();
        }

        /// <summary>
        /// Performs flood-fill from start positions to find all reachable tiles.
        /// Based on VCMI's RockFiller::run() pattern.
        /// </summary>
        /// <param name="startPositions">Starting positions (usually hero spawn points)</param>
        /// <returns>Set of all reachable positions</returns>
        public HashSet<Position> FindReachableTiles(IEnumerable<Position> startPositions)
        {
            _reachableTiles.Clear();
            var queue = new Queue<Position>();

            // Initialize queue with all start positions
            foreach (var startPos in startPositions)
            {
                if (_map.IsInBounds(startPos) && _map.IsPassable(startPos))
                {
                    queue.Enqueue(startPos);
                    _reachableTiles.Add(startPos);
                }
            }

            // Flood-fill algorithm
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                // Check all 8 adjacent positions (cardinal + diagonal)
                for (var dx = -1; dx <= 1; dx++)
                {
                    for (var dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0)
                            continue;

                        var neighbor = new Position(current.X + dx, current.Y + dy);

                        // Skip if already visited
                        if (_reachableTiles.Contains(neighbor))
                            continue;

                        // Skip if out of bounds
                        if (!_map.IsInBounds(neighbor))
                            continue;

                        // Skip if impassable
                        if (!_map.IsPassable(neighbor))
                            continue;

                        // Add to reachable set and queue
                        _reachableTiles.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return _reachableTiles;
        }

        /// <summary>
        /// Finds all objects that are unreachable from start positions.
        /// </summary>
        /// <param name="startPositions">Starting positions (usually hero spawn points)</param>
        /// <returns>List of unreachable objects</returns>
        public List<MapObject> FindUnreachableObjects(IEnumerable<Position> startPositions)
        {
            var reachableTiles = FindReachableTiles(startPositions);
            var unreachableObjects = new List<MapObject>();

            foreach (var obj in _map.GetAllObjects())
            {
                // Check if object position is reachable
                if (!reachableTiles.Contains(obj.Position))
                {
                    unreachableObjects.Add(obj);
                    continue;
                }

                // For visitable objects, check if any visitable position is reachable
                if (obj.IsVisitable)
                {
                    var visitablePositions = obj.GetVisitablePositions();
                    var hasReachableVisitPosition = visitablePositions.Any(pos => reachableTiles.Contains(pos));

                    if (!hasReachableVisitPosition)
                    {
                        unreachableObjects.Add(obj);
                    }
                }
            }

            return unreachableObjects;
        }

        /// <summary>
        /// Removes all unreachable objects from the map.
        /// Based on VCMI's RockFiller approach to clean up inaccessible objects.
        /// </summary>
        /// <param name="startPositions">Starting positions (usually hero spawn points)</param>
        /// <returns>Number of objects removed</returns>
        public int RemoveUnreachableObjects(IEnumerable<Position> startPositions)
        {
            var unreachableObjects = FindUnreachableObjects(startPositions);

            foreach (var obj in unreachableObjects)
            {
                _map.RemoveObject(obj.InstanceId);
            }

            return unreachableObjects.Count;
        }

        /// <summary>
        /// Attempts to relocate unreachable objects to nearby reachable positions.
        /// More player-friendly than removing objects entirely.
        /// </summary>
        /// <param name="startPositions">Starting positions (usually hero spawn points)</param>
        /// <param name="maxSearchRadius">Maximum distance to search for relocation spot</param>
        /// <returns>Statistics about relocated/removed objects</returns>
        public ReachabilityResult FixUnreachableObjects(IEnumerable<Position> startPositions, int maxSearchRadius = 5)
        {
            var reachableTiles = FindReachableTiles(startPositions);
            var unreachableObjects = FindUnreachableObjects(startPositions);

            var result = new ReachabilityResult
            {
                TotalReachableTiles = reachableTiles.Count,
                TotalUnreachableObjects = unreachableObjects.Count
            };

            foreach (var obj in unreachableObjects)
            {
                // Try to find a nearby reachable position
                var newPosition = FindNearestReachablePosition(obj.Position, reachableTiles, maxSearchRadius);

                if (newPosition.HasValue && _map.IsClear(newPosition.Value))
                {
                    // Relocate object
                    _map.RemoveObject(obj.InstanceId);
                    obj.Position = newPosition.Value;
                    _map.AddObject(obj);
                    result.ObjectsRelocated++;
                }
                else
                {
                    // Cannot relocate, remove object
                    _map.RemoveObject(obj.InstanceId);
                    result.ObjectsRemoved++;
                }
            }

            return result;
        }

        /// <summary>
        /// Finds the nearest reachable position to a target position.
        /// Uses expanding circle search pattern.
        /// </summary>
        private Position? FindNearestReachablePosition(Position target, HashSet<Position> reachableTiles, int maxRadius)
        {
            for (var radius = 1; radius <= maxRadius; radius++)
            {
                // Search in expanding circle
                for (var dx = -radius; dx <= radius; dx++)
                {
                    for (var dy = -radius; dy <= radius; dy++)
                    {
                        // Skip if not on current radius boundary (check diamond pattern)
                        if (System.Math.Abs(dx) + System.Math.Abs(dy) != radius)
                            continue;

                        var candidate = new Position(target.X + dx, target.Y + dy);

                        if (reachableTiles.Contains(candidate) && _map.IsClear(candidate))
                        {
                            return candidate;
                        }
                    }
                }
            }

            return null; // No reachable position found
        }

        /// <summary>
        /// Calculates reachability statistics for the map.
        /// Useful for debugging and map generation quality metrics.
        /// </summary>
        public ReachabilityStats CalculateStats(IEnumerable<Position> startPositions)
        {
            var reachableTiles = FindReachableTiles(startPositions);
            var unreachableObjects = FindUnreachableObjects(startPositions);

            var totalPassableTiles = 0;
            for (var y = 0; y < _map.Height; y++)
            {
                for (var x = 0; x < _map.Width; x++)
                {
                    if (_map.IsPassable(new Position(x, y)))
                        totalPassableTiles++;
                }
            }

            return new ReachabilityStats
            {
                TotalTiles = _map.Width * _map.Height,
                PassableTiles = totalPassableTiles,
                ReachableTiles = reachableTiles.Count,
                UnreachableTiles = totalPassableTiles - reachableTiles.Count,
                TotalObjects = _map.GetAllObjects().Count(),
                UnreachableObjects = unreachableObjects.Count,
                ReachabilityPercentage = totalPassableTiles > 0
                    ? (float)reachableTiles.Count / totalPassableTiles
                    : 0f
            };
        }
    }

    /// <summary>
    /// Result of reachability fix operation.
    /// </summary>
    public class ReachabilityResult
    {
        public int TotalReachableTiles { get; set; }
        public int TotalUnreachableObjects { get; set; }
        public int ObjectsRelocated { get; set; }
        public int ObjectsRemoved { get; set; }

        public override string ToString()
        {
            return $"Reachability Fix Results:\n" +
                   $"  Reachable Tiles: {TotalReachableTiles}\n" +
                   $"  Unreachable Objects Found: {TotalUnreachableObjects}\n" +
                   $"  Objects Relocated: {ObjectsRelocated}\n" +
                   $"  Objects Removed: {ObjectsRemoved}";
        }
    }

    /// <summary>
    /// Statistics about map reachability.
    /// </summary>
    public class ReachabilityStats
    {
        public int TotalTiles { get; set; }
        public int PassableTiles { get; set; }
        public int ReachableTiles { get; set; }
        public int UnreachableTiles { get; set; }
        public int TotalObjects { get; set; }
        public int UnreachableObjects { get; set; }
        public float ReachabilityPercentage { get; set; }

        public override string ToString()
        {
            return $"Map Reachability Stats:\n" +
                   $"  Total Tiles: {TotalTiles}\n" +
                   $"  Passable Tiles: {PassableTiles}\n" +
                   $"  Reachable Tiles: {ReachableTiles} ({ReachabilityPercentage:P0})\n" +
                   $"  Unreachable Tiles: {UnreachableTiles}\n" +
                   $"  Total Objects: {TotalObjects}\n" +
                   $"  Unreachable Objects: {UnreachableObjects}";
        }
    }
}
