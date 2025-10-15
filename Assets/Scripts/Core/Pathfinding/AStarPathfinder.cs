using System;
using System.Collections.Generic;

namespace RealmsOfEldor.Core
{
    /// <summary>
    /// A* pathfinding implementation for hero movement.
    /// Based on VCMI's CPathfinder approach with Dijkstra-style pathfinding.
    /// Supports 8-directional movement with terrain cost consideration.
    /// </summary>
    public static class AStarPathfinder
    {
        private class PathNode : IComparable<PathNode>
        {
            public Position Position { get; set; }
            public PathNode Parent { get; set; }
            public int GCost { get; set; }  // Cost from start
            public int HCost { get; set; }  // Heuristic cost to end
            public int FCost => GCost + HCost;

            public int CompareTo(PathNode other)
            {
                var result = FCost.CompareTo(other.FCost);
                if (result == 0)
                {
                    // Tie-breaker: prefer nodes closer to goal (better heuristic)
                    result = HCost.CompareTo(other.HCost);
                }
                return result;
            }
        }

        /// <summary>
        /// Finds the optimal path from start to end using A* algorithm.
        /// Returns list of positions from start to end (inclusive).
        /// Returns null if no path exists.
        /// </summary>
        public static List<Position> FindPath(GameMap map, Position start, Position end)
        {
            if (map == null || !map.IsInBounds(start) || !map.IsInBounds(end))
                return null;

            if (start.Equals(end))
                return new List<Position> { start };

            // Check if destination is passable
            if (!map.GetTile(end).IsPassable())
                return null;

            var openSet = new PriorityQueue<PathNode>();
            var closedSet = new HashSet<Position>();
            var allNodes = new Dictionary<Position, PathNode>();

            var startNode = new PathNode
            {
                Position = start,
                GCost = 0,
                HCost = ManhattanDistance(start, end)
            };

            openSet.Enqueue(startNode);
            allNodes[start] = startNode;

            while (openSet.Count > 0)
            {
                var current = openSet.Dequeue();

                if (current.Position.Equals(end))
                {
                    // Reconstruct path
                    return ReconstructPath(current);
                }

                closedSet.Add(current.Position);

                // Check all 8 neighbors (like VCMI)
                foreach (var neighborPos in GetNeighbors(current.Position))
                {
                    if (!map.IsInBounds(neighborPos) || closedSet.Contains(neighborPos))
                        continue;

                    var neighborTile = map.GetTile(neighborPos);
                    if (!neighborTile.IsPassable())
                        continue;

                    // Check if movement between tiles is valid
                    if (!map.CanMoveBetween(current.Position, neighborPos))
                        continue;

                    int movementCost = map.GetMovementCost(current.Position, neighborPos);
                    int tentativeGCost = current.GCost + movementCost;

                    if (!allNodes.ContainsKey(neighborPos))
                    {
                        var neighborNode = new PathNode
                        {
                            Position = neighborPos,
                            Parent = current,
                            GCost = tentativeGCost,
                            HCost = ManhattanDistance(neighborPos, end)
                        };

                        allNodes[neighborPos] = neighborNode;
                        openSet.Enqueue(neighborNode);
                    }
                    else
                    {
                        var neighborNode = allNodes[neighborPos];
                        if (tentativeGCost < neighborNode.GCost)
                        {
                            neighborNode.GCost = tentativeGCost;
                            neighborNode.Parent = current;

                            // Update priority in queue (re-add with new priority)
                            openSet.UpdatePriority(neighborNode);
                        }
                    }
                }
            }

            // No path found
            return null;
        }

        /// <summary>
        /// Calculates movement cost for a given path.
        /// </summary>
        public static int CalculatePathCost(GameMap map, List<Position> path)
        {
            if (map == null || path == null || path.Count < 2)
                return 0;

            var totalCost = 0;

            for (int i = 0; i < path.Count - 1; i++)
            {
                totalCost += map.GetMovementCost(path[i], path[i + 1]);
            }

            return totalCost;
        }

