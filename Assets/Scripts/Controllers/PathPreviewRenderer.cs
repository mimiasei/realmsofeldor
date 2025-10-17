using System.Collections.Generic;
using UnityEngine;
using RealmsOfEldor.Core;
using RealmsOfEldor.Utilities;

namespace RealmsOfEldor.Controllers
{
    /// <summary>
    /// Renders path preview line on the adventure map when player clicks a destination.
    /// Shows a line from hero through each tile of the A* path to destination.
    /// Line follows exact pathfinding route, avoiding obstacles and high-cost terrain.
    /// Splits into green (reachable) and red (unreachable) sections based on available movement.
    /// </summary>
    public class PathPreviewRenderer : MonoBehaviour
    {
        [Header("Line Settings")]
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private LineRenderer unreachableLineRenderer; // Red portion
        [SerializeField] private float lineWidth = 0.1f;
        [SerializeField] private float lineZPosition = 0f; // Z position for line (above terrain)

        [Header("Visual Settings")]
        [SerializeField] private Color reachablePathColor = new Color(0.3f, 1f, 0.3f, 0.8f); // Green
        [SerializeField] private Color unreachablePathColor = new Color(1f, 0.3f, 0.3f, 0.8f); // Red

        private List<Position> currentPath;
        private int currentMovementCost;
        private int reachablePathIndex; // Last index reachable with available movement

        void Awake()
        {
            // Create LineRenderer for reachable portion if not assigned
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            // Configure reachable LineRenderer (green)
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.positionCount = 0;
            lineRenderer.useWorldSpace = true;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.sortingOrder = 20; // Above terrain and objects
            lineRenderer.enabled = false;

            // Create LineRenderer for unreachable portion (red)
            if (unreachableLineRenderer == null)
            {
                var unreachableGO = new GameObject("UnreachablePathLine");
                unreachableGO.transform.SetParent(transform);
                unreachableLineRenderer = unreachableGO.AddComponent<LineRenderer>();
            }

            // Configure unreachable LineRenderer (red)
            unreachableLineRenderer.startWidth = lineWidth;
            unreachableLineRenderer.endWidth = lineWidth;
            unreachableLineRenderer.positionCount = 0;
            unreachableLineRenderer.useWorldSpace = true;
            unreachableLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            unreachableLineRenderer.sortingOrder = 20;
            unreachableLineRenderer.enabled = false;
        }

        /// <summary>
        /// Shows path preview line through each tile in the path.
        /// Calculates reachable portion and splits line into green (reachable) and red (unreachable).
        /// </summary>
        public void ShowPath(List<Position> path, int movementCost, int availableMovement)
        {
            ShowPath(path, null, movementCost, availableMovement);
        }

        /// <summary>
        /// Shows path preview line with explicit per-step costs.
        /// </summary>
        public void ShowPath(List<Position> path, List<int> pathStepCosts, int totalMovementCost, int availableMovement)
        {
            if (path == null || path.Count < 2)
            {
                ClearPath();
                return;
            }

            currentPath = new List<Position>(path);
            currentMovementCost = totalMovementCost;

            // Calculate reachable portion of path
            reachablePathIndex = CalculateReachableIndex(path, pathStepCosts, availableMovement);

            // If entire path is reachable, show only green line
            if (reachablePathIndex >= path.Count - 1)
            {
                RenderSingleLine(lineRenderer, path, 0, path.Count, reachablePathColor);
                unreachableLineRenderer.enabled = false;
            }
            // If at least part of path is reachable, show green + red split
            else if (reachablePathIndex > 0)
            {
                RenderSingleLine(lineRenderer, path, 0, reachablePathIndex + 1, reachablePathColor);
                RenderSingleLine(unreachableLineRenderer, path, reachablePathIndex, path.Count, unreachablePathColor);
            }
            // If nothing is reachable (edge case), show only red
            else
            {
                lineRenderer.enabled = false;
                RenderSingleLine(unreachableLineRenderer, path, 0, path.Count, unreachablePathColor);
            }

            Debug.Log($"PathPreviewRenderer: Showing path with {path.Count} waypoints, reachable={reachablePathIndex + 1}, cost={totalMovementCost}, available={availableMovement}");
        }

