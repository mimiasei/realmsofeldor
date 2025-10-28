using UnityEngine;
using System.Collections.Generic;
using RealmsOfEldor.Core;
using RealmsOfEldor.Core.Battle;

namespace RealmsOfEldor.Controllers.Battle
{
    /// <summary>
    /// Handles player input for battle (mouse clicks, stack selection, hex targeting).
    /// Phase 7C: Player interaction with battle system.
    /// Based on VCMI's BattleActionsController pattern.
    /// </summary>
    public class BattleInputController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BattleController battleController;
        [SerializeField] private BattleFieldRenderer fieldRenderer;
        [SerializeField] private BattleStackRenderer stackRenderer;
        [SerializeField] private BattleAnimator battleAnimator;

        [Header("Input Settings")]
        [SerializeField] private LayerMask stackLayerMask = -1;
        [SerializeField] private bool enablePlayerInput = true;

        [Header("Highlight Colors")]
        [SerializeField] private Color moveRangeColor = new Color(0f, 1f, 0f, 0.5f);      // Green
        [SerializeField] private Color attackRangeColor = new Color(1f, 0f, 0f, 0.5f);    // Red
        [SerializeField] private Color selectedColor = new Color(1f, 1f, 0f, 0.8f);       // Yellow

        // Input state
        private InputMode currentMode = InputMode.Idle;
        private int selectedStackId = -1;
        private BattleUnit selectedUnit = null;
        private List<Vector2Int> validMoveHexes = new List<Vector2Int>();
        private List<int> validAttackTargets = new List<int>();

        private Camera battleCamera;

        public enum InputMode
        {
            Idle,                // No selection, waiting for player input
            StackSelected,       // Stack selected, showing movement range
            TargetingAttack,     // Attack button clicked, selecting target
            TargetingSpell       // Spell selected, targeting hex/unit
        }

        void Awake()
        {
            // Auto-find components if not assigned
            if (battleController == null)
                battleController = FindFirstObjectByType<BattleController>();

            if (fieldRenderer == null)
                fieldRenderer = FindFirstObjectByType<BattleFieldRenderer>();

            if (stackRenderer == null)
                stackRenderer = FindFirstObjectByType<BattleStackRenderer>();

            if (battleAnimator == null)
                battleAnimator = FindFirstObjectByType<BattleAnimator>();

            // Get camera from BattleFieldRenderer (which has the correct camera)
            if (fieldRenderer != null)
            {
                battleCamera = fieldRenderer.GetComponent<Camera>();
                Debug.Log("BattleInputController: Using camera from BattleFieldRenderer");
            }
            else
            {
                battleCamera = Camera.main;
                Debug.LogWarning("BattleInputController: BattleFieldRenderer not found, using Camera.main");
            }

            // Debug camera setup
            if (battleCamera != null)
            {
                Debug.Log($"BattleInputController: Camera is {(battleCamera.orthographic ? "Orthographic" : "Perspective")}");
                Debug.Log($"BattleInputController: Camera position: {battleCamera.transform.position}, ortho size: {battleCamera.orthographicSize}");
            }
            else
            {
                Debug.LogError("BattleInputController: No battle camera found!");
            }
        }

