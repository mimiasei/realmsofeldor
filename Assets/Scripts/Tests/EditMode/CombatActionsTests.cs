using NUnit.Framework;
using RealmsOfEldor.Core;
using RealmsOfEldor.Core.Battle;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.Tests.EditMode
{
    [TestFixture]
    public class CombatActionsTests
    {
        private BattleState battleState;
        private Hero attackerHero;
        private Hero defenderHero;

        [SetUp]
        public void SetUp()
        {
            attackerHero = new Hero(1, "Attacker", HeroClass.KNIGHT);
            defenderHero = new Hero(2, "Defender", HeroClass.KNIGHT);
            battleState = new BattleState(attackerHero, defenderHero);
        }

        private CreatureData CreateTestCreature(string name, int attack, int defense, int minDmg, int maxDmg, int hp, int speed, int shots = 0)
        {
            var creature = ScriptableObject.CreateInstance<CreatureData>();
            creature.creatureName = name;
            creature.attack = attack;
            creature.defense = defense;
            creature.minDamage = minDmg;
            creature.maxDamage = maxDmg;
            creature.hitPoints = hp;
            creature.speed = speed;
            creature.shots = shots;
            return creature;
        }

        // ===== Melee Attack Tests =====

        [Test]
        public void ExecuteAttack_DealsDamageToDefender()
        {
            var creature = CreateTestCreature("Swordsman", 6, 6, 6, 9, 35, 5);
            var stack = new CreatureStack { CreatureType = creature, Count = 10 };

            var attacker = battleState.AddUnit(stack, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = battleState.AddUnit(stack, BattleSide.Defender, 0, new BattleHex(100));

            var initialDefenderHP = defender.TotalHealth;

            var result = battleState.ExecuteAttack(attacker, defender);

            Assert.IsNotNull(result);
            Assert.AreEqual(attacker, result.Attacker);
            Assert.AreEqual(defender, result.Defender);
            Assert.Greater(result.DamageDealt, 0);
            Assert.Less(defender.TotalHealth, initialDefenderHP);
        }

        [Test]
        public void ExecuteAttack_MarksAttackerAsActed()
        {
            var creature = CreateTestCreature("Swordsman", 6, 6, 6, 9, 35, 5);
            var stack = new CreatureStack { CreatureType = creature, Count = 1 };

            var attacker = battleState.AddUnit(stack, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = battleState.AddUnit(stack, BattleSide.Defender, 0, new BattleHex(100));

            Assert.IsFalse(attacker.HasMoved);

            battleState.ExecuteAttack(attacker, defender);

            Assert.IsTrue(attacker.HasMoved);
        }

        [Test]
        public void ExecuteAttack_TriggersRetaliation()
        {
            var creature = CreateTestCreature("Swordsman", 6, 6, 6, 9, 35, 5);
            var stack = new CreatureStack { CreatureType = creature, Count = 10 };

            var attacker = battleState.AddUnit(stack, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = battleState.AddUnit(stack, BattleSide.Defender, 0, new BattleHex(100));

            var initialAttackerHP = attacker.TotalHealth;

            var result = battleState.ExecuteAttack(attacker, defender);

            // Retaliation should occur (defender survives and retaliates)
            Assert.IsNotNull(result.Retaliation);
            Assert.AreEqual(defender, result.Retaliation.Attacker);
            Assert.AreEqual(attacker, result.Retaliation.Defender);
            Assert.Greater(result.Retaliation.DamageDealt, 0);
            Assert.Less(attacker.TotalHealth, initialAttackerHP);
        }

        [Test]
        public void ExecuteAttack_NoRetaliationIfDefenderDies()
        {
            var weakCreature = CreateTestCreature("Peasant", 1, 1, 1, 1, 5, 3);
            var strongCreature = CreateTestCreature("Dragon", 27, 1, 100, 100, 200, 16);

            var attackerStack = new CreatureStack { CreatureType = strongCreature, Count = 10 };
            var defenderStack = new CreatureStack { CreatureType = weakCreature, Count = 1 };

            var attacker = battleState.AddUnit(attackerStack, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = battleState.AddUnit(defenderStack, BattleSide.Defender, 0, new BattleHex(100));

            var result = battleState.ExecuteAttack(attacker, defender);

            // Defender should be dead (overkill)
            Assert.IsFalse(defender.IsAlive);
            Assert.IsTrue(result.IsKilled);

            // No retaliation if defender is dead
            Assert.IsNull(result.Retaliation);
        }

        [Test]
        public void ExecuteAttack_NoRetaliationIfAttackerHasNoMeleeRetal()
        {
            var normalCreature = CreateTestCreature("Swordsman", 6, 6, 6, 9, 35, 5);
            var noRetalCreature = CreateTestCreature("Vampire", 10, 9, 5, 8, 30, 6);
            noRetalCreature.noMeleeRetal = true;

            var attackerStack = new CreatureStack { CreatureType = noRetalCreature, Count = 10 };
            var defenderStack = new CreatureStack { CreatureType = normalCreature, Count = 10 };

            var attacker = battleState.AddUnit(attackerStack, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = battleState.AddUnit(defenderStack, BattleSide.Defender, 0, new BattleHex(100));

            var result = battleState.ExecuteAttack(attacker, defender);

            // No retaliation if attacker has no melee retaliation ability
            Assert.IsNull(result.Retaliation);
        }

        [Test]
        public void ExecuteAttack_NullIfAttackerDead()
        {
            var creature = CreateTestCreature("Swordsman", 6, 6, 6, 9, 35, 5);
            var stack = new CreatureStack { CreatureType = creature, Count = 10 };

            var attacker = battleState.AddUnit(stack, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = battleState.AddUnit(stack, BattleSide.Defender, 0, new BattleHex(100));

            // Kill attacker
            attacker.TakeDamage(10000);

            var result = battleState.ExecuteAttack(attacker, defender);

            Assert.IsNull(result);
        }

        // ===== Ranged Attack Tests =====

        [Test]
        public void ExecuteShoot_DealsDamageToDefender()
        {
            var archerCreature = CreateTestCreature("Archer", 6, 3, 2, 3, 10, 4, shots: 12);
            var targetCreature = CreateTestCreature("Swordsman", 6, 6, 6, 9, 35, 5);

            var attackerStack = new CreatureStack { CreatureType = archerCreature, Count = 10 };
            var defenderStack = new CreatureStack { CreatureType = targetCreature, Count = 10 };

            var attacker = battleState.AddUnit(attackerStack, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = battleState.AddUnit(defenderStack, BattleSide.Defender, 0, new BattleHex(100));

            var initialDefenderHP = defender.TotalHealth;

            var result = battleState.ExecuteShoot(attacker, defender);

            Assert.IsNotNull(result);
            Assert.AreEqual(attacker, result.Attacker);
            Assert.AreEqual(defender, result.Defender);
            Assert.IsTrue(result.IsShooting);
            Assert.Greater(result.DamageDealt, 0);
            Assert.Less(defender.TotalHealth, initialDefenderHP);
        }

        [Test]
        public void ExecuteShoot_UsesOneShot()
        {
            var archerCreature = CreateTestCreature("Archer", 6, 3, 2, 3, 10, 4, shots: 12);
            var targetCreature = CreateTestCreature("Swordsman", 6, 6, 6, 9, 35, 5);

            var attackerStack = new CreatureStack { CreatureType = archerCreature, Count = 1 };
            var defenderStack = new CreatureStack { CreatureType = targetCreature, Count = 1 };

            var attacker = battleState.AddUnit(attackerStack, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = battleState.AddUnit(defenderStack, BattleSide.Defender, 0, new BattleHex(100));

            var initialShots = attacker.ShotsRemaining;

            battleState.ExecuteShoot(attacker, defender);

            Assert.AreEqual(initialShots - 1, attacker.ShotsRemaining);
        }

        [Test]
        public void ExecuteShoot_NoRetaliation()
        {
            var archerCreature = CreateTestCreature("Archer", 6, 3, 2, 3, 10, 4, shots: 12);
            var targetCreature = CreateTestCreature("Swordsman", 6, 6, 6, 9, 35, 5);

            var attackerStack = new CreatureStack { CreatureType = archerCreature, Count = 10 };
            var defenderStack = new CreatureStack { CreatureType = targetCreature, Count = 10 };

            var attacker = battleState.AddUnit(attackerStack, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = battleState.AddUnit(defenderStack, BattleSide.Defender, 0, new BattleHex(100));

            var initialAttackerHP = attacker.TotalHealth;

            var result = battleState.ExecuteShoot(attacker, defender);

            // No retaliation for ranged attacks
            Assert.IsNull(result.Retaliation);
            Assert.AreEqual(initialAttackerHP, attacker.TotalHealth);
        }

        [Test]
        public void ExecuteShoot_NullIfNoShotsRemaining()
        {
            var archerCreature = CreateTestCreature("Archer", 6, 3, 2, 3, 10, 4, shots: 12);
            var targetCreature = CreateTestCreature("Swordsman", 6, 6, 6, 9, 35, 5);

            var attackerStack = new CreatureStack { CreatureType = archerCreature, Count = 1 };
            var defenderStack = new CreatureStack { CreatureType = targetCreature, Count = 1 };

            var attacker = battleState.AddUnit(attackerStack, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = battleState.AddUnit(defenderStack, BattleSide.Defender, 0, new BattleHex(100));

            // Use all shots
            while (attacker.ShotsRemaining > 0)
            {
                attacker.UseShot();
            }

            var result = battleState.ExecuteShoot(attacker, defender);

            Assert.IsNull(result);
        }

        [Test]
        public void ExecuteShoot_NullIfNotRangedUnit()
        {
            var meleeCreature = CreateTestCreature("Swordsman", 6, 6, 6, 9, 35, 5, shots: 0);

            var attackerStack = new CreatureStack { CreatureType = meleeCreature, Count = 1 };
            var defenderStack = new CreatureStack { CreatureType = meleeCreature, Count = 1 };

            var attacker = battleState.AddUnit(attackerStack, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = battleState.AddUnit(defenderStack, BattleSide.Defender, 0, new BattleHex(100));

            var result = battleState.ExecuteShoot(attacker, defender);

            Assert.IsNull(result);
        }

        // ===== Retaliation Tests =====

        [Test]
        public void Retaliation_OnlyOncePerTurn()
        {
            var creature = CreateTestCreature("Swordsman", 6, 6, 6, 9, 35, 5);
            var stack = new CreatureStack { CreatureType = creature, Count = 10 };

            var attacker1 = battleState.AddUnit(stack, BattleSide.Attacker, 0, new BattleHex(50));
            var attacker2 = battleState.AddUnit(stack, BattleSide.Attacker, 1, new BattleHex(51));
            var defender = battleState.AddUnit(stack, BattleSide.Defender, 0, new BattleHex(100));

            // First attack - should trigger retaliation
            var result1 = battleState.ExecuteAttack(attacker1, defender);
            Assert.IsNotNull(result1.Retaliation);

            // Second attack from different unit - defender already retaliated
            var result2 = battleState.ExecuteAttack(attacker2, defender);
            Assert.IsNull(result2.Retaliation);  // No retaliation (already used)
        }

        [Test]
        public void Retaliation_ResetsEachTurn()
        {
            var creature = CreateTestCreature("Swordsman", 6, 6, 6, 9, 35, 5);
            var stack = new CreatureStack { CreatureType = creature, Count = 10 };

            var attacker = battleState.AddUnit(stack, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = battleState.AddUnit(stack, BattleSide.Defender, 0, new BattleHex(100));

            // First attack - triggers retaliation
            var result1 = battleState.ExecuteAttack(attacker, defender);
            Assert.IsNotNull(result1.Retaliation);
            Assert.IsFalse(defender.CanRetaliate);

            // Start new turn for defender
            defender.StartTurn();
            Assert.IsTrue(defender.CanRetaliate);

            // Second attack - should trigger retaliation again
            var result2 = battleState.ExecuteAttack(attacker, defender);
            Assert.IsNotNull(result2.Retaliation);
        }

        // ===== Kill Tracking Tests =====

        [Test]
        public void AttackResult_TracksKills()
        {
            var weakCreature = CreateTestCreature("Peasant", 1, 1, 1, 1, 5, 3);
            var strongCreature = CreateTestCreature("Knight", 12, 9, 10, 12, 35, 7);

            var attackerStack = new CreatureStack { CreatureType = strongCreature, Count = 10 };
            var defenderStack = new CreatureStack { CreatureType = weakCreature, Count = 20 };

            var attacker = battleState.AddUnit(attackerStack, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = battleState.AddUnit(defenderStack, BattleSide.Defender, 0, new BattleHex(100));

            var result = battleState.ExecuteAttack(attacker, defender);

            Assert.Greater(result.KillsDealt, 0);
            Assert.LessOrEqual(result.KillsDealt, 20);  // Can't kill more than exist
        }

        [Test]
        public void AttackResult_IsKilledFlagWhenStackDies()
        {
            var weakCreature = CreateTestCreature("Peasant", 1, 1, 1, 1, 5, 3);
            var strongCreature = CreateTestCreature("Dragon", 27, 1, 100, 100, 200, 16);

            var attackerStack = new CreatureStack { CreatureType = strongCreature, Count = 10 };
            var defenderStack = new CreatureStack { CreatureType = weakCreature, Count = 1 };

            var attacker = battleState.AddUnit(attackerStack, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = battleState.AddUnit(defenderStack, BattleSide.Defender, 0, new BattleHex(100));

            var result = battleState.ExecuteAttack(attacker, defender);

            Assert.IsTrue(result.IsKilled);
            Assert.IsFalse(defender.IsAlive);
        }

        [Test]
        public void AttackResult_IsKilledFalsWhenStackSurvives()
        {
            var creature = CreateTestCreature("Swordsman", 6, 6, 1, 1, 35, 5);
            var stack = new CreatureStack { CreatureType = creature, Count = 10 };

            var attacker = battleState.AddUnit(stack, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = battleState.AddUnit(stack, BattleSide.Defender, 0, new BattleHex(100));

            var result = battleState.ExecuteAttack(attacker, defender);

            Assert.IsFalse(result.IsKilled);
            Assert.IsTrue(defender.IsAlive);
        }

        // ===== Edge Cases =====

        [Test]
        public void ExecuteAttack_HandlesPartiallyDamagedDefender()
        {
            var creature = CreateTestCreature("Swordsman", 6, 6, 6, 9, 35, 5);
            var stack = new CreatureStack { CreatureType = creature, Count = 10 };

            var attacker = battleState.AddUnit(stack, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = battleState.AddUnit(stack, BattleSide.Defender, 0, new BattleHex(100));

            // Damage defender partially
            defender.TakeDamage(20);
            var hpBefore = defender.TotalHealth;

            var result = battleState.ExecuteAttack(attacker, defender);

            Assert.IsNotNull(result);
            Assert.Less(defender.TotalHealth, hpBefore);
        }

        [Test]
        public void ExecuteAttack_ChargeDistance_PassedToCalculator()
        {
            var creature = CreateTestCreature("Champion", 16, 16, 20, 25, 100, 9);
            var stack = new CreatureStack { CreatureType = creature, Count = 5 };

            var attacker = battleState.AddUnit(stack, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = battleState.AddUnit(stack, BattleSide.Defender, 0, new BattleHex(100));

            // Attack with charge distance
            var result = battleState.ExecuteAttack(attacker, defender, chargeDistance: 5);

            Assert.IsNotNull(result);
            // Damage should be > base (if jousting is implemented)
            // For now, just verify execution succeeds
        }
    }
}
