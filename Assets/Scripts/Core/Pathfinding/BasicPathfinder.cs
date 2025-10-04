using System.Collections.Generic;

namespace RealmsOfEldor.Core.Pathfinding
{
    /// <summary>
    /// Pathfinding implementation for hero movement.
    /// Tries to use A* Pathfinding Project if available, falls back to simple adjacent-only movement.
    /// </summary>
    public static class BasicPathfinder
    {
        /// <summary>
        /// Delegate for A* pathfinding (set by AstarPathfindingAdapter at runtime).
        /// </summary>
        public static System.Func<Position, Position, List<Position>> AstarFindPath;

        /// <summary>
        /// Delegate for A* reachable positions query.
        /// </summary>
        public static System.Func<Position, int, List<Position>> AstarGetReachable;

        /// <summary>
        /// Delegate for A* path cost calculation.
        /// </summary>
        public static System.Func<List<Position>, int> AstarCalculatePathCost;

        /// <summary>
        /// Finds a path from start to end position.
        /// Uses A* if available, otherwise falls back to adjacent-only movement.
        /// Returns null if no valid path exists.
        /// </summary>
        public static List<Position> FindPath(GameMap map, Position start, Position end)
        {
            if (map == null || !map.IsInBounds(start) || !map.IsInBounds(end))
                return null;

            // Same position
            if (start == end)
                return new List<Position> { start };

            // Try A* first if available
            if (AstarFindPath != null)
            {
                var astarPath = AstarFindPath(start, end);
                if (astarPath != null && astarPath.Count > 0)
                    return astarPath;
            }

            // Fallback to basic adjacent-only movement
            return FindPathBasic(map, start, end);
        }

        /// <summary>
        /// Basic pathfinding fallback (adjacent tiles only).
        /// </summary>
        private static List<Position> FindPathBasic(GameMap map, Position start, Position end)
        {
            // Only support adjacent tile movement
            if (!IsAdjacent(start, end))
                return null;

            // Check if target is passable
            if (!map.GetTile(end).IsPassable())
                return null;

            // Check if movement is valid
            if (!map.CanMoveBetween(start, end))
                return null;

            // Simple two-step path
            return new List<Position> { start, end };
        }

        /// <summary>
        /// Calculates movement cost for a path.
        /// Uses A* if available for accurate cost calculation.
        /// </summary>
        public static int CalculatePathCost(GameMap map, List<Position> path)
        {
            if (map == null || path == null || path.Count < 2)
                return 0;

            // Try A* cost calculation if available
            if (AstarCalculatePathCost != null)
            {
                var cost = AstarCalculatePathCost(path);
                if (cost >= 0)
                    return cost;
            }

            // Fallback to manual calculation
            var totalCost = 0;

            for (int i = 0; i < path.Count - 1; i++)
            {
                totalCost += map.GetMovementCost(path[i], path[i + 1]);
            }

            return totalCost;
        }

        /// <summary>
        /// Gets all reachable positions from start within given movement points.
        /// Uses A* for accurate reachability if available, otherwise returns adjacent tiles only.
        /// </summary>
        public static List<Position> GetReachablePositions(GameMap map, Position start, int movementPoints)
        {
            if (map == null || !map.IsInBounds(start) || movementPoints <= 0)
                return new List<Position>();

            // Try A* reachability query if available
            if (AstarGetReachable != null)
            {
                var reachable = AstarGetReachable(start, movementPoints);
                if (reachable != null)
                    return reachable;
            }

            // Fallback to basic adjacent tiles
            return GetReachablePositionsBasic(map, start, movementPoints);
        }

        /// <summary>
        /// Basic reachability fallback (adjacent tiles only).
        /// </summary>
        private static List<Position> GetReachablePositionsBasic(GameMap map, Position start, int movementPoints)
        {
            var reachable = new List<Position>();
            var adjacent = GetAdjacentPositions(start);

            foreach (var pos in adjacent)
            {
                if (!map.IsInBounds(pos))
                    continue;

                if (!map.GetTile(pos).IsPassable())
                    continue;

                var cost = map.GetMovementCost(start, pos);
                if (cost <= movementPoints)
                {
                    reachable.Add(pos);
                }
            }

            return reachable;
        }

        /// <summary>
        /// Checks if two positions are adjacent (8-directional).
        /// </summary>
        public static bool IsAdjacent(Position pos1, Position pos2)
        {
            var dx = System.Math.Abs(pos1.X - pos2.X);
            var dy = System.Math.Abs(pos1.Y - pos2.Y);
            return dx <= 1 && dy <= 1 && (dx + dy) > 0;
        }

        /// <summary>
        /// Gets all 8 adjacent positions (cardinal + diagonal).
        /// </summary>
        public static List<Position> GetAdjacentPositions(Position center)
        {
            return new List<Position>
            {
                new Position(center.X - 1, center.Y - 1), // NW
                new Position(center.X,     center.Y - 1), // N
                new Position(center.X + 1, center.Y - 1), // NE
                new Position(center.X - 1, center.Y),     // W
                new Position(center.X + 1, center.Y),     // E
                new Position(center.X - 1, center.Y + 1), // SW
                new Position(center.X,     center.Y + 1), // S
                new Position(center.X + 1, center.Y + 1)  // SE
            };
        }

        /// <summary>
        /// Validates if a hero can move to a target position.
        /// </summary>
        public static bool CanReachPosition(GameMap map, Hero hero, Position target)
        {
            if (map == null || hero == null || !map.IsInBounds(target))
                return false;

            if (hero.MovementPoints <= 0)
                return false;

            var path = FindPath(map, hero.Position, target);
            if (path == null)
                return false;

            var cost = CalculatePathCost(map, path);
            return cost <= hero.MovementPoints;
        }

        /// <summary>
        /// Gets the optimal next step toward target (for multi-turn movement).
        /// For MVP: Returns target if adjacent, otherwise null.
        /// TODO: Implement proper pathfinding for multi-step movement.
        /// </summary>
        public static Position? GetNextStep(GameMap map, Position start, Position target)
        {
            if (map == null || !map.IsInBounds(start) || !map.IsInBounds(target))
                return null;

            if (start == target)
                return null;

            if (IsAdjacent(start, target))
            {
                return map.GetTile(target).IsPassable() ? target : null;
            }

            // For MVP, no multi-step pathfinding
            return null;
        }

        /// <summary>
        /// Manhattan distance between two positions.
        /// </summary>
        public static int ManhattanDistance(Position pos1, Position pos2)
        {
            return System.Math.Abs(pos1.X - pos2.X) + System.Math.Abs(pos1.Y - pos2.Y);
        }

        /// <summary>
        /// Chebyshev distance (diagonal distance) between two positions.
        /// </summary>
        public static int ChebyshevDistance(Position pos1, Position pos2)
        {
            return System.Math.Max(System.Math.Abs(pos1.X - pos2.X), System.Math.Abs(pos1.Y - pos2.Y));
        }
    }
}
