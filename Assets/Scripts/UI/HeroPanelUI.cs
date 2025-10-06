using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RealmsOfEldor.Core;
using RealmsOfEldor.Data;
using RealmsOfEldor.Data.EventChannels;

namespace RealmsOfEldor.UI
{
    /// <summary>
    /// Compact hero info panel displayed on adventure map (VCMI's CInfoBar pattern).
    /// Shows selected hero's portrait, stats, and army when a hero is selected.
    /// Based on VCMI's client/adventureMap/CInfoBar.h/cpp (192x192px compact view)
    /// </summary>
    public class HeroPanelUI : MonoBehaviour
    {
        [Header("Event Channels")]
        [SerializeField] private UIEventChannel uiEvents;
        [SerializeField] private GameEventChannel gameEvents;

        [Header("UI References - Hero Info")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Image heroPortrait;
        [SerializeField] private TextMeshProUGUI heroNameText;
        [SerializeField] private TextMeshProUGUI heroLevelClassText;

        [Header("UI References - Primary Skills")]
        [SerializeField] private TextMeshProUGUI attackText;
        [SerializeField] private TextMeshProUGUI defenseText;
        [SerializeField] private TextMeshProUGUI spellPowerText;
        [SerializeField] private TextMeshProUGUI knowledgeText;

        [Header("UI References - Stats")]
        [SerializeField] private TextMeshProUGUI experienceText;
        [SerializeField] private TextMeshProUGUI manaText;
        [SerializeField] private TextMeshProUGUI movementText;
        [SerializeField] private TextMeshProUGUI moraleText;
        [SerializeField] private TextMeshProUGUI luckText;

        [Header("UI References - Army Garrison (7 slots)")]
        [SerializeField] private GarrisonSlotUI[] garrisonSlots = new GarrisonSlotUI[7];

        [Header("Settings")]
        [SerializeField] private bool showOnStart = false;

        private Hero currentHero;

        private void Awake()
        {
            // Hide panel by default
            if (!showOnStart && panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (uiEvents != null)
            {
                uiEvents.OnHeroSelected += HandleHeroSelected;
                uiEvents.OnSelectionCleared += HandleSelectionCleared;
                uiEvents.OnShowHeroInfo += HandleShowHeroInfo;
            }

            if (gameEvents != null)
            {
                gameEvents.OnHeroMoved += HandleHeroMoved;
                gameEvents.OnHeroLeveledUp += HandleHeroLeveledUp;
            }
        }

        private void OnDisable()
        {
            if (uiEvents != null)
            {
                uiEvents.OnHeroSelected -= HandleHeroSelected;
                uiEvents.OnSelectionCleared -= HandleSelectionCleared;
                uiEvents.OnShowHeroInfo -= HandleShowHeroInfo;
            }

            if (gameEvents != null)
            {
                gameEvents.OnHeroMoved -= HandleHeroMoved;
                gameEvents.OnHeroLeveledUp -= HandleHeroLeveledUp;
            }
        }

        // ===== Event Handlers =====

        private void HandleHeroSelected(Hero hero)
        {
            ShowHeroInfo(hero);
        }

        private void HandleSelectionCleared()
        {
            HidePanel();
        }

        private void HandleShowHeroInfo(Hero hero)
        {
            ShowHeroInfo(hero);
        }

        private void HandleHeroMoved(int heroId, Position newPosition)
        {
            // Refresh if this is the currently displayed hero
            if (currentHero != null && currentHero.Id == heroId)
            {
                RefreshHeroInfo();
            }
        }

        private void HandleHeroLeveledUp(int heroId, int newLevel)
        {
            // Refresh if this is the currently displayed hero
            if (currentHero != null && currentHero.Id == heroId)
            {
                RefreshHeroInfo();
            }
        }

        // ===== Public Methods =====

        public void ShowHeroInfo(Hero hero)
        {
            if (hero == null)
            {
                HidePanel();
                return;
            }

            currentHero = hero;

            // Show panel
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            RefreshHeroInfo();
        }

        public void HidePanel()
        {
            currentHero = null;

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        // ===== Private Methods =====

        private void RefreshHeroInfo()
        {
            if (currentHero == null)
                return;

            // Update hero name and level/class
            if (heroNameText != null)
            {
                var displayName = !string.IsNullOrEmpty(currentHero.CustomName)
                    ? currentHero.CustomName
                    : $"Hero {currentHero.Id}";
                heroNameText.text = displayName;
            }

            if (heroLevelClassText != null)
            {
                // TODO: Get hero class name from HeroTypeData when available
                heroLevelClassText.text = $"Level {currentHero.Level}";
            }

            // Update primary skills (VCMI pattern: Attack/Defense/Power/Knowledge)
            if (attackText != null)
                attackText.text = currentHero.Attack.ToString();
            if (defenseText != null)
                defenseText.text = currentHero.Defense.ToString();
            if (spellPowerText != null)
                spellPowerText.text = currentHero.SpellPower.ToString();
            if (knowledgeText != null)
                knowledgeText.text = currentHero.Knowledge.ToString();

            // Update stats
            if (experienceText != null)
            {
                experienceText.text = currentHero.Experience.ToString();
            }

            if (manaText != null)
            {
                var maxMana = CalculateMaxMana(currentHero);
                manaText.text = $"{currentHero.Mana}/{maxMana}";
            }

            if (movementText != null)
            {
                var maxMovement = CalculateMaxMovement(currentHero);
                movementText.text = $"{currentHero.Movement}/{maxMovement}";
            }

            // Morale and Luck (placeholder - TODO: implement proper calculation)
            if (moraleText != null)
            {
                var morale = CalculateMorale(currentHero);
                moraleText.text = FormatMoraleDisplay(morale);
            }

            if (luckText != null)
            {
                var luck = CalculateLuck(currentHero);
                luckText.text = FormatLuckDisplay(luck);
            }

            // Update army garrison
            RefreshGarrison();

            // Update hero portrait (if sprite available)
            if (heroPortrait != null)
            {
                // TODO: Load hero portrait sprite when asset system is ready
                // heroPortrait.sprite = HeroDatabase.GetPortrait(currentHero.HeroTypeId);
            }
        }

        private void RefreshGarrison()
        {
            if (currentHero?.Army == null)
                return;

            for (var i = 0; i < garrisonSlots.Length; i++)
            {
                if (garrisonSlots[i] == null)
                    continue;

                var stack = currentHero.Army.GetSlot(i);
                if (stack != null)
                {
                    garrisonSlots[i].SetStack(stack);
                    garrisonSlots[i].Show();
                }
                else
                {
                    garrisonSlots[i].Hide();
                }
            }
        }

        // ===== Calculation Helpers (VCMI formulas) =====

        private int CalculateMaxMana(Hero hero)
        {
            // VCMI formula: base 10 + (Knowledge * 10)
            return 10 + (hero.Knowledge * 10);
        }

        private int CalculateMaxMovement(Hero hero)
        {
            // VCMI base movement: 1500 for land heroes
            // TODO: Add movement bonuses from artifacts, skills, terrain
            return 1500;
        }

        private int CalculateMorale(Hero hero)
        {
            // TODO: Implement VCMI morale calculation
            // - Base: 0
            // - Different creature factions in army: -1 per faction over 1
            // - Artifacts/skills/buildings: +/- bonuses
            // Range: -3 to +3
            return 0;
        }

        private int CalculateLuck(Hero hero)
        {
            // TODO: Implement VCMI luck calculation
            // - Base: 0
            // - Artifacts/skills/buildings: +/- bonuses
            // Range: -3 to +3
            return 0;
        }

        private string FormatMoraleDisplay(int morale)
        {
            if (morale > 0) return $"+{morale}";
            if (morale < 0) return morale.ToString();
            return "0";
        }

        private string FormatLuckDisplay(int luck)
        {
            if (luck > 0) return $"+{luck}";
            if (luck < 0) return luck.ToString();
            return "0";
        }

        // ===== Properties =====

        public Hero CurrentHero => currentHero;
        public bool IsVisible => panelRoot != null && panelRoot.activeSelf;
    }

    /// <summary>
    /// Single garrison slot UI component for army display.
    /// Shows creature icon and stack count.
    /// Based on VCMI's CGarrisonSlot widget.
    /// </summary>
    [System.Serializable]
    public class GarrisonSlotUI
    {
        [SerializeField] private GameObject slotRoot;
        [SerializeField] private Image creatureIcon;
        [SerializeField] private TextMeshProUGUI countText;

        private CreatureStack currentStack;

        public void SetStack(CreatureStack stack)
        {
            currentStack = stack;

            if (stack == null)
            {
                Hide();
                return;
            }

            // Update count
            if (countText != null)
            {
                countText.text = stack.Count.ToString();
            }

            // Update creature icon
            if (creatureIcon != null)
            {
                // TODO: Load creature sprite when asset system is ready
                // creatureIcon.sprite = CreatureDatabase.GetIcon(stack.CreatureId);
            }
        }

        public void Show()
        {
            if (slotRoot != null)
                slotRoot.SetActive(true);
        }

        public void Hide()
        {
            if (slotRoot != null)
                slotRoot.SetActive(false);
        }

        public CreatureStack CurrentStack => currentStack;
    }
}
