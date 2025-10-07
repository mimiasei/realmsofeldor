using UnityEngine;
using TMPro;
using RealmsOfEldor.Core;
using RealmsOfEldor.Controllers;
using RealmsOfEldor.Core.Events;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.UI
{
    /// <summary>
    /// Displays player resources (gold, wood, ore, etc.) and current game date.
    /// Updates automatically via event subscription (no Update() loop).
    /// Based on VCMI's CResDataBar pattern.
    /// </summary>
    public class ResourceBarUI : MonoBehaviour
    {
        [Header("Resource Text Fields")]
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI woodText;
        [SerializeField] private TextMeshProUGUI oreText;
        [SerializeField] private TextMeshProUGUI mercuryText;
        [SerializeField] private TextMeshProUGUI sulfurText;
        [SerializeField] private TextMeshProUGUI crystalText;
        [SerializeField] private TextMeshProUGUI gemsText;

        [Header("Date Display")]
        [SerializeField] private TextMeshProUGUI dateText;

        [Header("Event Channels")]
        [SerializeField] private GameEventChannel gameEvents;

        [Header("Formatting")]
        [SerializeField] private string resourceFormat = "{0}";
        [SerializeField] private string dateFormat = "M:{0}, W:{1}, D:{2}";

        private int currentPlayerId = 0; // Track which player we're displaying resources for

        void OnEnable()
        {
            if (gameEvents != null)
            {
                gameEvents.OnResourcesChanged += HandleResourcesChanged;
                gameEvents.OnDayAdvanced += HandleDayAdvanced;
                gameEvents.OnTurnChanged += HandleTurnChanged;
            }

            // Initial display
            RefreshAllResources();
            RefreshDate();
        }

        void OnDisable()
        {
            if (gameEvents != null)
            {
                gameEvents.OnResourcesChanged -= HandleResourcesChanged;
                gameEvents.OnDayAdvanced -= HandleDayAdvanced;
                gameEvents.OnTurnChanged -= HandleTurnChanged;
            }
        }

        private void HandleResourcesChanged(int playerId, ResourceSet newResources)
        {
            // Only update if it's the current player's resources
            // For now, assume player 0 is the human player
            if (playerId != 0)
                return;

            RefreshAllResources();
        }

        private void HandleDayAdvanced(int day)
        {
            RefreshDate();
        }

        private void HandleTurnChanged(int playerId)
        {
            // Update display when turn changes
            RefreshAllResources();
            RefreshDate();
        }

        /// <summary>
        /// Updates a single resource display.
        /// </summary>
        private void UpdateResourceDisplay(ResourceType type, int amount)
        {
            var formattedAmount = string.Format(resourceFormat, amount);

            switch (type)
            {
                case ResourceType.Gold:
                    if (goldText != null) goldText.text = formattedAmount;
                    break;
                case ResourceType.Wood:
                    if (woodText != null) woodText.text = formattedAmount;
                    break;
                case ResourceType.Ore:
                    if (oreText != null) oreText.text = formattedAmount;
                    break;
                case ResourceType.Mercury:
                    if (mercuryText != null) mercuryText.text = formattedAmount;
                    break;
                case ResourceType.Sulfur:
                    if (sulfurText != null) sulfurText.text = formattedAmount;
                    break;
                case ResourceType.Crystal:
                    if (crystalText != null) crystalText.text = formattedAmount;
                    break;
                case ResourceType.Gems:
                    if (gemsText != null) gemsText.text = formattedAmount;
                    break;
            }
        }

        /// <summary>
        /// Refreshes all resource displays from GameState.
        /// </summary>
        private void RefreshAllResources()
        {
            if (Controllers.GameStateManager.Instance == null)
                return;

            var player = Controllers.GameStateManager.Instance.State.GetPlayer(currentPlayerId);
            if (player == null)
                return;

            UpdateResourceDisplay(ResourceType.Gold, player.Resources.Gold);
            UpdateResourceDisplay(ResourceType.Wood, player.Resources.Wood);
            UpdateResourceDisplay(ResourceType.Ore, player.Resources.Ore);
            UpdateResourceDisplay(ResourceType.Mercury, player.Resources.Mercury);
            UpdateResourceDisplay(ResourceType.Sulfur, player.Resources.Sulfur);
            UpdateResourceDisplay(ResourceType.Crystal, player.Resources.Crystal);
            UpdateResourceDisplay(ResourceType.Gems, player.Resources.Gems);
        }

        /// <summary>
        /// Refreshes the date display from GameState.
        /// </summary>
        private void RefreshDate()
        {
            if (Controllers.GameStateManager.Instance == null || dateText == null)
                return;

            var gameState = Controllers.GameStateManager.Instance.State;
            int month = GetMonth(gameState.CurrentDay);
            int week = GetWeek(gameState.CurrentDay);
            int dayOfWeek = GetDayOfWeek(gameState.CurrentDay);

            dateText.text = string.Format(dateFormat, month, week, dayOfWeek);
        }

        /// <summary>
        /// Calculates month from day number (1-indexed).
        /// HOMM3 pattern: 4 weeks per month, 7 days per week.
        /// </summary>
        private int GetMonth(int day)
        {
            return (day - 1) / 28 + 1;
        }

        /// <summary>
        /// Calculates week within month from day number (1-indexed).
        /// </summary>
        private int GetWeek(int day)
        {
            return ((day - 1) % 28) / 7 + 1;
        }

        /// <summary>
        /// Calculates day of week from day number (1-indexed).
        /// </summary>
        private int GetDayOfWeek(int day)
        {
            return ((day - 1) % 7) + 1;
        }

        /// <summary>
        /// Sets the current player whose resources to display.
        /// </summary>
        public void SetCurrentPlayer(int playerId)
        {
            currentPlayerId = playerId;
            RefreshAllResources();
        }

#if UNITY_EDITOR
        [ContextMenu("Refresh Display")]
        private void EditorRefreshDisplay()
        {
            RefreshAllResources();
            RefreshDate();
        }
#endif
    }
}
