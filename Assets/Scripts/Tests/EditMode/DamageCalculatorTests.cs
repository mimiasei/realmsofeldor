using NUnit.Framework;
using RealmsOfEldor.Core.Battle;
using RealmsOfEldor.Data;

namespace RealmsOfEldor.Tests.EditMode
{
    [TestFixture]
    public class DamageCalculatorTests
    {
        private CreatureData CreateTestCreature(string name, int attack, int defense, int minDmg, int maxDmg, int hp, int speed)
        {
            var creature = ScriptableObject.CreateInstance<CreatureData>();
            creature.creatureName = name;
            creature.attack = attack;
            creature.defense = defense;
            creature.minDamage = minDmg;
            creature.maxDamage = maxDmg;
            creature.hitPoints = hp;
            creature.speed = speed;
            creature.shots = 0;
            return creature;
        }

        // ===== Base Damage Tests =====

        [Test]
        public void BaseDamageSingle_ReturnsCorrectRange()
        {
            var creature = CreateTestCreature("Peasant", 1, 1, 1, 1, 1, 3);
            var attacker = new BattleUnit(1, creature, 1, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = new BattleUnit(2, creature, 1, BattleSide.Defender, 0, new BattleHex(100));

            var info = new AttackInfo(attacker, defender);
            var calculator = new DamageCalculator(info);
            var result = calculator.CalculateDamageRange();

            // Single peasant: 1-1 damage
            Assert.AreEqual(1, result.Damage.Min);
            Assert.AreEqual(1, result.Damage.Max);
        }

        [Test]
        public void BaseDamageStack_MultipliesByStackCount()
        {
            var creature = CreateTestCreature("Archer", 6, 3, 2, 3, 10, 4);
            var attacker = new BattleUnit(1, creature, 10, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = new BattleUnit(2, creature, 1, BattleSide.Defender, 0, new BattleHex(100));

            var info = new AttackInfo(attacker, defender);
            var calculator = new DamageCalculator(info);
            var result = calculator.CalculateDamageRange();

            // 10 archers: (2-3) × 10 = 20-30 base damage
            // With 6 attack vs 3 defense: +15% (3 × 5%)
            var expectedMin = (int)System.Math.Floor(20 * 1.15);
            var expectedMax = (int)System.Math.Floor(30 * 1.15);

            Assert.AreEqual(expectedMin, result.Damage.Min);
            Assert.AreEqual(expectedMax, result.Damage.Max);
        }

        // ===== Attack/Defense Skill Tests =====

        [Test]
        public void AttackAdvantage_IncreaseDamage()
        {
            var weakCreature = CreateTestCreature("Peasant", 1, 1, 1, 1, 1, 3);
            var strongCreature = CreateTestCreature("Knight", 12, 1, 1, 1, 1, 7);

            var attacker = new BattleUnit(1, strongCreature, 1, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = new BattleUnit(2, weakCreature, 1, BattleSide.Defender, 0, new BattleHex(100));

            var info = new AttackInfo(attacker, defender);
            var calculator = new DamageCalculator(info);
            var result = calculator.CalculateDamageRange();

            // Attack advantage: 12 - 1 = 11
            // Damage bonus: 11 × 5% = 55%
            // Base damage: 1-1
            // Final: floor(1 × 1.55) = 1
            Assert.AreEqual(1, result.Damage.Min);
            Assert.AreEqual(1, result.Damage.Max);
        }

        [Test]
        public void DefenseAdvantage_DecreaseDamage()
        {
            var weakCreature = CreateTestCreature("Peasant", 1, 1, 10, 10, 1, 3);
            var tankCreature = CreateTestCreature("Golem", 1, 10, 1, 1, 30, 3);

            var attacker = new BattleUnit(1, weakCreature, 1, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = new BattleUnit(2, tankCreature, 1, BattleSide.Defender, 0, new BattleHex(100));

            var info = new AttackInfo(attacker, defender);
            var calculator = new DamageCalculator(info);
            var result = calculator.CalculateDamageRange();

            // Defense advantage: 10 - 1 = 9
            // Damage reduction: 9 × 2.5% = 22.5%
            // Base damage: 10-10
            // Final: floor(10 × (1 - 0.225)) = floor(7.75) = 7
            Assert.AreEqual(7, result.Damage.Min);
            Assert.AreEqual(7, result.Damage.Max);
        }

        [Test]
        public void AttackDefenseEqual_NoBonusOrPenalty()
        {
            var creature = CreateTestCreature("Pikeman", 4, 5, 1, 3, 10, 4);

            var attacker = new BattleUnit(1, creature, 1, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = new BattleUnit(2, creature, 1, BattleSide.Defender, 0, new BattleHex(100));

            var info = new AttackInfo(attacker, defender);
            var calculator = new DamageCalculator(info);
            var result = calculator.CalculateDamageRange();

            // Equal attack/defense: no bonus/penalty
            // Base damage: 1-3
            Assert.AreEqual(1, result.Damage.Min);
            Assert.AreEqual(3, result.Damage.Max);
        }

        [Test]
        public void AttackAdvantage_CappedAt300Percent()
        {
            var weakCreature = CreateTestCreature("Peasant", 1, 1, 1, 1, 1, 3);
            var godCreature = CreateTestCreature("Dragon", 100, 1, 10, 10, 100, 20);

            var attacker = new BattleUnit(1, godCreature, 1, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = new BattleUnit(2, weakCreature, 1, BattleSide.Defender, 0, new BattleHex(100));

            var info = new AttackInfo(attacker, defender);
            var calculator = new DamageCalculator(info);
            var result = calculator.CalculateDamageRange();

            // Attack advantage: 100 - 1 = 99 (would be 495% bonus)
            // Capped at 300% (60 points advantage)
            // Base damage: 10-10
            // Final: floor(10 × (1 + 3.0)) = floor(40) = 40
            Assert.AreEqual(40, result.Damage.Min);
            Assert.AreEqual(40, result.Damage.Max);
        }

        [Test]
        public void DefenseAdvantage_CappedAt70Percent()
        {
            var weakCreature = CreateTestCreature("Peasant", 1, 1, 100, 100, 1, 3);
            var godTankCreature = CreateTestCreature("Titan", 1, 100, 1, 1, 300, 11);

            var attacker = new BattleUnit(1, weakCreature, 1, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = new BattleUnit(2, godTankCreature, 1, BattleSide.Defender, 0, new BattleHex(100));

            var info = new AttackInfo(attacker, defender);
            var calculator = new DamageCalculator(info);
            var result = calculator.CalculateDamageRange();

            // Defense advantage: 100 - 1 = 99 (would be 247.5% reduction)
            // Capped at 70% (28 points advantage)
            // Base damage: 100-100
            // Final: floor(100 × (1 - 0.7)) = floor(30) = 30
            Assert.AreEqual(30, result.Damage.Min);
            Assert.AreEqual(30, result.Damage.Max);
        }

        // ===== Range Penalty Tests =====

        [Test]
        public void RangedUnitInMelee_ReceivesPenalty()
        {
            var archerCreature = CreateTestCreature("Archer", 6, 3, 2, 3, 10, 4);
            archerCreature.shots = 12;  // Make it ranged

            var attacker = new BattleUnit(1, archerCreature, 1, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = new BattleUnit(2, archerCreature, 1, BattleSide.Defender, 0, new BattleHex(100));

            // Melee attack (shooting = false)
            var info = new AttackInfo(attacker, defender, shooting: false);
            var calculator = new DamageCalculator(info);
            var result = calculator.CalculateDamageRange();

            // Base: 2-3 with equal attack/defense (6 vs 3 = +15%)
            // Range penalty: -50%
            // Final: floor(2 × 1.15 × 0.5) = floor(1.15) = 1
            //        floor(3 × 1.15 × 0.5) = floor(1.725) = 1
            Assert.AreEqual(1, result.Damage.Min);
            Assert.AreEqual(1, result.Damage.Max);
        }

        [Test]
        public void RangedAttack_NoPenalty()
        {
            var archerCreature = CreateTestCreature("Archer", 6, 3, 2, 3, 10, 4);
            archerCreature.shots = 12;

            var attacker = new BattleUnit(1, archerCreature, 1, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = new BattleUnit(2, archerCreature, 1, BattleSide.Defender, 0, new BattleHex(100));

            // Ranged attack (shooting = true)
            var info = new AttackInfo(attacker, defender, shooting: true);
            var calculator = new DamageCalculator(info);
            var result = calculator.CalculateDamageRange();

            // Base: 2-3 with +15% attack bonus
            // No range penalty for shooting
            // Final: floor(2 × 1.15) = 2, floor(3 × 1.15) = 3
            Assert.AreEqual(2, result.Damage.Min);
            Assert.AreEqual(3, result.Damage.Max);
        }

        [Test]
        public void CanShootInMelee_NoPenalty()
        {
            var elfCreature = CreateTestCreature("Wood Elf", 9, 5, 3, 5, 15, 7);
            elfCreature.shots = 24;
            elfCreature.canShootInMelee = true;  // No melee penalty

            var attacker = new BattleUnit(1, elfCreature, 1, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = new BattleUnit(2, elfCreature, 1, BattleSide.Defender, 0, new BattleHex(100));

            // Melee attack (shooting = false)
            var info = new AttackInfo(attacker, defender, shooting: false);
            var calculator = new DamageCalculator(info);
            var result = calculator.CalculateDamageRange();

            // Base: 3-5 with equal attack/defense (9 vs 5 = +20%)
            // No range penalty (canShootInMelee = true)
            // Final: floor(3 × 1.20) = 3, floor(5 × 1.20) = 6
            Assert.AreEqual(3, result.Damage.Min);
            Assert.AreEqual(6, result.Damage.Max);
        }

        // ===== Luck Tests =====

        [Test]
        public void LuckyStrike_DoublesDamage()
        {
            var creature = CreateTestCreature("Swordsman", 6, 6, 6, 9, 35, 5);

            var attacker = new BattleUnit(1, creature, 1, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = new BattleUnit(2, creature, 1, BattleSide.Defender, 0, new BattleHex(100));

            var info = new AttackInfo(attacker, defender)
            {
                LuckyStrike = true
            };
            var calculator = new DamageCalculator(info);
            var result = calculator.CalculateDamageRange();

            // Base: 6-9 with equal attack/defense
            // Lucky strike: +100% damage
            // Final: floor(6 × 2.0) = 12, floor(9 × 2.0) = 18
            Assert.AreEqual(12, result.Damage.Min);
            Assert.AreEqual(18, result.Damage.Max);
        }

        [Test]
        public void UnluckyStrike_HalvesDamage()
        {
            var creature = CreateTestCreature("Swordsman", 6, 6, 6, 9, 35, 5);

            var attacker = new BattleUnit(1, creature, 1, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = new BattleUnit(2, creature, 1, BattleSide.Defender, 0, new BattleHex(100));

            var info = new AttackInfo(attacker, defender)
            {
                UnluckyStrike = true
            };
            var calculator = new DamageCalculator(info);
            var result = calculator.CalculateDamageRange();

            // Base: 6-9 with equal attack/defense
            // Unlucky strike: -50% damage
            // Final: floor(6 × 0.5) = 3, floor(9 × 0.5) = 4
            Assert.AreEqual(3, result.Damage.Min);
            Assert.AreEqual(4, result.Damage.Max);
        }

        // ===== Casualties Calculation Tests =====

        [Test]
        public void Casualties_NoKillsIfDamageTooLow()
        {
            var creature = CreateTestCreature("Peasant", 1, 1, 1, 1, 5, 3);

            var attacker = new BattleUnit(1, creature, 1, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = new BattleUnit(2, creature, 10, BattleSide.Defender, 0, new BattleHex(100));

            var info = new AttackInfo(attacker, defender);
            var calculator = new DamageCalculator(info);
            var result = calculator.CalculateDamageRange();

            // Damage: 1-1, Defender HP: 5
            // Not enough to kill even one creature
            Assert.AreEqual(0, result.Kills.Min);
            Assert.AreEqual(0, result.Kills.Max);
        }

        [Test]
        public void Casualties_KillsOneCreature()
        {
            var creature = CreateTestCreature("Peasant", 1, 1, 5, 5, 5, 3);

            var attacker = new BattleUnit(1, creature, 1, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = new BattleUnit(2, creature, 10, BattleSide.Defender, 0, new BattleHex(100));

            var info = new AttackInfo(attacker, defender);
            var calculator = new DamageCalculator(info);
            var result = calculator.CalculateDamageRange();

            // Damage: 5-5, Defender HP: 5
            // Kills exactly 1 creature
            Assert.AreEqual(1, result.Kills.Min);
            Assert.AreEqual(1, result.Kills.Max);
        }

        [Test]
        public void Casualties_KillsMultipleCreatures()
        {
            var creature = CreateTestCreature("Dragon", 27, 27, 40, 50, 200, 16);

            var attacker = new BattleUnit(1, creature, 10, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = new BattleUnit(2, creature, 10, BattleSide.Defender, 0, new BattleHex(100));

            var info = new AttackInfo(attacker, defender);
            var calculator = new DamageCalculator(info);
            var result = calculator.CalculateDamageRange();

            // 10 dragons: (40-50) × 10 = 400-500 damage
            // Defender HP per creature: 200
            // Kills: 400/200 = 2, 500/200 = 2
            Assert.AreEqual(2, result.Kills.Min);
            Assert.AreEqual(2, result.Kills.Max);
        }

        [Test]
        public void Casualties_CappedAtDefenderCount()
        {
            var weakCreature = CreateTestCreature("Peasant", 1, 1, 1, 1, 5, 3);
            var strongCreature = CreateTestCreature("Dragon", 27, 1, 100, 100, 200, 16);

            var attacker = new BattleUnit(1, strongCreature, 10, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = new BattleUnit(2, weakCreature, 3, BattleSide.Defender, 0, new BattleHex(100));

            var info = new AttackInfo(attacker, defender);
            var calculator = new DamageCalculator(info);
            var result = calculator.CalculateDamageRange();

            // Massive overkill damage, but only 3 defenders
            // Kills capped at defender count
            Assert.LessOrEqual(result.Kills.Min, 3);
            Assert.LessOrEqual(result.Kills.Max, 3);
        }

        // ===== Minimum Damage Tests =====

        [Test]
        public void MinimumDamage_AlwaysAtLeastOne()
        {
            var weakCreature = CreateTestCreature("Peasant", 1, 1, 1, 1, 1, 3);
            var tankCreature = CreateTestCreature("Titan", 1, 50, 1, 1, 300, 11);

            var attacker = new BattleUnit(1, weakCreature, 1, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = new BattleUnit(2, tankCreature, 1, BattleSide.Defender, 0, new BattleHex(100));

            var info = new AttackInfo(attacker, defender);
            var calculator = new DamageCalculator(info);
            var result = calculator.CalculateDamageRange();

            // Massive defense advantage would reduce to 0, but minimum is 1
            Assert.GreaterOrEqual(result.Damage.Min, 1);
            Assert.GreaterOrEqual(result.Damage.Max, 1);
        }

        // ===== Complex Scenario Tests =====

        [Test]
        public void ComplexScenario_AttackBonusAndLuck()
        {
            var creature = CreateTestCreature("Griffin", 8, 8, 3, 6, 25, 6);

            var attacker = new BattleUnit(1, creature, 20, BattleSide.Attacker, 0, new BattleHex(50));
            var defender = new BattleUnit(2, creature, 1, BattleSide.Defender, 0, new BattleHex(100));

            // Modify defender defense to create attack advantage
            var weakDefender = CreateTestCreature("Weak", 8, 4, 3, 6, 25, 6);
            defender = new BattleUnit(2, weakDefender, 1, BattleSide.Defender, 0, new BattleHex(100));

            var info = new AttackInfo(attacker, defender)
            {
                LuckyStrike = true
            };
            var calculator = new DamageCalculator(info);
            var result = calculator.CalculateDamageRange();

            // 20 griffins: (3-6) × 20 = 60-120 base
            // Attack bonus: 8 - 4 = 4 × 5% = 20%
            // Lucky strike: +100%
            // Total: (1 + 0.20 + 1.00) = 2.20
            // Final: floor(60 × 2.20) = 132, floor(120 × 2.20) = 264
            Assert.AreEqual(132, result.Damage.Min);
            Assert.AreEqual(264, result.Damage.Max);
        }
    }
}