        void Update()
        {
            if (!enablePlayerInput || battleController == null || !battleController.IsBattleActive)
                return;

            // Only allow player input for attacker side (player)
            var activeUnit = GetActiveUnit();
            if (activeUnit == null)
            {
                // No active unit - this might be expected in Battle System Tester
                // Allow input anyway for testing purposes
                if (Input.GetMouseButtonDown(0))
                {
                    Debug.LogWarning("BattleInputController: No active unit, but allowing click for testing");
                }
                // Still allow clicks for testing
            }
            else if (activeUnit.Side != BattleSide.Attacker)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Debug.LogWarning($"BattleInputController: Active unit is {activeUnit.Side}, not Attacker - blocking input");
                }
                return; // Block input for defender turns
            }

            HandleMouseInput();
        }

        /// <summary>
        /// Handles mouse clicks for stack selection and hex targeting.
        /// </summary>
        private void HandleMouseInput()
        {
            if (!Input.GetMouseButtonDown(0))
                return;

            // Safety check: ensure mouse is within screen bounds
            var mousePos = Input.mousePosition;
            if (mousePos.x < 0 || mousePos.x > Screen.width ||
                mousePos.y < 0 || mousePos.y > Screen.height)
            {
                return; // Mouse outside window, ignore
            }

            // Convert screen position to world position on the Z=0 plane (where battlefield is)
            var ray = battleCamera.ScreenPointToRay(mousePos);

            // Calculate intersection with Z=0 plane
            // Ray equation: P = origin + t * direction
            // For Z=0: origin.z + t * direction.z = 0
            // So: t = -origin.z / direction.z
            var t = -ray.origin.z / ray.direction.z;
            var worldPos = ray.origin + ray.direction * t;

            Debug.Log($"BattleInputController: Mouse clicked at screen {Input.mousePosition} -> world {worldPos}");

            // Try to click on a stack first
            var clickedStack = GetStackAtWorldPosition(worldPos);
            if (clickedStack != null)
            {
                HandleStackClick(clickedStack);
                return;
            }

            // Try to click on a hex
            var clickedHex = GetHexAtWorldPosition(worldPos);
            if (clickedHex.HasValue)
            {
                Debug.Log($"BattleInputController: Clicked on hex {clickedHex.Value}");
                HandleHexClick(clickedHex.Value);
            }
            else
            {
                Debug.Log($"BattleInputController: Click did not hit a valid hex");
            }
        }

        /// <summary>
        /// Handles clicking on a battle stack.
        /// </summary>
        private void HandleStackClick(BattleUnit unit)
        {
            switch (currentMode)
            {
                case InputMode.Idle:
                case InputMode.StackSelected:
                    // Select this stack if it's ours and active
                    var activeUnit = GetActiveUnit();
                    if (unit.UnitId == activeUnit?.UnitId)
                    {
                        SelectStack(unit);
                    }
                    // Attack enemy stack if we have one selected
                    else if (selectedUnit != null && unit.Side != selectedUnit.Side)
                    {
                        AttemptAttack(unit);
                    }
                    break;

                case InputMode.TargetingAttack:
                    // Attack the clicked enemy
                    if (unit.Side != selectedUnit?.Side)
                    {
                        AttemptAttack(unit);
                    }
                    break;

                case InputMode.TargetingSpell:
                    // TODO: Cast spell on unit
                    Debug.Log($"Spell targeting not yet implemented");
                    break;
            }
        }

        /// <summary>
        /// Handles clicking on an empty hex.
        /// </summary>
        private void HandleHexClick(Vector2Int hex)
        {
            if (currentMode == InputMode.StackSelected && selectedUnit != null)
            {
                // Try to move to this hex
                if (validMoveHexes.Contains(hex))
                {
                    AttemptMove(hex);
                }
                else
                {
                    Debug.Log($"Cannot move to hex {hex} - out of range or blocked");
                }
            }
        }

        /// <summary>
        /// Selects a stack and shows its movement range.
        /// </summary>
        private void SelectStack(BattleUnit unit)
        {
            if (selectedUnit == unit)
                return; // Already selected

            selectedStackId = unit.UnitId;
            selectedUnit = unit;
            currentMode = InputMode.StackSelected;

            Debug.Log($"Selected stack {unit.UnitId}: {unit.CreatureType?.creatureName ?? "Unknown"}");

            // Highlight selected stack
            if (battleAnimator != null)
            {
                battleAnimator.AnimateSelection(unit.UnitId);
            }

            // Calculate and highlight valid moves
            CalculateValidMoves();
            HighlightValidMoves();
        }

        /// <summary>
        /// Deselects current stack and clears highlights.
        /// </summary>
        public void DeselectStack()
        {
            selectedStackId = -1;
            selectedUnit = null;
            currentMode = InputMode.Idle;
            validMoveHexes.Clear();
            validAttackTargets.Clear();

            // Clear hex highlights
            fieldRenderer?.ClearHighlights();
        }

        /// <summary>
        /// Calculates valid movement hexes for selected unit.
        /// </summary>
        private void CalculateValidMoves()
        {
            validMoveHexes.Clear();
            validAttackTargets.Clear();

            if (selectedUnit == null || battleController?.State == null)
                return;

            // Get all reachable hexes (simple radius-based for now)
            // TODO: Use proper pathfinding and movement points
            var speed = selectedUnit.CreatureType?.speed ?? 5;
            var startPos = selectedUnit.Position;

            for (var x = 0; x < BattleHexGrid.BATTLE_WIDTH; x++)
            {
                for (var y = 0; y < BattleHexGrid.BATTLE_HEIGHT; y++)
                {
                    if (!BattleHexGrid.IsValidHex(x, y))
                        continue;

                    var hex = new Vector2Int(x, y);
                    var distance = BattleHexGrid.GetHexDistance(startPos.X, startPos.Y, x, y);

                    // Can move to hex if within speed range and not occupied by friendly unit
                    if (distance <= speed && distance > 0)
                    {
                        // Check if hex is occupied
                        var occupyingUnit = battleController.State.GetUnitAtPosition(new BattleHex(x, y));
                        if (occupyingUnit == null)
                        {
                            validMoveHexes.Add(hex);
                        }
                        else if (occupyingUnit.Side != selectedUnit.Side)
                        {
                            // Can attack adjacent enemy units
                            if (distance <= 1)
                            {
                                validAttackTargets.Add(occupyingUnit.UnitId);
                            }
                        }
                    }
                }
            }

            Debug.Log($"Valid moves: {validMoveHexes.Count}, Valid attacks: {validAttackTargets.Count}");
        }

        /// <summary>
        /// Highlights valid movement and attack hexes.
        /// </summary>
        private void HighlightValidMoves()
        {
            if (fieldRenderer == null)
                return;

            // Clear previous highlights
            fieldRenderer.ClearHighlights();

            // Highlight movement range
            foreach (var hex in validMoveHexes)
            {
                fieldRenderer.HighlightHex(hex.x, hex.y, moveRangeColor);
            }

            // Highlight attack targets
            foreach (var targetId in validAttackTargets)
            {
                var target = battleController.State.GetUnit(targetId);
                if (target != null)
                {
                    fieldRenderer.HighlightHex(target.Position.X, target.Position.Y, attackRangeColor);
                }
            }

            // Highlight selected unit's position
            if (selectedUnit != null)
            {
                fieldRenderer.HighlightHex(selectedUnit.Position.X, selectedUnit.Position.Y, selectedColor);
            }
        }

        /// <summary>
        /// Attempts to move selected unit to target hex.
        /// </summary>
        private void AttemptMove(Vector2Int targetHex)
        {
            if (selectedUnit == null || battleController?.State == null)
                return;

            Debug.Log($"Moving unit {selectedUnit.UnitId} to {targetHex}");

            // Create move action
            var action = new BattleAction
            {
                UnitId = selectedUnit.UnitId,
                Type = ActionType.WALK,
                DestinationHex = new BattleHex(targetHex.x, targetHex.y)
            };

            // TODO: Execute action through BattleController
            // For now, just update position directly
            selectedUnit.Position = new BattleHex(targetHex.x, targetHex.y);

            // Animate movement
            var fromHex = new Vector2Int(selectedUnit.Position.X, selectedUnit.Position.Y);
            battleAnimator?.AnimateMovement(selectedUnit.UnitId, fromHex, targetHex);

            // End turn after moving
            selectedUnit.EndTurn();
            DeselectStack();
        }

        /// <summary>
        /// Attempts to attack target unit.
        /// </summary>
        private void AttemptAttack(BattleUnit target)
        {
            if (selectedUnit == null || target == null || battleController?.State == null)
                return;

            Debug.Log($"Unit {selectedUnit.UnitId} attacking unit {target.UnitId}");

            // Execute attack through battle state
            var result = battleController.State.ExecuteAttack(selectedUnit, target, chargeDistance: 0);

            if (result != null)
            {
                Debug.Log($"Attack result: {result}");

                // Animate attack
                if (selectedUnit.CreatureType?.IsRanged() == true)
                {
                    battleAnimator?.AnimateRangedAttack(selectedUnit.UnitId, target.UnitId, result.DamageDealt);
                }
                else
                {
                    battleAnimator?.AnimateMeleeAttack(selectedUnit.UnitId, target.UnitId, result.DamageDealt);
                }

                // Check if target died
                if (!target.IsAlive)
                {
                    battleAnimator?.AnimateDeath(target.UnitId);
                }
            }

            // End turn after attacking
            selectedUnit.EndTurn();
            DeselectStack();

            // Check if battle ended
            if (battleController.State.CheckBattleEnd())
            {
                Debug.Log($"Battle ended! Winner: {battleController.State.WinningSide}");
            }
        }

        /// <summary>
        /// Enters attack targeting mode.
        /// </summary>
        public void EnterAttackMode()
        {
            if (selectedUnit == null)
            {
                Debug.LogWarning("No unit selected for attack");
                return;
            }

            currentMode = InputMode.TargetingAttack;
            Debug.Log("Entering attack targeting mode - click an enemy unit");

            // Highlight enemy units in range
            // TODO: Calculate actual attack range
        }

        /// <summary>
        /// Defend action for selected unit.
        /// </summary>
        public void DefendSelectedUnit()
        {
            if (selectedUnit == null)
            {
                Debug.LogWarning("No unit selected for defend");
                return;
            }

            Debug.Log($"Unit {selectedUnit.UnitId} defending");
            selectedUnit.IsDefending = true;
            selectedUnit.EndTurn();
            DeselectStack();
        }

        /// <summary>
        /// Wait action for selected unit.
        /// </summary>
        public void WaitSelectedUnit()
        {
            if (selectedUnit == null)
            {
                Debug.LogWarning("No unit selected for wait");
                return;
            }

            Debug.Log($"Unit {selectedUnit.UnitId} waiting");
            battleController?.WaitCurrentUnit();
            DeselectStack();
        }

        #region Helper Methods

        /// <summary>
        /// Gets the currently active unit.
        /// </summary>
        private BattleUnit GetActiveUnit()
        {
            if (battleController?.State == null)
                return null;

            var activeId = battleController.State.ActiveUnitId;
            return battleController.State.GetUnit(activeId);
        }

        /// <summary>
        /// Gets the stack at a world position via raycast.
        /// </summary>
        private BattleUnit GetStackAtWorldPosition(Vector3 worldPos)
        {
            var hit = Physics2D.Raycast(worldPos, Vector2.zero, Mathf.Infinity, stackLayerMask);
            if (hit.collider != null)
            {
                var stackView = hit.collider.GetComponentInParent<BattleStackView>();
                if (stackView != null)
                {
                    return battleController?.State?.GetUnit(stackView.StackId);
                }
            }

            return null;
        }

        /// <summary>
        /// Converts world position to hex coordinates.
        /// </summary>
        private Vector2Int? GetHexAtWorldPosition(Vector3 worldPos)
        {
            var hex = BattleHexGrid.WorldToHex(worldPos);
            Debug.Log($"BattleInputController: WorldToHex({worldPos}) = ({hex.x}, {hex.y}), valid: {BattleHexGrid.IsValidHex(hex.x, hex.y)}");

            if (BattleHexGrid.IsValidHex(hex.x, hex.y))
            {
                return hex;
            }

            return null;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Enable or disable player input.
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            enablePlayerInput = enabled;
            if (!enabled)
            {
                DeselectStack();
            }
        }

        /// <summary>
        /// Gets the currently selected unit ID.
        /// </summary>
        public int SelectedStackId => selectedStackId;

        /// <summary>
        /// Gets the current input mode.
        /// </summary>
        public InputMode CurrentMode => currentMode;

        #endregion
    }
}
