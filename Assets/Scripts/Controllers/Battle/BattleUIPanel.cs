using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RealmsOfEldor.Controllers.Battle
{
    /// <summary>
    /// Manages the battle UI panel (bottom control panel with buttons).
    /// Based on VCMI's BattleWindow.
    /// </summary>
    public class BattleUIPanel : MonoBehaviour
    {
        [Header("Button References")]
        [SerializeField] private Button attackButton;
        [SerializeField] private Button defendButton;
        [SerializeField] private Button waitButton;
        [SerializeField] private Button autoButton;
        [SerializeField] private Button spellbookButton;
        [SerializeField] private Button retreatButton;
        [SerializeField] private Button surrenderButton;

        [Header("Info Display")]
        [SerializeField] private TextMeshProUGUI turnInfoText;
        [SerializeField] private TextMeshProUGUI activeStackInfo;
        [SerializeField] private TextMeshProUGUI messageLog;

        [Header("Hero Panels")]
        [SerializeField] private GameObject leftHeroPanel;
        [SerializeField] private GameObject rightHeroPanel;
        [SerializeField] private TextMeshProUGUI leftHeroName;
        [SerializeField] private TextMeshProUGUI rightHeroName;

        // Events
        public event System.Action OnAttackClicked;
        public event System.Action OnDefendClicked;
        public event System.Action OnWaitClicked;
        public event System.Action OnAutoClicked;
        public event System.Action OnSpellbookClicked;
        public event System.Action OnRetreatClicked;
        public event System.Action OnSurrenderClicked;

        void Awake()
        {
            // Wire up button events
            if (attackButton != null)
                attackButton.onClick.AddListener(() => OnAttackClicked?.Invoke());

            if (defendButton != null)
                defendButton.onClick.AddListener(() => OnDefendClicked?.Invoke());

            if (waitButton != null)
                waitButton.onClick.AddListener(() => OnWaitClicked?.Invoke());

            if (autoButton != null)
                autoButton.onClick.AddListener(() => OnAutoClicked?.Invoke());

            if (spellbookButton != null)
                spellbookButton.onClick.AddListener(() => OnSpellbookClicked?.Invoke());

            if (retreatButton != null)
                retreatButton.onClick.AddListener(() => OnRetreatClicked?.Invoke());

            if (surrenderButton != null)
                surrenderButton.onClick.AddListener(() => OnSurrenderClicked?.Invoke());
        }

        /// <summary>
        /// Updates the turn info display (round number, current side).
        /// </summary>
        public void SetTurnInfo(int roundNumber, string currentSide)
        {
            if (turnInfoText != null)
            {
                turnInfoText.text = $"Round {roundNumber} - {currentSide}'s Turn";
            }
        }

        /// <summary>
        /// Updates the active stack info display.
        /// </summary>
        public void SetActiveStackInfo(string stackName, int count, int hp, int maxHp)
        {
            if (activeStackInfo != null)
            {
                activeStackInfo.text = $"{stackName} x{count}\nHP: {hp}/{maxHp}";
            }
        }

        /// <summary>
        /// Clears the active stack info (when no stack is selected).
        /// </summary>
        public void ClearActiveStackInfo()
        {
            if (activeStackInfo != null)
            {
                activeStackInfo.text = "";
            }
        }

        /// <summary>
        /// Adds a message to the combat log.
        /// </summary>
        public void LogMessage(string message)
        {
            if (messageLog != null)
            {
                // Append to existing log (keep last few lines)
                var currentLog = messageLog.text;
                var lines = currentLog.Split('\n');

                // Keep only last 4 lines
                if (lines.Length > 4)
                {
                    var newLog = "";
                    for (var i = lines.Length - 4; i < lines.Length; i++)
                    {
                        newLog += lines[i] + "\n";
                    }
                    currentLog = newLog;
                }

                messageLog.text = currentLog + message + "\n";
            }

            Debug.Log($"[BattleLog] {message}");
        }

        /// <summary>
        /// Clears the combat log.
        /// </summary>
        public void ClearLog()
        {
            if (messageLog != null)
            {
                messageLog.text = "";
            }
        }

        /// <summary>
        /// Sets the left hero panel info.
        /// </summary>
        public void SetLeftHero(string heroName)
        {
            if (leftHeroPanel != null)
            {
                leftHeroPanel.SetActive(true);
            }

            if (leftHeroName != null)
            {
                leftHeroName.text = heroName;
            }
        }

        /// <summary>
        /// Sets the right hero panel info.
        /// </summary>
        public void SetRightHero(string heroName)
        {
            if (rightHeroPanel != null)
            {
                rightHeroPanel.SetActive(true);
            }

            if (rightHeroName != null)
            {
                rightHeroName.text = heroName;
            }
        }

        /// <summary>
        /// Enables/disables action buttons based on game state.
        /// </summary>
        public void SetButtonsEnabled(bool attack, bool defend, bool wait, bool auto, bool spellbook, bool retreat, bool surrender)
        {
            if (attackButton != null) attackButton.interactable = attack;
            if (defendButton != null) defendButton.interactable = defend;
            if (waitButton != null) waitButton.interactable = wait;
            if (autoButton != null) autoButton.interactable = auto;
            if (spellbookButton != null) spellbookButton.interactable = spellbook;
            if (retreatButton != null) retreatButton.interactable = retreat;
            if (surrenderButton != null) surrenderButton.interactable = surrender;
        }

        /// <summary>
        /// Shows the victory screen.
        /// </summary>
        public void ShowVictory(string winnerSide)
        {
            LogMessage($"=== {winnerSide} VICTORY! ===");
            SetButtonsEnabled(false, false, false, false, false, false, false);
        }

        /// <summary>
        /// Shows the defeat screen.
        /// </summary>
        public void ShowDefeat()
        {
            LogMessage("=== DEFEAT ===");
            SetButtonsEnabled(false, false, false, false, false, false, false);
        }

        /// <summary>
        /// Highlights the attack button (when in attack mode).
        /// </summary>
        public void HighlightAttackButton(bool highlighted)
        {
            if (attackButton != null)
            {
                var colors = attackButton.colors;
                colors.normalColor = highlighted ? Color.yellow : Color.white;
                attackButton.colors = colors;
            }
        }

        /// <summary>
        /// Highlights the defend button (when in defend mode).
        /// </summary>
        public void HighlightDefendButton(bool highlighted)
        {
            if (defendButton != null)
            {
                var colors = defendButton.colors;
                colors.normalColor = highlighted ? Color.yellow : Color.white;
                defendButton.colors = colors;
            }
        }
    }
}
