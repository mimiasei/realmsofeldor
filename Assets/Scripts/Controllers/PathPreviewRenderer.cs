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
        [SerializeField] private float lineHeight = 0.1f; // Y position for line (above ground plane)

        [Header("Visual Settings")]
        [SerializeField] private Color reachablePathColor = new Color(0.3f, 1f, 0.3f, 0.8f); // Green
        [SerializeField] private Color unreachablePathColor = new Color(1f, 0.3f, 0.3f, 0.8f); // Red

        private List<Position> currentPath;
        private int currentMovementCost;
        private int reachablePathIndex; // Last index reachable with available movement

        void Awake()
        {
            Debug.Log("PathPreviewRenderer: Awake() called");

            // Create LineRenderer for reachable portion if not assigned
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
                Debug.Log("PathPreviewRenderer: Created reachable LineRenderer");
            }

            // Configure reachable LineRenderer (green)
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.positionCount = 0;
            lineRenderer.useWorldSpace = true;

            // Use URP Unlit shader for 3D rendering
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                Debug.LogWarning("PathPreviewRenderer: URP/Unlit not found, trying Unlit/Color");
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                Debug.LogError("PathPreviewRenderer: Could not find any suitable shader! Trying Sprites/Default as fallback");
                shader = Shader.Find("Sprites/Default");
            }

            if (shader != null)
            {
                lineRenderer.material = new Material(shader);
                lineRenderer.material.color = reachablePathColor;
                Debug.Log($"PathPreviewRenderer: Assigned shader '{shader.name}' to reachable line");
            }
            else
            {
                Debug.LogError("PathPreviewRenderer: No shader found at all!");
            }

            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            lineRenderer.enabled = false;

            // Create LineRenderer for unreachable portion (red)
            if (unreachableLineRenderer == null)
            {
                var unreachableGO = new GameObject("UnreachablePathLine");
                unreachableGO.transform.SetParent(transform);
                unreachableLineRenderer = unreachableGO.AddComponent<LineRenderer>();
                Debug.Log("PathPreviewRenderer: Created unreachable LineRenderer");
            }

            // Configure unreachable LineRenderer (red)
            unreachableLineRenderer.startWidth = lineWidth;
            unreachableLineRenderer.endWidth = lineWidth;
            unreachableLineRenderer.positionCount = 0;
            unreachableLineRenderer.useWorldSpace = true;

            if (shader != null)
            {
                unreachableLineRenderer.material = new Material(shader);
                unreachableLineRenderer.material.color = unreachablePathColor;
                Debug.Log($"PathPreviewRenderer: Assigned shader '{shader.name}' to unreachable line");
            }

            unreachableLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            unreachableLineRenderer.receiveShadows = false;
            unreachableLineRenderer.enabled = false;

            Debug.Log("PathPreviewRenderer: Initialization complete");
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
            Debug.Log($"PathPreviewRenderer.ShowPath() called - path={path?.Count ?? 0} positions, cost={totalMovementCost}, available={availableMovement}");

            if (path == null || path.Count < 2)
            {
                Debug.LogWarning("PathPreviewRenderer: Path is null or too short, clearing path");
                ClearPath();
                return;
            }

            currentPath = new List<Position>(path);
            currentMovementCost = totalMovementCost;

            // Calculate reachable portion of path
            reachablePathIndex = CalculateReachableIndex(path, pathStepCosts, availableMovement);
            Debug.Log($"PathPreviewRenderer: Reachable index = {reachablePathIndex}");

            // If entire path is reachable, show only green line
            if (reachablePathIndex >= path.Count - 1)
            {
                Debug.Log("PathPreviewRenderer: Entire path reachable - rendering green line only");
                RenderSingleLine(lineRenderer, path, 0, path.Count, reachablePathColor);
                unreachableLineRenderer.enabled = false;
            }
            // If at least part of path is reachable, show green + red split
            else if (reachablePathIndex > 0)
            {
                Debug.Log($"PathPreviewRenderer: Partial path reachable - rendering split at index {reachablePathIndex}");
                RenderSingleLine(lineRenderer, path, 0, reachablePathIndex + 1, reachablePathColor);
                RenderSingleLine(unreachableLineRenderer, path, reachablePathIndex, path.Count, unreachablePathColor);
            }
            // If nothing is reachable (edge case), show only red
            else
            {
                Debug.Log("PathPreviewRenderer: Nothing reachable - rendering red line only");
                lineRenderer.enabled = false;
                RenderSingleLine(unreachableLineRenderer, path, 0, path.Count, unreachablePathColor);
            }

            Debug.Log($"PathPreviewRenderer: Path rendered. lineRenderer.enabled={lineRenderer.enabled}, unreachableLineRenderer.enabled={unreachableLineRenderer.enabled}");
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
        /// Uses 3D coordinate system (X, Z ground plane).
        /// </summary>
        private void RenderSingleLine(LineRenderer lr, List<Position> path, int startIndex, int endIndex, Color color)
        {
            var count = endIndex - startIndex;
            Debug.Log($"RenderSingleLine: startIndex={startIndex}, endIndex={endIndex}, count={count}, color={color}");

            if (count <= 0)
            {
                Debug.LogWarning($"RenderSingleLine: Invalid count {count}, disabling line");
                lr.enabled = false;
                return;
            }

            lr.positionCount = count;
            lr.startColor = color;
            lr.endColor = color;

            // Set material color too (for URP shaders)
            if (lr.material != null)
            {
                lr.material.color = color;
            }

            for (int i = 0; i < count; i++)
            {
                var pos = path[startIndex + i];
                // Convert map position to 3D world position (X,Z ground plane)
                var worldPos = new Vector3(
                    pos.X + 0.5f,      // Center on tile X
                    lineHeight,         // Slightly above ground plane
                    pos.Y + 0.5f       // Center on tile Z (map Y becomes world Z)
                );
                lr.SetPosition(i, worldPos);

                if (i == 0)
                {
                    Debug.Log($"RenderSingleLine: First position - map=({pos.X},{pos.Y}) world={worldPos}");
                }
            }

            lr.enabled = true;
            Debug.Log($"RenderSingleLine: Line enabled with {count} positions, width={lr.startWidth}");
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
