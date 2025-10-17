using NUnit.Framework;
using RealmsOfEldor.Core;
using RealmsOfEldor.Core.Battle;
using RealmsOfEldor.Data;
using UnityEngine;

namespace RealmsOfEldor.Tests
{
    /// <summary>
    /// Tests for Battle AI system.
    /// Phase 5E implementation.
    /// </summary>
    [TestFixture]
    public class BattleAITests
    {
        private BattleState battleState;
        private BattleAI ai;
        private Hero attacker;
        private Hero defender;
        private CreatureData meleeCreature;
        private CreatureData rangedCreature;

        [SetUp]
        public void Setup()
        {
            // Create test heroes
            attacker = new Hero { Id = 1, Owner = 0, CustomName = "Attacker" };
            defender = new Hero { Id = 2, Owner = 1, CustomName = "Defender" };

            // Create melee creature
            meleeCreature = ScriptableObject.CreateInstance<CreatureData>();
            meleeCreature.creatureId = 1;
            meleeCreature.creatureName = "Melee";
            meleeCreature.attack = 5;
            meleeCreature.defense = 5;
            meleeCreature.minDamage = 2;
            meleeCreature.maxDamage = 4;
            meleeCreature.hitPoints = 20;
            meleeCreature.speed = 5;
            meleeCreature.shots = 0;

            // Create ranged creature
            rangedCreature = ScriptableObject.CreateInstance<CreatureData>();
            rangedCreature.creatureId = 2;
            rangedCreature.creatureName = "Archer";
            rangedCreature.attack = 5;
            rangedCreature.defense = 3;
            rangedCreature.minDamage = 2;
            rangedCreature.maxDamage = 4;
            rangedCreature.hitPoints = 15;
            rangedCreature.speed = 4;
            rangedCreature.shots = 12;

            // Create battle
            battleState = new BattleState(attacker, defender);
            ai = new BattleAI(battleState);
        }

        #region Attack Possibility Tests

        [Test]
        public void AttackPossibility_CalculatesCorrectDamage()
        {
            // Arrange
            var attackerUnit = battleState.AddUnit(meleeCreature, 10, BattleSide.Attacker, 0, new BattleHex(1, 5));
            var defenderUnit = battleState.AddUnit(meleeCreature, 10, BattleSide.Defender, 0, new BattleHex(2, 5));

            // Act
            var possibility = AttackPossibility.Evaluate(
                attackerUnit,
                defenderUnit,
                new BattleHex(1, 5),
                isShooting: false,
                battleState
            );

            // Assert
            Assert.IsNotNull(possibility);
            Assert.Greater(possibility.DamageToDefender, 0, "Should calculate damage to defender");
            Assert.Greater(possibility.RetaliationDamage, 0, "Should calculate retaliation damage");
        }

        [Test]
        public void AttackPossibility_ScoresFavorableTrade()
        {
            // Arrange: Strong attacker vs weak defender
            var strongCreature = ScriptableObject.CreateInstance<CreatureData>();
            strongCreature.creatureId = 3;
            strongCreature.creatureName = "Strong";
            strongCreature.attack = 10;
            strongCreature.defense = 10;
            strongCreature.minDamage = 10;
            strongCreature.maxDamage = 15;
            strongCreature.hitPoints = 50;
            strongCreature.speed = 5;

            var attackerUnit = battleState.AddUnit(strongCreature, 10, BattleSide.Attacker, 0, new BattleHex(1, 5));
            var defenderUnit = battleState.AddUnit(meleeCreature, 5, BattleSide.Defender, 0, new BattleHex(2, 5));

            // Act
            var possibility = AttackPossibility.Evaluate(
                attackerUnit,
                defenderUnit,
                new BattleHex(1, 5),
                isShooting: false,
                battleState
            );

            // Assert
            Assert.IsNotNull(possibility);
            Assert.Greater(possibility.Score, 0, "Favorable trade should have positive score");
            Assert.Greater(possibility.DamageToDefender, possibility.RetaliationDamage,
                "Should deal more damage than taking");
        }

        [Test]
        public void AttackPossibility_ShootingHasNoRetaliation()
        {
            // Arrange
            var attackerUnit = battleState.AddUnit(rangedCreature, 10, BattleSide.Attacker, 0, new BattleHex(1, 5));
            var defenderUnit = battleState.AddUnit(meleeCreature, 10, BattleSide.Defender, 0, new BattleHex(14, 5));

            // Act
            var possibility = AttackPossibility.Evaluate(
                attackerUnit,
                defenderUnit,
                new BattleHex(1, 5),
                isShooting: true,
                battleState
            );

            // Assert
            Assert.IsNotNull(possibility);
            Assert.AreEqual(0, possibility.RetaliationDamage, "Shooting should have no retaliation");
            Assert.Greater(possibility.Score, 0, "Shooting with no retaliation should have positive score");
        }

