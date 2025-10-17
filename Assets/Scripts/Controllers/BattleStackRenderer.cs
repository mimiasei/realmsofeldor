using System.Collections.Generic;
using UnityEngine;
using RealmsOfEldor.Core.Battle;

namespace RealmsOfEldor.Controllers
{
    /// <summary>
    /// Manages rendering and visual updates for all battle stacks.
    /// Based on VCMI's BattleStacksController.
    /// </summary>
    public class BattleStackRenderer : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject stackViewPrefab;

        [Header("Container")]
        [SerializeField] private Transform stackContainer;

        // Maps stack ID to its visual representation
        private Dictionary<int, BattleStackView> stackViews = new Dictionary<int, BattleStackView>();

        // Currently selected stack
        private BattleStackView selectedStackView;

        void Awake()
        {
            // Create stack container if not assigned
            if (stackContainer == null)
            {
                var containerObj = new GameObject("StackContainer");
                containerObj.transform.SetParent(transform);
                stackContainer = containerObj.transform;
            }

            // Create prefab if not assigned
            if (stackViewPrefab == null)
            {
                stackViewPrefab = new GameObject("BattleStackView");
                stackViewPrefab.AddComponent<BattleStackView>();
                stackViewPrefab.SetActive(false); // Template object
            }
        }

        /// <summary>
        /// Spawns all stacks for a battle.
        /// </summary>
        public void SpawnStacks(List<BattleStack> stacks)
        {
            // Clear existing stacks
            ClearStacks();

            // Spawn each stack
            foreach (var stack in stacks)
            {
                SpawnStack(stack);
            }

            Debug.Log($"BattleStackRenderer: Spawned {stacks.Count} stacks");
        }

        /// <summary>
        /// Spawns a single stack at its position.
        /// </summary>
        public BattleStackView SpawnStack(BattleStack stack)
        {
            // Create stack view instance
            var stackViewObj = Instantiate(stackViewPrefab, stackContainer);
            stackViewObj.SetActive(true);

            var stackView = stackViewObj.GetComponent<BattleStackView>();
            if (stackView == null)
            {
                stackView = stackViewObj.AddComponent<BattleStackView>();
            }

            // Initialize view with stack data
            stackView.Initialize(stack);

            // Register in dictionary
            stackViews[stack.Id] = stackView;

            return stackView;
        }

        /// <summary>
        /// Updates a stack's visual position.
        /// </summary>
        public void MoveStack(int stackId, int newX, int newY)
        {
            if (stackViews.TryGetValue(stackId, out var stackView))
            {
                stackView.MoveTo(newX, newY);
            }
            else
            {
                Debug.LogWarning($"BattleStackRenderer: Stack {stackId} not found!");
            }
        }

        /// <summary>
        /// Updates a stack's amount after taking damage.
        /// </summary>
        public void UpdateStackAmount(int stackId)
        {
            if (stackViews.TryGetValue(stackId, out var stackView))
            {
                stackView.UpdateAmount();
            }
        }

        /// <summary>
        /// Shows damage dealt to a stack.
        /// </summary>
        public void ShowDamage(int stackId, int damage)
        {
            if (stackViews.TryGetValue(stackId, out var stackView))
            {
                stackView.ShowDamage(damage);
            }
        }

        /// <summary>
        /// Removes a dead stack from the battlefield.
        /// </summary>
        public void RemoveStack(int stackId)
        {
            if (stackViews.TryGetValue(stackId, out var stackView))
            {
                stackView.PlayDeathAnimation();
                stackViews.Remove(stackId);
            }
        }

        /// <summary>
        /// Selects a stack (highlights it).
        /// </summary>
        public void SelectStack(int stackId)
        {
            // Deselect previous stack
            if (selectedStackView != null)
            {
                selectedStackView.SetSelected(false);
            }

            // Select new stack
            if (stackViews.TryGetValue(stackId, out var stackView))
            {
                stackView.SetSelected(true);
                selectedStackView = stackView;
                Debug.Log($"BattleStackRenderer: Selected stack {stackId}");
            }
        }

        /// <summary>
        /// Deselects the currently selected stack.
        /// </summary>
        public void DeselectStack()
        {
            if (selectedStackView != null)
            {
                selectedStackView.SetSelected(false);
                selectedStackView = null;
            }
        }

        /// <summary>
        /// Gets the view for a specific stack.
        /// </summary>
        public BattleStackView GetStackView(int stackId)
        {
            stackViews.TryGetValue(stackId, out var stackView);
            return stackView;
        }

        /// <summary>
        /// Clears all stack views from the battlefield.
        /// </summary>
        public void ClearStacks()
        {
            foreach (var kvp in stackViews)
            {
                if (kvp.Value != null && kvp.Value.gameObject != null)
                {
                    Destroy(kvp.Value.gameObject);
                }
            }

            stackViews.Clear();
            selectedStackView = null;

            Debug.Log("BattleStackRenderer: Cleared all stacks");
        }

        /// <summary>
        /// Gets the stack at a specific hex position.
        /// </summary>
        public BattleStackView GetStackAtPosition(int hexX, int hexY)
        {
            foreach (var kvp in stackViews)
            {
                var stack = kvp.Value.Stack;
                if (stack != null && stack.Position.X == hexX && stack.Position.Y == hexY)
                {
                    return kvp.Value;
                }
            }

            return null;
        }

        /// <summary>
        /// Highlights stacks that can act this turn.
        /// </summary>
        public void HighlightActiveStacks(List<int> activeStackIds)
        {
            foreach (var kvp in stackViews)
            {
                var isActive = activeStackIds.Contains(kvp.Key);
                // TODO: Add visual indicator for active stacks
            }
        }
    }
}
