using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RealmsOfEldor.Core;
using RealmsOfEldor.Controllers;
using RealmsOfEldor.Data;
using RealmsOfEldor.Data.EventChannels;

namespace RealmsOfEldor.UI
{
    /// <summary>
    /// Controls for turn management: day/week counter and end turn button.
    /// Displays current day and provides button to end the player's turn.
    /// </summary>
    public class TurnControlUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI dayText;
        [SerializeField] private TextMeshProUGUI turnText;
        [SerializeField] private Button endTurnButton;
        [SerializeField] private TextMeshProUGUI endTurnButtonText;

        [Header("Event Channels")]
        [SerializeField] private GameEventChannel gameEvents;
        [SerializeField] private UIEventChannel uiEvents;

        [Header("Button States")]
        [SerializeField] private Color enabledColor = Color.white;
        [SerializeField] private Color disabledColor = Color.gray;
        [SerializeField] private string endTurnText = "End Turn";
        [SerializeField] private string waitingText = "Waiting...";

        [Header("Formatting")]
        [SerializeField] private string dayFormat = "Day {0}";
        [SerializeField] private string turnFormat = "Turn {0}";

        private bool canEndTurn = true;

        void OnEnable()
        {
            if (gameEvents != null)
            {
                gameEvents.OnDayAdvanced += HandleDayAdvanced;
                gameEvents.OnTurnChanged += HandleTurnChanged;
            }

            if (endTurnButton != null)
            {
                endTurnButton.onClick.AddListener(OnEndTurnClicked);
            }

            RefreshDisplay();
        }

        void OnDisable()
        {
            if (gameEvents != null)
            {
                gameEvents.OnDayAdvanced -= HandleDayAdvanced;
                gameEvents.OnTurnChanged -= HandleTurnChanged;
            }

            if (endTurnButton != null)
            {
                endTurnButton.onClick.RemoveListener(OnEndTurnClicked);
            }
        }

        private void HandleDayAdvanced(int day)
        {
            UpdateDayDisplay(day);
        }

        private void HandleTurnChanged(int playerId)
        {
            // Enable end turn button when it's the human player's turn (assume player 0)
            canEndTurn = (playerId == 0);
            UpdateEndTurnButton();
            RefreshDisplay();
        }

        /// <summary>
        /// Refreshes all display elements from GameState.
        /// </summary>
        private void RefreshDisplay()
        {
            if (Controllers.GameStateManager.Instance == null)
                return;

            var gameState = Controllers.GameStateManager.Instance.State;
            UpdateDayDisplay(gameState.CurrentDay);
            UpdateTurnDisplay(gameState.CurrentPlayerTurn);
            UpdateEndTurnButton();
        }

        /// <summary>
        /// Updates the day display.
        /// </summary>
        private void UpdateDayDisplay(int day)
        {
            if (dayText != null)
            {
                dayText.text = string.Format(dayFormat, day);
            }
        }

        /// <summary>
        /// Updates the turn display.
        /// </summary>
        private void UpdateTurnDisplay(int turn)
        {
            if (turnText != null)
            {
                turnText.text = string.Format(turnFormat, turn);
            }
        }

        /// <summary>
        /// Updates the end turn button's interactability and visual state.
        /// </summary>
        private void UpdateEndTurnButton()
        {
            if (endTurnButton == null)
                return;

            // Determine if button should be enabled
            var shouldEnable = CanEndTurn();

            endTurnButton.interactable = shouldEnable;

            // Update button text
            if (endTurnButtonText != null)
            {
                endTurnButtonText.text = shouldEnable ? endTurnText : waitingText;
                endTurnButtonText.color = shouldEnable ? enabledColor : disabledColor;
            }
        }

        /// <summary>
        /// Checks if the player can end their turn.
        /// </summary>
        private bool CanEndTurn()
        {
            if (!canEndTurn)
                return false;

            if (Controllers.GameStateManager.Instance == null)
                return false;

            // Additional conditions could be checked here:
            // - All heroes have moved or are sleeping
            // - No pending actions
            // - Turn timer hasn't expired
            // For MVP, just check the basic flag

            return true;
        }

        /// <summary>
        /// Called when the end turn button is clicked.
        /// </summary>
        private void OnEndTurnClicked()
        {
            if (!CanEndTurn())
                return;

            // Raise event for other systems to handle
            if (uiEvents != null)
            {
                uiEvents.RaiseEndTurnButtonClicked();
            }

            // Execute end turn via GameStateManager
            if (Controllers.GameStateManager.Instance != null)
            {
                Controllers.GameStateManager.Instance.EndTurn();
            }

            // Update button state
            canEndTurn = false;
            UpdateEndTurnButton();
        }

        /// <summary>
        /// Displays a temporary message on the end turn button.
        /// Useful for showing "All heroes moved" or "No actions remaining".
        /// </summary>
        public void ShowTemporaryMessage(string message, float duration = 2f)
        {
            if (endTurnButtonText == null)
                return;

            var originalText = endTurnButtonText.text;
            endTurnButtonText.text = message;

            Invoke(nameof(ResetButtonText), duration);
        }

        private void ResetButtonText()
        {
            UpdateEndTurnButton();
        }

#if UNITY_EDITOR
        [ContextMenu("Test End Turn")]
        private void TestEndTurn()
        {
            OnEndTurnClicked();
        }

        [ContextMenu("Refresh Display")]
        private void EditorRefreshDisplay()
        {
            RefreshDisplay();
        }
#endif
    }
}
