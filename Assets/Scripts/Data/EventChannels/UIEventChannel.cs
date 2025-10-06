using System;
using UnityEngine;


namespace RealmsOfEldor.Data.EventChannels
{
    /// <summary>
    /// Event channel for adventure map UI interactions and state changes.
    /// Decouples UI components from game logic and each other.
    /// </summary>
    [CreateAssetMenu(menuName = "Realms of Eldor/Event Channels/UI Event Channel", fileName = "UIEventChannel")]
    public class UIEventChannel : ScriptableObject
    {
        // ===== Hero Selection Events =====

        /// <summary>
        /// Raised when a hero is selected on the adventure map.
        /// </summary>
        public event Action<Hero> OnHeroSelected;

        /// <summary>
        /// Raised when hero selection is cleared (no hero selected).
        /// </summary>
        public event Action OnSelectionCleared;

        // ===== UI Button Events =====

        /// <summary>
        /// Raised when the End Turn button is clicked.
        /// </summary>
        public event Action OnEndTurnButtonClicked;

        /// <summary>
        /// Raised when the Sleep/Wake button is clicked.
        /// </summary>
        public event Action OnSleepWakeButtonClicked;

        /// <summary>
        /// Raised when the Spellbook button is clicked.
        /// </summary>
        public event Action OnSpellbookButtonClicked;

        /// <summary>
        /// Raised when the Next Hero button is clicked.
        /// </summary>
        public event Action OnNextHeroButtonClicked;

        /// <summary>
        /// Raised when the Next Town button is clicked.
        /// </summary>
        public event Action OnNextTownButtonClicked;

        /// <summary>
        /// Raised when the System Menu button is clicked.
        /// </summary>
        public event Action OnSystemMenuButtonClicked;

        // ===== Info Bar State Events =====

        /// <summary>
        /// Raised when the info bar should display hero information.
        /// </summary>
        public event Action<Hero> OnShowHeroInfo;

        /// <summary>
        /// Raised when the info bar should display town information.
        /// </summary>
        public event Action OnShowTownInfo;

        /// <summary>
        /// Raised when the info bar should display the date animation.
        /// </summary>
        public event Action OnShowDateAnimation;

        /// <summary>
        /// Raised when the info bar should display a pickup notification.
        /// </summary>
        public event Action<ResourceType, int, string> OnShowPickupNotification;

        /// <summary>
        /// Raised when the info bar should return to empty/default state.
        /// </summary>
        public event Action OnShowInfoBarDefault;

        // ===== Input State Events =====

        /// <summary>
        /// Raised when entering spell casting mode (selecting target).
        /// </summary>
        public event Action<int> OnEnterSpellCastingMode; // spellId

        /// <summary>
        /// Raised when exiting spell casting mode.
        /// </summary>
        public event Action OnExitSpellCastingMode;

        /// <summary>
        /// Raised when UI should be dimmed (e.g., dialog opened).
        /// </summary>
        public event Action<float> OnDimUI; // dim level 0-1

        /// <summary>
        /// Raised when UI dimming should be removed.
        /// </summary>
        public event Action OnUndimUI;

        // ===== Notification Events =====

        /// <summary>
        /// Raised to show a tooltip at cursor position.
        /// </summary>
        public event Action<string> OnShowTooltip;

        /// <summary>
        /// Raised to hide the current tooltip.
        /// </summary>
        public event Action OnHideTooltip;

        /// <summary>
        /// Raised to show a temporary message in the status bar.
        /// </summary>
        public event Action<string> OnShowStatusMessage;

        // ===== Raise Methods =====

        public void RaiseHeroSelected(Hero hero)
        {
            OnHeroSelected?.Invoke(hero);
        }

        public void RaiseSelectionCleared()
        {
            OnSelectionCleared?.Invoke();
        }

        public void RaiseEndTurnButtonClicked()
        {
            OnEndTurnButtonClicked?.Invoke();
        }

        public void RaiseSleepWakeButtonClicked()
        {
            OnSleepWakeButtonClicked?.Invoke();
        }

        public void RaiseSpellbookButtonClicked()
        {
            OnSpellbookButtonClicked?.Invoke();
        }

        public void RaiseNextHeroButtonClicked()
        {
            OnNextHeroButtonClicked?.Invoke();
        }

        public void RaiseNextTownButtonClicked()
        {
            OnNextTownButtonClicked?.Invoke();
        }

        public void RaiseSystemMenuButtonClicked()
        {
            OnSystemMenuButtonClicked?.Invoke();
        }

        public void RaiseShowHeroInfo(Hero hero)
        {
            OnShowHeroInfo?.Invoke(hero);
        }

        public void RaiseShowTownInfo()
        {
            OnShowTownInfo?.Invoke();
        }

        public void RaiseShowDateAnimation()
        {
            OnShowDateAnimation?.Invoke();
        }

        public void RaiseShowPickupNotification(ResourceType type, int amount, string message)
        {
            OnShowPickupNotification?.Invoke(type, amount, message);
        }

        public void RaiseShowInfoBarDefault()
        {
            OnShowInfoBarDefault?.Invoke();
        }

        public void RaiseEnterSpellCastingMode(int spellId)
        {
            OnEnterSpellCastingMode?.Invoke(spellId);
        }

        public void RaiseExitSpellCastingMode()
        {
            OnExitSpellCastingMode?.Invoke();
        }

        public void RaiseDimUI(float dimLevel)
        {
            OnDimUI?.Invoke(dimLevel);
        }

        public void RaiseUndimUI()
        {
            OnUndimUI?.Invoke();
        }

        public void RaiseShowTooltip(string text)
        {
            OnShowTooltip?.Invoke(text);
        }

        public void RaiseHideTooltip()
        {
            OnHideTooltip?.Invoke();
        }

        public void RaiseShowStatusMessage(string message)
        {
            OnShowStatusMessage?.Invoke(message);
        }

        /// <summary>
        /// Clear all event subscriptions. Call when unloading scenes to prevent memory leaks.
        /// </summary>
        public void ClearAllSubscriptions()
        {
            OnHeroSelected = null;
            OnSelectionCleared = null;
            OnEndTurnButtonClicked = null;
            OnSleepWakeButtonClicked = null;
            OnSpellbookButtonClicked = null;
            OnNextHeroButtonClicked = null;
            OnNextTownButtonClicked = null;
            OnSystemMenuButtonClicked = null;
            OnShowHeroInfo = null;
            OnShowTownInfo = null;
            OnShowDateAnimation = null;
            OnShowPickupNotification = null;
            OnShowInfoBarDefault = null;
            OnEnterSpellCastingMode = null;
            OnExitSpellCastingMode = null;
            OnDimUI = null;
            OnUndimUI = null;
            OnShowTooltip = null;
            OnHideTooltip = null;
            OnShowStatusMessage = null;
        }
    }
}