        /// <summary>
        /// Calculates the last index in path reachable with available movement.
        /// </summary>
        private int CalculateReachableIndex(List<Position> path, List<int> pathStepCosts, int availableMovement)
        {
            if (path == null || path.Count < 2)
                return -1;

            // If step costs provided, use them
            if (pathStepCosts != null && pathStepCosts.Count == path.Count - 1)
            {
                var accumulatedCost = 0;
                for (int i = 0; i < pathStepCosts.Count; i++)
                {
                    accumulatedCost += pathStepCosts[i];
                    if (accumulatedCost > availableMovement)
                        return i; // Return last reachable index
                }
                return path.Count - 1; // Entire path reachable
            }

            // Otherwise, assume uniform cost per tile (fallback)
            var avgCostPerStep = path.Count > 1 ? currentMovementCost / (path.Count - 1) : 100;
            var accumulatedCostEstimate = 0;
            for (int i = 1; i < path.Count; i++)
            {
                accumulatedCostEstimate += avgCostPerStep;
                if (accumulatedCostEstimate > availableMovement)
                    return i - 1;
            }
            return path.Count - 1;
        }

        /// <summary>
        /// Renders a line segment from startIndex to endIndex.
        /// </summary>
        private void RenderSingleLine(LineRenderer lr, List<Position> path, int startIndex, int endIndex, Color color)
        {
            var count = endIndex - startIndex;
            if (count <= 0)
            {
                lr.enabled = false;
                return;
            }

            lr.positionCount = count;
            lr.startColor = color;
            lr.endColor = color;

            for (int i = 0; i < count; i++)
            {
                var worldPos = path[startIndex + i].ToVector3();
                worldPos.x += 0.5f; // Center on tile
                worldPos.y += 0.5f;
                worldPos.z = lineZPosition;
                lr.SetPosition(i, worldPos);
            }

            lr.enabled = true;
        }

        /// <summary>
        /// Clears the path preview line.
        /// </summary>
        public void ClearPath()
        {
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
                lineRenderer.positionCount = 0;
            }

            if (unreachableLineRenderer != null)
            {
                unreachableLineRenderer.enabled = false;
                unreachableLineRenderer.positionCount = 0;
            }

            currentPath = null;
            currentMovementCost = 0;
            reachablePathIndex = -1;
        }

        /// <summary>
        /// Gets current preview path (full path).
        /// </summary>
        public List<Position> CurrentPath => currentPath;

        /// <summary>
        /// Gets reachable portion of current path based on available movement.
        /// </summary>
        public List<Position> ReachablePath
        {
            get
            {
                if (currentPath == null || reachablePathIndex < 0)
                    return null;
                return currentPath.GetRange(0, reachablePathIndex + 1);
            }
        }

        /// <summary>
        /// Gets current path movement cost (total).
        /// </summary>
        public int CurrentMovementCost => currentMovementCost;

        /// <summary>
        /// Gets index of last reachable position in path.
        /// </summary>
        public int ReachablePathIndex => reachablePathIndex;

        /// <summary>
        /// Checks if a path is currently being previewed.
        /// </summary>
        public bool IsShowingPath => currentPath != null && currentPath.Count > 0 && (lineRenderer.enabled || unreachableLineRenderer.enabled);

        /// <summary>
        /// Updates line width at runtime.
        /// </summary>
        public void SetLineWidth(float width)
        {
            lineWidth = width;
            if (lineRenderer != null)
            {
                lineRenderer.startWidth = width;
                lineRenderer.endWidth = width;
            }
        }
    }
}
