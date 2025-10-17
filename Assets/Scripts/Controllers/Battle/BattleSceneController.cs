using UnityEngine;
using Cysharp.Threading.Tasks;
using RealmsOfEldor.Core;
using RealmsOfEldor.Core.Battle;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.Controllers.Battle
{
    /// <summary>
    /// Main controller for the battle scene - wires visual layer to battle logic.
    /// Integrates BattleController (logic) with visual renderers and animations.
    /// Based on VCMI's BattleInterface orchestration pattern.
    /// Phase 7B: Added animation integration.
    /// </summary>
    public class BattleSceneController : MonoBehaviour
    {
        [Header("Controllers")]
        [SerializeField] private BattleController battleController;
        [SerializeField] private BattleFieldRenderer fieldRenderer;
        [SerializeField] private BattleStackRenderer stackRenderer;
        [SerializeField] private BattleUIPanel uiPanel;
        [SerializeField] private BattleAnimator battleAnimator;

        [Header("Settings")]
        [SerializeField] private bool autoProcessRounds = false;
        [SerializeField] private float roundDelay = 2f;
        [SerializeField] private bool enableAnimations = true;

        private bool isProcessingRound = false;
        private float timeSinceLastRound = 0f;

        // Track previous state for animation detection
        private System.Collections.Generic.Dictionary<int, BattleHex> previousPositions =
            new System.Collections.Generic.Dictionary<int, BattleHex>();
        private System.Collections.Generic.Dictionary<int, int> previousHealths =
            new System.Collections.Generic.Dictionary<int, int>();

        void Awake()
        {
            // Auto-find components if not assigned
            if (battleController == null)
                battleController = GetComponent<BattleController>();

            if (fieldRenderer == null)
                fieldRenderer = FindFirstObjectByType<BattleFieldRenderer>();

            if (stackRenderer == null)
                stackRenderer = FindFirstObjectByType<BattleStackRenderer>();

            if (uiPanel == null)
                uiPanel = FindFirstObjectByType<BattleUIPanel>();

            if (battleAnimator == null)
                battleAnimator = FindFirstObjectByType<BattleAnimator>();

            // Validate required components
            if (battleController == null)
            {
                Debug.LogError("BattleSceneController: BattleController not found!");
            }

            if (enableAnimations && battleAnimator == null)
            {
                Debug.LogWarning("BattleSceneController: BattleAnimator not found - animations disabled");
                enableAnimations = false;
            }
        }

        void Start()
        {
            // Wire up UI events
            if (uiPanel != null)
            {
                uiPanel.OnAttackClicked += HandleAttackClicked;
                uiPanel.OnDefendClicked += HandleDefendClicked;
                uiPanel.OnWaitClicked += HandleWaitClicked;
                uiPanel.OnAutoClicked += HandleAutoClicked;
                uiPanel.OnSpellbookClicked += HandleSpellbookClicked;
                uiPanel.OnRetreatClicked += HandleRetreatClicked;
                uiPanel.OnSurrenderClicked += HandleSurrenderClicked;
            }

            // Wait for battle to initialize
            Invoke(nameof(InitializeVisuals), 0.5f);
        }

        void Update()
        {
            // Auto-process rounds if enabled
            if (autoProcessRounds && battleController != null && battleController.IsBattleActive)
            {
                timeSinceLastRound += Time.deltaTime;

                if (timeSinceLastRound >= roundDelay && !isProcessingRound)
                {
                    ProcessRound();
                    timeSinceLastRound = 0f;
                }
            }

            // Handle keyboard shortcuts
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ProcessRound();
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                fieldRenderer?.ToggleHexGrid();
            }
        }

        /// <summary>
        /// Initialize visual elements based on battle state.
        /// </summary>
        private void InitializeVisuals()
        {
            if (battleController == null || battleController.State == null)
            {
                Debug.LogWarning("BattleSceneController: Cannot initialize visuals - no battle state");
                return;
            }

            var battleState = battleController.State;

            // Set battlefield background
            if (fieldRenderer != null)
            {
                var terrainType = ConvertFieldTypeToTerrain(battleState.FieldType);
                fieldRenderer.SetBackground(terrainType);
            }

            // Spawn all stacks
            if (stackRenderer != null)
            {
                var allStacks = battleState.GetAllUnits();
                var stackList = new System.Collections.Generic.List<BattleStack>();

                foreach (var unit in allStacks)
                {
                    // Create BattleStack from BattleUnit (temporary adapter)
                    var stack = new BattleStack
                    {
                        Id = unit.UnitId,
                        CreatureId = unit.CreatureType?.creatureId ?? 0,
                        Count = unit.Count,
                        Side = unit.Side,
                        Position = new BattleHex(unit.Position.X, unit.Position.Y)
                    };

                    stackList.Add(stack);
                }

                stackRenderer.SpawnStacks(stackList);
            }

            // Initialize UI
            if (uiPanel != null)
            {
                uiPanel.SetTurnInfo(battleState.CurrentRound, battleState.CurrentPhase.ToString());
                uiPanel.ClearLog();
                uiPanel.LogMessage("Battle started!");

                // Set hero names
                // TODO: Get hero names from battle state
                uiPanel.SetLeftHero("Attacker");
                uiPanel.SetRightHero("Defender");
            }

            Debug.Log("BattleSceneController: Visuals initialized");
        }

        /// <summary>
        /// Processes one battle round and updates visuals.
        /// Phase 7B: Now async with animation support.
        /// </summary>
        [ContextMenu("Process One Round")]
        public void ProcessRound()
        {
            if (!battleController.IsBattleActive)
            {
                Debug.Log("BattleSceneController: Battle not active");
                return;
            }

            ProcessRoundAsync().Forget();
        }

        /// <summary>
        /// Async version of ProcessRound with animation support.
        /// </summary>
        private async UniTask ProcessRoundAsync()
        {
            if (isProcessingRound)
            {
                Debug.LogWarning("BattleSceneController: Already processing round");
                return;
            }

            isProcessingRound = true;

            // Capture state before processing
            CaptureStateSnapshot();

            // Process logic
            battleController.ProcessRound();

            // Detect changes and play animations
            if (enableAnimations)
            {
                await DetectAndAnimateChanges();
            }

            // Update visuals
            UpdateVisuals();

            // Check for battle end
            if (battleController.State.IsFinished)
            {
                HandleBattleEnd();
            }

            isProcessingRound = false;
        }

        /// <summary>
        /// Updates all visual elements based on current battle state.
        /// </summary>
        private void UpdateVisuals()
        {
            if (battleController?.State == null)
                return;

            var battleState = battleController.State;

            // Update UI turn info
            if (uiPanel != null)
            {
                uiPanel.SetTurnInfo(battleState.CurrentRound, battleState.CurrentPhase.ToString());
            }

            // Update stack visuals (amounts, positions)
            if (stackRenderer != null)
            {
                foreach (var unit in battleState.GetAllUnits())
                {
                    stackRenderer.UpdateStackAmount(unit.UnitId);

                    // Check if unit moved (compare cached position)
                    // TODO: Track position changes and animate movement
                }
            }

            // Remove dead stacks
            if (stackRenderer != null)
            {
                foreach (var unit in battleState.GetAllUnits())
                {
                    if (!unit.IsAlive)
                    {
                        stackRenderer.RemoveStack(unit.UnitId);
                    }
                }
            }
        }

        /// <summary>
        /// Handles battle end condition.
        /// </summary>
        private void HandleBattleEnd()
        {
            if (battleController?.State == null)
                return;

            var winner = battleController.State.WinningSide;

            if (uiPanel != null)
            {
                if (winner.HasValue)
                {
                    uiPanel.ShowVictory(winner.Value.ToString());
                    uiPanel.LogMessage($"Victory to {winner.Value}!");
                }
                else
                {
                    uiPanel.LogMessage("Battle ended in a draw");
                }
            }

            Debug.Log($"BattleSceneController: Battle ended - Winner: {winner}");
        }

        #region UI Event Handlers

        private void HandleAttackClicked()
        {
            Debug.Log("BattleSceneController: Attack clicked");
            // TODO: Enter attack targeting mode
            uiPanel?.LogMessage("Select target to attack");
        }

        private void HandleDefendClicked()
        {
            Debug.Log("BattleSceneController: Defend clicked");
            // TODO: Set active unit to defend mode
            uiPanel?.LogMessage("Unit defending");
        }

        private void HandleWaitClicked()
        {
            Debug.Log("BattleSceneController: Wait clicked");
            battleController?.WaitCurrentUnit();
            uiPanel?.LogMessage("Unit waiting");
            UpdateVisuals();
        }

        private void HandleAutoClicked()
        {
            Debug.Log("BattleSceneController: Auto combat toggled");
            autoProcessRounds = !autoProcessRounds;
            uiPanel?.LogMessage(autoProcessRounds ? "Auto combat enabled" : "Auto combat disabled");
        }

        private void HandleSpellbookClicked()
        {
            Debug.Log("BattleSceneController: Spellbook clicked");
            // TODO: Open spellbook UI
            uiPanel?.LogMessage("Spellbook not yet implemented");
        }

        private void HandleRetreatClicked()
        {
            Debug.Log("BattleSceneController: Retreat clicked");
            // TODO: Confirm and execute retreat
            uiPanel?.LogMessage("Retreat not yet implemented");
        }

        private void HandleSurrenderClicked()
        {
            Debug.Log("BattleSceneController: Surrender clicked");
            // TODO: Confirm and execute surrender
            uiPanel?.LogMessage("Surrender not yet implemented");
        }

        #endregion

        #region Animation Support (Phase 7B)

        /// <summary>
        /// Captures current state snapshot for change detection.
        /// </summary>
        private void CaptureStateSnapshot()
        {
            if (battleController?.State == null)
                return;

            previousPositions.Clear();
            previousHealths.Clear();

            foreach (var unit in battleController.State.GetAllUnits())
            {
                previousPositions[unit.UnitId] = unit.Position;
                previousHealths[unit.UnitId] = unit.TotalHealth;
            }
        }

        /// <summary>
        /// Detects changes between snapshots and triggers animations.
        /// </summary>
        private async UniTask DetectAndAnimateChanges()
        {
            if (battleController?.State == null || battleAnimator == null)
                return;

            var currentUnits = battleController.State.GetAllUnits();

            foreach (var unit in currentUnits)
            {
                var unitId = unit.UnitId;

                // Check for movement
                if (previousPositions.TryGetValue(unitId, out var oldPos))
                {
                    if (oldPos.X != unit.Position.X || oldPos.Y != unit.Position.Y)
                    {
                        var fromHex = new Vector2Int(oldPos.X, oldPos.Y);
                        var toHex = new Vector2Int(unit.Position.X, unit.Position.Y);
                        battleAnimator.AnimateMovement(unitId, fromHex, toHex);
                    }
                }

                // Check for damage
                if (previousHealths.TryGetValue(unitId, out var oldHealth))
                {
                    if (oldHealth > unit.TotalHealth)
                    {
                        var damage = oldHealth - unit.TotalHealth;
                        // Damage animation is handled by attack animation
                    }
                }

                // Check for death
                if (!unit.IsAlive && previousHealths.ContainsKey(unitId))
                {
                    battleAnimator.AnimateDeath(unitId);
                }
            }

            // Wait for all animations to complete
            await battleAnimator.WaitForAnimations();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Converts BattleFieldType to TerrainType for background rendering.
        /// </summary>
        private TerrainType ConvertFieldTypeToTerrain(BattleFieldType fieldType)
        {
            return fieldType switch
            {
                BattleFieldType.GRASS => TerrainType.Grass,
                BattleFieldType.DIRT => TerrainType.Dirt,
                BattleFieldType.SAND => TerrainType.Sand,
                BattleFieldType.SWAMP => TerrainType.Swamp,
                BattleFieldType.ROUGH => TerrainType.Rough,
                BattleFieldType.SNOW => TerrainType.Snow,
                _ => TerrainType.Grass
            };
        }

        #endregion

        void OnDestroy()
        {
            // Unsubscribe from UI events
            if (uiPanel != null)
            {
                uiPanel.OnAttackClicked -= HandleAttackClicked;
                uiPanel.OnDefendClicked -= HandleDefendClicked;
                uiPanel.OnWaitClicked -= HandleWaitClicked;
                uiPanel.OnAutoClicked -= HandleAutoClicked;
                uiPanel.OnSpellbookClicked -= HandleSpellbookClicked;
                uiPanel.OnRetreatClicked -= HandleRetreatClicked;
                uiPanel.OnSurrenderClicked -= HandleSurrenderClicked;
            }
        }
    }
}
