using UnityEngine;
using RealmsOfEldor.Core;
using RealmsOfEldor.Core.Battle;
using RealmsOfEldor.Database;

namespace RealmsOfEldor.Controllers.Battle
{
    /// <summary>
    /// Main battle controller - orchestrates battle flow.
    /// Based on VCMI's BattleFlowProcessor.
    /// MonoBehaviour wrapper for BattleState (pure C# logic).
    /// Phase 5A: Basic initialization and state management.
    /// </summary>
    public class BattleController : MonoBehaviour
    {
        [Header("Battle Settings")]
        [SerializeField] private bool autoStartBattle = true;

        [Header("Testing")]
        [SerializeField] private Hero testAttacker;
        [SerializeField] private Hero testDefender;

        // Battle state (pure C#)
        private BattleState battleState;

        // Properties
        public BattleState State => battleState;
        public bool IsBattleActive => battleState != null && !battleState.IsFinished;

        #region Unity Lifecycle

        private void Start()
        {
            if (autoStartBattle)
            {
                InitializeTestBattle();
            }
        }

        #endregion

        #region Battle Initialization

        /// <summary>
        /// Initialize a new battle between two heroes.
        /// </summary>
        public void StartBattle(Hero attacker, Hero defender)
        {
            if (attacker == null || defender == null)
            {
                Debug.LogError("Cannot start battle: attacker or defender is null");
                return;
            }

            Debug.Log($"Starting battle: {attacker.CustomName} vs {defender.CustomName}");

            // Create battle state
            battleState = new BattleState(attacker, defender);

            // Initialize battlefield
            InitializeBattlefield();

            // Place units on battlefield
            PlaceUnits(attacker.Army, BattleSide.Attacker);
            PlaceUnits(defender.Army, BattleSide.Defender);

            // Start first round
            battleState.CurrentPhase = BattlePhase.NORMAL;
            battleState.StartNewRound();

            Debug.Log($"Battle initialized: {battleState.GetBattleSummary()}");
        }

        /// <summary>
        /// Initialize test battle with dummy data.
        /// </summary>
        [ContextMenu("Initialize Test Battle")]
        private void InitializeTestBattle()
        {
            // Create test heroes if not assigned
            if (testAttacker == null)
            {
                testAttacker = GameStateManager.Instance.State.AddHero(typeId: 0, owner: 0, position: new Position(0, 0));
                // testAttacker = new Hero(1, 0);
                testAttacker.CustomName = "Test Attacker";
            }

            if (testDefender == null)
            {
                testDefender = GameStateManager.Instance.State.AddHero(typeId: 1, owner: 1, position: new Position(0, 0));
                // testDefender = new Hero(2, 1);
                testDefender.CustomName = "Test Defender";
            }

            // Start battle
            StartBattle(testAttacker, testDefender);
        }

        /// <summary>
        /// Set up battlefield terrain and obstacles.
        /// </summary>
        private void InitializeBattlefield()
        {
            // TODO: Add obstacles based on battlefield type
            // For now, just set field type
            battleState.FieldType = BattleFieldType.GRASS;

            Debug.Log($"Battlefield initialized: {battleState.FieldType}");
        }

        /// <summary>
        /// Place army units on battlefield.
        /// Based on VCMI's starting positions.
        /// </summary>
        private void PlaceUnits(Army army, BattleSide side)
        {
            if (army == null)
            {
                Debug.LogWarning($"No army for {side} side");
                return;
            }

            // VCMI starting positions:
            // Attacker: columns 1-2 (left side)
            // Defender: columns 14-15 (right side)
            var startColumn = side == BattleSide.Attacker ? 1 : 14;
            const int row = 5;  // Middle row

            // Place up to 7 stacks (HOMM3 army slots)
            for (var slotIndex = 0; slotIndex < 7; slotIndex++)
            {
                var stack = army.GetSlot(slotIndex);
                if (stack is not { Count: > 0 }) continue;
                // Calculate position (stagger rows for visibility)
                var posRow = row + (slotIndex % 3) - 1;  // -1, 0, +1
                var posCol = startColumn + (slotIndex / 3);

                var position = new BattleHex(posCol, posRow);

                // Use army's existing CreatureStack
                var creatureType = CreatureDatabase.Instance.GetCreature(stack.CreatureId);
                var unit = battleState.AddUnit(creatureType, stack.Count, side, slotIndex, position);

                if (unit != null)
                {
                    Debug.Log($"Placed {side} unit {slotIndex}: {unit}");
                }
            }
        }

