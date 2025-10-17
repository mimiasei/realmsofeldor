using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using RealmsOfEldor.Core;
using RealmsOfEldor.Controllers;
using RealmsOfEldor.Core.Events;
using RealmsOfEldor.Core.Events.EventChannels;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.UI
{
    /// <summary>
    /// Context-sensitive information panel that displays hero, town, date, or pickup notifications.
    /// Implements state machine pattern matching VCMI's CInfoBar.
    /// Fixed size: 192x192 pixels (HOMM3 standard).
    /// </summary>
    public class InfoBarUI : MonoBehaviour
    {
        public enum InfoBarState
        {
            Empty,          // No selection
            Hero,           // Selected hero info
            Town,           // Selected town info (placeholder for now)
            Date,           // Day/week transition animation
            EnemyTurn,      // AI turn indicator (placeholder for now)
            Pickup          // Resource pickup notification
        }

        [Header("UI Panels")]
        [SerializeField] private GameObject emptyPanel;
        [SerializeField] private GameObject heroPanel;
        [SerializeField] private GameObject townPanel;
        [SerializeField] private GameObject datePanel;
        [SerializeField] private GameObject enemyTurnPanel;
        [SerializeField] private GameObject pickupPanel;

        [Header("Hero Panel Elements")]
        [SerializeField] private Image heroPortrait;
        [SerializeField] private TextMeshProUGUI heroNameText;
        [SerializeField] private TextMeshProUGUI heroClassText;
        [SerializeField] private TextMeshProUGUI attackText;
        [SerializeField] private TextMeshProUGUI defenseText;
        [SerializeField] private TextMeshProUGUI spellPowerText;
        [SerializeField] private TextMeshProUGUI knowledgeText;
        [SerializeField] private TextMeshProUGUI movementText;
        [SerializeField] private TextMeshProUGUI manaText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI experienceText;

        [Header("Date Panel Elements")]
        [SerializeField] private TextMeshProUGUI dayText;
        [SerializeField] private TextMeshProUGUI monthText;
        [SerializeField] private TextMeshProUGUI weekText;

        [Header("Pickup Panel Elements")]
        [SerializeField] private Image pickupIcon;
        [SerializeField] private TextMeshProUGUI pickupAmountText;
        [SerializeField] private TextMeshProUGUI pickupMessageText;

        [Header("Event Channels")]
        [SerializeField] private UIEventChannel uiEvents;
        [SerializeField] private GameEventChannel gameEvents;

        [Header("Settings")]
        [SerializeField] private float pickupDisplayDuration = 3f;
        [SerializeField] private bool clickToInteract = true;

        private InfoBarState currentState = InfoBarState.Empty;
        private Hero currentHero;
        private Queue<PickupInfo> pickupQueue = new Queue<PickupInfo>();
        private bool isDisplayingPickup = false;

        private struct PickupInfo
        {
            public ResourceType resourceType;
            public int amount;
            public string message;
            public float duration;

            public PickupInfo(ResourceType type, int amt, string msg, float dur)
            {
                resourceType = type;
                amount = amt;
                message = msg;
                duration = dur;
            }
        }

        void OnEnable()
        {
            if (uiEvents != null)
            {
                uiEvents.OnShowHeroInfo += ShowHeroInfo;
                uiEvents.OnShowTownInfo += ShowTownInfo;
                uiEvents.OnShowDateAnimation += ShowDateAnimation;
                uiEvents.OnShowPickupNotification += ShowPickupNotification;
                uiEvents.OnShowInfoBarDefault += ShowDefault;
            }

            if (gameEvents != null)
            {
                gameEvents.OnHeroMoved += HandleHeroMoved;
                gameEvents.OnHeroLeveledUp += HandleHeroLeveledUp;
            }

            SetState(InfoBarState.Empty);
        }

        void OnDisable()
        {
            if (uiEvents != null)
            {
                uiEvents.OnShowHeroInfo -= ShowHeroInfo;
                uiEvents.OnShowTownInfo -= ShowTownInfo;
                uiEvents.OnShowDateAnimation -= ShowDateAnimation;
                uiEvents.OnShowPickupNotification -= ShowPickupNotification;
                uiEvents.OnShowInfoBarDefault -= ShowDefault;
            }

            if (gameEvents != null)
            {
                gameEvents.OnHeroMoved -= HandleHeroMoved;
                gameEvents.OnHeroLeveledUp -= HandleHeroLeveledUp;
            }
        }

        /// <summary>
        /// Sets the current state and activates appropriate panel.
        /// </summary>
        private void SetState(InfoBarState newState)
        {
            currentState = newState;

            // Deactivate all panels
            if (emptyPanel != null) emptyPanel.SetActive(false);
            if (heroPanel != null) heroPanel.SetActive(false);
            if (townPanel != null) townPanel.SetActive(false);
            if (datePanel != null) datePanel.SetActive(false);
            if (enemyTurnPanel != null) enemyTurnPanel.SetActive(false);
            if (pickupPanel != null) pickupPanel.SetActive(false);

            // Activate current panel
            switch (currentState)
            {
                case InfoBarState.Empty:
                    if (emptyPanel != null) emptyPanel.SetActive(true);
                    break;
                case InfoBarState.Hero:
                    if (heroPanel != null) heroPanel.SetActive(true);
                    break;
                case InfoBarState.Town:
                    if (townPanel != null) townPanel.SetActive(true);
                    break;
                case InfoBarState.Date:
                    if (datePanel != null) datePanel.SetActive(true);
                    break;
                case InfoBarState.EnemyTurn:
                    if (enemyTurnPanel != null) enemyTurnPanel.SetActive(true);
                    break;
                case InfoBarState.Pickup:
                    if (pickupPanel != null) pickupPanel.SetActive(true);
                    break;
            }
        }

        /// <summary>
        /// Shows hero information panel.
        /// </summary>
        public void ShowHeroInfo(Hero hero)
        {
            if (hero == null)
            {
                ShowDefault();
                return;
            }

            currentHero = hero;
            SetState(InfoBarState.Hero);
            UpdateHeroDisplay();
        }

        /// <summary>
        /// Updates hero panel with current hero data.
        /// </summary>
        private void UpdateHeroDisplay()
        {
            if (currentHero == null || currentState != InfoBarState.Hero)
                return;

            // Basic info
            if (heroNameText != null) heroNameText.text = currentHero.CustomName;
            if (heroClassText != null) heroClassText.text = "Hero"; // TODO: Get from HeroTypeData via TypeId
            if (levelText != null) levelText.text = $"Lvl {currentHero.Level}";
            if (experienceText != null) experienceText.text = $"XP: {currentHero.Experience}";

            // Primary stats
            if (attackText != null) attackText.text = currentHero.GetTotalAttack().ToString();
            if (defenseText != null) defenseText.text = currentHero.GetTotalDefense().ToString();
            if (spellPowerText != null) spellPowerText.text = currentHero.GetTotalSpellPower().ToString();
            if (knowledgeText != null) knowledgeText.text = currentHero.Knowledge.ToString();

            // Movement and mana
            if (movementText != null) movementText.text = $"{currentHero.Movement}/{currentHero.MaxMovement}";
            if (manaText != null) manaText.text = $"{currentHero.Mana}/{currentHero.MaxMana}";

            // Portrait (placeholder - would load from hero sprite database)
            // if (heroPortrait != null) heroPortrait.sprite = GetHeroSprite(currentHero.TypeId);
        }

        /// <summary>
        /// Shows town information panel (placeholder).
        /// </summary>
        public void ShowTownInfo()
        {
            SetState(InfoBarState.Town);
            // TODO: Implement town panel when Town system is ready
        }

        /// <summary>
        /// Shows date/day transition animation.
        /// </summary>
        public void ShowDateAnimation()
        {
            SetState(InfoBarState.Date);
            UpdateDateDisplay();

            // Auto-hide after 3 seconds
            HideDateAfterDelayAsync(3f).Forget();
        }

        private async UniTaskVoid HideDateAfterDelayAsync(float delay)
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(delay), cancellationToken: this.GetCancellationTokenOnDestroy());
            if (currentState == InfoBarState.Date)
                ShowDefault();
        }

        private void UpdateDateDisplay()
        {
            if (Controllers.GameStateManager.Instance == null)
                return;

            var day = Controllers.GameStateManager.Instance.State.CurrentDay;
            if (dayText != null) dayText.text = $"Day {day}";
            if (monthText != null) monthText.text = $"Month {GetMonth(day)}";
            if (weekText != null) weekText.text = $"Week {GetWeek(day)}";
        }

        /// <summary>
        /// Shows pickup notification (resources, artifacts, etc.).
        /// Queued system allows multiple pickups without overlap.
        /// </summary>
        public void ShowPickupNotification(ResourceType type, int amount, string message)
        {
            var info = new PickupInfo(type, amount, message, pickupDisplayDuration);
            pickupQueue.Enqueue(info);

            if (!isDisplayingPickup)
                ProcessPickupQueueAsync().Forget();
        }

        private async UniTaskVoid ProcessPickupQueueAsync()
        {
            isDisplayingPickup = true;

            while (pickupQueue.Count > 0)
            {
                var pickup = pickupQueue.Dequeue();

                SetState(InfoBarState.Pickup);

                // Update pickup panel
                if (pickupAmountText != null)
                    pickupAmountText.text = $"+{pickup.amount}";
                if (pickupMessageText != null)
                    pickupMessageText.text = pickup.message;

                // TODO: Set pickup icon based on resource type
                // if (pickupIcon != null) pickupIcon.sprite = GetResourceSprite(pickup.resourceType);

                // Wait for display duration
                await UniTask.Delay(System.TimeSpan.FromSeconds(pickup.duration), cancellationToken: this.GetCancellationTokenOnDestroy());
            }

            isDisplayingPickup = false;

            // Return to previous state (hero or empty)
            if (currentHero != null)
                ShowHeroInfo(currentHero);
            else
                ShowDefault();
        }

        /// <summary>
        /// Returns info bar to default/empty state.
        /// </summary>
        public void ShowDefault()
        {
            currentHero = null;
            SetState(InfoBarState.Empty);
        }

        /// <summary>
        /// Clears all queued pickups and returns to default.
        /// </summary>
        public void ClearAll()
        {
            pickupQueue.Clear();
            isDisplayingPickup = false;
            ShowDefault();
        }

        // ===== Event Handlers =====

        private void HandleHeroMoved(int heroId, Position newPos)
        {
            // Update movement points if this is the currently displayed hero
            if (currentHero != null && currentHero.Id == heroId && currentState == InfoBarState.Hero)
            {
                currentHero.Position = newPos;
                UpdateHeroDisplay();
            }
        }

        private void HandleHeroLeveledUp(int heroId, int newLevel)
        {
            // Update stats if this is the currently displayed hero
            if (currentHero != null && currentHero.Id == heroId && currentState == InfoBarState.Hero)
            {
                currentHero.Level = newLevel;
                UpdateHeroDisplay();
            }
        }

        // ===== Utility Methods =====

        private int GetMonth(int day)
        {
            return (day - 1) / 28 + 1;
        }

        private int GetWeek(int day)
        {
            return ((day - 1) % 28) / 7 + 1;
        }

        /// <summary>
        /// Handle click on info bar (e.g., open hero screen).
        /// </summary>
        public void OnInfoBarClicked()
        {
            if (!clickToInteract)
                return;

            switch (currentState)
            {
                case InfoBarState.Hero:
                    // TODO: Open hero details screen
                    Debug.Log($"Open hero screen for {currentHero.CustomName}");
                    break;
                case InfoBarState.Town:
                    // TODO: Open town screen
                    Debug.Log("Open town screen");
                    break;
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Test Show Date")]
        private void TestShowDate()
        {
            ShowDateAnimation();
        }

        [ContextMenu("Test Show Pickup")]
        private void TestShowPickup()
        {
            ShowPickupNotification(ResourceType.Gold, 500, "Found treasure!");
        }
#endif
    }
}