        #endregion

        #region AI Action Selection Tests

        [Test]
        public void AI_SelectsShootActionForRangedUnit()
        {
            // Arrange
            var archerUnit = battleState.AddUnit(rangedCreature, 10, BattleSide.Attacker, 0, new BattleHex(1, 5));
            var targetUnit = battleState.AddUnit(meleeCreature, 10, BattleSide.Defender, 0, new BattleHex(14, 5));

            // Act
            var action = ai.SelectAction(archerUnit);

            // Assert
            Assert.IsNotNull(action);
            Assert.AreEqual(BattleActionType.Shoot, action.Type);
            Assert.AreEqual(targetUnit.UnitId, action.TargetUnitId.Value);
        }

        [Test]
        public void AI_SelectsAttackActionForAdjacentMeleeUnit()
        {
            // Arrange
            var meleeUnit = battleState.AddUnit(meleeCreature, 10, BattleSide.Attacker, 0, new BattleHex(1, 5));
            var targetUnit = battleState.AddUnit(meleeCreature, 10, BattleSide.Defender, 0, new BattleHex(2, 5));

            // Act
            var action = ai.SelectAction(meleeUnit);

            // Assert
            Assert.IsNotNull(action);
            Assert.AreEqual(BattleActionType.Attack, action.Type);
            Assert.AreEqual(targetUnit.UnitId, action.TargetUnitId.Value);
        }

        [Test]
        public void AI_SelectsWaitWhenNoValidAttacks()
        {
            // Arrange: Melee unit far from enemy, can't reach
            var meleeUnit = battleState.AddUnit(meleeCreature, 10, BattleSide.Attacker, 0, new BattleHex(1, 5));
            var targetUnit = battleState.AddUnit(meleeCreature, 10, BattleSide.Defender, 0, new BattleHex(14, 5));

            // Act
            var action = ai.SelectAction(meleeUnit);

            // Assert
            Assert.IsNotNull(action);
            Assert.AreEqual(BattleActionType.Wait, action.Type);
        }

        [Test]
        public void AI_SelectsBestTargetAmongMultiple()
        {
            // Arrange: Archer with multiple targets
            var archerUnit = battleState.AddUnit(rangedCreature, 10, BattleSide.Attacker, 0, new BattleHex(1, 5));
            var weakTarget = battleState.AddUnit(meleeCreature, 2, BattleSide.Defender, 0, new BattleHex(14, 5)); // Low count
            var strongTarget = battleState.AddUnit(meleeCreature, 10, BattleSide.Defender, 1, new BattleHex(14, 6)); // High count

            // Act
            var action = ai.SelectAction(archerUnit);

            // Assert
            Assert.IsNotNull(action);
            Assert.AreEqual(BattleActionType.Shoot, action.Type);
            // AI should target one of them (implementation may vary)
            Assert.IsTrue(
                action.TargetUnitId == weakTarget.UnitId || action.TargetUnitId == strongTarget.UnitId,
                "Should target one of the available enemies"
            );
        }

        [Test]
        public void AI_ReturnsNullForDeadUnit()
        {
            // Arrange
            var unit = battleState.AddUnit(meleeCreature, 10, BattleSide.Attacker, 0, new BattleHex(1, 5));
            unit.TakeDamage(1000); // Kill unit

            // Act
            var action = ai.SelectAction(unit);

            // Assert
            Assert.IsNull(action, "Should return null for dead unit");
        }

        #endregion

        #region Target Selection Tests

        [Test]
        public void GetBestTarget_ReturnsTargetForValidAction()
        {
            // Arrange
            var archerUnit = battleState.AddUnit(rangedCreature, 10, BattleSide.Attacker, 0, new BattleHex(1, 5));
            var targetUnit = battleState.AddUnit(meleeCreature, 10, BattleSide.Defender, 0, new BattleHex(14, 5));

            // Act
            var bestTarget = ai.GetBestTarget(archerUnit);

            // Assert
            Assert.IsNotNull(bestTarget);
            Assert.AreEqual(targetUnit.UnitId, bestTarget.UnitId);
        }

        [Test]
        public void GetBestTarget_ReturnsNullWhenNoTargets()
        {
            // Arrange: Only one side
            var unit = battleState.AddUnit(meleeCreature, 10, BattleSide.Attacker, 0, new BattleHex(1, 5));

            // Act
            var bestTarget = ai.GetBestTarget(unit);

            // Assert
            Assert.IsNull(bestTarget, "Should return null when no enemies available");
        }

        #endregion
    }
}
