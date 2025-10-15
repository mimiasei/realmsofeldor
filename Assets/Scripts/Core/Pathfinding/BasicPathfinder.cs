using System.Collections.Generic;

namespace RealmsOfEldor.Core
{
    /// <summary>
    /// Pathfinding implementation for hero movement.
    /// Uses built-in A* pathfinding with optional external pathfinding support.
    /// Based on VCMI's CPathfinder approach.
    /// </summary>
    public static class BasicPathfinder
    {
        /// <summary>
        /// Delegate for external A* pathfinding (set by AstarPathfindingAdapter at runtime if using A* Pathfinding Project).
        /// If null, uses built-in A* implementation.
        /// </summary>
        public static System.Func<GameMap, Position, Position, List<Position>> AstarFindPath;

        /// <summary>
        /// Delegate for external A* reachable positions query.
        /// If null, uses built-in implementation.
        /// </summary>
        public static System.Func<GameMap, Position, int, List<Position>> AstarGetReachable;

        /// <summary>
        /// Delegate for external A* path cost calculation.
        /// If null, uses built-in implementation.
        /// </summary>
        public static System.Func<GameMap, List<Position>, int> AstarCalculatePathCost;

        /// <summary>
        /// Finds a path from start to end position.
        /// Uses external A* if available, otherwise uses built-in A* implementation.
        /// Returns null if no valid path exists.
        /// </summary>
        public static List<Position> FindPath(GameMap map, Position start, Position end)
        {
            if (map == null || !map.IsInBounds(start) || !map.IsInBounds(end))
                return null;

            // Same position
            if (start == end)
                return new List<Position> { start };

            // Try external A* first if available (A* Pathfinding Project integration)
            if (AstarFindPath != null)
            {
                var astarPath = AstarFindPath(map, start, end);
                if (astarPath != null && astarPath.Count > 0)
                    return astarPath;
            }

            // Use built-in A* pathfinding
            return AStarPathfinder.FindPath(map, start, end);
        }


        /// <summary>
        /// Calculates movement cost for a path.
        /// Uses external A* if available, otherwise uses built-in calculation.
        /// </summary>
        public static int CalculatePathCost(GameMap map, List<Position> path)
        {
            if (map == null || path == null || path.Count < 2)
                return 0;

            // Try external A* cost calculation if available
            if (AstarCalculatePathCost != null)
            {
                var cost = AstarCalculatePathCost(map, path);
                if (cost >= 0)
                    return cost;
            }

            // Use built-in cost calculation
            return AStarPathfinder.CalculatePathCost(map, path);
        }

        /// <summary>
        /// Gets all reachable positions from start within given movement points.
        /// Uses external A* if available, otherwise uses built-in Dijkstra implementation.
        /// </summary>
        public static List<Position> GetReachablePositions(GameMap map, Position start, int movementPoints)
        {
            if (map == null || !map.IsInBounds(start) || movementPoints <= 0)
                return new List<Position>();

            // Try external A* reachability query if available
            if (AstarGetReachable != null)
            {
                var reachable = AstarGetReachable(map, start, movementPoints);
                if (reachable != null)
                    return reachable;
            }

            // Use built-in reachability calculation
            return AStarPathfinder.GetReachablePositions(map, start, movementPoints);
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

            if (hero.Movement <= 0)
                return false;

            var path = FindPath(map, hero.Position, target);
            if (path == null)
                return false;

            var cost = CalculatePathCost(map, path);
            return cost <= hero.Movement;
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
