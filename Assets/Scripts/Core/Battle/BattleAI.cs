using System.Collections.Generic;
using System.Linq;

namespace RealmsOfEldor.Core.Battle
{
    /// <summary>
    /// Simple battle AI for automatic combat.
    /// Based on VCMI's BattleEvaluator.
    /// Phase 5E: Battle AI implementation.
    /// </summary>
    public class BattleAI
    {
        private readonly BattleState battleState;

        public BattleAI(BattleState battleState)
        {
            this.battleState = battleState;
        }

        /// <summary>
        /// Select the best action for the active unit.
        /// Main AI decision method based on VCMI's selectStackAction().
        /// </summary>
        public BattleAction SelectAction(BattleUnit activeUnit)
        {
            if (activeUnit == null || !activeUnit.IsAlive)
                return null;

            // Get all enemy units
            var enemies = battleState.GetUnitsForSide(
                activeUnit.Side == BattleSide.Attacker ? BattleSide.Defender : BattleSide.Attacker
            ).Where(u => u.IsAlive).ToList();

            if (enemies.Count == 0)
                return null;

            // Evaluate all attack possibilities
            var possibilities = EvaluateAttackPossibilities(activeUnit, enemies);

            if (possibilities.Count == 0)
            {
                // No valid attacks - wait
                return BattleAction.MakeWait(activeUnit);
            }

            // Pick best scoring option
            var best = possibilities.OrderByDescending(p => p.Score).First();

            // Create action based on best possibility
            if (best.IsShooting)
            {
                return BattleAction.MakeShoot(activeUnit, best.Defender);
            }
            else
            {
                // For melee, we need to move adjacent to target first
                // For now, just attack if already adjacent, otherwise wait
                var distance = BattleHex.GetDistance(activeUnit.Position, best.Defender.Position);
                if (distance <= 1)
                {
                    return BattleAction.MakeMeleeAttack(activeUnit, best.Defender, activeUnit.Position);
                }
                else
                {
                    // TODO: Implement pathfinding to move towards target
                    // For now, just wait
                    return BattleAction.MakeWait(activeUnit);
                }
            }
        }

        /// <summary>
        /// Evaluate all possible attacks for the active unit.
        /// </summary>
        private List<AttackPossibility> EvaluateAttackPossibilities(BattleUnit attacker, List<BattleUnit> enemies)
        {
            var possibilities = new List<AttackPossibility>();

            foreach (var enemy in enemies)
            {
                // Check if can shoot
                if (attacker.CanShoot)
                {
                    var shootPossibility = AttackPossibility.Evaluate(
                        attacker,
                        enemy,
                        attacker.Position, // Shoot from current position
                        isShooting: true,
                        battleState
                    );

                    if (shootPossibility != null)
                        possibilities.Add(shootPossibility);
                }

                // Check if can melee attack (must be adjacent)
                var distance = BattleHex.GetDistance(attacker.Position, enemy.Position);
                if (distance <= 1)
                {
                    var meleePossibility = AttackPossibility.Evaluate(
                        attacker,
                        enemy,
                        attacker.Position,
                        isShooting: false,
                        battleState
                    );

                    if (meleePossibility != null)
                        possibilities.Add(meleePossibility);
                }
            }

            return possibilities;
        }

        /// <summary>
        /// Get the best target for the active unit (highest damage potential).
        /// Simple helper method.
        /// </summary>
        public BattleUnit GetBestTarget(BattleUnit activeUnit)
        {
            var action = SelectAction(activeUnit);

            return action?.TargetUnitId > -1
                ? battleState.GetUnit(action.TargetUnitId)
                : null;
        }
    }
}
