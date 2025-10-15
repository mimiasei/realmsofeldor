#if ASTAR_EXISTS
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using RealmsOfEldor.Core;
using RealmsOfEldor.Utilities;

namespace RealmsOfEldor.Controllers
{
    /// <summary>
    /// Adapter for A* Pathfinding Project integration with Realms of Eldor.
    /// Converts between our Position system and A* GridGraph nodes.
    /// Handles pathfinding requests and returns results in our core types.
    /// </summary>
    public class AstarPathfindingAdapter : MonoBehaviour
    {
        [Header("A* Configuration")]
        [SerializeField] private AstarPath astarPath;
        [SerializeField] private GridGraph gridGraph;

        [Header("Map Configuration")]
        [SerializeField] private float nodeSize = 1f;
        [SerializeField] private Vector3 mapOrigin = Vector3.zero;

        private static AstarPathfindingAdapter instance;
        public static AstarPathfindingAdapter Instance => instance;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Get A* instance if not set
            if (astarPath == null)
                astarPath = AstarPath.active;

            // Register pathfinding delegates with BasicPathfinder
            RegisterDelegates();
        }

        void OnDestroy()
        {
            if (instance == this)
            {
                // Unregister delegates
                UnregisterDelegates();
                instance = null;
            }
        }

        /// <summary>
        /// Registers A* pathfinding delegates with BasicPathfinder.
        /// </summary>
        private void RegisterDelegates()
        {
            BasicPathfinder.AstarFindPath = FindPath;
            BasicPathfinder.AstarGetReachable = GetReachablePositions;
            BasicPathfinder.AstarCalculatePathCost = CalculatePathCost;
        }

        /// <summary>
        /// Unregisters A* pathfinding delegates.
        /// </summary>
        private void UnregisterDelegates()
        {
            BasicPathfinder.AstarFindPath = null;
            BasicPathfinder.AstarGetReachable = null;
            BasicPathfinder.AstarCalculatePathCost = null;
        }

        /// <summary>
        /// Initializes the grid graph from GameMap dimensions.
        /// Call this when loading a new map.
        /// </summary>
        public void InitializeGrid(GameMap map)
        {
            if (astarPath == null)
            {
                Debug.LogError("A* Pathfinding not initialized!");
                return;
            }

            // Get or create grid graph
            if (gridGraph == null)
            {
                gridGraph = astarPath.data.gridGraph;
                if (gridGraph == null)
                {
                    Debug.LogError("No GridGraph found in A* configuration!");
                    return;
                }
            }

            // Configure grid dimensions
            gridGraph.SetDimensions(map.Width, map.Height, nodeSize);
            gridGraph.center = mapOrigin + new Vector3(map.Width * nodeSize / 2f, 0, map.Height * nodeSize / 2f);

            // Scan the graph
            astarPath.Scan(gridGraph);

            // Update node walkability based on map tiles
            UpdateWalkability(map);
        }

        /// <summary>
        /// Updates node walkability from GameMap tiles.
        /// </summary>
        public void UpdateWalkability(GameMap map)
        {
            if (gridGraph == null)
                return;

            gridGraph.GetNodes(node =>
            {
                var gridNode = node as GridNode;
                if (gridNode == null) return true;

                // Convert node index to Position
                var pos = GridNodeToPosition(gridNode);

                if (!map.IsInBounds(pos))
                {
                    gridNode.Walkable = false;
                    return true;
                }

                var tile = map.GetTile(pos);
                gridNode.Walkable = tile.IsPassable();

                // Set penalty based on terrain movement cost
                gridNode.Penalty = (uint)(tile.MovementCost * 100);

                return true;
            });
        }

        /// <summary>
        /// Updates a single node's walkability when a tile changes.
        /// </summary>
        public void UpdateNodeWalkability(Position pos, bool walkable, int movementCost)
        {
            var node = PositionToGridNode(pos);
            if (node != null)
            {
                node.Walkable = walkable;
                node.Penalty = (uint)(movementCost * 100);
            }
        }

