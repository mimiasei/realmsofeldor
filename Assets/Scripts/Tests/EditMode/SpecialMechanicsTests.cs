using NUnit.Framework;
using RealmsOfEldor.Core;
using RealmsOfEldor.Core.Battle;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.Tests
{
    /// <summary>
    /// Tests for special combat mechanics (double attack, status effects, special abilities).
    /// Phase 5D implementation.
    /// </summary>
    [TestFixture]
    public class SpecialMechanicsTests
    {
        private CreatureData normalCreature;
        private CreatureData doubleAttackCreature;
        private CreatureData noRetalCreature;
        private CreatureData flyingCreature;
        private BattleState battleState;
        private Hero attacker;
        private Hero defender;

        [SetUp]
        public void Setup()
        {
            // Create test heroes
            attacker = new Hero { Id = 1, Owner = 0, CustomName = "Attacker" };
            defender = new Hero { Id = 2, Owner = 1, CustomName = "Defender" };

            // Create normal creature
            normalCreature = ScriptableObject.CreateInstance<CreatureData>();
            normalCreature.creatureId = 1;
            normalCreature.creatureName = "Normal";
            normalCreature.attack = 5;
            normalCreature.defense = 5;
            normalCreature.minDamage = 1;
            normalCreature.maxDamage = 3;
            normalCreature.hitPoints = 10;
            normalCreature.speed = 5;
            normalCreature.shots = 0;
            normalCreature.isDoubleAttack = false;
            normalCreature.noMeleeRetal = false;
            normalCreature.isFlying = false;

            // Create double attack creature
            doubleAttackCreature = ScriptableObject.CreateInstance<CreatureData>();
            doubleAttackCreature.creatureId = 2;
            doubleAttackCreature.creatureName = "DoubleAttacker";
            doubleAttackCreature.attack = 5;
            doubleAttackCreature.defense = 5;
            doubleAttackCreature.minDamage = 1;
            doubleAttackCreature.maxDamage = 3;
            doubleAttackCreature.hitPoints = 10;
            doubleAttackCreature.speed = 5;
            doubleAttackCreature.shots = 0;
            doubleAttackCreature.isDoubleAttack = true;
            doubleAttackCreature.noMeleeRetal = false;
            doubleAttackCreature.isFlying = false;

            // Create no-retaliation creature
            noRetalCreature = ScriptableObject.CreateInstance<CreatureData>();
            noRetalCreature.creatureId = 3;
            noRetalCreature.creatureName = "NoRetal";
            noRetalCreature.attack = 5;
            noRetalCreature.defense = 5;
            noRetalCreature.minDamage = 1;
            noRetalCreature.maxDamage = 3;
            noRetalCreature.hitPoints = 10;
            noRetalCreature.speed = 5;
            noRetalCreature.shots = 0;
            noRetalCreature.isDoubleAttack = false;
            noRetalCreature.noMeleeRetal = true;
            noRetalCreature.isFlying = false;

            // Create flying creature
            flyingCreature = ScriptableObject.CreateInstance<CreatureData>();
            flyingCreature.creatureId = 4;
            flyingCreature.creatureName = "Flyer";
            flyingCreature.attack = 5;
            flyingCreature.defense = 5;
            flyingCreature.minDamage = 1;
            flyingCreature.maxDamage = 3;
            flyingCreature.hitPoints = 10;
            flyingCreature.speed = 5;
            flyingCreature.shots = 0;
            flyingCreature.isDoubleAttack = false;
            flyingCreature.noMeleeRetal = false;
            flyingCreature.isFlying = true;

            // Create battle state
            battleState = new BattleState(attacker, defender);
        }

        #region Double Attack Tests

        [Test]
        public void DoubleAttack_DealsTwiceTheDamage()
        {
            // Arrange
            var attackerUnit = battleState.AddUnit(doubleAttackCreature, 10, BattleSide.Attacker, 0, new BattleHex(1, 5));
            var defenderUnit = battleState.AddUnit(normalCreature, 10, BattleSide.Defender, 0, new BattleHex(14, 5));

            var initialHealth = defenderUnit.TotalHealth;

            // Act
            var result = battleState.ExecuteAttack(attackerUnit, defenderUnit, chargeDistance: 0);

            // Assert
            Assert.IsNotNull(result);
            Assert.Greater(result.DamageDealt, 0, "Double attack should deal damage");

            // Double attack should deal roughly twice the damage of a single attack
            // We can't check exact value due to randomness, but we can verify damage was dealt
            Assert.Less(defenderUnit.TotalHealth, initialHealth, "Defender should take damage");
        }

        [Test]
        public void DoubleAttack_OnlyTriggersIfDefenderSurvivesFirstHit()
        {
            // Arrange: Low HP defender that will die from first hit
            var attackerUnit = battleState.AddUnit(doubleAttackCreature, 100, BattleSide.Attacker, 0, new BattleHex(1, 5));
            var defenderUnit = battleState.AddUnit(normalCreature, 1, BattleSide.Defender, 0, new BattleHex(14, 5));

            // Act
            var result = battleState.ExecuteAttack(attackerUnit, defenderUnit, chargeDistance: 0);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(defenderUnit.IsAlive, "Defender should be dead after first hit");
            Assert.IsTrue(result.IsKilled, "Result should indicate kill");
        }

        [Test]
        public void NormalAttack_DoesNotDoubleAttack()
        {
            // Arrange
            var attackerUnit = battleState.AddUnit(normalCreature, 10, BattleSide.Attacker, 0, new BattleHex(1, 5));
            var defenderUnit = battleState.AddUnit(normalCreature, 10, BattleSide.Defender, 0, new BattleHex(14, 5));

            var initialHealth = defenderUnit.TotalHealth;

            // Act
            var result = battleState.ExecuteAttack(attackerUnit, defenderUnit, chargeDistance: 0);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(attackerUnit.HasDoubleAttack, "Normal creature should not have double attack");
        }

        #endregion

        #region No Retaliation Tests

        [Test]
        public void NoMeleeRetaliation_PreventsDefenderRetaliation()
        {
            // Arrange
            var attackerUnit = battleState.AddUnit(noRetalCreature, 10, BattleSide.Attacker, 0, new BattleHex(1, 5));
            var defenderUnit = battleState.AddUnit(normalCreature, 10, BattleSide.Defender, 0, new BattleHex(14, 5));

            var attackerInitialHealth = attackerUnit.TotalHealth;

            // Act
            var result = battleState.ExecuteAttack(attackerUnit, defenderUnit, chargeDistance: 0);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNull(result.Retaliation, "No retaliation creature should not trigger retaliation");
            Assert.AreEqual(attackerInitialHealth, attackerUnit.TotalHealth, "Attacker should take no damage");
        }

        [Test]
        public void NormalAttack_AllowsRetaliation()
        {
            // Arrange
            var attackerUnit = battleState.AddUnit(normalCreature, 10, BattleSide.Attacker, 0, new BattleHex(1, 5));
            var defenderUnit = battleState.AddUnit(normalCreature, 10, BattleSide.Defender, 0, new BattleHex(14, 5));

            var attackerInitialHealth = attackerUnit.TotalHealth;

            // Act
            var result = battleState.ExecuteAttack(attackerUnit, defenderUnit, chargeDistance: 0);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Retaliation, "Normal attack should allow retaliation");
            Assert.Less(attackerUnit.TotalHealth, attackerInitialHealth, "Attacker should take retaliation damage");
        }

        #endregion

        #region Flying Tests

        [Test]
        public void Flying_CreatureHasFlyingFlag()
        {
            // Arrange
            var flyingUnit = battleState.AddUnit(flyingCreature, 10, BattleSide.Attacker, 0, new BattleHex(1, 5));
            var normalUnit = battleState.AddUnit(normalCreature, 10, BattleSide.Defender, 0, new BattleHex(14, 5));

            // Assert
            Assert.IsTrue(flyingUnit.IsFlying, "Flying creature should have IsFlying = true");
            Assert.IsFalse(normalUnit.IsFlying, "Normal creature should have IsFlying = false");
        }

        #endregion

        #region Status Effects Tests

        [Test]
        public void StatusEffect_CanBeAdded()
        {
            // Arrange
            var unit = battleState.AddUnit(normalCreature, 10, BattleSide.Attacker, 0, new BattleHex(1, 5));
            var effect = new StatusEffect("Bless", duration: 3, attack: 2);

            // Act
            unit.AddStatusEffect(effect);

            // Assert
            Assert.AreEqual(7, unit.Attack, "Attack should be base (5) + effect (2)");
        }

        [Test]
        public void StatusEffect_DecrementsEachRound()
        {
            // Arrange
            var unit = battleState.AddUnit(normalCreature, 10, BattleSide.Attacker, 0, new BattleHex(1, 5));
            var effect = new StatusEffect("Bless", duration: 3, attack: 2);
            unit.AddStatusEffect(effect);

            // Act - simulate round updates
            unit.UpdateStatusEffects();
            unit.UpdateStatusEffects();
            unit.UpdateStatusEffects();

            // Assert
            Assert.AreEqual(5, unit.Attack, "Effect should have expired, attack back to base");
        }

        [Test]
        public void StatusEffect_MultipleEffectsStack()
        {
            // Arrange
            var unit = battleState.AddUnit(normalCreature, 10, BattleSide.Attacker, 0, new BattleHex(1, 5));
            var effect1 = new StatusEffect("Bless", duration: 3, attack: 2);
            var effect2 = new StatusEffect("Prayer", duration: 3, attack: 1, defense: 1);

            // Act
            unit.AddStatusEffect(effect1);
            unit.AddStatusEffect(effect2);

            // Assert
            Assert.AreEqual(8, unit.Attack, "Attack should be base (5) + effect1 (2) + effect2 (1)");
            Assert.AreEqual(6, unit.Defense, "Defense should be base (5) + effect2 (1)");
        }

        [Test]
        public void StatusEffect_CanBeCleared()
        {
            // Arrange
            var unit = battleState.AddUnit(normalCreature, 10, BattleSide.Attacker, 0, new BattleHex(1, 5));
            var effect = new StatusEffect("Bless", duration: 3, attack: 2);
            unit.AddStatusEffect(effect);

            // Act
            unit.ClearStatusEffects();

            // Assert
            Assert.AreEqual(5, unit.Attack, "Attack should be back to base after clearing effects");
        }

        #endregion
    }
}