        /// <summary>
        /// Gets all reachable positions from start within given movement points.
        /// Uses Dijkstra's algorithm (A* without heuristic).
        /// </summary>
        public static List<Position> GetReachablePositions(GameMap map, Position start, int movementPoints)
        {
            if (map == null || !map.IsInBounds(start) || movementPoints <= 0)
                return new List<Position>();

            var reachable = new List<Position>();
            var openSet = new PriorityQueue<PathNode>();
            var visited = new Dictionary<Position, int>(); // Position -> cost to reach

            var startNode = new PathNode
            {
                Position = start,
                GCost = 0,
                HCost = 0
            };

            openSet.Enqueue(startNode);
            visited[start] = 0;

            while (openSet.Count > 0)
            {
                var current = openSet.Dequeue();

                // Add to reachable if within movement budget
                if (current.GCost <= movementPoints && !current.Position.Equals(start))
                {
                    reachable.Add(current.Position);
                }

                // Explore neighbors
                foreach (var neighborPos in GetNeighbors(current.Position))
                {
                    if (!map.IsInBounds(neighborPos))
                        continue;

                    var neighborTile = map.GetTile(neighborPos);
                    if (!neighborTile.IsPassable())
                        continue;

                    if (!map.CanMoveBetween(current.Position, neighborPos))
                        continue;

                    int movementCost = map.GetMovementCost(current.Position, neighborPos);
                    int tentativeGCost = current.GCost + movementCost;

                    // Only explore if within movement budget
                    if (tentativeGCost > movementPoints)
                        continue;

                    if (!visited.ContainsKey(neighborPos) || tentativeGCost < visited[neighborPos])
                    {
                        visited[neighborPos] = tentativeGCost;

                        var neighborNode = new PathNode
                        {
                            Position = neighborPos,
                            Parent = current,
                            GCost = tentativeGCost,
                            HCost = 0
                        };

                        openSet.Enqueue(neighborNode);
                    }
                }
            }

            return reachable;
        }

        /// <summary>
        /// Reconstructs path from end node by following parent pointers.
        /// Returns path from start to end.
        /// </summary>
        private static List<Position> ReconstructPath(PathNode endNode)
        {
            var path = new List<Position>();
            var current = endNode;

            while (current != null)
            {
                path.Add(current.Position);
                current = current.Parent;
            }

            path.Reverse();  // Start → End order
            return path;
        }

        /// <summary>
        /// Gets all 8 neighbors of a position (cardinal + diagonal).
        /// </summary>
        private static List<Position> GetNeighbors(Position pos)
        {
            return new List<Position>
            {
                new Position(pos.X - 1, pos.Y - 1), // NW
                new Position(pos.X,     pos.Y - 1), // N
                new Position(pos.X + 1, pos.Y - 1), // NE
                new Position(pos.X - 1, pos.Y),     // W
                new Position(pos.X + 1, pos.Y),     // E
                new Position(pos.X - 1, pos.Y + 1), // SW
                new Position(pos.X,     pos.Y + 1), // S
                new Position(pos.X + 1, pos.Y + 1)  // SE
            };
        }

        /// <summary>
        /// Manhattan distance heuristic (admissible for grid-based pathfinding).
        /// </summary>
        private static int ManhattanDistance(Position a, Position b)
        {
            return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        }

        /// <summary>
        /// Chebyshev distance (diagonal distance) - alternative heuristic.
        /// More accurate for 8-directional movement.
        /// </summary>
        private static int ChebyshevDistance(Position a, Position b)
        {
            return Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
        }
    }

    /// <summary>
    /// Simple priority queue implementation using binary heap.
    /// Used by A* pathfinding for efficient node selection.
    /// </summary>
    public class PriorityQueue<T> where T : IComparable<T>
    {
        private readonly List<T> data = new List<T>();
        private readonly HashSet<T> itemSet = new HashSet<T>();

        public int Count => data.Count;

        public void Enqueue(T item)
        {
            if (itemSet.Contains(item))
                return; // Already in queue

            data.Add(item);
            itemSet.Add(item);
            var childIndex = data.Count - 1;

            while (childIndex > 0)
            {
                var parentIndex = (childIndex - 1) / 2;

                if (data[childIndex].CompareTo(data[parentIndex]) >= 0)
                    break;

                Swap(childIndex, parentIndex);
                childIndex = parentIndex;
            }
        }

        public T Dequeue()
        {
            var lastIndex = data.Count - 1;
            var frontItem = data[0];

            itemSet.Remove(frontItem);
            data[0] = data[lastIndex];
            data.RemoveAt(lastIndex);

            lastIndex--;
            var parentIndex = 0;

            while (true)
            {
                var leftChild = parentIndex * 2 + 1;
                if (leftChild > lastIndex) break;

                var rightChild = leftChild + 1;
                var minChild = leftChild;

                if (rightChild <= lastIndex && data[rightChild].CompareTo(data[leftChild]) < 0)
                    minChild = rightChild;

                if (data[parentIndex].CompareTo(data[minChild]) <= 0)
                    break;

                Swap(parentIndex, minChild);
                parentIndex = minChild;
            }

            return frontItem;
        }

        public void UpdatePriority(T item)
        {
            // Simple implementation: remove and re-add
            // A proper implementation would use handles like VCMI's Fibonacci heap
            if (itemSet.Contains(item))
            {
                data.Remove(item);
                itemSet.Remove(item);
            }
            Enqueue(item);
        }

        private void Swap(int i, int j)
        {
            var tmp = data[i];
            data[i] = data[j];
            data[j] = tmp;
        }

        public void Clear()
        {
            data.Clear();
            itemSet.Clear();
        }
    }
}