        #endregion

        #region Battle Flow

        /// <summary>
        /// Process one battle round.
        /// Units act in initiative order until round ends.
        /// </summary>
        public void ProcessRound()
        {
            if (!IsBattleActive)
            {
                Debug.LogWarning("Cannot process round: battle not active");
                return;
            }

            Debug.Log($"=== Round {battleState.CurrentRound} Begin ===");

            // Process all units in turn order
            while (battleState.HasRemainingTurns())
            {
                var activeUnit = battleState.GetNextUnit();
                if (activeUnit == null)
                    break;

                Debug.Log($"Turn: Unit {activeUnit.UnitId} ({activeUnit.CreatureType?.creatureName ?? "Unknown"}) - Initiative: {activeUnit.Initiative}");

                // TODO Phase 5C: Get action from AI or player
                // For now, units just "act" and end their turn
                ProcessUnitTurn(activeUnit);

                // Check battle end
                if (battleState.CheckBattleEnd())
                {
                    Debug.Log($"Battle ended! Winner: {battleState.WinningSide}");
                    return;
                }
            }

            Debug.Log($"=== Round {battleState.CurrentRound} End ===");

            // Start next round
            battleState.StartNewRound();
        }

        /// <summary>
        /// Process one unit's turn.
        /// Placeholder - will get action from AI/player in Phase 5C.
        /// </summary>
        private void ProcessUnitTurn(BattleUnit unit)
        {
            // TODO Phase 5C: Get action from AI or player input
            // For now, just log and end turn
            Debug.Log($"  Unit {unit.UnitId} acts (placeholder)");
        }

        /// <summary>
        /// Execute wait action for current unit.
        /// Moves unit to later in turn queue.
        /// </summary>
        public void WaitCurrentUnit()
        {
            if (!IsBattleActive)
            {
                Debug.LogWarning("Cannot wait: battle not active");
                return;
            }

            battleState.WaitCurrentUnit();
            Debug.Log($"Unit {battleState.ActiveUnitId} is waiting");
        }

        /// <summary>
        /// Get turn order for UI display.
        /// </summary>
        public int[] GetTurnOrder()
        {
            if (battleState == null)
                return new int[0];

            return battleState.GetTurnOrder().ToArray();
        }

        /// <summary>
        /// Execute a battle action.
        /// Placeholder for Phase 5C (Combat System implementation).
        /// </summary>
        public void ExecuteAction(BattleAction action)
        {
            if (!IsBattleActive || action == null || !action.IsValid())
            {
                Debug.LogWarning($"Cannot execute action: battle={IsBattleActive}, action valid={action?.IsValid()}");
                return;
            }

            // TODO Phase 5C: Implement action execution
            Debug.Log($"ExecuteAction() - {action} - Not yet implemented (Phase 5C)");
        }

        /// <summary>
        /// End the battle.
        /// </summary>
        public void EndBattle(BattleSide? winner)
        {
            if (battleState == null)
                return;

            battleState.EndBattle(winner);

            var winnerText = winner.HasValue ? winner.Value.ToString() : "Draw";
            Debug.Log($"Battle ended. Winner: {winnerText}");
        }

        #endregion

        #region Debug Helpers

        [ContextMenu("Print Battle State")]
        private void PrintBattleState()
        {
            if (battleState == null)
            {
                Debug.Log("No active battle");
                return;
            }

            Debug.Log(battleState.GetBattleSummary());
            Debug.Log($"Phase: {battleState.CurrentPhase}");
            Debug.Log($"Active Unit: {battleState.ActiveUnitId}");

            Debug.Log("\nAttacker Units:");
            foreach (var unit in battleState.GetUnitsForSide(BattleSide.Attacker))
            {
                Debug.Log($"  {unit}");
            }

            Debug.Log("\nDefender Units:");
            foreach (var unit in battleState.GetUnitsForSide(BattleSide.Defender))
            {
                Debug.Log($"  {unit}");
            }

            Debug.Log("\nTurn Order:");
            var turnOrder = battleState.GetTurnOrder();
            for (var i = 0; i < turnOrder.Count; i++)
            {
                var unit = battleState.GetUnit(turnOrder[i]);
                Debug.Log($"  {i + 1}. Unit {unit.UnitId} - Initiative {unit.Initiative}");
            }
        }

        [ContextMenu("Process One Round")]
        private void DebugProcessRound()
        {
            if (battleState == null)
            {
                Debug.LogWarning("No active battle to process");
                return;
            }

            ProcessRound();
        }

        #endregion
    }
}