        /// <summary>
        /// Finds a path from start to end using A* algorithm.
        /// Returns list of positions, or null if no path found.
        /// </summary>
        public List<Position> FindPath(Position start, Position end)
        {
            if (gridGraph == null)
            {
                Debug.LogWarning("GridGraph not initialized, using fallback pathfinding");
                return null;
            }

            var startNode = PositionToGridNode(start);
            var endNode = PositionToGridNode(end);

            if (startNode == null || endNode == null)
                return null;

            if (!startNode.Walkable || !endNode.Walkable)
                return null;

            // Calculate path using A*
            var path = PathUtilities.BFS(startNode, endNode);

            if (path == null || path.Count == 0)
                return null;

            // Convert nodes to positions
            var positions = new List<Position>();
            foreach (GraphNode node in path)
            {
                var gridNode = node as GridNode;
                if (gridNode != null)
                {
                    positions.Add(GridNodeToPosition(gridNode));
                }
            }

            return positions;
        }

        /// <summary>
        /// Gets all reachable positions within movement range.
        /// </summary>
        public List<Position> GetReachablePositions(Position start, int maxMovementCost)
        {
            if (gridGraph == null)
                return new List<Position>();

            var startNode = PositionToGridNode(start);
            if (startNode == null || !startNode.Walkable)
                return new List<Position>();

            var reachable = new List<Position>();
            var visited = new HashSet<GraphNode>();
            var queue = new Queue<(GraphNode node, int cost)>();

            queue.Enqueue((startNode, 0));
            visited.Add(startNode);

            while (queue.Count > 0)
            {
                var (currentNode, currentCost) = queue.Dequeue();
                var gridNode = currentNode as GridNode;

                if (gridNode == null) continue;

                // Add to reachable (except start position)
                var pos = GridNodeToPosition(gridNode);
                if (pos != start)
                    reachable.Add(pos);

                // Explore neighbors
                var connections = currentNode.connections;
                if (connections != null)
                {
                    foreach (var connection in connections)
                    {
                        if (visited.Contains(connection.node))
                            continue;

                        var neighborCost = currentCost + (int)connection.cost / 100; // Convert penalty back to movement cost

                        if (neighborCost <= maxMovementCost)
                        {
                            var neighborGridNode = connection.node as GridNode;
                            if (neighborGridNode != null && neighborGridNode.Walkable)
                            {
                                queue.Enqueue((connection.node, neighborCost));
                                visited.Add(connection.node);
                            }
                        }
                    }
                }
            }

            return reachable;
        }

        /// <summary>
        /// Calculates the movement cost for a path.
        /// </summary>
        public int CalculatePathCost(List<Position> path)
        {
            if (path == null || path.Count < 2)
                return 0;

            int totalCost = 0;

            for (int i = 0; i < path.Count - 1; i++)
            {
                var node = PositionToGridNode(path[i + 1]);
                if (node != null)
                {
                    totalCost += (int)node.Penalty / 100;
                }
            }

            return totalCost;
        }

        // ===== Conversion Methods =====

        /// <summary>
        /// Converts Position to GridNode.
        /// </summary>
        private GridNode PositionToGridNode(Position pos)
        {
            if (gridGraph == null)
                return null;

            var worldPos = PositionToWorldPoint(pos);
            var nearestNode = gridGraph.GetNearest(worldPos);

            return nearestNode.node as GridNode;
        }

        /// <summary>
        /// Converts GridNode to Position.
        /// </summary>
        private Position GridNodeToPosition(GridNode node)
        {
            var worldPos = (Vector3)node.position;
            return WorldPointToPosition(worldPos);
        }

        /// <summary>
        /// Converts Position to world space Vector3.
        /// </summary>
        private Vector3 PositionToWorldPoint(Position pos)
        {
            return mapOrigin + new Vector3(pos.X * nodeSize, 0, pos.Y * nodeSize);
        }

        /// <summary>
        /// Converts world space Vector3 to Position.
        /// </summary>
        private Position WorldPointToPosition(Vector3 worldPos)
        {
            var localPos = worldPos - mapOrigin;
            return new Position(
                Mathf.RoundToInt(localPos.x / nodeSize),
                Mathf.RoundToInt(localPos.z / nodeSize)
            );
        }

        /// <summary>
        /// Checks if A* is available and initialized.
        /// </summary>
        public bool IsAvailable()
        {
            return astarPath != null && gridGraph != null;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (gridGraph == null) return;

            // Draw grid bounds
            Gizmos.color = Color.cyan;
            var center = gridGraph.center;
            var size = new Vector3(gridGraph.width * nodeSize, 0.1f, gridGraph.depth * nodeSize);
            Gizmos.DrawWireCube(center, size);
        }
#endif
    }
}
#endif
